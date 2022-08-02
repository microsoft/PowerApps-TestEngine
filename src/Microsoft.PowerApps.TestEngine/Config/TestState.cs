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
                logger.LogCritical("Missing test config file.");
                logger.LogTrace("Test Config File: " + nameof(testConfigFile));
            }

            TestPlanDefinition = _testConfigParser.ParseTestConfig<TestPlanDefinition>(testConfigFile, logger);
            if (TestPlanDefinition.TestSuite != null)
            {
                TestCases = TestPlanDefinition.TestSuite.TestCases;

                if (string.IsNullOrEmpty(TestPlanDefinition.TestSuite.TestSuiteName))
                {
                    logger.LogCritical("Missing test suite name from test suite definition");
                }

                if (string.IsNullOrEmpty(TestPlanDefinition.TestSuite.Persona))
                {
                    logger.LogCritical("Missing persona from test suite definition");
                }

                if (string.IsNullOrEmpty(TestPlanDefinition.TestSuite.AppLogicalName))
                {
                    logger.LogCritical("Missing app logical name from test suite definition");
                }
            }

            if (TestCases.Count == 0)
            {
                logger.LogCritical("Must be at least one test case");
            }

            foreach(var testCase in TestCases)
            {
                if (string.IsNullOrEmpty(testCase.TestCaseName))
                {
                    logger.LogCritical("Missing test case name from test definition");
                }

                if (string.IsNullOrEmpty(testCase.TestSteps))
                {
                    logger.LogCritical("Missing test steps from test case");
                }
            }

            if (TestPlanDefinition.TestSettings == null)
            {
                logger.LogCritical("Missing test settings from test plan");
            }
            else if (!string.IsNullOrEmpty(TestPlanDefinition.TestSettings.FilePath))
            {
                TestPlanDefinition.TestSettings = _testConfigParser.ParseTestConfig<TestSettings>(TestPlanDefinition.TestSettings.FilePath, logger);
            }

            if (TestPlanDefinition.TestSettings.BrowserConfigurations == null 
                || TestPlanDefinition.TestSettings.BrowserConfigurations.Count == 0)
            {
                logger.LogCritical("Missing browser configuration from test plan");
            }

            foreach (var browserConfig in TestPlanDefinition.TestSettings.BrowserConfigurations)
            {
                if (string.IsNullOrWhiteSpace(browserConfig.Browser))
                {
                    logger.LogCritical("Missing browser from browser configuration");
                }

                if (browserConfig.ScreenWidth == null && browserConfig.ScreenHeight != null
                    || browserConfig.ScreenHeight == null && browserConfig.ScreenWidth != null)
                {
                    logger.LogCritical("Screen width and height both need to be specified or not specified");
                }
            }

            if (TestPlanDefinition.EnvironmentVariables == null)
            {
                logger.LogCritical("Missing environment variables from test plan");
            }
            else if (!string.IsNullOrEmpty(TestPlanDefinition.EnvironmentVariables.FilePath))
            {
                TestPlanDefinition.EnvironmentVariables = _testConfigParser.ParseTestConfig<EnvironmentVariables>(TestPlanDefinition.EnvironmentVariables.FilePath, logger);
            }

            if (TestPlanDefinition.EnvironmentVariables.Users == null
                || TestPlanDefinition.EnvironmentVariables.Users.Count == 0)
            {
                logger.LogCritical("At least one user must be specified");
            }

            foreach(var userConfig in TestPlanDefinition.EnvironmentVariables.Users)
            {
                if (string.IsNullOrEmpty(userConfig.PersonaName))
                {
                    logger.LogCritical("Missing persona name");
                }

                if (string.IsNullOrEmpty(userConfig.EmailKey))
                {
                    logger.LogCritical("Missing email key");
                }

                if (string.IsNullOrEmpty(userConfig.PasswordKey))
                {
                    logger.LogCritical("Missing password key");
                }
            }

            if (TestPlanDefinition.EnvironmentVariables.Users.Where(x => x.PersonaName == TestPlanDefinition.TestSuite?.Persona).FirstOrDefault() == null)
            {
                logger.LogCritical("Persona specified in test is not listed in environment variables");
            }

            IsValid = true;
        }

        public void SetEnvironment(string environmentId, ILogger logger)
        {
            if (string.IsNullOrEmpty(environmentId))
            {
                logger.LogCritical("Environment cannot be null nor empty.");
                logger.LogTrace("Environment: " + nameof(environmentId));
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
                logger.LogCritical("Cloud cannot be null nor empty.");
                logger.LogTrace("Cloud: " + nameof(cloud));
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
                logger.LogCritical("Tenant cannot be null nor empty.");
                logger.LogTrace("Tenant: " + nameof(tenantId));
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
                logger.LogCritical("Output directory cannot be null nor empty.");
                logger.LogTrace("Output directory: " + nameof(outputDirectory));
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
                logger.LogCritical("TestPlanDefinition is not valid");
            }

            var userConfiguration = TestPlanDefinition?.EnvironmentVariables?.Users?.Where(x => x.PersonaName == persona).FirstOrDefault();

            if (userConfiguration == null)
            {
                logger.LogCritical("Unable to find user configuration for persona.");
                logger.LogTrace($"Persona: {persona}");
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
