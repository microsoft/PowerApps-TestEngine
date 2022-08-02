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
                throw new ArgumentNullException();
            }

            TestPlanDefinition = _testConfigParser.ParseTestConfig<TestPlanDefinition>(testConfigFile, logger);
            if (TestPlanDefinition.TestSuite != null)
            {
                TestCases = TestPlanDefinition.TestSuite.TestCases;

                if (string.IsNullOrEmpty(TestPlanDefinition.TestSuite.TestSuiteName))
                {
                    logger.LogCritical("Missing test suite name from test suite definition");
                    throw new InvalidOperationException();
                }

                if (string.IsNullOrEmpty(TestPlanDefinition.TestSuite.Persona))
                {
                    logger.LogCritical("Missing persona from test suite definition");
                    throw new InvalidOperationException();
                }

                if (string.IsNullOrEmpty(TestPlanDefinition.TestSuite.AppLogicalName))
                {
                    logger.LogCritical("Missing app logical name from test suite definition");
                    throw new InvalidOperationException();
                }
            }

            if (TestCases.Count == 0)
            {
                logger.LogCritical("Must be at least one test case");
                throw new InvalidOperationException();
            }

            foreach(var testCase in TestCases)
            {
                if (string.IsNullOrEmpty(testCase.TestCaseName))
                {
                    logger.LogCritical("Missing test case name from test definition");
                    throw new InvalidOperationException();
                }

                if (string.IsNullOrEmpty(testCase.TestSteps))
                {
                    logger.LogCritical("Missing test steps from test case");
                    throw new InvalidOperationException();
                }
            }

            if (TestPlanDefinition.TestSettings == null)
            {
                logger.LogCritical("Missing test settings from test plan");
                throw new InvalidOperationException();
            }
            else if (!string.IsNullOrEmpty(TestPlanDefinition.TestSettings.FilePath))
            {
                TestPlanDefinition.TestSettings = _testConfigParser.ParseTestConfig<TestSettings>(TestPlanDefinition.TestSettings.FilePath, logger);
            }

            if (TestPlanDefinition.TestSettings.BrowserConfigurations == null 
                || TestPlanDefinition.TestSettings.BrowserConfigurations.Count == 0)
            {
                logger.LogCritical("Missing browser configuration from test plan");
                throw new InvalidOperationException();
            }

            foreach (var browserConfig in TestPlanDefinition.TestSettings.BrowserConfigurations)
            {
                if (string.IsNullOrWhiteSpace(browserConfig.Browser))
                {
                    logger.LogCritical("Missing browser from browser configuration");
                    throw new InvalidOperationException();
                }

                if (browserConfig.ScreenWidth == null && browserConfig.ScreenHeight != null
                    || browserConfig.ScreenHeight == null && browserConfig.ScreenWidth != null)
                {
                    logger.LogCritical("Screen width and height both need to be specified or not specified");
                    throw new InvalidOperationException();
                }
            }

            if (TestPlanDefinition.EnvironmentVariables == null)
            {
                logger.LogCritical("Missing environment variables from test plan");
                throw new InvalidOperationException();
            }
            else if (!string.IsNullOrEmpty(TestPlanDefinition.EnvironmentVariables.FilePath))
            {
                TestPlanDefinition.EnvironmentVariables = _testConfigParser.ParseTestConfig<EnvironmentVariables>(TestPlanDefinition.EnvironmentVariables.FilePath, logger);
            }

            if (TestPlanDefinition.EnvironmentVariables.Users == null
                || TestPlanDefinition.EnvironmentVariables.Users.Count == 0)
            {
                logger.LogCritical("At least one user must be specified");
                throw new InvalidOperationException();
            }

            foreach(var userConfig in TestPlanDefinition.EnvironmentVariables.Users)
            {
                if (string.IsNullOrEmpty(userConfig.PersonaName))
                {
                    logger.LogCritical("Missing persona name");
                    throw new InvalidOperationException();
                }

                if (string.IsNullOrEmpty(userConfig.EmailKey))
                {
                    logger.LogCritical("Missing email key");
                    throw new InvalidOperationException();
                }

                if (string.IsNullOrEmpty(userConfig.PasswordKey))
                {
                    logger.LogCritical("Missing password key");
                    throw new InvalidOperationException();
                }
            }

            if (TestPlanDefinition.EnvironmentVariables.Users.Where(x => x.PersonaName == TestPlanDefinition.TestSuite?.Persona).FirstOrDefault() == null)
            {
                logger.LogCritical("Persona specified in test is not listed in environment variables");
                throw new InvalidOperationException();
            }

            IsValid = true;
        }

        public void SetEnvironment(string environmentId, ILogger logger)
        {
            if (string.IsNullOrEmpty(environmentId))
            {
                logger.LogCritical("Environment cannot be null nor empty.");
                logger.LogTrace("Environment: " + nameof(environmentId));
                throw new ArgumentNullException();
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
                throw new ArgumentNullException();
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
                throw new ArgumentNullException();
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
                throw new ArgumentNullException();
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
                throw new InvalidOperationException();
            }

            var userConfiguration = TestPlanDefinition?.EnvironmentVariables?.Users?.Where(x => x.PersonaName == persona).FirstOrDefault();

            if (userConfiguration == null)
            {
                logger.LogCritical("Unable to find user configuration for persona.");
                logger.LogTrace($"Persona: {persona}");
                throw new InvalidOperationException();
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
