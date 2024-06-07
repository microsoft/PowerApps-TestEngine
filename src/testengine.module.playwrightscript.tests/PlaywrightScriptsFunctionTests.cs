using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.Playwright;
using Moq;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Providers;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.Extensions.Logging;
using Microsoft.PowerFx.Types;

namespace testengine.module.browserlocale.tests
{
    public class PlaywrightScriptsFunctionTests
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

        public PlaywrightScriptsFunctionTests()
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

        [Theory]
        [InlineData(@"c:\test.csx", @"#r ""Microsoft.Playwright.dll""
#r ""Microsoft.Extensions.Logging.dll""
using Microsoft.Playwright;
using Microsoft.Extensions.Logging;

public class PlaywrightScript
{
    public static void Run(IBrowserContext context, ILogger logger)
    {
    }
}")]
        public void PlaywrightExecute(string file, string code)
        {
            // Arrange

            var function = new PlaywrightScriptFunction(MockTestInfraFunctions.Object, MockTestState.Object, MockFileSystem.Object, MockLogger.Object);

            MockLogger.Setup(x => x.Log(
               It.IsAny<LogLevel>(),
               It.IsAny<EventId>(),
               It.IsAny<It.IsAnyType>(),
               It.IsAny<Exception>(),
               (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()));

            MockFileSystem.Setup(x => x.IsValidFilePath(file)).Returns(true);
            MockFileSystem.Setup(x => x.ReadAllText(file)).Returns(code);

            MockTestInfraFunctions.Setup(x => x.GetContext()).Returns(new Mock<IBrowserContext>().Object);

            // Act
            function.Execute(StringValue.New(file));

            // Assert
            MockLogVerify(LogLevel.Information, "------------------------------\n\n" +
                "Executing PlaywrightScript function.");

            MockLogVerify(LogLevel.Debug, "Loading file");

            MockLogVerify(LogLevel.Information, "Successfully finished executing PlaywrightScript function.");
        }

        private void MockLogVerify(LogLevel logLevel, string message)
        {
            MockLogger.Verify(l => l.Log(It.Is<LogLevel>(l => l == logLevel),
               It.IsAny<EventId>(),
               It.Is<It.IsAnyType>((v, t) => v.ToString() == message),
               It.IsAny<Exception>(),
               It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.AtLeastOnce);
        }
    }
}