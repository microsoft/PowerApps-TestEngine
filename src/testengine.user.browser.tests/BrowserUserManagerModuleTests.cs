using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Moq;
using testengine.common.user;

namespace testengine.user.browser.tests
{
    public class BrowserUserManagerModuleTests
    {
        private Mock<IBrowserContext> MockBrowserState;
        private Mock<ITestInfraFunctions> MockTestInfraFunctions;
        private Mock<ITestState> MockTestState;
        private Mock<ISingleTestInstanceState> MockSingleTestInstanceState;
        private Mock<IEnvironmentVariable> MockEnvironmentVariable;
        private Mock<ILogger> MockLogger;
        private Mock<IPage> MockPage;
        private Mock<IElementHandle> MockElementHandle;
        private Mock<IFileSystem> MockFileSystem;
        private Mock<IUserManagerLogin> MockUserManagerLogin;

        public BrowserUserManagerModuleTests()
        {
            MockBrowserState = new Mock<IBrowserContext>(MockBehavior.Strict);
            MockTestInfraFunctions = new Mock<ITestInfraFunctions>(MockBehavior.Strict);
            MockTestState = new Mock<ITestState>(MockBehavior.Strict);
            MockSingleTestInstanceState = new Mock<ISingleTestInstanceState>(MockBehavior.Strict);
            MockEnvironmentVariable = new Mock<IEnvironmentVariable>(MockBehavior.Strict);
            MockLogger = new Mock<ILogger>(MockBehavior.Strict);
            MockPage = new Mock<IPage>(MockBehavior.Strict);
            MockElementHandle = new Mock<IElementHandle>(MockBehavior.Strict);
            MockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            MockUserManagerLogin = new Mock<IUserManagerLogin>(MockBehavior.Strict);
        }

        [Theory]
        [InlineData(false, true, "", "https://localhost")]
        [InlineData(true, false, "", "https://localhost")]
        [InlineData(true, false, "a.txt", "https://localhost")]
        [InlineData(true, false, "a.txt", "about:blank,https://localhost")]
        public async Task LoginWithBrowserState(bool exists, bool isDirectoryCreated, string files, string pages)
        {
            // Arrange
            MockTestState.Setup(x => x.GetTimeout()).Returns(1000);
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);

            MockLogger.Setup(x => x.Log(
               It.IsAny<LogLevel>(),
               It.IsAny<EventId>(),
               It.IsAny<It.IsAnyType>(),
               It.IsAny<Exception>(),
               (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()));

            var created = false;

            var userManager = new BrowserUserManagerModule();
            userManager.DirectoryExists = (path) => exists;
            userManager.CreateDirectory = (path) => created = true;
            userManager.GetFiles = (path) => files.Split(',');
            userManager.Page = MockPage.Object;

            var mockPages = new List<IPage>();
            foreach (var page in pages.Split(new[] { ',' }))
            {

                var mockPage = new Mock<IPage>(MockBehavior.Strict);
                mockPage.SetupGet(x => x.Url).Returns(page);
                mockPage.Setup(x => x.EvaluateAsync<string>(PowerPlatformLogin.DIAGLOG_CHECK_JAVASCRIPT, null)).ReturnsAsync("");
                mockPage.Setup(x => x.EvaluateAsync<string>(PowerPlatformLogin.DEFAULT_OFFICE_365_CHECK, null)).ReturnsAsync("Loaded");

                if (page == "about:blank")
                {
                    mockPage.Setup(x => x.CloseAsync(null)).Returns(Task.CompletedTask);
                }

                mockPages.Add(mockPage.Object);
            }
            MockBrowserState.Setup(x => x.Pages).Returns(mockPages);

            userManager.LoginHelper.LoginIsComplete = (IPage page) => Task.FromResult(true);

            // Act
            await userManager.LoginAsUserAsync("https://localhost",
                MockBrowserState.Object,
                MockTestState.Object,
                MockSingleTestInstanceState.Object,
                MockEnvironmentVariable.Object,
                MockUserManagerLogin.Object);

            // Assert
            Assert.True(created == isDirectoryCreated);
        }

        [Fact]
        public async Task ThrowExceptionIfDesiredUrlNotFound()
        {
            // Arrange
            MockTestState.Setup(x => x.GetTimeout()).Returns(100);
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);

            MockLogger.Setup(x => x.Log(
               It.IsAny<LogLevel>(),
               It.IsAny<EventId>(),
               It.IsAny<It.IsAnyType>(),
               It.IsAny<Exception>(),
               (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()));

            var userManager = new BrowserUserManagerModule();
            userManager.DirectoryExists = (path) => true;
            userManager.CreateDirectory = (path) => { };
            userManager.GetFiles = (path) => new[] { "a.txt " };
            userManager.Page = MockPage.Object;

            var mockPages = new List<IPage>();

            var mockPage = new Mock<IPage>(MockBehavior.Strict);
            mockPage.Setup(x => x.EvaluateAsync<string>(PowerPlatformLogin.DIAGLOG_CHECK_JAVASCRIPT, null)).ReturnsAsync("");
            mockPage.Setup(x => x.EvaluateAsync<string>(PowerPlatformLogin.DEFAULT_OFFICE_365_CHECK, null)).ReturnsAsync("Loaded");
            mockPage.SetupGet(x => x.Url).Returns("about:blank");
            mockPages.Add(mockPage.Object);

            MockBrowserState.Setup(x => x.Pages).Returns(mockPages);

            userManager.LoginHelper.LoginIsComplete = (IPage page) => Task.FromResult(true);

            // Act & Assert
            await Assert.ThrowsAsync<UserInputException>(() => userManager.LoginAsUserAsync(
                        "https://localhost",
                        MockBrowserState.Object,
                        MockTestState.Object,
                        MockSingleTestInstanceState.Object,
                        MockEnvironmentVariable.Object,
                        MockUserManagerLogin.Object));
        }
    }
}
