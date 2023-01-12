using System;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Helpers;
using Microsoft.PowerApps.TestEngine.PowerApps;
using Moq;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.Helpers
{
    public class ExceptionHandlingHelperTest
    {
        private Mock<ILogger> MockLogger;

        public ExceptionHandlingHelperTest()
        {
            MockLogger = new Mock<ILogger>(MockBehavior.Strict);
        }

        [Fact]
        public void CheckIfOutDatedPublishedAppTrue()
        {
            Exception exception= new Exception(ExceptionHandlingHelper.PublishedAppWithoutJSSDKErrorCode);
            LoggingTestHelper.SetupMock(MockLogger);
            ExceptionHandlingHelper.CheckIfOutDatedPublishedApp(exception,MockLogger.Object);

            // Verify the message is logged in this case
            LoggingTestHelper.VerifyLogging(MockLogger, ExceptionHandlingHelper.PublishedAppWithoutJSSDKMessage, LogLevel.Error, Times.Once());
        }
        [Fact]
        public void CheckIfOutDatedPublishedAppFalse()
        {
            Exception exception = new Exception();
            LoggingTestHelper.SetupMock(MockLogger);
            ExceptionHandlingHelper.CheckIfOutDatedPublishedApp(exception, MockLogger.Object);

            // Verify the message is never logged in this case
            LoggingTestHelper.VerifyLogging(MockLogger, ExceptionHandlingHelper.PublishedAppWithoutJSSDKMessage, LogLevel.Error, Times.Never());
        }

    }
}
