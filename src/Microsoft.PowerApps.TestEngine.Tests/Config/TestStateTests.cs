// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Castle.Core.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Moq;
using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.Config
{
    public class TestStateTests
    {
        private Mock<ITestConfigParser> MockTestConfigParser;
        private List<TestCase> TestCases = new List<TestCase>();
        private Mock<Microsoft.Extensions.Logging.ILogger> MockLogger;

        public TestStateTests()
        {
            MockLogger = new Mock<Microsoft.Extensions.Logging.ILogger>(MockBehavior.Loose);
            MockTestConfigParser = new Mock<ITestConfigParser>(MockBehavior.Strict);
            var testCase = new TestCase
            {
                TestCaseName = "Test Case Name",
                TestCaseDescription = "Test Case Description",
                TestSteps = "= 1+1"
            };
            TestCases.Add(testCase);
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

            var userConfiguration = state.GetUserConfiguration("User1", MockLogger.Object);
            Assert.Equal(testPlanDefinition.EnvironmentVariables.Users[0], userConfiguration);

            Assert.Throws<InvalidOperationException>(() => state.GetUserConfiguration("NonExistentUser", MockLogger.Object));

            var environmentId = Guid.NewGuid().ToString();
            var tenantId = Guid.NewGuid().ToString();
            var cloud = "Prod";
            var outputDirectory = Guid.NewGuid().ToString();

            state.SetEnvironment(environmentId, MockLogger.Object);
            Assert.Equal(environmentId, state.GetEnvironment());
            state.SetTenant(tenantId, MockLogger.Object);
            Assert.Equal(tenantId, state.GetTenant());
            state.SetCloud(cloud, MockLogger.Object);
            Assert.Equal(cloud, state.GetCloud());
            state.SetOutputDirectory(outputDirectory, MockLogger.Object);
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

            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>(), MockLogger.Object)).Returns(testPlanDefinition);
            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestSettings>(It.IsAny<string>(), MockLogger.Object)).Returns(testSettings);
            MockTestConfigParser.Setup(x => x.ParseTestConfig<EnvironmentVariables>(It.IsAny<string>(), MockLogger.Object)).Returns(environmentVariables);
            state.ParseAndSetTestState(testConfigFile, MockLogger.Object);

            var actualTestSettings = state.GetTestSettings();
            Assert.NotNull(actualTestSettings);
            Assert.Equal(testSettings, actualTestSettings);

            var userConfiguration = state.GetUserConfiguration("User1", MockLogger.Object);
            Assert.Equal(environmentVariables.Users[0], userConfiguration);

            Assert.Throws<InvalidOperationException>(() => state.GetUserConfiguration("NonExistentUser", MockLogger.Object));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ParseAndSetTestStateThrowsOnInvalidTestConfigFile(string? testConfigPath)
        {
            var state = new TestState(MockTestConfigParser.Object);
            Assert.Throws<ArgumentNullException>(() => state.ParseAndSetTestState(testConfigPath, MockLogger.Object));
        }

        [Fact]
        public void ParseAndSetTestStateThrowsOnNoTestSuiteDefinition()
        {
            var state = new TestState(MockTestConfigParser.Object);
            var testConfigFile = "testPlan.fx.yaml";
            var testPlanDefinition = GenerateTestPlanDefinition();
            testPlanDefinition.TestSuite = null;

            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>(), MockLogger.Object)).Returns(testPlanDefinition);
            Assert.Throws<InvalidOperationException>(() => state.ParseAndSetTestState(testConfigFile, MockLogger.Object));
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

            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>(), MockLogger.Object)).Returns(testPlanDefinition);
            Assert.Throws<InvalidOperationException>(() => state.ParseAndSetTestState(testConfigFile, MockLogger.Object));
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

            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>(), MockLogger.Object)).Returns(testPlanDefinition);
            Assert.Throws<InvalidOperationException>(() => state.ParseAndSetTestState(testConfigFile, MockLogger.Object));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ParseAndSetTestStateThrowsOnNoAppLogicalNameInTestSuiteDefinition(string appLogicalName)
        {
            var state = new TestState(MockTestConfigParser.Object);
            var testConfigFile = "testPlan.fx.yaml";
            var testPlanDefinition = GenerateTestPlanDefinition();
            testPlanDefinition.TestSuite.AppLogicalName = appLogicalName;

            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>(), MockLogger.Object)).Returns(testPlanDefinition);
            Assert.Throws<InvalidOperationException>(() => state.ParseAndSetTestState(testConfigFile, MockLogger.Object));
        }

        [Fact]
        public void ParseAndSetTestStateThrowsOnNoTestCaseInTestSuiteDefinition()
        {
            var state = new TestState(MockTestConfigParser.Object);
            var testConfigFile = "testPlan.fx.yaml";
            var testPlanDefinition = GenerateTestPlanDefinition();
            testPlanDefinition.TestSuite.TestCases = new List<TestCase>();

            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>(), MockLogger.Object)).Returns(testPlanDefinition);
            Assert.Throws<InvalidOperationException>(() => state.ParseAndSetTestState(testConfigFile, MockLogger.Object));
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

            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>(), MockLogger.Object)).Returns(testPlanDefinition);
            Assert.Throws<InvalidOperationException>(() => state.ParseAndSetTestState(testConfigFile, MockLogger.Object));
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

            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>(), MockLogger.Object)).Returns(testPlanDefinition);
            Assert.Throws<InvalidOperationException>(() => state.ParseAndSetTestState(testConfigFile, MockLogger.Object));
        }

        [Fact]
        public void ParseAndSetTestStateThrowsOnNoTestSettings()
        {
            var state = new TestState(MockTestConfigParser.Object);
            var testConfigFile = "testPlan.fx.yaml";
            var testPlanDefinition = GenerateTestPlanDefinition();
            testPlanDefinition.TestSettings = null;

            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>(), MockLogger.Object)).Returns(testPlanDefinition);
            Assert.Throws<InvalidOperationException>(() => state.ParseAndSetTestState(testConfigFile, MockLogger.Object));
        }

        [Fact]
        public void ParseAndSetTestStateThrowsOnNullBrowserConfigurationInTestSettings()
        {
            var state = new TestState(MockTestConfigParser.Object);
            var testConfigFile = "testPlan.fx.yaml";
            var testPlanDefinition = GenerateTestPlanDefinition();
            testPlanDefinition.TestSettings.BrowserConfigurations = null;

            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>(), MockLogger.Object)).Returns(testPlanDefinition);
            Assert.Throws<InvalidOperationException>(() => state.ParseAndSetTestState(testConfigFile, MockLogger.Object));
        }

        [Fact]
        public void ParseAndSetTestStateThrowsOnNoBrowserConfigurationInTestSettings()
        {
            var state = new TestState(MockTestConfigParser.Object);
            var testConfigFile = "testPlan.fx.yaml";
            var testPlanDefinition = GenerateTestPlanDefinition();
            testPlanDefinition.TestSettings.BrowserConfigurations = new List<BrowserConfiguration>();

            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>(), MockLogger.Object)).Returns(testPlanDefinition);
            Assert.Throws<InvalidOperationException>(() => state.ParseAndSetTestState(testConfigFile, MockLogger.Object));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ParseAndSetTestStateThrowsOnNoBrowserInBrowserConfigurationInTestSettings(string? browser)
        {
            var state = new TestState(MockTestConfigParser.Object);
            var testConfigFile = "testPlan.fx.yaml";
            var testPlanDefinition = GenerateTestPlanDefinition();
            testPlanDefinition.TestSettings.BrowserConfigurations.Add(new BrowserConfiguration() { Browser = browser });

            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>(), MockLogger.Object)).Returns(testPlanDefinition);
            Assert.Throws<InvalidOperationException>(() => state.ParseAndSetTestState(testConfigFile, MockLogger.Object));
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

            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>(), MockLogger.Object)).Returns(testPlanDefinition);
            Assert.Throws<InvalidOperationException>(() => state.ParseAndSetTestState(testConfigFile, MockLogger.Object));
        }

        [Fact]
        public void ParseAndSetTestStateThrowsOnNoEnvironmentVariables()
        {
            var state = new TestState(MockTestConfigParser.Object);
            var testConfigFile = "testPlan.fx.yaml";
            var testPlanDefinition = GenerateTestPlanDefinition();
            testPlanDefinition.EnvironmentVariables = null;

            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>(), MockLogger.Object)).Returns(testPlanDefinition);
            Assert.Throws<InvalidOperationException>(() => state.ParseAndSetTestState(testConfigFile, MockLogger.Object));
        }

        [Fact]
        public void ParseAndSetTestStateThrowsOnNullUserConfigurationInEnvironmentVariables()
        {
            var state = new TestState(MockTestConfigParser.Object);
            var testConfigFile = "testPlan.fx.yaml";
            var testPlanDefinition = GenerateTestPlanDefinition();
            testPlanDefinition.EnvironmentVariables.Users = null;

            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>(), MockLogger.Object)).Returns(testPlanDefinition);
            Assert.Throws<InvalidOperationException>(() => state.ParseAndSetTestState(testConfigFile, MockLogger.Object));
        }

        [Fact]
        public void ParseAndSetTestStateThrowsOnNoUserConfigurationInEnvironmentVariables()
        {
            var state = new TestState(MockTestConfigParser.Object);
            var testConfigFile = "testPlan.fx.yaml";
            var testPlanDefinition = GenerateTestPlanDefinition();
            testPlanDefinition.EnvironmentVariables.Users = new List<UserConfiguration>();

            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>(), MockLogger.Object)).Returns(testPlanDefinition);
            Assert.Throws<InvalidOperationException>(() => state.ParseAndSetTestState(testConfigFile, MockLogger.Object));
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

            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>(), MockLogger.Object)).Returns(testPlanDefinition);
            Assert.Throws<InvalidOperationException>(() => state.ParseAndSetTestState(testConfigFile, MockLogger.Object));
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

            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>(), MockLogger.Object)).Returns(testPlanDefinition);
            Assert.Throws<InvalidOperationException>(() => state.ParseAndSetTestState(testConfigFile, MockLogger.Object));
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

            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>(), MockLogger.Object)).Returns(testPlanDefinition);
            Assert.Throws<InvalidOperationException>(() => state.ParseAndSetTestState(testConfigFile, MockLogger.Object));
        }

        [Fact]
        public void ParseAndSetTestStateThrowsOnTestSuiteDefinitionUserNotDefined()
        {
            var state = new TestState(MockTestConfigParser.Object);
            var testConfigFile = "testPlan.fx.yaml";
            var testPlanDefinition = GenerateTestPlanDefinition();
            testPlanDefinition.TestSuite.Persona = Guid.NewGuid().ToString();

            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>(), MockLogger.Object)).Returns(testPlanDefinition);
            Assert.Throws<InvalidOperationException>(() => state.ParseAndSetTestState(testConfigFile, MockLogger.Object));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void SetEnvironmentThrowsOnNullInput(string environment)
        {
            var state = new TestState(MockTestConfigParser.Object);
            Assert.Throws<ArgumentNullException>(() => state.SetEnvironment(environment, MockLogger.Object));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void SetCloudThrowsOnNullInput(string cloud)
        {
            var state = new TestState(MockTestConfigParser.Object);
            Assert.Throws<ArgumentNullException>(() => state.SetCloud(cloud, MockLogger.Object));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void SetTenantThrowsOnNullInput(string tenant)
        {
            var state = new TestState(MockTestConfigParser.Object);
            Assert.Throws<ArgumentNullException>(() => state.SetTenant(tenant, MockLogger.Object));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void SetOutputDirectoryThrowsOnNullInput(string outputDirectory)
        {
            var state = new TestState(MockTestConfigParser.Object);
            Assert.Throws<ArgumentNullException>(() => state.SetOutputDirectory(outputDirectory, MockLogger.Object));
        }
    }
}
