using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerApps.TestEngine.Tests.Helpers;
using Moq;
using Xunit;

namespace testengine.user.storagestate.tests
{
    public class StorageStateUserManagerModuleTests
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

        public StorageStateUserManagerModuleTests()
        {
            MockBrowserState = new Mock<IBrowserContext>(MockBehavior.Strict);
            MockTestInfraFunctions = new Mock<ITestInfraFunctions>(MockBehavior.Strict);
            MockTestState = new Mock<ITestState>(MockBehavior.Strict);
            MockSingleTestInstanceState = new Mock<ISingleTestInstanceState>(MockBehavior.Strict);
            MockEnvironmentVariable = new Mock<IEnvironmentVariable>(MockBehavior.Strict);
            TestSuiteDefinition = new TestSuiteDefinition();
            MockLogger = new Mock<ILogger>(MockBehavior.Strict);
            MockBrowserContext = new Mock<IBrowserContext>(MockBehavior.Strict);
            MockPage = new Mock<IPage>(MockBehavior.Strict);
            MockElementHandle = new Mock<IElementHandle>(MockBehavior.Strict);
            MockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            MockUserManagerLogin = new Mock<IUserManagerLogin>(MockBehavior.Strict);
        }

        [Theory]
        [InlineData(null, "")]
        [InlineData("", "")]
        [InlineData("user1Email", "")]
        [InlineData("user1Email", null)]
        public async Task LoginUserAsyncThrowsOnInvalidUserConfigTest(string? emailKey, string? emailValue)
        {
            UserConfiguration userConfiguration = new UserConfiguration()
            {
                PersonaName = "User1",
                EmailKey = emailKey
            };

            MockSingleTestInstanceState.Setup(x => x.GetTestSuiteDefinition()).Returns(TestSuiteDefinition);
            MockTestState.Setup(x => x.GetUserConfiguration(It.IsAny<string>())).Returns(userConfiguration);
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            MockEnvironmentVariable.Setup(x => x.GetVariable(emailKey)).Returns(emailValue);
            LoggingTestHelper.SetupMock(MockLogger);

            var userManager = new StorageStateUserManagerModule();
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await userManager.LoginAsUserAsync("*",
                MockBrowserState.Object,
                MockTestState.Object,
                MockSingleTestInstanceState.Object,
                MockEnvironmentVariable.Object,
                MockUserManagerLogin.Object));
        }

        [Theory]
        [InlineData(null, "", false, "", "")]
        [InlineData("", "", false, "", "")]
        [InlineData("user1Email", "", false, "", "")]
        [InlineData("user1Email", null, false, "", "")]
        [InlineData("user1Email", "user1", false, "", "")]
        [InlineData("user1Email", "user1@example.com", false, "", "")]
        [InlineData("user1Email", "user1@example.com", true, "TEXT", "TEXT")]
        public async Task LoadState(string? emailKey, string? emailValue, bool exists, string content, string expectedState)
        {
            // Arrange
            UserConfiguration userConfiguration = new UserConfiguration()
            {
                PersonaName = "User1",
                EmailKey = emailKey
            };

            MockSingleTestInstanceState.Setup(x => x.GetTestSuiteDefinition()).Returns(TestSuiteDefinition);
            MockTestState.Setup(x => x.GetUserConfiguration(It.IsAny<string>())).Returns(userConfiguration);
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            MockEnvironmentVariable.Setup(x => x.GetVariable(emailKey)).Returns(emailValue);
            LoggingTestHelper.SetupMock(MockLogger);

            MockFileSystem.Setup(x => x.Exists(".storage-state-user1")).Returns(exists);
            if (content != null)
            {
                MockFileSystem.Setup(x => x.ReadAllText(Path.Combine(".storage-state-user1", "state.json"))).Returns(content);
            }

            var userManager = new StorageStateUserManagerModule();

            // Act
            var loadState = userManager.Settings["LoadState"] as Func<IEnvironmentVariable, ISingleTestInstanceState, ITestState, IFileSystem, string>;
            var state = loadState.DynamicInvoke(MockEnvironmentVariable.Object, MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object);

            // Assert
            Assert.Equal(expectedState, state);
        }

        [Theory]
        [InlineData("user1Email", "user1@example.com")]
        public async Task ValidLogin(string emailKey, string emailValue)
        {
            // Arrange
            var userManager = new StorageStateUserManagerModule();

            UserConfiguration userConfiguration = new UserConfiguration()
            {
                PersonaName = "User1",
                EmailKey = emailKey
            };

            userManager.Settings.Add("FileSystem", MockFileSystem.Object);

            MockSingleTestInstanceState.Setup(x => x.GetTestSuiteDefinition()).Returns(TestSuiteDefinition);
            MockTestState.Setup(x => x.GetUserConfiguration(It.IsAny<string>())).Returns(userConfiguration);
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            MockEnvironmentVariable.Setup(x => x.GetVariable(emailKey)).Returns(emailValue);
            MockFileSystem.Setup(x => x.Exists(".storage-state-user1")).Returns(true);
            MockBrowserContext.Setup(x => x.Pages).Returns(new List<IPage>() { MockPage.Object });
            MockPage.SetupGet(x => x.Url).Returns("https://example.com");
            MockPage.Setup(x => x.EvaluateAsync<string>(It.IsAny<string>(), null)).Returns(Task.FromResult("Idle"));
            LoggingTestHelper.SetupMock(MockLogger);
            MockTestState.Setup(x => x.GetTimeout()).Returns(0);
            MockBrowserContext.Setup(x => x.StorageStateAsync(It.IsAny<BrowserContextStorageStateOptions>())).Returns(Task.FromResult(""));

            // Act
            await userManager.LoginAsUserAsync("https://example.com",
                MockBrowserContext.Object,
                MockTestState.Object,
                MockSingleTestInstanceState.Object,
                MockEnvironmentVariable.Object,
                MockUserManagerLogin.Object);

            // Assert
        }
    }
}
