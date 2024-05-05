using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.Playwright;
using Moq;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Providers;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.Extensions.Logging;

namespace testengine.module.browserlocale.tests
{
    public class PauseModuleTests
    {
        private Mock<ITestInfraFunctions> MockTestInfraFunctions;
        private Mock<ITestState> MockTestState;
        private Mock<ITestWebProvider> MockTestWebProvider;
        private Mock<ISingleTestInstanceState> MockSingleTestInstanceState;
        private Mock<IFileSystem> MockFileSystem;
        private Mock<IPage> MockPage;
        private PowerFxConfig TestConfig;
        private NetworkRequestMock TestNetworkRequestMock;
        private Mock<ILogger> MockLogger;

        public PauseModuleTests()
        {
            MockTestInfraFunctions = new Mock<ITestInfraFunctions>(MockBehavior.Strict);
            MockTestState = new Mock<ITestState>(MockBehavior.Strict);
            MockTestWebProvider = new Mock<ITestWebProvider>();
            MockSingleTestInstanceState = new Mock<ISingleTestInstanceState>(MockBehavior.Strict);
            MockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            MockPage = new Mock<IPage>(MockBehavior.Strict);
            TestConfig = new PowerFxConfig();
            TestNetworkRequestMock = new NetworkRequestMock();
            MockLogger = new Mock<ILogger>(MockBehavior.Strict);
        }

        [Fact]
        public void ExtendBrowserContextOptionsLocaleUpdate()
        {
            // Arrange
            var module = new PauseModule();
            var options = new BrowserNewContextOptions();
            var settings = new TestSettings() { };

            // Act
            module.ExtendBrowserContextOptions(options, settings);
        }

        [Fact]
        public void RegisterPowerFxFunction()
        {
            // Arrange
            var module = new PauseModule();

            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);

            MockLogger.Setup(x => x.Log(
               It.IsAny<LogLevel>(),
               It.IsAny<EventId>(),
               It.IsAny<It.IsAnyType>(),
               It.IsAny<Exception>(),
               (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()));


            // Act
            module.RegisterPowerFxFunction(TestConfig, MockTestInfraFunctions.Object, MockTestWebProvider.Object, MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object);

            // Assert
            MockLogger.Verify(l => l.Log(It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(), 
                It.Is<It.IsAnyType>((v, t) => v.ToString() == "Registered Pause()"), 
                It.IsAny<Exception>(), 
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task RegisterNetworkRoute()
        {
            // Arrange
            var module = new PauseModule();

           
            // Act
            await module.RegisterNetworkRoute(MockTestState.Object, MockSingleTestInstanceState.Object, MockFileSystem.Object, MockPage.Object, TestNetworkRequestMock);

            // Assert
        }
    }
}