// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.PowerApps;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerApps.TestEngine.Users;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.Users
{
    public class UserManagerTests
    {
        private Mock<ITestInfraFunctions> MockTestInfraFunctions;
        private Mock<ITestState> MockTestState;
        private Mock<IUrlMapper> MockUrlMapper;
        private Mock<ISingleTestInstanceState> MockSingleTestInstanceState;
        private Mock<IEnvironmentVariable> MockEnvironmentVariable;

        public UserManagerTests()
        {
            MockTestInfraFunctions = new Mock<ITestInfraFunctions>(MockBehavior.Loose);
            MockTestState = new Mock<ITestState>(MockBehavior.Strict);
            MockUrlMapper = new Mock<IUrlMapper>(MockBehavior.Strict);
            MockSingleTestInstanceState = new Mock<ISingleTestInstanceState>(MockBehavior.Strict);
            MockEnvironmentVariable = new Mock<IEnvironmentVariable>(MockBehavior.Strict);
        }

        [Fact]
        public async Task LoginAsUserSuccessTest()
        {
            var testDefinition = new TestDefinition()
            {
                Name = "Test1",
                Description = "First test",
                AppLogicalName = "logicalAppName1",
                Persona = "User1",
                TestSteps = "Assert(1 + 1 = 2, \"1 + 1 should be 2 \")"
            };

            var userConfiguration = new UserConfiguration()
            {
                PersonaName = "User1",
                EmailKey = "user1Email",
                PasswordKey = "user1Password"
            };

            var email = "someone@example.com";
            var password = "myPassword1234";
            var loginUrl = "https://make.powerapps.com";

            MockSingleTestInstanceState.Setup(x => x.GetTestDefinition()).Returns(testDefinition);
            MockTestState.Setup(x => x.GetUserConfiguration(It.IsAny<string>())).Returns(userConfiguration);
            MockEnvironmentVariable.Setup(x => x.GetVariable(userConfiguration.EmailKey)).Returns(email);
            MockEnvironmentVariable.Setup(x => x.GetVariable(userConfiguration.PasswordKey)).Returns(password);
            MockUrlMapper.Setup(x => x.GenerateLoginUrl()).Returns(loginUrl);
            MockTestInfraFunctions.Setup(x => x.GoToUrlAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            MockTestInfraFunctions.Setup(x => x.FillAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            MockTestInfraFunctions.Setup(x => x.ClickAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

            var userManager = new UserManager(MockTestInfraFunctions.Object, MockTestState.Object, MockUrlMapper.Object, MockSingleTestInstanceState.Object, MockEnvironmentVariable.Object);
            await userManager.LoginAsUserAsync();

            MockSingleTestInstanceState.Verify(x => x.GetTestDefinition(), Times.Once());
            MockTestState.Verify(x => x.GetUserConfiguration(userConfiguration.PersonaName), Times.Once());
            MockEnvironmentVariable.Verify(x => x.GetVariable(userConfiguration.EmailKey), Times.Once());
            MockEnvironmentVariable.Verify(x => x.GetVariable(userConfiguration.PasswordKey), Times.Once());
            MockUrlMapper.Verify(x => x.GenerateLoginUrl(), Times.Once());
            MockTestInfraFunctions.Verify(x => x.GoToUrlAsync(loginUrl), Times.Once());
            /* MockTestInfraFunctions.Verify(x => x.FillAsync("[id=\"i0116\"]", email), Times.Once());
            MockTestInfraFunctions.Verify(x => x.ClickAsync("[id=\"idSIButton9\"]"), Times.Exactly(2));
            MockTestInfraFunctions.Verify(x => x.FillAsync("[id=\"i0118\"]", password), Times.Once());
            MockTestInfraFunctions.Verify(x => x.ClickAsync("[id=\"idBtn_Back\"]"), Times.Once());
            */
        }

        [Fact]
        public async Task LoginUserAsyncThrowsOnNullTestDefinitionTest()
        {
            TestDefinition testDefinition = null;

            MockSingleTestInstanceState.Setup(x => x.GetTestDefinition()).Returns(testDefinition);

            var userManager = new UserManager(MockTestInfraFunctions.Object, MockTestState.Object, MockUrlMapper.Object, MockSingleTestInstanceState.Object, MockEnvironmentVariable.Object);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await userManager.LoginAsUserAsync());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task LoginUserAsyncThrowsOnInvalidPersonaTest(string? persona)
        {
            var testDefinition = new TestDefinition()
            {
                Name = "Test1",
                Description = "First test",
                AppLogicalName = "logicalAppName1",
                Persona = persona,
                TestSteps = "Assert(1 + 1 = 2, \"1 + 1 should be 2 \")"
            };

            MockSingleTestInstanceState.Setup(x => x.GetTestDefinition()).Returns(testDefinition);

            var userManager = new UserManager(MockTestInfraFunctions.Object, MockTestState.Object, MockUrlMapper.Object, MockSingleTestInstanceState.Object, MockEnvironmentVariable.Object);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await userManager.LoginAsUserAsync());
        }

        [Fact]
        public async Task LoginUserAsyncThrowsOnNullUserConfigTest()
        {
            var testDefinition = new TestDefinition()
            {
                Name = "Test1",
                Description = "First test",
                AppLogicalName = "logicalAppName1",
                Persona = "User1",
                TestSteps = "Assert(1 + 1 = 2, \"1 + 1 should be 2 \")"
            };

            UserConfiguration userConfiguration = null;

            MockSingleTestInstanceState.Setup(x => x.GetTestDefinition()).Returns(testDefinition);
            MockTestState.Setup(x => x.GetUserConfiguration(It.IsAny<string>())).Returns(userConfiguration);

            var userManager = new UserManager(MockTestInfraFunctions.Object, MockTestState.Object, MockUrlMapper.Object, MockSingleTestInstanceState.Object, MockEnvironmentVariable.Object);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await userManager.LoginAsUserAsync());
        }

        [Theory]
        [InlineData(null, "myPassword1234")]
        [InlineData("", "myPassword1234")]
        [InlineData("user1Email",  null)]
        [InlineData("user1Email", "")]
        public async Task LoginUserAsyncThrowsOnInvalidUserConfigTest(string? emailKey, string? passwordKey)
        {
            var testDefinition = new TestDefinition()
            {
                Name = "Test1",
                Description = "First test",
                AppLogicalName = "logicalAppName1",
                Persona = "User1",
                TestSteps = "Assert(1 + 1 = 2, \"1 + 1 should be 2 \")"
            };

            UserConfiguration userConfiguration = new UserConfiguration()
            {
                PersonaName = "User1",
                EmailKey = emailKey,
                PasswordKey = passwordKey
            };

            MockSingleTestInstanceState.Setup(x => x.GetTestDefinition()).Returns(testDefinition);
            MockTestState.Setup(x => x.GetUserConfiguration(It.IsAny<string>())).Returns(userConfiguration);

            var userManager = new UserManager(MockTestInfraFunctions.Object, MockTestState.Object, MockUrlMapper.Object, MockSingleTestInstanceState.Object, MockEnvironmentVariable.Object);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await userManager.LoginAsUserAsync());
        }

        [Theory]
        [InlineData(null, "user1Password")]
        [InlineData("", "user1Password")]
        [InlineData("someone@example.com", null)]
        [InlineData("someone@example.com", "")]
        public async Task LoginUserAsyncThrowsOnInvalidEnviromentVariablesTest(string? email, string? password)
        {
            var testDefinition = new TestDefinition()
            {
                Name = "Test1",
                Description = "First test",
                AppLogicalName = "logicalAppName1",
                Persona = "User1",
                TestSteps = "Assert(1 + 1 = 2, \"1 + 1 should be 2 \")"
            };

            UserConfiguration userConfiguration = new UserConfiguration()
            {
                PersonaName = "User1",
                EmailKey = "user1Email",
                PasswordKey = "user1Password"
            };

            MockSingleTestInstanceState.Setup(x => x.GetTestDefinition()).Returns(testDefinition);
            MockTestState.Setup(x => x.GetUserConfiguration(It.IsAny<string>())).Returns(userConfiguration);
            MockEnvironmentVariable.Setup(x => x.GetVariable(userConfiguration.EmailKey)).Returns(email);
            MockEnvironmentVariable.Setup(x => x.GetVariable(userConfiguration.PasswordKey)).Returns(password);

            var userManager = new UserManager(MockTestInfraFunctions.Object, MockTestState.Object, MockUrlMapper.Object, MockSingleTestInstanceState.Object, MockEnvironmentVariable.Object);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await userManager.LoginAsUserAsync());
        }
    }
}
