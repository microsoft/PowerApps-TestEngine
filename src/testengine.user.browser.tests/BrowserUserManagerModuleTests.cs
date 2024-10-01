using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Moq;
using testengine.user.browser;

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
        [InlineData(false, true, "", true, "", "")]
        [InlineData(true, false, "", true, "", "")]
        [InlineData(true, false, "a.txt", false, "ESTSAUTHPERSISTENT", "")]
        [InlineData(true, false, "a.txt", true, "", "")]
        [InlineData(true, false, "a.txt", true, "", "about:blank")]
        [InlineData(true, false, "a.txt", true, "", "https://localhost")]
        public async Task LoginWithBrowserState(bool exists, bool isDirectoryCreated, string files, bool willPause, string state, string pages)
        {
            // Arrange
            if (willPause)
            {
                MockPage.Setup(x => x.PauseAsync()).Returns(Task.CompletedTask);
            }

            var created = false;

            var userManager = new BrowserUserManagerModule();
            userManager.DirectoryExists = (path) => exists;
            userManager.CreateDirectory = (path) => created = true;
            userManager.GetFiles = (path) => files.Split(',');
            userManager.Page = MockPage.Object;

            MockBrowserState.Setup(x => x.StorageStateAsync(It.IsAny<BrowserContextStorageStateOptions>())).Returns(Task.FromResult(state));

            if (string.IsNullOrEmpty(pages))
            {
                MockBrowserState.Setup(x => x.Pages).Returns(new List<IPage>());
            } else
            {
                var mockPages = new List<IPage>();
                foreach (var page in pages.Split(new[] {','}))
                {

                    var mockPage = new Mock<IPage>(MockBehavior.Strict);
                    mockPage.SetupGet(x => x.Url).Returns(page);

                    if (page == "about:blank")
                    {
                        mockPage.Setup(x => x.CloseAsync(null)).Returns(Task.CompletedTask);
                    }

                    mockPages.Add(mockPage.Object);
                }
                MockBrowserState.Setup(x => x.Pages).Returns(mockPages);
            }

            // Act
            await userManager.LoginAsUserAsync("*",
                MockBrowserState.Object,
                MockTestState.Object,
                MockSingleTestInstanceState.Object,
                MockEnvironmentVariable.Object,
                MockUserManagerLogin.Object);

            // Assert
            Assert.True(created == isDirectoryCreated);
        }
    }
}
