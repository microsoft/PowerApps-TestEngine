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
    public class PlaywrightActionValueFunctionTests
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
        private Mock<ILocator> MockLocator;

        public PlaywrightActionValueFunctionTests()
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
            MockLocator = new Mock<ILocator>();
        }

        private void RunTestScenario(string id)
        {
            switch (id) {
                case "click-in-iframe":
                    var mockFrame = new Mock<IFrame>();
                    MockPage.SetupGet(x => x.Frames).Returns(new List<IFrame>() { mockFrame.Object });
                    mockFrame.Setup(x => x.Locator("//foo", null)).Returns(MockLocator.Object);

                    MockLocator.Setup(x => x.IsVisibleAsync(null)).Returns(Task.FromResult(true));
                    MockLocator.Setup(x => x.ClickAsync(It.IsAny<LocatorClickOptions>())).Returns(Task.CompletedTask);
                    break;
                case "fill-in-iframe":
                    var mockFillFrame = new Mock<IFrame>();
                    MockPage.SetupGet(x => x.Frames).Returns(new List<IFrame>() { mockFillFrame.Object });
                    mockFillFrame.Setup(x => x.Locator("//foo", null)).Returns(MockLocator.Object);

                    MockLocator.Setup(x => x.IsVisibleAsync(null)).Returns(Task.FromResult(true));
                    MockLocator.Setup(x => x.TypeAsync("xyz", It.IsAny<LocatorTypeOptions>())).Returns(Task.CompletedTask);
                    break;
                case "fill":
                    MockTestInfraFunctions.Setup(x => x.FillAsync("//foo", "xyz")).Returns(Task.CompletedTask);
                    break;
                case "screenshot":
                    MockSingleTestInstanceState.Setup(x => x.GetTestResultsDirectory()).Returns(@"c:\");
                    MockFileSystem.Setup(x => x.IsValidFilePath(@"c:\")).Returns(true);
                    MockTestInfraFunctions.Setup(x => x.FillAsync("//foo", "xyz")).Returns(Task.CompletedTask);
                    MockPage.Setup(x => x.Locator("//foo", null)).Returns(MockLocator.Object);
                    MockLocator.Setup(x => x.ScreenshotAsync(It.IsAny<LocatorScreenshotOptions>())).Returns(Task.FromResult(new byte[] { }));
                    break;
            }
        }

        private void RunTestCheckScenario(string id)
        {
            switch (id)
            {
                case "click-in-iframe":
                    MockLocator.Verify(x => x.ClickAsync(It.Is<LocatorClickOptions>(o => o.Delay >= 200)));
                    break;
                case "fill-in-iframe":
                    MockLocator.Verify(x => x.TypeAsync("xyz", It.Is<LocatorTypeOptions>(o => o.Delay >= 100)));
                    break;
            }
        }

        [Theory]
        [InlineData("//foo", "click-in-iframe", "", null, new string[] { }, true)]
        [InlineData("//foo", "fill-in-iframe", "xyz", null, new string[] { }, true)]
        [InlineData("//foo", "fill", "xyz", null, new string[] { }, true)]
        [InlineData("//foo", "screenshot", @"test.jpg", null, new string[] { }, true)]
        public void PlaywrightExecute(string locator, string action, string value, string scenario, string[] messages, bool standardEnd)
        {
            // Arrange

            var function = new PlaywrightActionValueFunction(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object,  MockFileSystem.Object, MockTestState.Object, MockLogger.Object);
            var mockBrowserContext = new Mock<IBrowserContext>();

            MockLogger.Setup(x => x.Log(
               It.IsAny<LogLevel>(),
               It.IsAny<EventId>(),
               It.IsAny<It.IsAnyType>(),
               It.IsAny<Exception>(),
               (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()));


            MockTestInfraFunctions.Setup(x => x.GetContext()).Returns(mockBrowserContext.Object);
            mockBrowserContext.SetupGet(x => x.Pages).Returns(new List<IPage>() { MockPage.Object });

            RunTestScenario(string.IsNullOrEmpty(scenario) ? action : scenario);

            // Act
            function.Execute(StringValue.New(locator),StringValue.New(action), StringValue.New(value));

            // Assert
            MockLogger.VerifyMessage(LogLevel.Information, "------------------------------\n\n" +
                "Executing PlaywrightActionValue function.");

            foreach ( var message in messages )
            {
                MockLogger.VerifyMessage(LogLevel.Information, message);
            }
            
            if ( standardEnd )
            {
                MockLogger.VerifyMessage(LogLevel.Information, "Successfully finished executing PlaywrightActionValue function.");
            }
            
            RunTestCheckScenario(string.IsNullOrEmpty(scenario) ? action : scenario);
        }
    }
}