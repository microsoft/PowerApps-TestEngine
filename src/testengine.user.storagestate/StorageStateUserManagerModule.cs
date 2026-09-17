// Copyright(c) Microsoft Corporation.
// Licensed under the MIT license.

using System.ComponentModel.Composition;
using System.Net.Mail;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.Users;
using testengine.common.user;

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
        /// <summary>
        /// The namespace of namespaces that this provider relates to
        /// </summary>
        public string[] Namespaces { get; private set; } = new string[] { "TestEngine" };

        public Dictionary<string, object> Settings { get; private set; }

        public Action<IFileSystem, string> Protect = static (IFileSystem filesystem, string fileName) =>
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new InvalidOperationException("DPAPI protection only available on Windows");
            }

            bool fileSaved = false;
            try
            {
                var content = filesystem.ReadAllText(fileName);

                byte[] dataBytes = Encoding.UTF8.GetBytes(content);
                byte[] encryptedData = ProtectedData.Protect(dataBytes, null, DataProtectionScope.CurrentUser);
                filesystem.WriteTextToFile(fileName, Convert.ToBase64String(encryptedData), true);
                fileSaved = true;
            }
            catch
            {
                throw;
            }
            finally
            {
                if (!fileSaved)
                {
                    filesystem.Delete(fileName);
                }
            }
        };

        public Func<IFileSystem, string, string> Unprotect = (IFileSystem filesystem, string fileName) =>
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new InvalidOperationException("DPAPI protection only available on Windows");
            }
            var base64Text = filesystem.ReadAllText(fileName);
            byte[] dataBytes = Convert.FromBase64String(base64Text);
            byte[] decryptedData = ProtectedData.Unprotect(dataBytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decryptedData);
        };

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
                Location = !Path.IsPathRooted(Location) ? Path.Combine(fileSystem.GetDefaultRootTestEngine(), Location) : Location;
                if (!IsValidEmail(user))
                {
                    return String.Empty;
                }

                if (!Path.IsPathRooted(Location))
                {
                    Location = Path.Combine(fileSystem.GetDefaultRootTestEngine(), Location);
                }

                if (!fileSystem.Exists(Location))
                {
                    return String.Empty;
                }

                var stateFile = Path.Combine(Location, "state.json");
                if (fileSystem.FileExists(stateFile))
                {
                    return Unprotect(fileSystem, stateFile);
                }

                return String.Empty;
            };
        }

        public virtual string Name => "storagestate";

        public int Priority => 300;

        public bool UseStaticContext { get; set; } = false;

        public string Location { get; set; } = ".storage-state";

        public string ContextLocation { get; } = ".TemporaryContext";

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
            Location = !Path.IsPathRooted(Location) ? Path.Combine(fileSystem.GetDefaultRootTestEngine(), Location) : Location;

            if (!Path.IsPathRooted(Location))
            {
                Location = Path.Combine(fileSystem.GetDefaultRootTestEngine(), Location);
            }

            if (!fileSystem.Exists(Location))
            {
                fileSystem.CreateDirectory(Location);
            }

            var started = DateTime.Now;

            // Wait a minimum of a five minutes
            var timeout = Math.Max(5 * 60000, testState.GetTimeout());

            var state = new LoginState()
            {
                DesiredUrl = desiredUrl,
                UserEmail = user,
                CallbackErrorFound = async () =>
                {
                    var stateFile = Path.Combine(Location, "state.json");
                    try
                    {
                        await context.StorageStateAsync(new BrowserContextStorageStateOptions { Path = stateFile });
                        Protect(fileSystem, stateFile);
                        logger.LogInformation("Storage state saved saved successfully");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to save storage state after error");
                        // Ensure cleanup happens even if storage state save fails
                        if (fileSystem.FileExists(stateFile))
                        {
                            fileSystem.Delete(stateFile);
                            logger.LogDebug($"Deleted unprotected state file: {stateFile}");
                        }
                    }
                },
                CallbackDesiredUrlFound = async (match) =>
                {
                    var stateFile = Path.Combine(Location, "state.json");
                    try
                    {
                        await context.StorageStateAsync(new BrowserContextStorageStateOptions { Path = stateFile });
                        Protect(fileSystem, stateFile);
                        logger.LogInformation("Storage state saved successfully");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to save storage state after successful login");
                        // Ensure cleanup happens even if storage state save fails
                        if (fileSystem.FileExists(stateFile))
                        {
                            fileSystem.Delete(stateFile);
                            logger.LogDebug($"Deleted unprotected state file: {stateFile}");
                        }
                        throw;
                    }
                },
                CallbackRedirectRequiredFound = !UseStaticContext ? null : async (matchPage) =>
                {
                    try
                    {
                        var present = context.Pages.First(p => p == matchPage);
                        if (present != null)
                        {
                            var resp = await present.GotoAsync(desiredUrl);
                            if (resp?.Ok == true)
                            {
                                await present.WaitForLoadStateAsync(LoadState.NetworkIdle);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to redirect to desired URL");
                    }
                },
                Module = this
            };

            logger.LogDebug($"Waiting for {timeout} milliseconds for desired url");
            while (DateTime.Now.Subtract(started).TotalMilliseconds < timeout && !state.FoundMatch && !state.IsError)
            {
                try
                {
                    foreach (var page in context.Pages)
                    {
                        var loginHelper = new PowerPlatformLogin();
                        state.Page = page;

                        await loginHelper.HandleCommonLoginState(state);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error waiting for login");
                }

                if (!state.FoundMatch && !state.IsError)
                {
                    logger.LogDebug($"Desired page not found, waiting {DateTime.Now.Subtract(started).TotalSeconds}");
                    System.Threading.Thread.Sleep(1000);
                }
                else
                {
                    logger.LogInformation($"Test page found");
                }
            }

            if (!state.FoundMatch && !state.IsError)
            {
                logger.LogError($"Desired url {desiredUrl} not found");
                throw new UserInputException(UserInputException.ErrorMapping.UserInputExceptionLoginCredential.ToString());
            }

            if (string.IsNullOrEmpty(testState.GetDomain()) && !string.IsNullOrEmpty(state.MatchHost))
            {
                testState.SetDomain($"https://{state.MatchHost}");
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
                if (string.IsNullOrEmpty(emailAddress))
                {
                    return false;
                }
                var email = new MailAddress(emailAddress);
                return email.Address == emailAddress.Trim();
            }
            catch
            {
                return false;
            }
        }

        public string GetUserNameFromEmail(string emailAddress)
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
    }
}
