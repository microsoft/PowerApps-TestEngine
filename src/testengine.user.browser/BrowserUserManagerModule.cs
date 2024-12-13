// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.ComponentModel.Composition;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.Users;
using testengine.common.user;

namespace testengine.user.browser
{
    /// <summary>
    /// Implement single sign on to a test resource from a login.microsoftonline.com resource assuming Persisant Session State
    /// </summary>
    /// <remarks>
    /// Requires the user to select "Stay signed in" or Conditional access policies enabled to allow browser persitance that does not <c href="https://learn.microsoft.com/entra/identity/conditional-access/howto-policy-persistent-browser-session">Require reauthentication and disable browser persistence</c>
    /// </remarks>
    [Export(typeof(IUserManager))]
    public class BrowserUserManagerModule : IConfigurableUserManager
    {
        /// <summary>
        /// The namespace of namespaces that this provider relates to
        /// </summary>
        public string[] Namespaces { get; private set; } = new string[] { "Deprecated" };

        public string Name { get { return "browser"; } }

        public int Priority { get { return 100; } }

        public bool UseStaticContext { get { return true; } }

        public string Location { get; set; } = "BrowserContext";

        private IBrowserContext? Context { get; set; }

        public IPage? Page { get; set; }

        public Func<string, bool> DirectoryExists { get; set; } = (location) => Directory.Exists(location);

        public Action<string> CreateDirectory { get; set; } = (location) => Directory.CreateDirectory(location);

        public Func<string, string[]> GetFiles { get; set; } = (path) => Directory.GetFiles(path);

        public PowerPlatformLogin LoginHelper { get; set; } = new PowerPlatformLogin();

        public Dictionary<string, object> Settings => new Dictionary<string, object>();

        public async Task LoginAsUserAsync(
            string desiredUrl,
            IBrowserContext context,
            ITestState testState,
            ISingleTestInstanceState singleTestInstanceState,
            IEnvironmentVariable environmentVariable,
            IUserManagerLogin userManagerLogin)
        {
            Context = context;

            if (!DirectoryExists(Location))
            {
                CreateDirectory(Location);
            }

            var started = DateTime.Now;

            // Wait a minimum of a minute
            var timeout = Math.Max(60000, testState.GetTimeout());
            var logger = singleTestInstanceState.GetLogger();

            var state = new LoginState()
            {
                DesiredUrl = desiredUrl,
                Module = this
            };

            logger.LogDebug($"Waiting for {timeout} milliseconds for desired url");
            while (DateTime.Now.Subtract(started).TotalMilliseconds < timeout && !state.FoundMatch && !state.IsError)
            {
                try
                {
                    foreach (var page in context.Pages)
                    {
                        state.Page = page;

                        await LoginHelper.HandleCommonLoginState(state);
                    }
                }
                catch
                {

                }

                if (!state.FoundMatch || !state.IsError)
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

        private void ValidatePage()
        {
            if (Page == null)
            {
                Page = Context.Pages.Where(p => !p.IsClosed).First();
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
    }
}
