using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.Playwright;
using Moq;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.Tests.Helpers;
using Microsoft.Extensions.Logging;
using testengine.user.environment;

namespace testengine.user.environment.tests
{
    public class BrowserUserManagerModuleTests
    {
        private Mock<IBrowserContext> MockBrowserState;
        private Mock<ITestInfraFunctions> MockTestInfraFunctions;
        private Mock<ITestState> MockTestState;
        private Mock<ISingleTestInstanceState> MockSingleTestInstanceState;
        private Mock<IEnvironmentVariable> MockEnvironmentVariable;
        private Mock<ILogger> MockLogger;
        private Mock<IBrowserContext> MockBrowserContext;
        private Mock<IPage> MockPage;
        private Mock<IElementHandle> MockElementHandle;
        private Mock<IFileSystem> MockFileSystem;

        public BrowserUserManagerModuleTests()
        {
            MockBrowserState = new Mock<IBrowserContext>(MockBehavior.Strict);
            MockTestInfraFunctions = new Mock<ITestInfraFunctions>(MockBehavior.Strict);
            MockTestState = new Mock<ITestState>(MockBehavior.Strict);
            MockSingleTestInstanceState = new Mock<ISingleTestInstanceState>(MockBehavior.Strict);
            MockEnvironmentVariable = new Mock<IEnvironmentVariable>(MockBehavior.Strict);
            MockLogger = new Mock<ILogger>(MockBehavior.Strict);
            MockBrowserContext = new Mock<IBrowserContext>(MockBehavior.Strict);
            MockPage = new Mock<IPage>(MockBehavior.Strict);
            MockElementHandle = new Mock<IElementHandle>(MockBehavior.Strict);
            MockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        }

        [Theory]
        [InlineData(false, true, "", true)]
        [InlineData(true, false, "", true)]
        [InlineData(true, false, "a.txt", false)]
        public async Task LoginWithBrowserState(bool exists, bool isDirectoryCreated, string files, bool willPause)
        {
            // Arrange
            if ( willPause ) {
                 MockPage.Setup(x => x.PauseAsync()).Returns(Task.CompletedTask);
            }
            
            var created = false;

            var userManager = new BrowserUserManagerModule();
            userManager.DirectoryExists = (path) => exists;
            userManager.CreateDirectory = (path) => created = true;
            userManager.GetFiles = (path) => files.Split(',');
            userManager.Page = MockPage.Object;

            // Act
            await userManager.LoginAsUserAsync("*",
                MockBrowserState.Object,
                MockTestState.Object,
                MockSingleTestInstanceState.Object,
                MockEnvironmentVariable.Object);

            // Assert
            Assert.True(created == isDirectoryCreated);
        }
    }
}
