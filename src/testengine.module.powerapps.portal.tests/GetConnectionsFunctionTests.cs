using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Providers;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Moq;

namespace testengine.module.powerappsportal.tests
{
    public class PowerAppsPortalFunctionTests
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
        private Mock<IBrowserContext> MockBrowserContext;

        public PowerAppsPortalFunctionTests()
        {
            MockTestInfraFunctions = new Mock<ITestInfraFunctions>(MockBehavior.Strict);
            MockTestState = new Mock<ITestState>(MockBehavior.Strict);
            MockTestWebProvider = new Mock<ITestWebProvider>();
            MockSingleTestInstanceState = new Mock<ISingleTestInstanceState>(MockBehavior.Strict);
            MockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            MockPage = new Mock<IPage>(MockBehavior.Strict);
            TestConfig = new PowerFxConfig();
            TestNetworkRequestMock = new NetworkRequestMock();
            MockLogger = new Mock<ILogger>();
            MockBrowserContext = new Mock<IBrowserContext>(MockBehavior.Strict);
        }

        [Theory]
        [InlineData("[]", "http://make.powerapps.com", 0)]
        [InlineData("[]", "http://make.powerapps.com/", 0)]
        [InlineData("[{\"Name\":\"Test\",\"Id\":\"\",\"Status\":\"\"}]", "http://make.powerapps.com/", 1)]
        public void ExecuteGetConnections(string json, string baseDomain, int expectedCount)
        {
            // Arrange
            MockTestInfraFunctions.Setup(x => x.GetContext()).Returns(MockBrowserContext.Object);
            MockBrowserContext.Setup(x => x.NewPageAsync()).Returns(Task.FromResult(MockPage.Object));
            MockTestState.Setup(x => x.GetDomain()).Returns(baseDomain);

            // Goto and return json
            MockPage.Setup(x => x.GotoAsync(new Uri(new Uri(baseDomain), "connections?source=testengine").ToString(), It.IsAny<PageGotoOptions>())).Returns(Task.FromResult(new Mock<IResponse>().Object));
            MockPage.Setup(x => x.AddScriptTagAsync(It.IsAny<PageAddScriptTagOptions>())).Returns(Task.FromResult(new Mock<IElementHandle>().Object));
            MockPage.Setup(x => x.EvaluateAsync<string>(It.IsAny<string>(), null)).Returns(Task.FromResult(json));
            MockPage.Setup(x => x.CloseAsync(null)).Returns(Task.CompletedTask);
            
            // Wait until the container exists
            var mockLocator = new Mock<ILocator>();
            MockPage.Setup(x => x.Locator(".connections-list-container", null)).Returns(mockLocator.Object);
            mockLocator.Setup(x => x.WaitForAsync(null)).Returns(Task.FromResult(Task.CompletedTask));

            var function = new GetConnectionsFunction(MockTestInfraFunctions.Object, MockTestState.Object, MockLogger.Object);

            // Act
            var result = function.Execute();

            // Assert
            Assert.Equal(expectedCount, result.Count());
        }
        
    }
}
