using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Providers;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerApps.TestEngine.Tests.Helpers;
using Microsoft.PowerFx;
using Moq;
using testengine.user.environment;

namespace testengine.user.environment.tests
{
    public class EnvironmentUserManagerModuleTests
    {
        private Mock<IBrowserContext> MockBrowserState;
        private Mock<ITestInfraFunctions> MockTestInfraFunctions;
        private Mock<ITestState> MockTestState;
        private Mock<ISingleTestInstanceState> MockSingleTestInstanceState;
        private Mock<IEnvironmentVariable> MockEnvironmentVariable;
        private TestSuiteDefinition TestSuiteDefinition;
        private Mock<ILogger> MockLogger;
        private Mock<IBrowserContext> MockBrowserContext;
        private Mock<IPage> MockPage;
        private Mock<IElementHandle> MockElementHandle;
        private Mock<IFileSystem> MockFileSystem;
        private Mock<IUserManagerLogin> MockUserManagerLogin;

        public EnvironmentUserManagerModuleTests()
        {
            MockBrowserState = new Mock<IBrowserContext>(MockBehavior.Strict);
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
            MockBrowserContext = new Mock<IBrowserContext>(MockBehavior.Strict);
            MockPage = new Mock<IPage>(MockBehavior.Strict);
            MockElementHandle = new Mock<IElementHandle>(MockBehavior.Strict);
            MockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            MockUserManagerLogin = new Mock<IUserManagerLogin>(MockBehavior.Strict);
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

            MockLogger = new Mock<ILogger>();

            MockSingleTestInstanceState.Setup(x => x.GetTestSuiteDefinition()).Returns(TestSuiteDefinition);
            MockTestState.Setup(x => x.GetUserConfiguration(It.IsAny<string>())).Returns(userConfiguration);
            MockEnvironmentVariable.Setup(x => x.GetVariable(userConfiguration.EmailKey)).Returns(email);
            MockEnvironmentVariable.Setup(x => x.GetVariable(userConfiguration.PasswordKey)).Returns(password);
            MockTestInfraFunctions.Setup(x => x.GoToUrlAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            var keyboard = new Mock<IKeyboard>(MockBehavior.Strict);

            // Email Address
            MockPage.Setup(x => x.Locator(EnvironmentUserManagerModule.EmailSelector, null)).Returns(new Mock<ILocator>().Object);
            MockPage.Setup(x => x.TypeAsync(EnvironmentUserManagerModule.EmailSelector, email, It.IsAny<PageTypeOptions>())).Returns(Task.CompletedTask);
            keyboard.Setup(x => x.PressAsync("Tab", It.IsAny<KeyboardPressOptions>()))
                    .Returns(Task.CompletedTask);
            MockPage.SetupGet(x => x.Keyboard).Returns(keyboard.Object);
            MockPage.Setup(x => x.ClickAsync(EnvironmentUserManagerModule.SubmitButtonSelector, null)).Returns(Task.CompletedTask);

            // Enter Password and keep me signed in
            MockPage.Setup(x => x.Locator(EnvironmentUserManagerModule.PasswordSelector, null)).Returns(new Mock<ILocator>().Object);
            MockPage.Setup(x => x.FillAsync(EnvironmentUserManagerModule.PasswordSelector, password, null)).Returns(Task.CompletedTask);
            MockPage.Setup(x => x.ClickAsync(EnvironmentUserManagerModule.SubmitButtonSelector, null)).Returns(Task.CompletedTask);
            MockPage.Setup(x => x.ClickAsync(EnvironmentUserManagerModule.StaySignedInSelector, null)).Returns(Task.CompletedTask);
            MockPage.Setup(x => x.ClickAsync(EnvironmentUserManagerModule.KeepMeSignedInNoSelector, null)).Returns(Task.CompletedTask);

            // Now wait for the requested URL assuming login now complete
            MockPage.Setup(x => x.WaitForURLAsync("*", null)).Returns(Task.CompletedTask);

            var userManager = new EnvironmentUserManagerModule();
            userManager.Page = MockPage.Object;

            await userManager.LoginAsUserAsync("*",
                MockBrowserState.Object,
                MockTestState.Object,
                MockSingleTestInstanceState.Object,
                MockEnvironmentVariable.Object,
                MockUserManagerLogin.Object);

            MockSingleTestInstanceState.Verify(x => x.GetTestSuiteDefinition(), Times.Once());
            MockTestState.Verify(x => x.GetUserConfiguration(userConfiguration.PersonaName), Times.Once());
            MockEnvironmentVariable.Verify(x => x.GetVariable(userConfiguration.EmailKey), Times.Once());
            MockEnvironmentVariable.Verify(x => x.GetVariable(userConfiguration.PasswordKey), Times.Once());
        }

        [Fact]
        public async Task LoginUserAsyncThrowsOnNullTestDefinitionTest()
        {
            TestSuiteDefinition testSuiteDefinition = null;

            MockSingleTestInstanceState.Setup(x => x.GetTestSuiteDefinition()).Returns(testSuiteDefinition);
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);

            var userManager = new EnvironmentUserManagerModule();
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await userManager.LoginAsUserAsync("*",
                MockBrowserState.Object,
                MockTestState.Object,
                MockSingleTestInstanceState.Object,
                MockEnvironmentVariable.Object,
                MockUserManagerLogin.Object));
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

            var userManager = new EnvironmentUserManagerModule();
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await userManager.LoginAsUserAsync("*",
                MockBrowserState.Object,
                MockTestState.Object,
                MockSingleTestInstanceState.Object,
                MockEnvironmentVariable.Object,
                MockUserManagerLogin.Object));
        }

        [Fact]
        public async Task LoginUserAsyncThrowsOnNullUserConfigTest()
        {
            UserConfiguration userConfiguration = null;

            MockSingleTestInstanceState.Setup(x => x.GetTestSuiteDefinition()).Returns(TestSuiteDefinition);
            MockTestState.Setup(x => x.GetUserConfiguration(It.IsAny<string>())).Returns(userConfiguration);
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);

            var userManager = new EnvironmentUserManagerModule();
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await userManager.LoginAsUserAsync("*",
                MockBrowserState.Object,
                MockTestState.Object,
                MockSingleTestInstanceState.Object,
                MockEnvironmentVariable.Object,
                MockUserManagerLogin.Object));
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

            var userManager = new EnvironmentUserManagerModule();
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await userManager.LoginAsUserAsync("*",
                MockBrowserState.Object,
                MockTestState.Object,
                MockSingleTestInstanceState.Object,
                MockEnvironmentVariable.Object,
                MockUserManagerLogin.Object));
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
            MockBrowserContext.SetupGet(x => x.Pages).Returns(new List<IPage> { MockPage.Object });

            var userManager = new EnvironmentUserManagerModule();
            userManager.Page = MockPage.Object;

            var ex = await Assert.ThrowsAsync<UserInputException>(async () => await userManager.LoginAsUserAsync("*",
                MockBrowserState.Object,
                MockTestState.Object,
                MockSingleTestInstanceState.Object,
                MockEnvironmentVariable.Object,
                MockUserManagerLogin.Object));

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

        [Fact]
        public async Task HandleUserPasswordScreen()
        {
            string testSelector = "input:has-text('Password')";
            string testTextEntry = "*****";
            string desiredUrl = "https://make.powerapps.com";

            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);

            var mockLocator = new Mock<ILocator>(MockBehavior.Strict);
            MockPage.Setup(x => x.Locator(It.IsAny<string>(), null)).Returns(mockLocator.Object);
            mockLocator.Setup(x => x.WaitForAsync(null)).Returns(Task.CompletedTask);

            MockPage.Setup(x => x.FillAsync(testSelector, testTextEntry, null)).Returns(Task.CompletedTask);
            MockPage.Setup(x => x.ClickAsync("input[type=\"submit\"]", null)).Returns(Task.CompletedTask);
            // Assume ask already logged in
            MockPage.Setup(x => x.WaitForSelectorAsync("[id=\"KmsiCheckboxField\"]", It.IsAny<PageWaitForSelectorOptions>())).Returns(Task.FromResult(MockElementHandle.Object));
            // Simulate Click to stay signed in 
            MockPage.Setup(x => x.ClickAsync("[id=\"idBtn_Back\"]", null)).Returns(Task.CompletedTask);
            // Wait until login is complete and redirect to desired page
            MockPage.Setup(x => x.WaitForURLAsync(desiredUrl, null)).Returns(Task.CompletedTask);

            MockBrowserContext.SetupGet(x => x.Pages).Returns(new List<IPage> { MockPage.Object });

            var userManagerModule = new EnvironmentUserManagerModule();
            userManagerModule.Page = MockPage.Object;

            await userManagerModule.HandleUserPasswordScreen(testSelector, testTextEntry, desiredUrl, MockLogger.Object);

            MockPage.Verify(x => x.Locator(It.Is<string>(v => v.Equals(testSelector)), null));
            MockPage.Verify(x => x.WaitForSelectorAsync("[id=\"KmsiCheckboxField\"]", It.Is<PageWaitForSelectorOptions>(v => v.Timeout >= 8000)));
        }

        [Fact]
        public async Task HandleUserPasswordScreenErrorEntry()
        {
            string testSelector = "input:has-text('Password')";
            string testTextEntry = "*****";
            string desiredUrl = "https://make.powerapps.com";

            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);

            var mockLocator = new Mock<ILocator>(MockBehavior.Strict);
            MockPage.Setup(x => x.Locator(It.IsAny<string>(), null)).Returns(mockLocator.Object);
            mockLocator.Setup(x => x.WaitForAsync(null)).Returns(Task.CompletedTask);

            MockPage.Setup(x => x.FillAsync(testSelector, testTextEntry, null)).Returns(Task.CompletedTask);
            MockPage.Setup(x => x.ClickAsync("input[type=\"submit\"]", null)).Returns(Task.CompletedTask);
            // Not ask to sign in as selector not found
            MockPage.Setup(x => x.WaitForSelectorAsync("[id=\"KmsiCheckboxField\"]", It.IsAny<PageWaitForSelectorOptions>())).Throws(new TimeoutException());
            // Simulate error response for password error
            MockPage.Setup(x => x.WaitForSelectorAsync("[id=\"passwordError\"]", It.IsAny<PageWaitForSelectorOptions>())).Returns(Task.FromResult(MockElementHandle.Object));
            // Throw exception as not make it to desired url
            MockPage.Setup(x => x.WaitForURLAsync(desiredUrl, null)).Throws(new TimeoutException());

            MockBrowserContext.SetupGet(x => x.Pages).Returns(new List<IPage> { MockPage.Object });

            var userManagerModule = new EnvironmentUserManagerModule();
            userManagerModule.Page = MockPage.Object;

            // scenario where password error or missing
            var ex = await Assert.ThrowsAsync<UserInputException>(async () => await userManagerModule.HandleUserPasswordScreen(testSelector, testTextEntry, desiredUrl, MockLogger.Object));

            MockPage.Verify(x => x.Locator(It.Is<string>(v => v.Equals(testSelector)), null));
            MockPage.Verify(x => x.WaitForSelectorAsync("[id=\"passwordError\"]", It.Is<PageWaitForSelectorOptions>(v => v.Timeout >= 2000)));
            Assert.Equal(UserInputException.ErrorMapping.UserInputExceptionLoginCredential.ToString(), ex.Message);
        }

        [Fact]
        public async Task HandleUserPasswordScreenUnknownError()
        {
            string testSelector = "input:has-text('Password')";
            string testTextEntry = "*****";
            string desiredUrl = "https://make.powerapps.com";

            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);
            var mockLocator = new Mock<ILocator>(MockBehavior.Strict);
            MockPage.Setup(x => x.Locator(It.IsAny<string>(), null)).Returns(mockLocator.Object);
            mockLocator.Setup(x => x.WaitForAsync(null)).Returns(Task.CompletedTask);

            MockPage.Setup(x => x.FillAsync(testSelector, testTextEntry, null)).Returns(Task.CompletedTask);
            MockPage.Setup(x => x.ClickAsync("input[type=\"submit\"]", null)).Returns(Task.CompletedTask);
            // Not ask to sign in as selector not found
            MockPage.Setup(x => x.WaitForSelectorAsync("[id=\"KmsiCheckboxField\"]", null)).Throws(new TimeoutException());
            // Also not able to find password error, must be some other error
            MockPage.Setup(x => x.WaitForSelectorAsync("[id=\"passwordError\"]", It.IsAny<PageWaitForSelectorOptions>())).Throws(new TimeoutException());
            // Throw exception as not make it to desired url
            MockPage.Setup(x => x.WaitForURLAsync(desiredUrl, null)).Throws(new TimeoutException());

            MockBrowserContext.SetupGet(x => x.Pages).Returns(new List<IPage> { MockPage.Object });

            var environmentUserManager = new EnvironmentUserManagerModule();
            environmentUserManager.Page = MockPage.Object;

            await Assert.ThrowsAsync<TimeoutException>(async () => await environmentUserManager.HandleUserPasswordScreen(testSelector, testTextEntry, desiredUrl, MockLogger.Object));

            MockPage.Verify(x => x.Locator(It.Is<string>(v => v.Equals(testSelector)), null));
        }
    }
}
