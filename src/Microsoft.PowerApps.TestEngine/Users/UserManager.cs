﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

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

        public async Task LoginAsUserAsync()
        {
            var testSuiteDefinition = _singleTestInstanceState.GetTestSuiteDefinition();
            if (testSuiteDefinition == null)
            {
                throw new InvalidOperationException("Test definition cannot be null");
            }

            if (string.IsNullOrEmpty(testSuiteDefinition.Persona))
            {
                throw new InvalidOperationException("Persona cannot be empty");
            }

            var userConfig = _testState.GetUserConfiguration(testSuiteDefinition.Persona);

            if (userConfig == null)
            {
                throw new InvalidOperationException("Cannot find user config for persona");
            }

            if (string.IsNullOrEmpty(userConfig.EmailKey))
            {
                throw new InvalidOperationException("Email key for persona cannot be empty");
            }

            if (string.IsNullOrEmpty(userConfig.PasswordKey))
            {
                throw new InvalidOperationException("Password key for persona cannot be empty");
            }

            var user = _environmentVariable.GetVariable(userConfig.EmailKey);
            var password = _environmentVariable.GetVariable(userConfig.PasswordKey);

            if (string.IsNullOrEmpty(user))
            {
                throw new InvalidOperationException("User email cannot be null");
            }

            if (string.IsNullOrEmpty(password))
            {
                throw new InvalidOperationException("Password cannot be null");
            }

            await _testInfraFunctions.HandleUserEmailScreen(EmailSelector,user);

            await _testInfraFunctions.ClickAsync(SubmitButtonSelector);

            // Wait for the sliding animation to finish
            await Task.Delay(1000);

            await _testInfraFunctions.HandleUserPasswordScreen(PasswordSelector, password);

            await _testInfraFunctions.ClickAsync(SubmitButtonSelector);

            // Click No button to indicate we don't want to stay signed in
            await _testInfraFunctions.HandleKeepSignedInNoScreen(KeepMeSignedInNoSelector);
        }
    }
}
