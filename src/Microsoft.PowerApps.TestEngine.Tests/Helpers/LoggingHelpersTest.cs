// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Helpers;
using Microsoft.PowerApps.TestEngine.Providers;
using Microsoft.PowerApps.TestEngine.System;
using Moq;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.Helpers
{
    public class LoggingHelpersTest
    {
        private Mock<ILogger> MockLogger;
        private Mock<ITestWebProvider> MockTestWebProvider;
        private Mock<ISingleTestInstanceState> MockSingleTestInstanceState;
        private Mock<ITestEngineEvents> MockTestEngineEventHandler;

        public LoggingHelpersTest()
        {
            MockTestWebProvider = new Mock<ITestWebProvider>(MockBehavior.Strict);
            MockLogger = new Mock<ILogger>(MockBehavior.Strict);
            MockSingleTestInstanceState = new Mock<ISingleTestInstanceState>(MockBehavior.Strict);
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            MockTestEngineEventHandler = new Mock<ITestEngineEvents>(MockBehavior.Strict);

            LoggingTestHelper.SetupMock(MockLogger);
        }

        [Fact]
        public async Task DebugInfoNullSessionTest()
        {
            MockTestWebProvider.Setup(x => x.GetDebugInfo()).Returns(Task.FromResult((object)null));
            var loggingHelper = new LoggingHelper(MockTestWebProvider.Object, MockSingleTestInstanceState.Object, MockTestEngineEventHandler.Object);
            loggingHelper.DebugInfo();

            MockTestWebProvider.Verify(x => x.GetDebugInfo(), Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, "------------------------------\n Debug Info \n------------------------------", LogLevel.Information, Times.Never());
        }

        [Fact]
        public async Task DebugInfoWithSessionTest()
        {
            IDictionary<string, object> obj = new ExpandoObject() as IDictionary<string, object>;
            obj["sessionId"] = "somesessionId";

            MockTestWebProvider.Setup(x => x.GetDebugInfo()).Returns(Task.FromResult((object)obj));
            var loggingHelper = new LoggingHelper(MockTestWebProvider.Object, MockSingleTestInstanceState.Object, MockTestEngineEventHandler.Object);
            loggingHelper.DebugInfo();

            MockTestWebProvider.Verify(x => x.GetDebugInfo(), Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, "------------------------------\n Debug Info \n------------------------------", LogLevel.Information, Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, "sessionId:\tsomesessionId", LogLevel.Information, Times.Once());
        }

        [Fact]
        public async Task DebugInfoReturnDetailsTest()
        {
            IDictionary<string, object> obj = new ExpandoObject() as IDictionary<string, object>;
            obj["appId"] = "someAppId";
            obj["appVersion"] = "someAppVersionId";
            obj["environmentId"] = "someEnvironmentId";
            obj["sessionId"] = "someSessionId";

            MockTestWebProvider.Setup(x => x.GetDebugInfo()).Returns(Task.FromResult((object)obj));
            var loggingHelper = new LoggingHelper(MockTestWebProvider.Object, MockSingleTestInstanceState.Object, MockTestEngineEventHandler.Object);
            loggingHelper.DebugInfo();

            MockTestWebProvider.Verify(x => x.GetDebugInfo(), Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, "------------------------------\n Debug Info \n------------------------------", LogLevel.Information, Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, "appId:\tsomeAppId", LogLevel.Information, Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, "appVersion:\tsomeAppVersionId", LogLevel.Information, Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, "environmentId:\tsomeEnvironmentId", LogLevel.Information, Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, "sessionId:\tsomeSessionId", LogLevel.Information, Times.Once());
        }

        [Fact]
        public async Task DebugInfoWithNullValuesTest()
        {
            IDictionary<string, object> obj = new ExpandoObject() as IDictionary<string, object>;
            obj["appId"] = "someAppId";
            obj["appVersion"] = null;
            obj["environmentId"] = null;
            obj["sessionId"] = "someSessionId";

            MockTestWebProvider.Setup(x => x.GetDebugInfo()).Returns(Task.FromResult((object)obj));
            MockTestEngineEventHandler.Setup(x => x.EncounteredException(It.IsAny<Exception>()));
            var loggingHelper = new LoggingHelper(MockTestWebProvider.Object, MockSingleTestInstanceState.Object, MockTestEngineEventHandler.Object);
            loggingHelper.DebugInfo();

            MockTestWebProvider.Verify(x => x.GetDebugInfo(), Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, "------------------------------\n Debug Info \n------------------------------", LogLevel.Information, Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, "appId:\tsomeAppId", LogLevel.Information, Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, "appVersion:\t", LogLevel.Information, Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, "environmentId:\t", LogLevel.Information, Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, "sessionId:\tsomeSessionId", LogLevel.Information, Times.Once());
        }

        [Fact]
        public async Task DebugInfoThrowsTest()
        {
            MockTestWebProvider.Setup(x => x.GetDebugInfo()).Throws(new Exception()); ;
            MockTestEngineEventHandler.Setup(x => x.EncounteredException(It.IsAny<Exception>()));
            var loggingHelper = new LoggingHelper(MockTestWebProvider.Object, MockSingleTestInstanceState.Object, MockTestEngineEventHandler.Object);
            loggingHelper.DebugInfo();

            // Verify UserAppException is passed to TestEngineEventHandler
            MockTestEngineEventHandler.Verify(x => x.EncounteredException(It.IsAny<UserAppException>()), Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, "Issue getting DebugInfo. This can be a result of not being properly logged in.", LogLevel.Debug, Times.Once());
        }
    }
}
