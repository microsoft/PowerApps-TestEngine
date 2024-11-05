// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.ComponentModel.Composition;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.Users;

namespace testengine.user.browser
{
    /// <summary>
    /// Implement single sign on to a test resource from a login.microsoftonline.com resource assuming Persisant Session State
    /// </summary>
    /// <remarks>
    /// Requires the user to select "Stay signed in" or Conditional access policies enabled to allow browser persitance that does not <c href="https://learn.microsoft.com/entra/identity/conditional-access/howto-policy-persistent-browser-session">Require reauthentication and disable browser persistence</c>
    /// </remarks>
    [Export(typeof(IUserManager))]
    public class BrowserUserManagerModule : IUserManager
    {
        public string Name { get { return "browser"; } }

        public int Priority { get { return 100; } }

        public bool UseStaticContext { get { return true; } }

        public string Location { get; set; } = "BrowserContext";

        private IBrowserContext? Context { get; set; }

        public IPage? Page { get; set; }

        public Func<string, bool> DirectoryExists { get; set; } = (location) => Directory.Exists(location);

        public Action<string> CreateDirectory { get; set; } = (location) => Directory.CreateDirectory(location);

        public Func<string, string[]> GetFiles { get; set; } = (path) => Directory.GetFiles(path);

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
            var timeout = Math.Min(60000, testState.GetTimeout());
            var logger = singleTestInstanceState.GetLogger();
            var foundMatch = false;

            logger.LogDebug($"Waiting for {timeout} milliseconds for desired url");
            while (DateTime.Now.Subtract(started).TotalMilliseconds < timeout && !foundMatch)
            {
                try
                {
                    foreach (var page in context.Pages)
                    {
                        if (page.Url.IndexOf(desiredUrl) >= 0)
                        {
                            foundMatch = true;
                            break;
                        }
                    }
                }
                catch
                {

                }


                if (!foundMatch)
                {
                    logger.LogDebug($"Desired page not found, waiting {DateTime.Now.Subtract(started).TotalSeconds}");
                    System.Threading.Thread.Sleep(1000);
                }
                else
                {
                    logger.LogInformation($"Test page found");
                }
            }

            if (!foundMatch)
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
    }
}
