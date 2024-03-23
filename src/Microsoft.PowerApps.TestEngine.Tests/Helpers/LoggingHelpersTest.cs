using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Helpers;
using Microsoft.PowerApps.TestEngine.PowerApps;
using Microsoft.PowerApps.TestEngine.System;
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
            var loggingHelper = new LoggingHelper(MockPowerAppFunctions.Object, MockSingleTestInstanceState.Object, MockTestEngineEventHandler.Object);
            loggingHelper.DebugInfo();

            MockPowerAppFunctions.Verify(x => x.GetDebugInfo(), Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, "------------------------------\n Debug Info \n------------------------------", LogLevel.Information, Times.Never());
        }

        [Fact]
        public async Task DebugInfoWithSessionTest()
        {
            var obj = new ExpandoObject();
            obj.TryAdd("sessionId", "somesessionId");

            MockPowerAppFunctions.Setup(x => x.GetDebugInfo()).Returns(Task.FromResult((object)obj));
            var loggingHelper = new LoggingHelper(MockPowerAppFunctions.Object, MockSingleTestInstanceState.Object, MockTestEngineEventHandler.Object);
            loggingHelper.DebugInfo();

            MockPowerAppFunctions.Verify(x => x.GetDebugInfo(), Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, "------------------------------\n Debug Info \n------------------------------", LogLevel.Information, Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, "sessionId:\tsomesessionId", LogLevel.Information, Times.Once());
        }

        [Fact]
        public async Task DebugInfoReturnDetailsTest()
        {
            var obj = new ExpandoObject();
            obj.TryAdd("appId", "someAppId");
            obj.TryAdd("appVersion", "someAppVersionId");
            obj.TryAdd("environmentId", "someEnvironmentId");
            obj.TryAdd("sessionId", "someSessionId");

            MockPowerAppFunctions.Setup(x => x.GetDebugInfo()).Returns(Task.FromResult((object)obj));
            var loggingHelper = new LoggingHelper(MockPowerAppFunctions.Object, MockSingleTestInstanceState.Object, MockTestEngineEventHandler.Object);
            loggingHelper.DebugInfo();

            MockPowerAppFunctions.Verify(x => x.GetDebugInfo(), Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, "------------------------------\n Debug Info \n------------------------------", LogLevel.Information, Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, "appId:\tsomeAppId", LogLevel.Information, Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, "appVersion:\tsomeAppVersionId", LogLevel.Information, Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, "environmentId:\tsomeEnvironmentId", LogLevel.Information, Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, "sessionId:\tsomeSessionId", LogLevel.Information, Times.Once());
        }

        [Fact]
        public async Task DebugInfoWithNullValuesTest()
        {
            var obj = new ExpandoObject();
            obj.TryAdd("appId", "someAppId");
            obj.TryAdd("appVersion", null);
            obj.TryAdd("environmentId", null);
            obj.TryAdd("sessionId", "someSessionId");

            MockPowerAppFunctions.Setup(x => x.GetDebugInfo()).Returns(Task.FromResult((object)obj));
            MockTestEngineEventHandler.Setup(x => x.EncounteredException(It.IsAny<Exception>()));
            var loggingHelper = new LoggingHelper(MockPowerAppFunctions.Object, MockSingleTestInstanceState.Object, MockTestEngineEventHandler.Object);
            loggingHelper.DebugInfo();

            MockPowerAppFunctions.Verify(x => x.GetDebugInfo(), Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, "------------------------------\n Debug Info \n------------------------------", LogLevel.Information, Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, "appId:\tsomeAppId", LogLevel.Information, Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, "appVersion:\t", LogLevel.Information, Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, "environmentId:\t", LogLevel.Information, Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, "sessionId:\tsomeSessionId", LogLevel.Information, Times.Once());
        }

        [Fact]
        public async Task DebugInfoThrowsTest()
        {
            MockPowerAppFunctions.Setup(x => x.GetDebugInfo()).Throws(new Exception()); ;
            MockTestEngineEventHandler.Setup(x => x.EncounteredException(It.IsAny<Exception>()));
            var loggingHelper = new LoggingHelper(MockPowerAppFunctions.Object, MockSingleTestInstanceState.Object, MockTestEngineEventHandler.Object);
            loggingHelper.DebugInfo();

            // Verify UserAppException is passed to TestEngineEventHandler
            MockTestEngineEventHandler.Verify(x => x.EncounteredException(It.IsAny<UserAppException>()), Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, "Issue getting DebugInfo. This can be a result of not being properly logged in.", LogLevel.Debug, Times.Once());
        }
    }
}
