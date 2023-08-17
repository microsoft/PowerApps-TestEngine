// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.PowerApps;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;

namespace Microsoft.PowerApps.TestEngine.Users
{
    /// <summary>
    /// Handles anything related to the user
    /// </summary>
    public class UserManager : IUserManager
    {
        private readonly ITestInfraFunctions _testInfraFunctions;
        private readonly ITestState _testState;
        private readonly ISingleTestInstanceState _singleTestInstanceState;
        private readonly IEnvironmentVariable _environmentVariable;

        private const string EmailSelector = "input[type=\"email\"]";
        private const string PasswordSelector = "input[type=\"password\"]";
        private const string SubmitButtonSelector = "input[type=\"submit\"]";
        private const string KeepMeSignedInNoSelector = "[id=\"idBtn_Back\"]";


        public UserManager(ITestInfraFunctions testInfraFunctions, ITestState testState,
            ISingleTestInstanceState singleTestInstanceState, IEnvironmentVariable environmentVariable)
        {
            _testInfraFunctions = testInfraFunctions;
            _testState = testState;
            _singleTestInstanceState = singleTestInstanceState;
            _environmentVariable = environmentVariable;
        }

        public async Task LoginAsUserAsync(string desiredUrl)
        {
            var testSuiteDefinition = _singleTestInstanceState.GetTestSuiteDefinition();
            var logger = _singleTestInstanceState.GetLogger();

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

            var userConfig = _testState.GetUserConfiguration(testSuiteDefinition.Persona);

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

            var user = _environmentVariable.GetVariable(userConfig.EmailKey);
            var password = _environmentVariable.GetVariable(userConfig.PasswordKey);

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

            await _testInfraFunctions.HandleUserEmailScreen(EmailSelector, user);

            await _testInfraFunctions.ClickAsync(SubmitButtonSelector);

            // Wait for the sliding animation to finish
            await Task.Delay(1000);

            await _testInfraFunctions.HandleUserPasswordScreen(PasswordSelector, password, desiredUrl);
        }
    }
}
