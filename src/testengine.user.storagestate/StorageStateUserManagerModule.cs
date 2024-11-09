// Copyright(c) Microsoft Corporation.
// Licensed under the MIT license.using Microsoft.Extensions.Logging;

using System.ComponentModel.Composition;
using System.Net.Mail;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Helpers;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerApps.TestEngine.Users;

namespace testengine.user.storagestate
{
    /// <summary>
    /// Implement single sign on to a test resource from a login.microsoftonline.com resource assuming storage state
    /// </summary>
    /// <remarks>
    ///Requires cookies 
    /// </remarks>
    [Export(typeof(IUserManager))]
    public class StorageStateUserManagerModule : IConfigurableUserManager
    {
        public Dictionary<string, object> Settings { get; private set; }

        private bool emailHandled = false;

        public StorageStateUserManagerModule()
        {
            Settings = ConfigureSettings();
        }

        private Dictionary<string, object> ConfigureSettings()
        {
            var result = new Dictionary<string, object>
            {
                { "LoadState", LoadStateIfExists() }
            };

            return result;
        }

        private Func<IEnvironmentVariable, ISingleTestInstanceState, ITestState, IFileSystem, string> LoadStateIfExists()
        {
            return (IEnvironmentVariable environmentVariable, ISingleTestInstanceState singleTestInstanceState, ITestState testState, IFileSystem fileSystem) =>
            {
                var testSuiteDefinition = singleTestInstanceState.GetTestSuiteDefinition();
                var userConfig = testState.GetUserConfiguration(testSuiteDefinition.Persona);

                if (string.IsNullOrEmpty(userConfig.EmailKey))
                {
                    return String.Empty;
                }

                var user = environmentVariable.GetVariable(userConfig.EmailKey);
                var userName = GetUserNameFromEmail(user);
                Location = !Location.EndsWith($"-{userName}") ? Location += $"-{userName}" : Location;
                if (!IsValidEmail(user))
                {
                    return String.Empty;
                }

                if (!fileSystem.Exists(Location))
                {
                    return "";
                }

                var content = fileSystem.ReadAllText(Path.Combine(Location, "state.json"));

                // TODO Decrypt content

                return content;
            };
        }

        public string Name => "storagestate";

        public int Priority => 300;

        /// <summary>
        /// Not required as store data in storage state file
        /// </summary>
        public bool UseStaticContext => false;

        public string Location { get; set; } = ".storage-state";

        public static string EmailSelector = "input[type=\"email\"]";

        /// <summary>
        /// Assume that navigation to the resoure url has started.
        /// </summary>
        /// <param name="desiredUrl">The page that has been requested to be opened</param>
        /// <param name="context">The created browser context</param>
        /// <param name="testState">Current test settings to execute</param>
        /// <param name="singleTestInstanceState">The test instance that will be executed to obtain a result</param>
        /// <param name="environmentVariable">Access to environments</param>
        /// <param name="userManagerLogin">User manager that may be required as part of login process</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="UserInputException"></exception>
        public async Task LoginAsUserAsync(
            string desiredUrl,
            IBrowserContext context,
            ITestState testState,
            ISingleTestInstanceState singleTestInstanceState,
            IEnvironmentVariable environmentVariable,
            IUserManagerLogin userManagerLogin)
        {
            var testSuiteDefinition = singleTestInstanceState.GetTestSuiteDefinition();
            var logger = singleTestInstanceState.GetLogger();

            var userConfig = testState.GetUserConfiguration(testSuiteDefinition.Persona);

            if (userConfig == null)
            {
                logger.LogError("Cannot find user config for persona");
                throw new InvalidOperationException();
            }

            var user = environmentVariable.GetVariable(userConfig.EmailKey);

            if (string.IsNullOrEmpty(user))
            {
                logger.LogError("Email key for persona cannot be empty");
                throw new InvalidOperationException();
            }

            if (!IsValidEmail(user))
            {
                logger.LogError($"Invalid email {userConfig.EmailKey} for {testSuiteDefinition.Persona}");
                throw new UserInputException();
            }

            if (!Settings.ContainsKey("FileSystem") || Settings.ContainsKey("FileSystem") && !(Settings["FileSystem"] is IFileSystem))
            {
                throw new UserInputException("File system not provided");
            }

            var fileSystem = Settings["FileSystem"] as IFileSystem;

            var userName = GetUserNameFromEmail(user);
            Location = !Location.EndsWith($"-{userName}") ? Location += $"-{userName}" : Location;

            if (!fileSystem.Exists(Location))
            {
                fileSystem.CreateDirectory(Location);
            }

            var started = DateTime.Now;

            // Wait a minimum of a five minutes
            var timeout = Math.Max(5 * 60000, testState.GetTimeout());
            var foundMatch = false;
            var errorState = false;

            var matchHost = string.Empty;

            logger.LogDebug($"Waiting for {timeout} milliseconds for desired url");
            while (DateTime.Now.Subtract(started).TotalMilliseconds < timeout && !foundMatch && !errorState)
            {
                try
                {
                    foreach (var page in context.Pages)
                    {
                        var url = page.Url;

                        // Error Checks - Power Apps Scenarios
                        //TODO: Verify App not shared
                        //TODO: Handle unlicenced
                        //TODO: DLP Violation
                        //TODO: No dataverse access rights (MDA)
                        var title = await DialogTitle(page);
                        if (!string.IsNullOrEmpty(title))
                        {
                            Settings.Add("ErrorDialogTitle", title);
                            errorState = true;
                        }

                        // Remove any redirect added by Microsoft Cloud for Web Apps so we get the desired url
                        url = url?.Replace(".mcas.ms", "");

                        // Need to check if page is idle as be get race condition before redirect to login
                        if (url.IndexOf(desiredUrl) >= 0 && await CheckIsIdleAsync(page) && !errorState)
                        {
                            //TODO: Encrypt the storage
                            await context.StorageStateAsync(new BrowserContextStorageStateOptions { Path = Path.Combine(Location, "state.json") });

                            foundMatch = true;
                            matchHost = new Uri(page.Url).Host;
                            break;
                        }

                        if (!(page.Url.IndexOf(desiredUrl) >= 0) && !errorState)
                        {
                            // Default the user into the dialog if it is visible
                            await HandleUserEmailScreen(EmailSelector, user, page);

                            // Next user could be presented with password
                            // Could also be presented with others configured MFA options
                        }
                    }
                }
                catch
                {

                }

                if (!foundMatch || !errorState)
                {
                    logger.LogDebug($"Desired page not found, waiting {DateTime.Now.Subtract(started).TotalSeconds}");
                    System.Threading.Thread.Sleep(1000);
                }
                else
                {
                    logger.LogInformation($"Test page found");
                }
            }

            if (!foundMatch && !errorState)
            {
                logger.LogError($"Desired url {desiredUrl} not found");
                throw new UserInputException(UserInputException.ErrorMapping.UserInputExceptionLoginCredential.ToString());
            }

            if (string.IsNullOrEmpty(testState.GetDomain()) && !string.IsNullOrEmpty(matchHost))
            {
                testState.SetDomain($"https://{matchHost}");
            }

            var pages = context.Pages;
            var blank = pages.Where(p => p.Url == "about:blank").ToList();
            if (blank.Count() > 0)
            {
                // Close any blank pages
                foreach (var blankPage in blank)
                {
                    await blankPage.CloseAsync();
                }
            }
        }

        public bool IsValidEmail(string emailAddress)
        {
            try
            {
                var email = new MailAddress(emailAddress);
                return email.Address == emailAddress.Trim();
            }
            catch
            {
                return false;
            }
        }

        public string GetUserNameFromEmail(string? emailAddress)
        {
            if (string.IsNullOrEmpty(emailAddress))
            {
                return String.Empty;
            }

            // Extract the username part of the email address
            var userName = emailAddress.Split('@');

            // Remove invalid characters for file paths
            string invalidChars = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            string invalidCharsPattern = string.Format("[{0}]", Regex.Escape(invalidChars));
            string validUserName = Regex.Replace(userName[0], invalidCharsPattern, "_");

            return validUserName;
        }

        public async Task HandleUserEmailScreen(string selector, string value, IPage page)
        {
            if (emailHandled)
            {
                return;
            }
            try
            {
                if (await page.Locator(selector).IsEditableAsync() && !emailHandled)
                {
                    emailHandled = true;
                    await page.Locator(selector).PressSequentiallyAsync(value, new LocatorPressSequentiallyOptions { Delay = 50 });
                    await page.Keyboard.PressAsync("Tab", new KeyboardPressOptions { Delay = 20 });
                }
            }
            catch
            {
            }
        }

        public async Task<bool> CheckIsIdleAsync(IPage page)
        {
            try
            {
                var expression = "var element = document.getElementById('O365_MainLink_Settings'); if (typeof(element) != 'undefined' && element != null) { 'Idle' } else { 'Loading' }";
                return (await page.EvaluateAsync<string>(expression)) == "Idle";
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> DialogTitle(IPage page)
        {
            try
            {
                var expression = "var element = document.querySelector('.ms-Dialog-title, #ErrorTitle, .NotificationTitle'); if (typeof(element) != 'undefined' && element != null) { element.textContent.trim() } else { '' }";
                return await page.EvaluateAsync<string>(expression);
            }
            catch
            {
                return "";
            }
        }
    }
}
