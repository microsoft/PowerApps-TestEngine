using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.Playwright;
using Moq;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Providers;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.Extensions.Logging;
using Microsoft.PowerFx.Types;
using testengine.module.tests.common;
using System.Text.RegularExpressions;

namespace testengine.module.browserlocale.tests
{
    public class PlaywrightActionFunctionTests
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

        public PlaywrightActionFunctionTests()
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

        private void RunTestScenario(string id)
        {
            switch (id) {
                case "click":
                    MockTestInfraFunctions.Setup(x => x.ClickAsync("//foo")).Returns(Task.CompletedTask);
                    break;
                case "navigate":
                    MockPage.Setup(x => x.GotoAsync("https://make.powerapps.com", null)).Returns(Task.FromResult(new Mock<IResponse>().Object));
                    break;
                case "wait":
                    MockPage.Setup(x => x.WaitForSelectorAsync("//foo", null)).Returns(Task.FromResult<IElementHandle?>(null));
                    break;
                case "exists":
                case "exists-true":
                    var mockLocator = new Mock<ILocator>();
                    MockPage.Setup(x => x.Locator("//foo", null)).Returns(mockLocator.Object);
                    mockLocator.Setup(x => x.CountAsync()).Returns(Task.FromResult(id == "exists" ? 0 : 1));
                    break;
            }
        }

        [Theory]
        [InlineData("//foo", "click", "click", new string[] { "Click item" }, true)]
        [InlineData("https://make.powerapps.com", "navigate", "navigate", new string[] { "Navigate to page" }, true)]
        [InlineData("//foo", "wait", "wait", new string[] { "Wait for locator" }, true)]
        [InlineData("//foo", "exists", "exists", new string[] { "Check if locator exists", "Exists False" }, false)]
        [InlineData("//foo", "exists", "exists-true", new string[] { "Check if locator exists", "Exists True" }, false)]
        public void PlaywrightExecute(string locator, string value, string scenario, string[] messages, bool standardEnd)
        {
            // Arrange

            var function = new PlaywrightActionFunction(MockTestInfraFunctions.Object, MockTestState.Object, MockLogger.Object);
            var mockBrowserContext = new Mock<IBrowserContext>();

            MockLogger.Setup(x => x.Log(
               It.IsAny<LogLevel>(),
               It.IsAny<EventId>(),
               It.IsAny<It.IsAnyType>(),
               It.IsAny<Exception>(),
               (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()));

            MockPage.Setup(x => x.Url).Returns("http://localhost");

            MockTestInfraFunctions.Setup(x => x.GetContext()).Returns(mockBrowserContext.Object);
            mockBrowserContext.Setup(x => x.Pages).Returns(new List<IPage>() { MockPage.Object });

            RunTestScenario(scenario);

            // Act
            function.Execute(StringValue.New(locator),StringValue.New(value));

            // Assert
            MockLogger.VerifyMessage(LogLevel.Information, "------------------------------\n\n" +
                "Executing PlaywrightAction function.");

            foreach ( var message in messages )
            {
                MockLogger.VerifyMessage(LogLevel.Information, message);
            }
            
            if ( standardEnd )
            {
                MockLogger.VerifyMessage(LogLevel.Information, "Successfully finished executing PlaywrightAction function.");
            }
        }

        [Theory]
        [InlineData("about:blank",0, "//foo")]
        [InlineData("about:blank,https://localhost", 1, "//foo")]
        [InlineData("about:blank,https://localhost,https://microsoft.com", 1, "//foo")]
        public void WaitPage(string pages, int waitOnPage, string locator)
        {
            // Arrange

            var function = new PlaywrightActionFunction(MockTestInfraFunctions.Object, MockTestState.Object, MockLogger.Object);
            var mockBrowserContext = new Mock<IBrowserContext>();

            MockLogger.Setup(x => x.Log(
               It.IsAny<LogLevel>(),
               It.IsAny<EventId>(),
               It.IsAny<It.IsAnyType>(),
               It.IsAny<Exception>(),
               (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()));

            MockTestInfraFunctions.Setup(x => x.GetContext()).Returns(mockBrowserContext.Object);

            var mockPages = new List<IPage>();
            int index = 0;
            foreach (var page in pages.Split(','))
            {
                var mockPage = new Mock<IPage>();

                if (index <= waitOnPage)
                {
                    mockPage.Setup(m => m.Url).Returns(page);
                }

                if ( index == waitOnPage )
                {
                    mockPage.Setup(x => x.WaitForSelectorAsync(locator, null)).Returns(Task.FromResult<IElementHandle?>(null));
                }

                mockPages.Add(mockPage.Object);
            }

            mockBrowserContext.Setup(x => x.Pages).Returns(mockPages);

            // Act
            function.Execute(StringValue.New(locator), StringValue.New("wait"));

            // Assert
            Mock.VerifyAll();
        }
    }
}
