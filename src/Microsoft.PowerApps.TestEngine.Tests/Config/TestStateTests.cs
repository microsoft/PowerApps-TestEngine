// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Castle.Core.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Moq;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.Config
{
    public class TestStateTests
    {
        private Mock<ITestConfigParser> MockTestConfigParser;
        private List<TestCase> TestCases = new List<TestCase>();

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

            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>())).Returns(testPlanDefinition);
            state.ParseAndSetTestState(testConfigFile);

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

            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>())).Returns(testPlanDefinition);
            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestSettings>(It.IsAny<string>())).Returns(testSettings);
            MockTestConfigParser.Setup(x => x.ParseTestConfig<EnvironmentVariables>(It.IsAny<string>())).Returns(environmentVariables);
            state.ParseAndSetTestState(testConfigFile);

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
            Assert.Throws<ArgumentNullException>(() => state.ParseAndSetTestState(testConfigPath));
        }

        [Fact]
        public void ParseAndSetTestStateThrowsOnNoTestSuiteDefinition()
        {
            var state = new TestState(MockTestConfigParser.Object);
            var testConfigFile = "testPlan.fx.yaml";
            var testPlanDefinition = GenerateTestPlanDefinition();
            testPlanDefinition.TestSuite = null;

            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>())).Returns(testPlanDefinition);
            Assert.Throws<InvalidOperationException>(() => state.ParseAndSetTestState(testConfigFile));
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

            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>())).Returns(testPlanDefinition);
            Assert.Throws<InvalidOperationException>(() => state.ParseAndSetTestState(testConfigFile));
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

            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>())).Returns(testPlanDefinition);
            Assert.Throws<InvalidOperationException>(() => state.ParseAndSetTestState(testConfigFile));
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

            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>())).Returns(testPlanDefinition);
            Assert.Throws<InvalidOperationException>(() => state.ParseAndSetTestState(testConfigFile));
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

            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>())).Returns(testPlanDefinition);
            state.ParseAndSetTestState(testConfigFile);
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

            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>())).Returns(testPlanDefinition);
            Assert.Throws<InvalidOperationException>(() => state.ParseAndSetTestState(testConfigFile));
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

            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>())).Returns(testPlanDefinition);
            Assert.Throws<InvalidOperationException>(() => state.ParseAndSetTestState(testConfigFile));
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

            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>())).Returns(testPlanDefinition);
            Assert.Throws<InvalidOperationException>(() => state.ParseAndSetTestState(testConfigFile));
        }

        [Fact]
        public void ParseAndSetTestStateThrowsOnNoTestSettings()
        {
            var state = new TestState(MockTestConfigParser.Object);
            var testConfigFile = "testPlan.fx.yaml";
            var testPlanDefinition = GenerateTestPlanDefinition();
            testPlanDefinition.TestSettings = null;

            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>())).Returns(testPlanDefinition);
            Assert.Throws<InvalidOperationException>(() => state.ParseAndSetTestState(testConfigFile));
        }

        [Fact]
        public void ParseAndSetTestStateThrowsOnNullBrowserConfigurationInTestSettings()
        {
            var state = new TestState(MockTestConfigParser.Object);
            var testConfigFile = "testPlan.fx.yaml";
            var testPlanDefinition = GenerateTestPlanDefinition();
            testPlanDefinition.TestSettings.BrowserConfigurations = null;

            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>())).Returns(testPlanDefinition);
            Assert.Throws<InvalidOperationException>(() => state.ParseAndSetTestState(testConfigFile));
        }

        [Fact]
        public void ParseAndSetTestStateThrowsOnNoBrowserConfigurationInTestSettings()
        {
            var state = new TestState(MockTestConfigParser.Object);
            var testConfigFile = "testPlan.fx.yaml";
            var testPlanDefinition = GenerateTestPlanDefinition();
            testPlanDefinition.TestSettings.BrowserConfigurations = new List<BrowserConfiguration>();

            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>())).Returns(testPlanDefinition);
            Assert.Throws<InvalidOperationException>(() => state.ParseAndSetTestState(testConfigFile));
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

            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>())).Returns(testPlanDefinition);
            Assert.Throws<InvalidOperationException>(() => state.ParseAndSetTestState(testConfigFile));
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

            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>())).Returns(testPlanDefinition);
            Assert.Throws<InvalidOperationException>(() => state.ParseAndSetTestState(testConfigFile));
        }

        [Fact]
        public void ParseAndSetTestStateThrowsOnNoEnvironmentVariables()
        {
            var state = new TestState(MockTestConfigParser.Object);
            var testConfigFile = "testPlan.fx.yaml";
            var testPlanDefinition = GenerateTestPlanDefinition();
            testPlanDefinition.EnvironmentVariables = null;

            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>())).Returns(testPlanDefinition);
            Assert.Throws<InvalidOperationException>(() => state.ParseAndSetTestState(testConfigFile));
        }

        [Fact]
        public void ParseAndSetTestStateThrowsOnNullUserConfigurationInEnvironmentVariables()
        {
            var state = new TestState(MockTestConfigParser.Object);
            var testConfigFile = "testPlan.fx.yaml";
            var testPlanDefinition = GenerateTestPlanDefinition();
            testPlanDefinition.EnvironmentVariables.Users = null;

            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>())).Returns(testPlanDefinition);
            Assert.Throws<InvalidOperationException>(() => state.ParseAndSetTestState(testConfigFile));
        }

        [Fact]
        public void ParseAndSetTestStateThrowsOnNoUserConfigurationInEnvironmentVariables()
        {
            var state = new TestState(MockTestConfigParser.Object);
            var testConfigFile = "testPlan.fx.yaml";
            var testPlanDefinition = GenerateTestPlanDefinition();
            testPlanDefinition.EnvironmentVariables.Users = new List<UserConfiguration>();

            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>())).Returns(testPlanDefinition);
            Assert.Throws<InvalidOperationException>(() => state.ParseAndSetTestState(testConfigFile));
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

            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>())).Returns(testPlanDefinition);
            Assert.Throws<InvalidOperationException>(() => state.ParseAndSetTestState(testConfigFile));
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

            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>())).Returns(testPlanDefinition);
            Assert.Throws<InvalidOperationException>(() => state.ParseAndSetTestState(testConfigFile));
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

            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>())).Returns(testPlanDefinition);
            Assert.Throws<InvalidOperationException>(() => state.ParseAndSetTestState(testConfigFile));
        }

        [Fact]
        public void ParseAndSetTestStateThrowsOnTestSuiteDefinitionUserNotDefined()
        {
            var state = new TestState(MockTestConfigParser.Object);
            var testConfigFile = "testPlan.fx.yaml";
            var testPlanDefinition = GenerateTestPlanDefinition();
            testPlanDefinition.TestSuite.Persona = Guid.NewGuid().ToString();

            MockTestConfigParser.Setup(x => x.ParseTestConfig<TestPlanDefinition>(It.IsAny<string>())).Returns(testPlanDefinition);
            Assert.Throws<InvalidOperationException>(() => state.ParseAndSetTestState(testConfigFile));
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
