// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.PowerApps;
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

        public UserManager(ITestInfraFunctions testInfraFunctions, ITestState testState, IUrlMapper urlMapper,
            ISingleTestInstanceState singleTestInstanceState)
        {
            _testInfraFunctions = testInfraFunctions;
            _testState = testState;
            _urlMapper = urlMapper;
            _singleTestInstanceState = singleTestInstanceState;
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

            var user = Environment.GetEnvironmentVariable(userConfig.EmailKey);
            var password = Environment.GetEnvironmentVariable(userConfig.PasswordKey);

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

            await _testInfraFunctions.FillAsync("[id=\"i0116\"]", user);

            await _testInfraFunctions.ClickAsync("[id=\"idSIButton9\"]");

            await _testInfraFunctions.FillAsync("[id=\"i0118\"]", password);

            await _testInfraFunctions.ClickAsync("[id=\"idSIButton9\"]");

            // Click No button to indicate we don't want to stay signed in
            await _testInfraFunctions.ClickAsync("[id=\"idBtn_Back\"]");
        }
    }
}
