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

        public TestState(ITestConfigParser yamlParser)
        {
            _testConfigParser = yamlParser;
        }

        public List<TestDefinition> GetTestDefinitions()
        {
            return TestDefinitions;
        }

        public void ParseAndSetTestState(string testConfigFile)
        {
            TestPlanDefinition = _testConfigParser.ParseTestConfig(testConfigFile);
            if (TestPlanDefinition.Test != null)
            {
                TestDefinitions.Add(TestPlanDefinition.Test);
            }
        }

        public void SetEnvironment(string environmentId)
        {
            EnvironmentId = environmentId;
        }

        public string? GetEnvironment()
        {
            return EnvironmentId;
        }

        public void SetCloud(string cloud)
        {
            Cloud = cloud;
        }

        public string? GetCloud()
        {
            return Cloud;
        }

        public void SetTenant(string tenantId)
        {
            TenantId = tenantId;
        }

        public string? GetTenant()
        {
            return TenantId;
        }
        public void SetOutputDirectory(string outputDirectory)
        {
            OutputDirectory = outputDirectory;
        }
        public string? GetOutputDirectory()
        {
            return OutputDirectory;
        }

        public UserConfiguration GetUserConfiguration(string persona)
        {
            if (TestPlanDefinition == null)
            {
                throw new InvalidOperationException("TestPlanDefinition is null");
            }

            if (TestPlanDefinition.EnvironmentVariables == null)
            {
                throw new InvalidOperationException("TestPlanDefinition.EnvironmentVariables is null");
            }

            if (TestPlanDefinition.EnvironmentVariables.Users == null)
            {
                throw new InvalidOperationException("TestPlanDefinition.EnvironmentVariables.Users is null");
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
    }
}
