using Microsoft.Extensions.Logging;
using Moq;

namespace testengine.module.tests.common
{
    public static class MockLoggerHelper
    {
        public static void VerifyMessage(this Mock<ILogger> logger, LogLevel logLevel, string message)
        {
            logger.Verify(l => l.Log(It.Is<LogLevel>(l => l == logLevel),
               It.IsAny<EventId>(),
               It.Is<It.IsAnyType>((v, t) => v.ToString() == message),
               It.IsAny<Exception>(),
               It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.AtLeastOnce);
        }
    }
}