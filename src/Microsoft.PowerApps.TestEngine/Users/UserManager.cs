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
        private readonly IUrlMapper _urlMapper;
        private readonly ISingleTestInstanceState _singleTestInstanceState;
        private readonly IEnvironmentVariable _environmentVariable;

        public UserManager(ITestInfraFunctions testInfraFunctions, ITestState testState, IUrlMapper urlMapper,
            ISingleTestInstanceState singleTestInstanceState, IEnvironmentVariable environmentVariable)
        {
            _testInfraFunctions = testInfraFunctions;
            _testState = testState;
            _urlMapper = urlMapper;
            _singleTestInstanceState = singleTestInstanceState;
            _environmentVariable = environmentVariable;
        }

        public async Task LoginAsUserAsync()
        {
            var testDefinition = _singleTestInstanceState.GetTestDefinition();
            if (testDefinition == null)
            {
                throw new InvalidOperationException("Test definition cannot be null");
            }

            if (string.IsNullOrEmpty(testDefinition.Persona))
            {
                throw new InvalidOperationException("Persona cannot be empty");
            }

            var userConfig = _testState.GetUserConfiguration(testDefinition.Persona);

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

            var makerPortalUrl = _urlMapper.GenerateLoginUrl();

            await _testInfraFunctions.GoToUrlAsync(makerPortalUrl);
            var selector = "[id=\"i0116\"]";
            await _testInfraFunctions.WaitForSelectorAsync(selector);

            await _testInfraFunctions.FillAsync(selector, user);            

            // await Task.Delay(500);            
            await _testInfraFunctions.WaitForSelectorAsync(selector);
            // Additional check to make sure the username field is not null, returns when the expression returns a truthy value.
            await _testInfraFunctions.WaitForFunctionAsync("selector => document.querySelector(selector).value != ''", selector);

            await _testInfraFunctions.ClickAsync("[id=\"idSIButton9\"]");
            
            selector = "[id=\"i0118\"]";
            await _testInfraFunctions.WaitForSelectorAsync(selector);

            await _testInfraFunctions.FillAsync(selector, password);

            // await Task.Delay(500);GetAttributeAsync("src")
            selector = "[id=\"displayName\"]";
            await _testInfraFunctions.WaitForSelectorAsync(selector);
            await _testInfraFunctions.WaitForFunctionAsync("selector => document.querySelector(selector).title != ''", selector);

            selector = "[id=\"i0118\"]";
            // Additional check to make sure the password field is not null, returns when the expression returns a truthy value.
            await _testInfraFunctions.WaitForFunctionAsync("selector => document.querySelector(selector).value != ''", selector);            

            await _testInfraFunctions.ClickAsync("[id=\"idSIButton9\"]");
            
            // Click No button to indicate we don't want to stay signed in
            await _testInfraFunctions.ClickAsync("[id=\"idBtn_Back\"]");
        }
    }
}
