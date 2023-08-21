// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.System;

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

        public void ParseAndSetTestState(string testConfigFile, ILogger logger)
        {
            if (string.IsNullOrEmpty(testConfigFile))
            {
                throw new ArgumentNullException(nameof(testConfigFile));
            }

            List<string> userInputExceptionMessages = new List<string>();
            try
            {
                TestPlanDefinition = _testConfigParser.ParseTestConfig<TestPlanDefinition>(testConfigFile, logger);
                if (TestPlanDefinition.TestSuite != null)
                {
                    TestCases = TestPlanDefinition.TestSuite.TestCases;

                    if (string.IsNullOrEmpty(TestPlanDefinition.TestSuite.TestSuiteName))
                    {
                        userInputExceptionMessages.Add("Missing test suite name from test suite definition");
                    }

                    if (string.IsNullOrEmpty(TestPlanDefinition.TestSuite.Persona))
                    {
                        userInputExceptionMessages.Add("Missing persona from test suite definition");
                    }

                    if (string.IsNullOrEmpty(TestPlanDefinition.TestSuite.AppLogicalName) && string.IsNullOrEmpty(TestPlanDefinition.TestSuite.AppId))
                    {
                        userInputExceptionMessages.Add("At least one of the app logical name or app id must be present in test suite definition");
                    }
                }

                if (TestCases == null || TestCases?.Count == 0)
                {
                    userInputExceptionMessages.Add("Must be at least one test case");
                }
                else
                {
                    foreach (var testCase in TestCases)
                    {
                        if (string.IsNullOrEmpty(testCase.TestCaseName))
                        {
                            userInputExceptionMessages.Add("Missing test case name from test definition");
                        }

                        if (string.IsNullOrEmpty(testCase.TestSteps))
                        {
                            userInputExceptionMessages.Add("Missing test steps from test case");
                        }
                    }
                }

                if (TestPlanDefinition.TestSettings == null)
                {
                    userInputExceptionMessages.Add("Missing test settings from test plan");
                }
                else if (!string.IsNullOrEmpty(TestPlanDefinition.TestSettings?.FilePath))
                {
                    TestPlanDefinition.TestSettings = _testConfigParser.ParseTestConfig<TestSettings>(TestPlanDefinition.TestSettings.FilePath, logger);
                }

                if (TestPlanDefinition.TestSettings?.BrowserConfigurations == null
                    || TestPlanDefinition.TestSettings?.BrowserConfigurations?.Count == 0)
                {
                    userInputExceptionMessages.Add("Missing browser configuration from test plan");
                }
                else
                {
                    foreach (var browserConfig in TestPlanDefinition.TestSettings?.BrowserConfigurations)
                    {
                        if (string.IsNullOrWhiteSpace(browserConfig.Browser))
                        {
                            userInputExceptionMessages.Add("Missing browser from browser configuration");
                        }

                        if (browserConfig.ScreenWidth == null && browserConfig.ScreenHeight != null
                            || browserConfig.ScreenHeight == null && browserConfig.ScreenWidth != null)
                        {
                            userInputExceptionMessages.Add("Screen width and height both need to be specified or not specified");
                        }
                    }
                }

                if (TestPlanDefinition.EnvironmentVariables == null)
                {
                    userInputExceptionMessages.Add("Missing environment variables from test plan");
                }
                else if (!string.IsNullOrEmpty(TestPlanDefinition.EnvironmentVariables.FilePath))
                {
                    TestPlanDefinition.EnvironmentVariables = _testConfigParser.ParseTestConfig<EnvironmentVariables>(TestPlanDefinition.EnvironmentVariables.FilePath, logger);
                }

                if (TestPlanDefinition.EnvironmentVariables?.Users == null
                    || TestPlanDefinition.EnvironmentVariables?.Users?.Count == 0)
                {
                    userInputExceptionMessages.Add("At least one user must be specified");
                }
                else
                {
                    foreach (var userConfig in TestPlanDefinition.EnvironmentVariables?.Users)
                    {
                        if (string.IsNullOrEmpty(userConfig.PersonaName))
                        {
                            userInputExceptionMessages.Add("Missing persona name");
                        }

                        if (string.IsNullOrEmpty(userConfig.EmailKey))
                        {
                            userInputExceptionMessages.Add("Missing email key");
                        }

                        if (string.IsNullOrEmpty(userConfig.PasswordKey))
                        {
                            userInputExceptionMessages.Add("Missing password key");
                        }
                    }
                }

                if (TestPlanDefinition.EnvironmentVariables?.Users?.Where(x => x.PersonaName == TestPlanDefinition.TestSuite?.Persona).FirstOrDefault() == null)
                {
                    userInputExceptionMessages.Add("Persona specified in test is not listed in environment variables");
                }
            }
            catch (UserInputException ex)
            {
                throw new UserInputException(ex.Message);
            }

            if (userInputExceptionMessages.Count() > 0)
            {
                logger.LogError($"Invalid User Input(s): {String.Join(", ", userInputExceptionMessages)}");
                throw new UserInputException(UserInputException.ErrorMapping.UserInputExceptionTestConfig.ToString());
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
