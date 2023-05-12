using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Helpers;
using Microsoft.PowerApps.TestEngine.PowerApps;
using Moq;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.Helpers
{
    public class LoggingHelpersTest
    {
        private Mock<ILogger> MockLogger;
        private Mock<IPowerAppFunctions> MockPowerAppFunctions;
        private Mock<ISingleTestInstanceState> MockSingleTestInstanceState;
        private Mock<ITestEngineEvents> MockTestEngineEventHandler;

        public LoggingHelpersTest()
        {            
            MockPowerAppFunctions = new Mock<IPowerAppFunctions>(MockBehavior.Strict);
            MockLogger = new Mock<ILogger>(MockBehavior.Strict);
            MockSingleTestInstanceState = new Mock<ISingleTestInstanceState>(MockBehavior.Strict);
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object); 
            MockTestEngineEventHandler = new Mock<ITestEngineEvents>(MockBehavior.Strict);

            LoggingTestHelper.SetupMock(MockLogger);
        }

        [Fact]
        public async Task DebugInfoNullSessionTest()
        {
            MockPowerAppFunctions.Setup(x => x.GetDebugInfo()).Returns(Task.FromResult((object)null));
            var loggingHelper = new LoggingHelper(MockPowerAppFunctions.Object, MockSingleTestInstanceState.Object);
            loggingHelper.DebugInfo();

            MockPowerAppFunctions.Verify(x => x.GetDebugInfo(), Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, "------------------------------\n Debug Info \n------------------------------", LogLevel.Information, Times.Never());
        }

        [Fact]
        public async Task DebugInfoWithSessionTest()
        {
            var obj = new ExpandoObject();
            obj.TryAdd("sessionID", "somesessionId");

            MockPowerAppFunctions.Setup(x => x.GetDebugInfo()).Returns(Task.FromResult((object)obj));
            var loggingHelper = new LoggingHelper(MockPowerAppFunctions.Object, MockSingleTestInstanceState.Object);
            loggingHelper.DebugInfo();

            MockPowerAppFunctions.Verify(x => x.GetDebugInfo(), Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, "------------------------------\n Debug Info \n------------------------------", LogLevel.Information, Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, "sessionID:\tsomesessionId", LogLevel.Information, Times.Once());
        }
    }
}
