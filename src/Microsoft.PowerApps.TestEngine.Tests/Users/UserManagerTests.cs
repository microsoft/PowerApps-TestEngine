// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.PowerApps;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerApps.TestEngine.Tests.Helpers;
using Microsoft.PowerApps.TestEngine.Users;
using Moq;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.Users
{
    public class UserManagerTests
    {
        private Mock<ITestInfraFunctions> MockTestInfraFunctions;
        private Mock<ITestState> MockTestState;
        private Mock<ISingleTestInstanceState> MockSingleTestInstanceState;
        private Mock<IEnvironmentVariable> MockEnvironmentVariable;
        private TestSuiteDefinition TestSuiteDefinition;
        private Mock<ILogger> MockLogger;

        public UserManagerTests()
        {
            MockTestInfraFunctions = new Mock<ITestInfraFunctions>(MockBehavior.Strict);
            MockTestState = new Mock<ITestState>(MockBehavior.Strict);
            MockSingleTestInstanceState = new Mock<ISingleTestInstanceState>(MockBehavior.Strict);
            MockEnvironmentVariable = new Mock<IEnvironmentVariable>(MockBehavior.Strict);
            TestSuiteDefinition = new TestSuiteDefinition()
            {
                TestSuiteName = "Test1",
                TestSuiteDescription = "First test",
                AppLogicalName = "logicalAppName1",
                Persona = "User1",
                TestCases = new List<TestCase>()
                {
                    new TestCase
                    {
                        TestCaseName = "Test Case Name",
                        TestCaseDescription = "Test Case Description",
                        TestSteps = "Assert(1 + 1 = 2, \"1 + 1 should be 2 \")"
                    }
                }
            };
            MockLogger = new Mock<ILogger>(MockBehavior.Strict);
        }

        [Fact]
        public async Task LoginAsUserSuccessTest()
        {
            var userConfiguration = new UserConfiguration()
            {
                PersonaName = "User1",
                EmailKey = "user1Email",
                PasswordKey = "user1Password"
            };

            var email = "someone@example.com";
            var password = "myPassword1234";

            MockSingleTestInstanceState.Setup(x => x.GetTestSuiteDefinition()).Returns(TestSuiteDefinition);
            MockTestState.Setup(x => x.GetUserConfiguration(It.IsAny<string>())).Returns(userConfiguration);
            MockEnvironmentVariable.Setup(x => x.GetVariable(userConfiguration.EmailKey)).Returns(email);
            MockEnvironmentVariable.Setup(x => x.GetVariable(userConfiguration.PasswordKey)).Returns(password);
            MockTestInfraFunctions.Setup(x => x.GoToUrlAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            MockTestInfraFunctions.Setup(x => x.HandleUserEmailScreen(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            MockTestInfraFunctions.Setup(x => x.HandleUserPasswordScreen(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            MockTestInfraFunctions.Setup(x => x.ClickAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);

            var userManager = new UserManager(MockTestInfraFunctions.Object, MockTestState.Object, MockSingleTestInstanceState.Object, MockEnvironmentVariable.Object);
            await userManager.LoginAsUserAsync("*");

            MockSingleTestInstanceState.Verify(x => x.GetTestSuiteDefinition(), Times.Once());
            MockTestState.Verify(x => x.GetUserConfiguration(userConfiguration.PersonaName), Times.Once());
            MockEnvironmentVariable.Verify(x => x.GetVariable(userConfiguration.EmailKey), Times.Once());
            MockEnvironmentVariable.Verify(x => x.GetVariable(userConfiguration.PasswordKey), Times.Once());
            MockTestInfraFunctions.Verify(x => x.HandleUserEmailScreen("input[type=\"email\"]", email), Times.Once());
            MockTestInfraFunctions.Verify(x => x.ClickAsync("input[type=\"submit\"]"), Times.Once());
            MockTestInfraFunctions.Verify(x => x.HandleUserPasswordScreen("input[type=\"password\"]", password, "*"), Times.Once());
        }

        [Fact]
        public async Task LoginUserAsyncThrowsOnNullTestDefinitionTest()
        {
            TestSuiteDefinition testSuiteDefinition = null;

            MockSingleTestInstanceState.Setup(x => x.GetTestSuiteDefinition()).Returns(testSuiteDefinition);
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);

            var userManager = new UserManager(MockTestInfraFunctions.Object, MockTestState.Object, MockSingleTestInstanceState.Object, MockEnvironmentVariable.Object);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await userManager.LoginAsUserAsync("*"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task LoginUserAsyncThrowsOnInvalidPersonaTest(string persona)
        {
            var testSuiteDefinition = new TestSuiteDefinition()
            {
                TestSuiteName = "Test1",
                TestSuiteDescription = "First test",
                AppLogicalName = "logicalAppName1",
                Persona = persona,
                TestCases = new List<TestCase>()
                {
                    new TestCase
                    {
                        TestCaseName = "Test Case Name",
                        TestCaseDescription = "Test Case Description",
                        TestSteps = "Assert(1 + 1 = 2, \"1 + 1 should be 2 \")"
                    }
                }
            };

            MockSingleTestInstanceState.Setup(x => x.GetTestSuiteDefinition()).Returns(testSuiteDefinition);
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);

            var userManager = new UserManager(MockTestInfraFunctions.Object, MockTestState.Object, MockSingleTestInstanceState.Object, MockEnvironmentVariable.Object);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await userManager.LoginAsUserAsync("*"));
        }

        [Fact]
        public async Task LoginUserAsyncThrowsOnNullUserConfigTest()
        {
            UserConfiguration userConfiguration = null;

            MockSingleTestInstanceState.Setup(x => x.GetTestSuiteDefinition()).Returns(TestSuiteDefinition);
            MockTestState.Setup(x => x.GetUserConfiguration(It.IsAny<string>())).Returns(userConfiguration);
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);

            var userManager = new UserManager(MockTestInfraFunctions.Object, MockTestState.Object, MockSingleTestInstanceState.Object, MockEnvironmentVariable.Object);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await userManager.LoginAsUserAsync("*"));
        }

        [Theory]
        [InlineData(null, "myPassword1234")]
        [InlineData("", "myPassword1234")]
        [InlineData("user1Email", null)]
        [InlineData("user1Email", "")]
        public async Task LoginUserAsyncThrowsOnInvalidUserConfigTest(string emailKey, string passwordKey)
        {
            UserConfiguration userConfiguration = new UserConfiguration()
            {
                PersonaName = "User1",
                EmailKey = emailKey,
                PasswordKey = passwordKey
            };

            MockSingleTestInstanceState.Setup(x => x.GetTestSuiteDefinition()).Returns(TestSuiteDefinition);
            MockTestState.Setup(x => x.GetUserConfiguration(It.IsAny<string>())).Returns(userConfiguration);
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);

            var userManager = new UserManager(MockTestInfraFunctions.Object, MockTestState.Object, MockSingleTestInstanceState.Object, MockEnvironmentVariable.Object);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await userManager.LoginAsUserAsync("*"));
        }

        [Theory]
        [InlineData(null, "user1Password")]
        [InlineData("", "user1Password")]
        [InlineData("someone@example.com", null)]
        [InlineData("someone@example.com", "")]
        public async Task LoginUserAsyncThrowsOnInvalidEnviromentVariablesTest(string email, string password)
        {
            UserConfiguration userConfiguration = new UserConfiguration()
            {
                PersonaName = "User1",
                EmailKey = "user1Email",
                PasswordKey = "user1Password"
            };

            MockSingleTestInstanceState.Setup(x => x.GetTestSuiteDefinition()).Returns(TestSuiteDefinition);
            MockTestState.Setup(x => x.GetUserConfiguration(It.IsAny<string>())).Returns(userConfiguration);
            MockEnvironmentVariable.Setup(x => x.GetVariable(userConfiguration.EmailKey)).Returns(email);
            MockEnvironmentVariable.Setup(x => x.GetVariable(userConfiguration.PasswordKey)).Returns(password);
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);

            var userManager = new UserManager(MockTestInfraFunctions.Object, MockTestState.Object, MockSingleTestInstanceState.Object, MockEnvironmentVariable.Object);
            
            var ex = await Assert.ThrowsAsync<UserInputException>(async () => await userManager.LoginAsUserAsync("*"));
            Assert.Equal(UserInputException.ErrorMapping.UserInputExceptionLoginCredential.ToString(), ex.Message);
            if (String.IsNullOrEmpty(email))
            {
                LoggingTestHelper.VerifyLogging(MockLogger, "User email cannot be null. Please check if the environment variable is set properly.", LogLevel.Error, Times.Once());
            }
            if (String.IsNullOrEmpty(password))
            {
                LoggingTestHelper.VerifyLogging(MockLogger, "Password cannot be null. Please check if the environment variable is set properly.", LogLevel.Error, Times.Once());
            }
        }
    }
}
