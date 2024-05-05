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
    public class PauseFunctionTests
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

        public PauseFunctionTests()
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
        public void PauseExecute()
        {
            // Arrange

            var module = new PauseFunction(MockTestInfraFunctions.Object, MockTestState.Object, MockLogger.Object);
            var settings = new TestSettings() { Headless = false };
            var mockContext = new Mock<IBrowserContext>(MockBehavior.Strict);
            var mockPage = new Mock<IPage>(MockBehavior.Strict);

            MockTestState.Setup(x => x.GetTestSettings()).Returns(settings);
            MockTestInfraFunctions.Setup(x => x.GetContext()).Returns(mockContext.Object);
            mockContext.Setup(x => x.Pages).Returns(new List<IPage>() { mockPage.Object });
            mockPage.Setup(x => x.PauseAsync()).Returns(Task.CompletedTask);

            MockLogger.Setup(x => x.Log(
               It.IsAny<LogLevel>(),
               It.IsAny<EventId>(),
               It.IsAny<It.IsAnyType>(),
               It.IsAny<Exception>(),
               (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()));

            // Act
            module.Execute();

            // Assert
            MockLogger.Verify(l => l.Log(It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString() == "Successfully finished executing Pause function."),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.AtLeastOnce);
        }

        [Fact]
        public void SkipExecute()
        {
            // Arrange

            var module = new PauseFunction(MockTestInfraFunctions.Object, MockTestState.Object, MockLogger.Object);
            var settings = new TestSettings() { Headless = true };
            var mockContext = new Mock<IBrowserContext>(MockBehavior.Strict);
            var mockPage = new Mock<IPage>(MockBehavior.Strict);

            MockTestState.Setup(x => x.GetTestSettings()).Returns(settings);
            MockTestInfraFunctions.Setup(x => x.GetContext()).Returns(mockContext.Object);
            mockContext.Setup(x => x.Pages).Returns(new List<IPage>() { mockPage.Object });
            
            MockLogger.Setup(x => x.Log(
               It.IsAny<LogLevel>(),
               It.IsAny<EventId>(),
               It.IsAny<It.IsAnyType>(),
               It.IsAny<Exception>(),
               (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()));

            // Act
            module.Execute();

            // Assert
            MockLogger.Verify(l => l.Log(It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString() == "Skip Pause function as in headless mode."),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.AtLeastOnce);
        }
    }
}