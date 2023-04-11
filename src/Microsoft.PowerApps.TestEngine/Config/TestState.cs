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
        private TestPlanDefinition TestPlanDefinition { get; set; }
        private List<TestCase> TestCases { get; set; } = new List<TestCase>();
        private string EnvironmentId { get; set; }
        private string Domain { get; set; }

        private string TenantId { get; set; }

        private string OutputDirectory { get; set; }

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

        public void ParseAndSetTestState(string testConfigFile)
        {
            if (string.IsNullOrEmpty(testConfigFile))
            {
                throw new ArgumentNullException(nameof(testConfigFile));
            }

            TestPlanDefinition = _testConfigParser.ParseTestConfig<TestPlanDefinition>(testConfigFile);
            if (TestPlanDefinition.TestSuite != null)
            {
                TestCases = TestPlanDefinition.TestSuite.TestCases;

                if (string.IsNullOrEmpty(TestPlanDefinition.TestSuite.TestSuiteName))
                {
                    throw new InvalidOperationException("Missing test suite name from test suite definition");
                }

                if (string.IsNullOrEmpty(TestPlanDefinition.TestSuite.Persona))
                {
                    throw new InvalidOperationException("Missing persona from test suite definition");
                }

                if (string.IsNullOrEmpty(TestPlanDefinition.TestSuite.AppLogicalName) && string.IsNullOrEmpty(TestPlanDefinition.TestSuite.AppId))
                {
                    throw new InvalidOperationException("At least one of the app logical name or app id must be present in test suite definition");
                }
            }

            if (TestCases.Count == 0)
            {
                throw new InvalidOperationException("Must be at least one test case");
            }

            foreach (var testCase in TestCases)
            {
                if (string.IsNullOrEmpty(testCase.TestCaseName))
                {
                    throw new InvalidOperationException("Missing test case name from test definition");
                }

                if (string.IsNullOrEmpty(testCase.TestSteps))
                {
                    throw new InvalidOperationException("Missing test steps from test case");
                }
            }

            if (TestPlanDefinition.TestSettings == null)
            {
                throw new InvalidOperationException("Missing test settings from test plan");
            }
            else if (!string.IsNullOrEmpty(TestPlanDefinition.TestSettings.FilePath))
            {
                TestPlanDefinition.TestSettings = _testConfigParser.ParseTestConfig<TestSettings>(TestPlanDefinition.TestSettings.FilePath);
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
            else if (!string.IsNullOrEmpty(TestPlanDefinition.EnvironmentVariables.FilePath))
            {
                TestPlanDefinition.EnvironmentVariables = _testConfigParser.ParseTestConfig<EnvironmentVariables>(TestPlanDefinition.EnvironmentVariables.FilePath);
            }

            if (TestPlanDefinition.EnvironmentVariables.Users == null
                || TestPlanDefinition.EnvironmentVariables.Users.Count == 0)
            {
                throw new InvalidOperationException("At least one user must be specified");
            }

            foreach (var userConfig in TestPlanDefinition.EnvironmentVariables.Users)
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

            if (TestPlanDefinition.EnvironmentVariables.Users.Where(x => x.PersonaName == TestPlanDefinition.TestSuite?.Persona).FirstOrDefault() == null)
            {
                throw new InvalidOperationException("Persona specified in test is not listed in environment variables");
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

        public string GetEnvironment()
        {
            return EnvironmentId;
        }

        public void SetDomain(string domain)
        {
            if (string.IsNullOrEmpty(domain))
            {
                throw new ArgumentNullException(nameof(domain));
            }
            Domain = domain;
        }

        public string GetDomain()
        {
            return Domain;
        }

        public void SetTenant(string tenantId)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                throw new ArgumentNullException(nameof(tenantId));
            }
            TenantId = tenantId;
        }

        public string GetTenant()
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
        public string GetOutputDirectory()
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
        public TestSettings GetTestSettings()
        {
            return TestPlanDefinition?.TestSettings;
        }

        public int GetTimeout()
        {
            return GetTestSettings().Timeout;
        }
    }
}
