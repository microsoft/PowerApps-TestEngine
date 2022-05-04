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
            var environment = _testState.GetEnvironment();
            if (string.IsNullOrEmpty(environment))
            {
                throw new InvalidOperationException("Environment cannot be empty");
            }

            var cloud = _testState.GetCloud();
            
            if (cloud == null)
            {
                cloud = "";
            }

            // TODO: implement the other clouds
            switch(cloud.ToLower())
            {
                case "test":
                    return $"https://make.test.powerapps.com/environments/{environment}/home";
                case "prod":
                    return $"https://make.powerapps.com/environments/{environment}/home";
                default:
                    // TODO: determine what happens on default
                    return $"https://make.powerapps.com/environments/{environment}/home";
            }

        }

        public string GenerateAppUrl()
        {
            var environment = _testState.GetEnvironment();
            if (string.IsNullOrEmpty(environment))
            {
                throw new InvalidOperationException("Environment cannot be empty");
            }

            var testDefinition = _singleTestInstanceState.GetTestDefinition();
            if (testDefinition == null)
            {
                throw new InvalidOperationException("Test definition must be specified");
            }

            var appLogicalName = testDefinition.AppLogicalName;
            if (string.IsNullOrEmpty(appLogicalName))
            {
                throw new InvalidOperationException("App logical name cannot be empty");
            }

            var tenantId = _testState.GetTenant();
            if (string.IsNullOrEmpty(tenantId))
            {
                throw new InvalidOperationException("Tenant cannot be empty");
            }

            var cloud = _testState.GetCloud();

            if (cloud == null)
            {
                cloud = "";
            }

            string? domain;
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

            return $"https://{domain}/play/e/{environment}/an/{appLogicalName}?tenantId={tenantId}";
        }
    }
}
