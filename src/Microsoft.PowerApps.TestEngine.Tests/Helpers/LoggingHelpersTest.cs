using System;
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

        public LoggingHelpersTest()
        {
            MockLogger = new Mock<ILogger>(MockBehavior.Strict);
        }

        [Fact]
        public void DebugInfoNullSessionTest()
        {
            LoggingTestHelper.SetupMock(MockLogger);

            MockPowerAppFunctions.Setup(x => x.GetDebugInfo()).Returns(Task.FromResult((object)null));
            var loggingHelper = new LoggingHelper(MockPowerAppFunctions.Object, MockSingleTestInstanceState.Object);
            loggingHelper.DebugInfo();

            MockPowerAppFunctions.Verify(x => x.GetDebugInfo(), Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, "------------------------------\n Debug Info \n------------------------------", LogLevel.Debug, Times.Never());
        }

        [Fact]
        public void DebugInfoWithSessionTest()
        {
            var obj = new ExpandoObject();
            obj.TryAdd("sessionID", "somesessionId");
            LoggingTestHelper.SetupMock(MockLogger);

            MockPowerAppFunctions.Setup(x => x.GetDebugInfo()).Returns(Task.FromResult((object)obj));
            var loggingHelper = new LoggingHelper(MockPowerAppFunctions.Object, MockSingleTestInstanceState.Object);
            loggingHelper.DebugInfo();

            MockPowerAppFunctions.Verify(x => x.GetDebugInfo(), Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, "------------------------------\n Debug Info \n------------------------------", LogLevel.Debug, Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, "sessionID:\tsomesessionId", LogLevel.Debug, Times.Once());
        }
    }
}
