// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Modules;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerApps.TestEngine.Users;
using Microsoft.PowerFx;

namespace testengine.user.environment
{
    [Export(typeof(IUserManager))]
    public class EnvironmentUserManagerModule : IUserManager
    {
        public string Name { get { return "environment"; } }

        public int Priority { get { return 0; } }

        public bool UseStaticContext { get { return false; } }

        public string Location { get; set; } = string.Empty;

        public static string EmailSelector = "input[type=\"email\"]";
        public static string PasswordSelector = "input[type=\"password\"]";
        public static string SubmitButtonSelector = "input[type=\"submit\"]";
        public static string StaySignedInSelector = "[id=\"KmsiCheckboxField\"]";
        public static string KeepMeSignedInNoSelector = "[id=\"idBtn_Back\"]";

        private IBrowserContext? Context { get; set; }

        public IPage? Page { get; set; }

        public async Task LoginAsUserAsync(
            string desiredUrl,
            IBrowserContext context,
            ITestState testState,
            ISingleTestInstanceState singleTestInstanceState,
            IEnvironmentVariable environmentVariable,
            IUserManagerLogin userManagerLogin)
        {
            Context = context;

            var testSuiteDefinition = singleTestInstanceState.GetTestSuiteDefinition();
            var logger = singleTestInstanceState.GetLogger();

            if (testSuiteDefinition == null)
            {
                logger.LogError("Test definition cannot be null");
                throw new InvalidOperationException();
            }

            if (string.IsNullOrEmpty(testSuiteDefinition.Persona))
            {
                logger.LogError("Persona cannot be empty");
                throw new InvalidOperationException();
            }

            var userConfig = testState.GetUserConfiguration(testSuiteDefinition.Persona);

            if (userConfig == null)
            {
                logger.LogError("Cannot find user config for persona");
                throw new InvalidOperationException();
            }

            if (string.IsNullOrEmpty(userConfig.EmailKey))
            {
                logger.LogError("Email key for persona cannot be empty");
                throw new InvalidOperationException();
            }

            if (string.IsNullOrEmpty(userConfig.PasswordKey))
            {
                logger.LogError("Password key for persona cannot be empty");
                throw new InvalidOperationException();
            }

            var user = environmentVariable.GetVariable(userConfig.EmailKey);
            var password = environmentVariable.GetVariable(userConfig.PasswordKey);

            bool missingUserOrPassword = false;

            if (string.IsNullOrEmpty(user))
            {
                logger.LogError(("User email cannot be null. Please check if the environment variable is set properly."));
                missingUserOrPassword = true;
            }

            if (string.IsNullOrEmpty(password))
            {
                logger.LogError("Password cannot be null. Please check if the environment variable is set properly.");
                missingUserOrPassword = true;
            }

            if (missingUserOrPassword)
            {
                throw new UserInputException(UserInputException.ErrorMapping.UserInputExceptionLoginCredential.ToString());
            }

            if (Page == null)
            {
                Page = context.Pages.First();
            }

            await HandleUserEmailScreen(EmailSelector, user);

            await Page.ClickAsync(SubmitButtonSelector);

            // Wait for the sliding animation to finish
            await Task.Delay(1000);

            await HandleUserPasswordScreen(PasswordSelector, password, desiredUrl, logger);
        }

        private void ValidatePage()
        {
            if (Page == null)
            {
                Page = Context.Pages.First();
            }
        }

        private async Task ClickAsync(string selector)
        {
            ValidatePage();
            await Page.ClickAsync(selector);
        }

        public async Task HandleUserEmailScreen(string selector, string value)
        {
            ValidatePage();
            await Page.Locator(selector).WaitForAsync();
            await Page.TypeAsync(selector, value, new PageTypeOptions { Delay = 50 });
            await Page.Keyboard.PressAsync("Tab", new KeyboardPressOptions { Delay = 20 });
        }

        public async Task HandleUserPasswordScreen(string selector, string value, string desiredUrl, ILogger logger)
        {
            ValidatePage();

            try
            {
                // Find the password box
                await Page.Locator(selector).WaitForAsync();

                // Fill in the password
                await Page.FillAsync(selector, value);

                // Submit password form
                await this.ClickAsync(EnvironmentUserManagerModule.SubmitButtonSelector);

                PageWaitForSelectorOptions selectorOptions = new PageWaitForSelectorOptions();
                selectorOptions.Timeout = 8000;

                // For instances where there is a 'Stay signed in?' dialogue box
                try
                {
                    logger.LogDebug("Checking if asked to stay signed in.");

                    // Check if we received a 'Stay signed in?' box?
                    await Page.WaitForSelectorAsync(EnvironmentUserManagerModule.StaySignedInSelector, selectorOptions);
                    logger.LogDebug("Was asked to 'stay signed in'.");

                    // Click to stay signed in
                    await Page.ClickAsync(EnvironmentUserManagerModule.KeepMeSignedInNoSelector);
                }
                // If there is no 'Stay signed in?' box, an exception will throw; just catch and continue
                catch (Exception ssiException)
                {
                    logger.LogDebug("Exception encountered: " + ssiException.ToString());

                    // Keep record if passwordError was encountered
                    bool hasPasswordError = false;

                    try
                    {
                        selectorOptions.Timeout = 2000;

                        // Check if we received a password error
                        await Page.WaitForSelectorAsync("[id=\"passwordError\"]", selectorOptions);
                        hasPasswordError = true;
                    }
                    catch (Exception peException)
                    {
                        logger.LogDebug("Exception encountered: " + peException.ToString());
                    }

                    // If encountered password error, exit program
                    if (hasPasswordError)
                    {
                        logger.LogError("Incorrect password entered. Make sure you are using the correct credentials.");
                        throw new UserInputException(UserInputException.ErrorMapping.UserInputExceptionLoginCredential.ToString());
                    }
                    // If not, continue
                    else
                    {
                        logger.LogDebug("Did not encounter an invalid password error.");
                    }

                    logger.LogDebug("Was not asked to 'stay signed in'.");
                }

                await Page.WaitForURLAsync(desiredUrl);
            }
            catch (TimeoutException)
            {
                logger.LogError("Timed out during login attempt. In order to determine why, it may be beneficial to view the output recording. Make sure that your login credentials are correct.");
                throw new TimeoutException();
            }

            logger.LogDebug("Logged in successfully.");
        }
    }
}
