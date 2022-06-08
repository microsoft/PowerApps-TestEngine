// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerApps.TestEngine.Config
{
    /// <summary>
    /// State of the test run
    /// </summary>
    public class TestState : ITestState
    {
        private readonly ITestConfigParser _testConfigParser;
        private TestPlanDefinition? TestPlanDefinition { get; set; }
        private List<TestDefinition> TestDefinitions { get; set; } = new List<TestDefinition>();

        private string? EnvironmentId { get; set; }
        private string? Cloud { get; set; }

        private string? TenantId { get; set; }

        private string? OutputDirectory { get; set; }

        private bool IsValid { get; set; } = false;

        public TestState(ITestConfigParser testConfigParser)
        {
            _testConfigParser = testConfigParser;
        }

        public List<TestDefinition> GetTestDefinitions()
        {
            return TestDefinitions;
        }

        public void ParseAndSetTestState(string testConfigFile)
        {
            if (string.IsNullOrEmpty(testConfigFile))
            {
                throw new ArgumentNullException(nameof(testConfigFile));
            }

            TestPlanDefinition = _testConfigParser.ParseTestConfig(testConfigFile);
            if (TestPlanDefinition.Test != null)
            {
                TestDefinitions.Add(TestPlanDefinition.Test);
            }

            if (TestDefinitions.Count == 0)
            {
                throw new InvalidOperationException("Must be at least one test definition");
            }

            var personasReferenced = new List<string>();

            foreach(var testDefinition in TestDefinitions)
            {
                if (string.IsNullOrEmpty(testDefinition.Name))
                {
                    throw new InvalidOperationException("Missing name from test definition");
                }

                if (string.IsNullOrEmpty(testDefinition.Persona))
                {
                    throw new InvalidOperationException("Missing persona from test definition");
                }

                if (!personasReferenced.Contains(testDefinition.Persona))
                {
                    personasReferenced.Add(testDefinition.Persona);
                }

                if (string.IsNullOrEmpty(testDefinition.AppLogicalName))
                {
                    throw new InvalidOperationException("Missing app logical name from test definition");
                }

                if (string.IsNullOrEmpty(testDefinition.TestSteps))
                {
                    throw new InvalidOperationException("Missing test steps from test definition");
                }
            }

            if (TestPlanDefinition.TestSettings == null)
            {
                throw new InvalidOperationException("Missing test settings from test plan");
            }

            if (TestPlanDefinition.TestSettings.BrowserConfigurations == null 
                || TestPlanDefinition.TestSettings.BrowserConfigurations.Count == 0)
            {
                throw new InvalidOperationException("Missing browser configuration from test plan");
            }

            foreach (var browserConfig in TestPlanDefinition.TestSettings.BrowserConfigurations)
            {
                if (string.IsNullOrWhiteSpace(browserConfig.Browser))
                {
                    throw new InvalidOperationException("Missing browser from browser configuration");
                }

                if (browserConfig.ScreenWidth == null && browserConfig.ScreenHeight != null
                    || browserConfig.ScreenHeight == null && browserConfig.ScreenWidth != null)
                {
                    throw new InvalidOperationException("Screen width and height both need to be specified or not specified");
                }
            }

            if (TestPlanDefinition.EnvironmentVariables == null)
            {
                throw new InvalidOperationException("Missing environment variables from test plan");
            }

            if (TestPlanDefinition.EnvironmentVariables.Users == null
                || TestPlanDefinition.EnvironmentVariables.Users.Count == 0)
            {
                throw new InvalidOperationException("At least one user must be specified");
            }

            foreach(var userConfig in TestPlanDefinition.EnvironmentVariables.Users)
            {
                if (string.IsNullOrEmpty(userConfig.PersonaName))
                {
                    throw new InvalidOperationException("Missing persona name");
                }

                if (string.IsNullOrEmpty(userConfig.EmailKey))
                {
                    throw new InvalidOperationException("Missing email key");
                }

                if (string.IsNullOrEmpty(userConfig.PasswordKey))
                {
                    throw new InvalidOperationException("Missing password key");
                }
            }

            foreach(var referencedPersona in personasReferenced)
            {
                if (TestPlanDefinition.EnvironmentVariables.Users.Where(x => x.PersonaName == referencedPersona).FirstOrDefault() == null)
                {
                    throw new InvalidOperationException("Persona specified in test is not listed in environment variables");
                }
            }

            IsValid = true;
        }

        public void SetEnvironment(string environmentId)
        {
            if (string.IsNullOrEmpty(environmentId))
            {
                throw new ArgumentNullException(nameof(environmentId));
            }
            EnvironmentId = environmentId;
        }

        public string? GetEnvironment()
        {
            return EnvironmentId;
        }

        public void SetCloud(string cloud)
        {
            if (string.IsNullOrEmpty(cloud))
            {
                throw new ArgumentNullException(nameof(cloud));
            }
            // TODO: validate clouds
            Cloud = cloud;
        }

        public string? GetCloud()
        {
            return Cloud;
        }

        public void SetTenant(string tenantId)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                throw new ArgumentNullException(nameof(tenantId));
            }
            TenantId = tenantId;
        }

        public string? GetTenant()
        {
            return TenantId;
        }
        public void SetOutputDirectory(string outputDirectory)
        {
            if (string.IsNullOrEmpty(outputDirectory))
            {
                throw new ArgumentNullException(nameof(outputDirectory));
            }
            OutputDirectory = outputDirectory;
        }
        public string? GetOutputDirectory()
        {
            return OutputDirectory;
        }

        public UserConfiguration GetUserConfiguration(string persona)
        {
            if (!IsValid)
            {
                throw new InvalidOperationException("TestPlanDefinition is not valid");
            }

            var userConfiguration = TestPlanDefinition?.EnvironmentVariables?.Users?.Where(x => x.PersonaName == persona).FirstOrDefault();

            if (userConfiguration == null)
            {
                throw new InvalidOperationException($"Unable to find user configuration for persona: {persona}");
            }

            return userConfiguration;
        }
        public TestSettings? GetTestSettings()
        {
            return TestPlanDefinition?.TestSettings;
        }

        public int GetTimeout()
        {
            return GetTestSettings().Timeout;
        }
    }
}
