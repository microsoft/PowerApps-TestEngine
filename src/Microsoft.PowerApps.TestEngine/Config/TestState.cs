// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.PowerApps.TestEngine.Config
{
    /// <summary>
    /// State of the test run
    /// </summary>
    public class TestState : ITestState
    {
        private readonly ITestConfigParser _testConfigParser;
        private TestPlanDefinition? TestPlanDefinition { get; set; }
        private List<TestCase> TestCases { get; set; } = new List<TestCase>();
        private string? EnvironmentId { get; set; }
        private string? Cloud { get; set; }

        private string? TenantId { get; set; }

        private string? OutputDirectory { get; set; }

        private bool IsValid { get; set; } = false;

        public TestState(ITestConfigParser testConfigParser)
        {
            _testConfigParser = testConfigParser;
        }

        public TestSuiteDefinition GetTestSuiteDefinition()
        {
            return TestPlanDefinition?.TestSuite;
        }

        public List<TestCase> GetTestCases()
        {
            return TestCases;
        }

        public void ParseAndSetTestState(string testConfigFile, ILogger logger)
        {
            if (string.IsNullOrEmpty(testConfigFile))
            {
                logger.LogTrace("Test Config File: " + nameof(testConfigFile));
                logger.LogError("Missing test config file.");
            }

            TestPlanDefinition = _testConfigParser.ParseTestConfig<TestPlanDefinition>(testConfigFile, logger);
            if (TestPlanDefinition.TestSuite != null)
            {
                TestCases = TestPlanDefinition.TestSuite.TestCases;

                if (string.IsNullOrEmpty(TestPlanDefinition.TestSuite.TestSuiteName))
                {
                    logger.LogError("Missing test suite name from test suite definition");
                }

                if (string.IsNullOrEmpty(TestPlanDefinition.TestSuite.Persona))
                {
                    logger.LogError("Missing persona from test suite definition");
                }

                if (string.IsNullOrEmpty(TestPlanDefinition.TestSuite.AppLogicalName))
                {
                    logger.LogError("Missing app logical name from test suite definition");
                }
            }

            if (TestCases.Count == 0)
            {
                logger.LogError("Must be at least one test case");
            }

            foreach(var testCase in TestCases)
            {
                if (string.IsNullOrEmpty(testCase.TestCaseName))
                {
                    logger.LogError("Missing test case name from test definition");
                }

                if (string.IsNullOrEmpty(testCase.TestSteps))
                {
                    logger.LogError("Missing test steps from test case");
                }
            }

            if (TestPlanDefinition.TestSettings == null)
            {
                logger.LogError("Missing test settings from test plan");
            }
            else if (!string.IsNullOrEmpty(TestPlanDefinition.TestSettings.FilePath))
            {
                TestPlanDefinition.TestSettings = _testConfigParser.ParseTestConfig<TestSettings>(TestPlanDefinition.TestSettings.FilePath, logger);
            }

            if (TestPlanDefinition.TestSettings.BrowserConfigurations == null 
                || TestPlanDefinition.TestSettings.BrowserConfigurations.Count == 0)
            {
                logger.LogError("Missing browser configuration from test plan");
            }

            foreach (var browserConfig in TestPlanDefinition.TestSettings.BrowserConfigurations)
            {
                if (string.IsNullOrWhiteSpace(browserConfig.Browser))
                {
                    logger.LogError("Missing browser from browser configuration");
                }

                if (browserConfig.ScreenWidth == null && browserConfig.ScreenHeight != null
                    || browserConfig.ScreenHeight == null && browserConfig.ScreenWidth != null)
                {
                    logger.LogError("Screen width and height both need to be specified or not specified");
                }
            }

            if (TestPlanDefinition.EnvironmentVariables == null)
            {
                logger.LogError("Missing environment variables from test plan");
            }
            else if (!string.IsNullOrEmpty(TestPlanDefinition.EnvironmentVariables.FilePath))
            {
                TestPlanDefinition.EnvironmentVariables = _testConfigParser.ParseTestConfig<EnvironmentVariables>(TestPlanDefinition.EnvironmentVariables.FilePath, logger);
            }

            if (TestPlanDefinition.EnvironmentVariables.Users == null
                || TestPlanDefinition.EnvironmentVariables.Users.Count == 0)
            {
                logger.LogError("At least one user must be specified");
            }

            foreach(var userConfig in TestPlanDefinition.EnvironmentVariables.Users)
            {
                if (string.IsNullOrEmpty(userConfig.PersonaName))
                {
                    logger.LogError("Missing persona name");
                }

                if (string.IsNullOrEmpty(userConfig.EmailKey))
                {
                    logger.LogError("Missing email key");
                }

                if (string.IsNullOrEmpty(userConfig.PasswordKey))
                {
                    logger.LogError("Missing password key");
                }
            }

            if (TestPlanDefinition.EnvironmentVariables.Users.Where(x => x.PersonaName == TestPlanDefinition.TestSuite?.Persona).FirstOrDefault() == null)
            {
                logger.LogError("Persona specified in test is not listed in environment variables");
            }

            IsValid = true;
        }

        public void SetEnvironment(string environmentId, ILogger logger)
        {
            if (string.IsNullOrEmpty(environmentId))
            {
                logger.LogTrace("Environment: " + nameof(environmentId));
                logger.LogError("Environment cannot be null nor empty.");
            }
            EnvironmentId = environmentId;
        }

        public string? GetEnvironment()
        {
            return EnvironmentId;
        }

        public void SetCloud(string cloud, ILogger logger)
        {
            if (string.IsNullOrEmpty(cloud))
            {
                logger.LogTrace("Cloud: " + nameof(cloud));
                logger.LogError("Cloud cannot be null nor empty.");
            }
            // TODO: validate clouds
            Cloud = cloud;
        }

        public string? GetCloud()
        {
            return Cloud;
        }

        public void SetTenant(string tenantId, ILogger logger)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                logger.LogTrace("Tenant: " + nameof(tenantId));
                logger.LogError("Tenant cannot be null nor empty.");
            }
            TenantId = tenantId;
        }

        public string? GetTenant()
        {
            return TenantId;
        }
        public void SetOutputDirectory(string outputDirectory, ILogger logger)
        {
            if (string.IsNullOrEmpty(outputDirectory))
            {
                logger.LogTrace("Output directory: " + nameof(outputDirectory));
                logger.LogError("Output directory cannot be null nor empty.");
            }
            OutputDirectory = outputDirectory;
        }
        public string? GetOutputDirectory()
        {
            return OutputDirectory;
        }

        public UserConfiguration GetUserConfiguration(string persona, ILogger logger)
        {
            if (!IsValid)
            {
                logger.LogError("TestPlanDefinition is not valid");
            }

            var userConfiguration = TestPlanDefinition?.EnvironmentVariables?.Users?.Where(x => x.PersonaName == persona).FirstOrDefault();

            if (userConfiguration == null)
            {
                logger.LogTrace($"Persona: {persona}");
                logger.LogError("Unable to find user configuration for persona.");
            }

            return userConfiguration;
        }
        public TestSettings? GetTestSettings()
        {
            return TestPlanDefinition?.TestSettings;
        }

        public LogLevel GetEngineLoggingLevel()
        {
            return GetTestSettings().EngineLoggingLevel;
        }

        public int GetTimeout()
        {
            return GetTestSettings().Timeout;
        }

        public int GetWorkerCount()
        {
            return GetTestSettings().WorkerCount;
        }
    }
}
