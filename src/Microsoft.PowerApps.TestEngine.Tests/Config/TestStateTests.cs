// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.Tests.Helpers;
using Moq;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.Config
{
    public class TestStateTests
    {
        private Mock<ITestConfigParser> MockTestConfigParser;
        private List<TestCase> TestCases = new List<TestCase>();
        private Mock<ILoggerFactory> MockLoggerFactory;
        private Mock<ILogger> MockLogger;

        public TestStateTests()
        {
            MockTestConfigParser = new Mock<ITestConfigParser>(MockBehavior.Strict);
            var testCase = new TestCase
            {
                TestCaseName = "Test Case Name",
                TestCaseDescription = "Test Case Description",
                TestSteps = "= 1+1"
            };
            TestCases.Add(testCase);
            MockLoggerFactory = new Mock<ILoggerFactory>(MockBehavior.Strict);
            MockLogger = new Mock<ILogger>(MockBehavior.Strict);
        }

        private TestPlanDefinition GenerateTestPlanDefinition()
        {
            return new TestPlanDefinition()
            {
                TestSuite = new TestSuiteDefinition()
                {
                    TestSuiteName = "Test Suite Name",
                    TestSuiteDescription = "Test Suite Description",
                    Persona = "User1",
                    AppLogicalName = Guid.NewGuid().ToString(),
                    TestCases = TestCases
                },
                TestSettings = new TestSettings()
                {
                    BrowserConfigurations = new List<BrowserConfiguration>()
                    {
                        new BrowserConfiguration()
                        {
                            Browser = "Chromium",
                        },
                        new BrowserConfiguration()
                        {
                            Browser = "Safari"
                        }
                    }
                },
                EnvironmentVariables = new EnvironmentVariables()
                {
                    Users = new List<UserConfiguration>()
                    {
                        new UserConfiguration()
                        {
                            PersonaName = "User1",
                            EmailKey = "User1Email",
                            PasswordKey = "User1Password"
                        }
                    }
                }
            };
        }

        private TestSettings GenerateTestSettings()
        {
            return new TestSettings()
            {
                BrowserConfigurations = new List<BrowserConfiguration>()
                    {
                        new BrowserConfiguration()
                        {
                            Browser = "Chromium",
                        },
                        new BrowserConfiguration()
                        {
                            Browser = "Safari"
                        }
                    }
            };
        }

        private EnvironmentVariables GenerateEnvironmentVariables()
        {
            return new EnvironmentVariables()
            {
                Users = new List<UserConfiguration>()
                    {
                        new UserConfiguration()
                        {
                            PersonaName = "User1",
                            EmailKey = "User1Email",
                            PasswordKey = "User1Password"
                        }
                    }
            };
        }

        [Fact]
        public void TestStateSuccessTest()
        {
            var state = new TestState(MockTestConfigParser.Object);
            var testConfigFile = "testPlan.fx.yaml";

            var testPlanDefinition = GenerateTestPlanDefinition();
            MockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(MockLogger.Object);

            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>(), MockLogger.Object)).Returns(testPlanDefinition);
            state.ParseAndSetTestState(testConfigFile, MockLogger.Object);

            var testSuiteDefinitions = state.GetTestSuiteDefinition();
            Assert.NotNull(testSuiteDefinitions);
            Assert.Equal(testPlanDefinition.TestSuite, testSuiteDefinitions);

            var testCases = state.GetTestCases();
            Assert.NotNull(testCases);
            Assert.Single(testCases);
            Assert.Equal(testPlanDefinition.TestSuite.TestCases[0], testCases[0]);

            var testSettings = state.GetTestSettings();
            Assert.NotNull(testSettings);
            Assert.Equal(testPlanDefinition.TestSettings, testSettings);

            var userConfiguration = state.GetUserConfiguration("User1");
            Assert.Equal(testPlanDefinition.EnvironmentVariables.Users[0], userConfiguration);

            Assert.Throws<InvalidOperationException>(() => state.GetUserConfiguration("NonExistentUser"));

            var environmentId = Guid.NewGuid().ToString();
            var tenantId = Guid.NewGuid().ToString();
            var domain = "apps.powerapps.com";
            var outputDirectory = Guid.NewGuid().ToString();

            state.SetEnvironment(environmentId);
            Assert.Equal(environmentId, state.GetEnvironment());
            state.SetTenant(tenantId);
            Assert.Equal(tenantId, state.GetTenant());
            state.SetDomain(domain);
            Assert.Equal(domain, state.GetDomain());
            state.SetOutputDirectory(outputDirectory);
            Assert.Equal(outputDirectory, state.GetOutputDirectory());
        }

        [Fact]
        public void TestStateSuccessOnFilePathTest()
        {
            var state = new TestState(MockTestConfigParser.Object);
            var testConfigFile = "testPlan.fx.yaml";
            var testSettingsFile = "testSettings.fx.yaml";
            var environmentVariablesFile = "environmentVariables.fx.yaml";

            var testPlanDefinition = GenerateTestPlanDefinition();
            var testSettings = GenerateTestSettings();
            var environmentVariables = GenerateEnvironmentVariables();

            testPlanDefinition.TestSettings.FilePath = testSettingsFile;
            testPlanDefinition.EnvironmentVariables.FilePath = environmentVariablesFile;

            MockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(MockLogger.Object);

            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>(), MockLogger.Object)).Returns(testPlanDefinition);
            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestSettings>(It.IsAny<string>(), MockLogger.Object)).Returns(testSettings);
            MockTestConfigParser.Setup(x => x.ParseTestConfig<EnvironmentVariables>(It.IsAny<string>(), MockLogger.Object)).Returns(environmentVariables);
            state.ParseAndSetTestState(testConfigFile, MockLogger.Object);

            var actualTestSettings = state.GetTestSettings();
            Assert.NotNull(actualTestSettings);
            Assert.Equal(testSettings, actualTestSettings);

            var userConfiguration = state.GetUserConfiguration("User1");
            Assert.Equal(environmentVariables.Users[0], userConfiguration);

            Assert.Throws<InvalidOperationException>(() => state.GetUserConfiguration("NonExistentUser"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ParseAndSetTestStateThrowsOnInvalidTestConfigFile(string testConfigPath)
        {
            var state = new TestState(MockTestConfigParser.Object);
            MockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(MockLogger.Object);

            Assert.Throws<ArgumentNullException>(() => state.ParseAndSetTestState(testConfigPath, MockLogger.Object));
        }

        [Fact]
        public void ParseAndSetTestStateThrowsOnNoTestSuiteDefinition()
        {
            var state = new TestState(MockTestConfigParser.Object);
            var testConfigFile = "testPlan.fx.yaml";
            var testPlanDefinition = GenerateTestPlanDefinition();
            testPlanDefinition.TestSuite = null;
            MockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);
            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>(), MockLogger.Object)).Returns(testPlanDefinition);
            var expectedErrorMessage = "Invalid User Input(s): Must be at least one test case, Persona specified in test is not listed in environment variables";

            // Act and Arrange
            var ex = Assert.Throws<UserInputException>(() => state.ParseAndSetTestState(testConfigFile, MockLogger.Object));
            Assert.Equal(UserInputException.ErrorMapping.UserInputExceptionTestConfig.ToString(), ex.Message);            
            LoggingTestHelper.VerifyLogging(MockLogger, expectedErrorMessage, LogLevel.Error, Times.Once());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ParseAndSetTestStateThrowsOnNoNameInTestSuiteDefinition(string testName)
        {
            var state = new TestState(MockTestConfigParser.Object);
            var testConfigFile = "testPlan.fx.yaml";
            var testPlanDefinition = GenerateTestPlanDefinition();
            testPlanDefinition.TestSuite.TestSuiteName = testName;
            MockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);
            var expectedErrorMessage = "Invalid User Input(s): Missing test suite name from test suite definition";
            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>(), MockLogger.Object)).Returns(testPlanDefinition);

            var ex = Assert.Throws<UserInputException>(() => state.ParseAndSetTestState(testConfigFile, MockLogger.Object));
            Assert.Equal(UserInputException.ErrorMapping.UserInputExceptionTestConfig.ToString(), ex.Message);            
            LoggingTestHelper.VerifyLogging(MockLogger, expectedErrorMessage, LogLevel.Error, Times.Once());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ParseAndSetTestStateThrowsOnNoPersonaInTestSuiteDefinition(string persona)
        {
            var state = new TestState(MockTestConfigParser.Object);
            var testConfigFile = "testPlan.fx.yaml";
            var testPlanDefinition = GenerateTestPlanDefinition();
            testPlanDefinition.TestSuite.Persona = persona;
            MockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);
            var expectedErrorMessage = "Invalid User Input(s): Missing persona from test suite definition, Persona specified in test is not listed in environment variables";
            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>(), MockLogger.Object)).Returns(testPlanDefinition);

            var ex = Assert.Throws<UserInputException>(() => state.ParseAndSetTestState(testConfigFile, MockLogger.Object));
            Assert.Equal(UserInputException.ErrorMapping.UserInputExceptionTestConfig.ToString(), ex.Message);          
            LoggingTestHelper.VerifyLogging(MockLogger, expectedErrorMessage, LogLevel.Error, Times.Once());
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("", "")]
        [InlineData(null, "")]
        [InlineData("", null)]
        public void ParseAndSetTestStateThrowsOnNoAppLogicalNameOrAppIdInTestSuiteDefinition(string appLogicalName, string appId)
        {
            var state = new TestState(MockTestConfigParser.Object);
            var testConfigFile = "testPlan.fx.yaml";
            var testPlanDefinition = GenerateTestPlanDefinition();
            testPlanDefinition.TestSuite.AppLogicalName = appLogicalName;
            testPlanDefinition.TestSuite.AppId = appId;
            MockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);
            var expectedErrorMessage = "Invalid User Input(s): At least one of the app logical name or app id must be present in test suite definition";
            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>(), MockLogger.Object)).Returns(testPlanDefinition);

            var ex = Assert.Throws<UserInputException>(() => state.ParseAndSetTestState(testConfigFile, MockLogger.Object));
            Assert.Equal(UserInputException.ErrorMapping.UserInputExceptionTestConfig.ToString(), ex.Message);            
            LoggingTestHelper.VerifyLogging(MockLogger, expectedErrorMessage, LogLevel.Error, Times.Once());
        }

        [Theory]
        [InlineData("appLogicalName", null)]
        [InlineData(null, "appId")]
        public void ParseAndSetTestStateDoesNotThrowWhenEitherOfAppLogicalNameOrAppIdInTestSuiteDefinition(string appLogicalName, string appId)
        {
            var state = new TestState(MockTestConfigParser.Object);
            var testConfigFile = "testPlan.fx.yaml";
            var testPlanDefinition = GenerateTestPlanDefinition();
            testPlanDefinition.TestSuite.AppLogicalName = appLogicalName;
            testPlanDefinition.TestSuite.AppId = appId;
            MockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);

            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>(), MockLogger.Object)).Returns(testPlanDefinition);
            state.ParseAndSetTestState(testConfigFile, MockLogger.Object);
            Assert.Equal(state.GetTestSuiteDefinition().AppLogicalName, appLogicalName);
            Assert.Equal(state.GetTestSuiteDefinition().AppId, appId);
        }

        [Fact]
        public void ParseAndSetTestStateThrowsOnNoTestCaseInTestSuiteDefinition()
        {
            var state = new TestState(MockTestConfigParser.Object);
            var testConfigFile = "testPlan.fx.yaml";
            var testPlanDefinition = GenerateTestPlanDefinition();
            testPlanDefinition.TestSuite.TestCases = new List<TestCase>();
            MockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);
            var expectedErrorMessage = "Invalid User Input(s): Must be at least one test case";
            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>(), MockLogger.Object)).Returns(testPlanDefinition);

            var ex = Assert.Throws<UserInputException>(() => state.ParseAndSetTestState(testConfigFile, MockLogger.Object));
            Assert.Equal(UserInputException.ErrorMapping.UserInputExceptionTestConfig.ToString(), ex.Message);            
            LoggingTestHelper.VerifyLogging(MockLogger, expectedErrorMessage, LogLevel.Error, Times.Once());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ParseAndSetTestStateThrowsOnNoTestCaseNameInTestCase(string testCaseName)
        {
            var state = new TestState(MockTestConfigParser.Object);
            var testConfigFile = "testPlan.fx.yaml";
            var testPlanDefinition = GenerateTestPlanDefinition();
            testPlanDefinition.TestSuite.TestCases[0].TestCaseName = testCaseName;
            MockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);
            var expectedErrorMessage = "Invalid User Input(s): Missing test case name from test definition";
            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>(), MockLogger.Object)).Returns(testPlanDefinition);

            var ex = Assert.Throws<UserInputException>(() => state.ParseAndSetTestState(testConfigFile, MockLogger.Object));
            Assert.Equal(UserInputException.ErrorMapping.UserInputExceptionTestConfig.ToString(), ex.Message);            
            LoggingTestHelper.VerifyLogging(MockLogger, expectedErrorMessage, LogLevel.Error, Times.Once());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ParseAndSetTestStateThrowsOnNoTestStepsInTestCase(string testSteps)
        {
            var state = new TestState(MockTestConfigParser.Object);
            var testConfigFile = "testPlan.fx.yaml";
            var testPlanDefinition = GenerateTestPlanDefinition();
            testPlanDefinition.TestSuite.TestCases[0].TestSteps = testSteps;
            MockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);
            var expectedErrorMessage = "Invalid User Input(s): Missing test steps from test case";
            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>(), MockLogger.Object)).Returns(testPlanDefinition);

            var ex = Assert.Throws<UserInputException>(() => state.ParseAndSetTestState(testConfigFile, MockLogger.Object));
            Assert.Equal(UserInputException.ErrorMapping.UserInputExceptionTestConfig.ToString(), ex.Message);            
            LoggingTestHelper.VerifyLogging(MockLogger, expectedErrorMessage, LogLevel.Error, Times.Once());
        }

        [Fact]
        public void ParseAndSetTestStateThrowsOnNoTestSettings()
        {
            var state = new TestState(MockTestConfigParser.Object);
            var testConfigFile = "testPlan.fx.yaml";
            var testPlanDefinition = GenerateTestPlanDefinition();
            testPlanDefinition.TestSettings = null;
            MockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);
            var expectedErrorMessage = "Invalid User Input(s): Missing test settings from test plan, Missing browser configuration from test plan";
            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>(), MockLogger.Object)).Returns(testPlanDefinition);

            var ex = Assert.Throws<UserInputException>(() => state.ParseAndSetTestState(testConfigFile, MockLogger.Object));
            Assert.Equal(UserInputException.ErrorMapping.UserInputExceptionTestConfig.ToString(), ex.Message);            
            LoggingTestHelper.VerifyLogging(MockLogger, expectedErrorMessage, LogLevel.Error, Times.Once());
        }

        [Fact]
        public void ParseAndSetTestStateThrowsOnNullBrowserConfigurationInTestSettings()
        {
            var state = new TestState(MockTestConfigParser.Object);
            var testConfigFile = "testPlan.fx.yaml";
            var testPlanDefinition = GenerateTestPlanDefinition();
            testPlanDefinition.TestSettings.BrowserConfigurations = null;
            MockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);
            var expectedErrorMessage = "Invalid User Input(s): Missing browser configuration from test plan";
            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>(), MockLogger.Object)).Returns(testPlanDefinition);

            var ex = Assert.Throws<UserInputException>(() => state.ParseAndSetTestState(testConfigFile, MockLogger.Object));
            Assert.Equal(UserInputException.ErrorMapping.UserInputExceptionTestConfig.ToString(), ex.Message);            
            LoggingTestHelper.VerifyLogging(MockLogger, expectedErrorMessage, LogLevel.Error, Times.Once());
        }

        [Fact]
        public void ParseAndSetTestStateThrowsOnNoBrowserConfigurationInTestSettings()
        {
            var state = new TestState(MockTestConfigParser.Object);
            var testConfigFile = "testPlan.fx.yaml";
            var testPlanDefinition = GenerateTestPlanDefinition();
            testPlanDefinition.TestSettings.BrowserConfigurations = new List<BrowserConfiguration>();
            MockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);
            var expectedErrorMessage = "Invalid User Input(s): Missing browser configuration from test plan";
            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>(), MockLogger.Object)).Returns(testPlanDefinition);

            var ex = Assert.Throws<UserInputException>(() => state.ParseAndSetTestState(testConfigFile, MockLogger.Object));
            Assert.Equal(UserInputException.ErrorMapping.UserInputExceptionTestConfig.ToString(), ex.Message);            
            LoggingTestHelper.VerifyLogging(MockLogger, expectedErrorMessage, LogLevel.Error, Times.Once());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ParseAndSetTestStateThrowsOnNoBrowserInBrowserConfigurationInTestSettings(string browser)
        {
            var state = new TestState(MockTestConfigParser.Object);
            var testConfigFile = "testPlan.fx.yaml";
            var testPlanDefinition = GenerateTestPlanDefinition();
            testPlanDefinition.TestSettings.BrowserConfigurations.Add(new BrowserConfiguration() { Browser = browser });
            MockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);
            var expectedErrorMessage = "Invalid User Input(s): Missing browser from browser configuration";
            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>(), MockLogger.Object)).Returns(testPlanDefinition);

            var ex = Assert.Throws<UserInputException>(() => state.ParseAndSetTestState(testConfigFile, MockLogger.Object));
            Assert.Equal(UserInputException.ErrorMapping.UserInputExceptionTestConfig.ToString(), ex.Message);            
            LoggingTestHelper.VerifyLogging(MockLogger, expectedErrorMessage, LogLevel.Error, Times.Once());
        }

        [Theory]
        [InlineData(null, 800)]
        [InlineData(800, null)]
        public void ParseAndSetTestStateThrowsOnInvalidScreenConfigInBrowserConfigurationInTestSettings(int? screenWidth, int? screenHeight)
        {
            var state = new TestState(MockTestConfigParser.Object);
            var testConfigFile = "testPlan.fx.yaml";
            var testPlanDefinition = GenerateTestPlanDefinition();
            testPlanDefinition.TestSettings.BrowserConfigurations.Add(new BrowserConfiguration()
            {
                Browser = "Chromium",
                ScreenWidth = screenWidth,
                ScreenHeight = screenHeight
            });
            MockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);
            var expectedErrorMessage = "Invalid User Input(s): Screen width and height both need to be specified or not specified";
            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>(), MockLogger.Object)).Returns(testPlanDefinition);

            var ex = Assert.Throws<UserInputException>(() => state.ParseAndSetTestState(testConfigFile, MockLogger.Object));
            Assert.Equal(UserInputException.ErrorMapping.UserInputExceptionTestConfig.ToString(), ex.Message);            
            LoggingTestHelper.VerifyLogging(MockLogger, expectedErrorMessage, LogLevel.Error, Times.Once());
        }

        [Fact]
        public void ParseAndSetTestStateThrowsOnNoEnvironmentVariables()
        {
            var state = new TestState(MockTestConfigParser.Object);
            var testConfigFile = "testPlan.fx.yaml";
            var testPlanDefinition = GenerateTestPlanDefinition();
            testPlanDefinition.EnvironmentVariables = null;
            MockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);
            var expectedErrorMessage = "Invalid User Input(s): Missing environment variables from test plan, At least one user must be specified, Persona specified in test is not listed in environment variables";
            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>(), MockLogger.Object)).Returns(testPlanDefinition);

            var ex = Assert.Throws<UserInputException>(() => state.ParseAndSetTestState(testConfigFile, MockLogger.Object));
            Assert.Equal(UserInputException.ErrorMapping.UserInputExceptionTestConfig.ToString(), ex.Message);
            LoggingTestHelper.VerifyLogging(MockLogger, expectedErrorMessage, LogLevel.Error, Times.Once());
        }

        [Fact]
        public void ParseAndSetTestStateThrowsOnNullUserConfigurationInEnvironmentVariables()
        {
            var state = new TestState(MockTestConfigParser.Object);
            var testConfigFile = "testPlan.fx.yaml";
            var testPlanDefinition = GenerateTestPlanDefinition();
            testPlanDefinition.EnvironmentVariables.Users = null;
            MockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);
            var expectedErrorMessage = "Invalid User Input(s): At least one user must be specified, Persona specified in test is not listed in environment variables";
            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>(), MockLogger.Object)).Returns(testPlanDefinition);

            var ex = Assert.Throws<UserInputException>(() => state.ParseAndSetTestState(testConfigFile, MockLogger.Object));
            Assert.Equal(UserInputException.ErrorMapping.UserInputExceptionTestConfig.ToString(), ex.Message);
            LoggingTestHelper.VerifyLogging(MockLogger, expectedErrorMessage, LogLevel.Error, Times.Once());
        }

        [Fact]
        public void ParseAndSetTestStateThrowsOnNoUserConfigurationInEnvironmentVariables()
        {
            var state = new TestState(MockTestConfigParser.Object);
            var testConfigFile = "testPlan.fx.yaml";
            var testPlanDefinition = GenerateTestPlanDefinition();
            testPlanDefinition.EnvironmentVariables.Users = new List<UserConfiguration>();
            MockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);
            var expectedErrorMessage = "Invalid User Input(s): At least one user must be specified, Persona specified in test is not listed in environment variables";
            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>(), MockLogger.Object)).Returns(testPlanDefinition);

            var ex = Assert.Throws<UserInputException>(() => state.ParseAndSetTestState(testConfigFile, MockLogger.Object));
            Assert.Equal(UserInputException.ErrorMapping.UserInputExceptionTestConfig.ToString(), ex.Message);
            LoggingTestHelper.VerifyLogging(MockLogger, expectedErrorMessage, LogLevel.Error, Times.Once());
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void ParseAndSetTestStateThrowsOnNoPersonaNameInUserConfigurationInEnvironmentVariables(string personaName)
        {
            var state = new TestState(MockTestConfigParser.Object);
            var testConfigFile = "testPlan.fx.yaml";
            var testPlanDefinition = GenerateTestPlanDefinition();
            testPlanDefinition.EnvironmentVariables.Users[0].PersonaName = personaName;
            MockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);
            var expectedErrorMessage = "Invalid User Input(s): Missing persona name, Persona specified in test is not listed in environment variables";
            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>(), MockLogger.Object)).Returns(testPlanDefinition);

            var ex = Assert.Throws<UserInputException>(() => state.ParseAndSetTestState(testConfigFile, MockLogger.Object));
            Assert.Equal(UserInputException.ErrorMapping.UserInputExceptionTestConfig.ToString(), ex.Message);
            LoggingTestHelper.VerifyLogging(MockLogger, expectedErrorMessage, LogLevel.Error, Times.Once());
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void ParseAndSetTestStateThrowsOnNoEmailKeyInUserConfigurationInEnvironmentVariables(string emailKey)
        {
            var state = new TestState(MockTestConfigParser.Object);
            var testConfigFile = "testPlan.fx.yaml";
            var testPlanDefinition = GenerateTestPlanDefinition();
            testPlanDefinition.EnvironmentVariables.Users[0].EmailKey = emailKey;
            MockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);
            var expectedErrorMessage = "Invalid User Input(s): Missing email key";
            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>(), MockLogger.Object)).Returns(testPlanDefinition);

            var ex = Assert.Throws<UserInputException>(() => state.ParseAndSetTestState(testConfigFile, MockLogger.Object));
            Assert.Equal(UserInputException.ErrorMapping.UserInputExceptionTestConfig.ToString(), ex.Message);
            LoggingTestHelper.VerifyLogging(MockLogger, expectedErrorMessage, LogLevel.Error, Times.Once());
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void ParseAndSetTestStateThrowsOnNoPasswordKeyInUserConfigurationInEnvironmentVariables(string passwordKey)
        {
            var state = new TestState(MockTestConfigParser.Object);
            var testConfigFile = "testPlan.fx.yaml";
            var testPlanDefinition = GenerateTestPlanDefinition();
            testPlanDefinition.EnvironmentVariables.Users[0].PasswordKey = passwordKey;
            MockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);
            var expectedErrorMessage = "Invalid User Input(s): Missing password key";
            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>(), MockLogger.Object)).Returns(testPlanDefinition);

            var ex = Assert.Throws<UserInputException>(() => state.ParseAndSetTestState(testConfigFile, MockLogger.Object));
            Assert.Equal(UserInputException.ErrorMapping.UserInputExceptionTestConfig.ToString(), ex.Message);
            LoggingTestHelper.VerifyLogging(MockLogger, expectedErrorMessage, LogLevel.Error, Times.Once());
        }

        [Fact]
        public void ParseAndSetTestStateThrowsOnTestSuiteDefinitionUserNotDefined()
        {
            var state = new TestState(MockTestConfigParser.Object);
            var testConfigFile = "testPlan.fx.yaml";
            var testPlanDefinition = GenerateTestPlanDefinition();
            testPlanDefinition.TestSuite.Persona = Guid.NewGuid().ToString();
            MockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);
            var expectedErrorMessage = "Invalid User Input(s): Persona specified in test is not listed in environment variables";
            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>(), MockLogger.Object)).Returns(testPlanDefinition);

            var ex = Assert.Throws<UserInputException>(() => state.ParseAndSetTestState(testConfigFile, MockLogger.Object));
            Assert.Equal(UserInputException.ErrorMapping.UserInputExceptionTestConfig.ToString(), ex.Message);
            LoggingTestHelper.VerifyLogging(MockLogger, expectedErrorMessage, LogLevel.Error, Times.Once());
        }

        [Fact]
        public void ParseAndSetTestStateThrowsOnMulitpleMissingValues()
        {
            var state = new TestState(MockTestConfigParser.Object);
            var testConfigFile = "testPlan.fx.yaml";
            var testPlanDefinition = GenerateTestPlanDefinition();
            var expectedErrorMessage = "Invalid User Input(s): Must be at least one test case, Missing test settings from test plan, Missing browser configuration from test plan, Persona specified in test is not listed in environment variables";

            // setting testcases to null
            testPlanDefinition.TestSuite.TestCases = null;
            testPlanDefinition.TestSettings = null;
            testPlanDefinition.TestSuite.Persona = Guid.NewGuid().ToString();  
            
            MockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);
            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>(), MockLogger.Object)).Returns(testPlanDefinition);

            var ex = Assert.Throws<UserInputException>(() => state.ParseAndSetTestState(testConfigFile, MockLogger.Object));
            Assert.Equal(UserInputException.ErrorMapping.UserInputExceptionTestConfig.ToString(), ex.Message);
            LoggingTestHelper.VerifyLogging(MockLogger, expectedErrorMessage, LogLevel.Error, Times.Once());
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void SetEnvironmentThrowsOnNullInput(string environment)
        {
            var state = new TestState(MockTestConfigParser.Object);
            Assert.Throws<ArgumentNullException>(() => state.SetEnvironment(environment));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void SetDomainThrowsOnNullInput(string domain)
        {
            var state = new TestState(MockTestConfigParser.Object);
            Assert.Throws<ArgumentNullException>(() => state.SetDomain(domain));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void SetTenantThrowsOnNullInput(string tenant)
        {
            var state = new TestState(MockTestConfigParser.Object);
            Assert.Throws<ArgumentNullException>(() => state.SetTenant(tenant));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void SetOutputDirectoryThrowsOnNullInput(string outputDirectory)
        {
            var state = new TestState(MockTestConfigParser.Object);
            Assert.Throws<ArgumentNullException>(() => state.SetOutputDirectory(outputDirectory));
        }
    }
}
