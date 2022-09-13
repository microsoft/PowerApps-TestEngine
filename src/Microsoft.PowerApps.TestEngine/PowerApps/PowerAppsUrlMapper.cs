// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
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

        public string GenerateTestUrl(string queryParams)
        {
            var environment = _testState.GetEnvironment();
            if (string.IsNullOrEmpty(environment))
            {
                _singleTestInstanceState.GetLogger().LogError("Environment cannot be empty.");
                throw new InvalidOperationException();
            }

            var testSuiteDefinition = _singleTestInstanceState.GetTestSuiteDefinition();
            if (testSuiteDefinition == null)
            {
                _singleTestInstanceState.GetLogger().LogError("Test definition must be specified.");
                throw new InvalidOperationException();
            }

            var appLogicalName = testSuiteDefinition.AppLogicalName;
            var appId = testSuiteDefinition.AppId;

            if (string.IsNullOrEmpty(appLogicalName) && string.IsNullOrEmpty(appId))
            {
                _singleTestInstanceState.GetLogger().LogError("Atleast one of the App Logical Name or App Id must be valid.");
                throw new InvalidOperationException();
            }

            var tenantId = _testState.GetTenant();
            if (string.IsNullOrEmpty(tenantId))
            {
                _singleTestInstanceState.GetLogger().LogError("Tenant cannot be empty.");
                throw new InvalidOperationException();
            }

            var cloud = _testState.GetCloud();

            if (cloud == null)
            {
                cloud = "";
            }

            string domain;
            // TODO: implement the other clouds
            switch (cloud.ToLower())
            {
                case "test":
                    domain = "apps.test.powerapps.com";
                    break;
                case "prod":
                    domain = "apps.powerapps.com";
                    break;
                default:
                    // TODO: determine what happens on default
                    domain = "apps.powerapps.com";
                    break;
            }

            return !string.IsNullOrEmpty(appLogicalName) ?
                   $"https://{domain}/play/e/{environment}/an/{appLogicalName}?tenantId={tenantId}&source=testengine{queryParams}" :
                   $"https://{domain}/play/{appId}?tenantId={tenantId}&source=testengine{queryParams}";
        }
    }
}
