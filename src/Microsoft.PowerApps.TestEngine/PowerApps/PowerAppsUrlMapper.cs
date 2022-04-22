// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerApps.TestEngine.Config;

namespace Microsoft.PowerApps.TestEngine.PowerApps
{
    /// <summary>
    /// Map urls based on the cloud
    /// </summary>
    public class PowerAppsUrlMapper : IUrlMapper
    {
        private readonly ITestState _testState;
        private readonly ISingleTestInstanceState _singleTestInstanceState;

        public PowerAppsUrlMapper(ITestState testState, ISingleTestInstanceState singleTestInstanceState)
        {
            _testState = testState;
            _singleTestInstanceState = singleTestInstanceState;
        }

        public string GenerateLoginUrl()
        {
            // TODO: differentiate urls based on the cloud
            return $"https://make.powerapps.com/environments/{_testState.GetEnvironment()}/home";
        }

        public string GenerateAppUrl()
        {
            // TODO: differentiate urls based on the cloud
            return $"https://apps.powerapps.com/play/e/{_testState.GetEnvironment()}/an/{_singleTestInstanceState.GetTestDefinition().AppLogicalName}?tenantId={_testState.GetTenant()}";
        }
    }
}
