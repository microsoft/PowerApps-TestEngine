// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.Extensions.Logging;
using Moq;

namespace Microsoft.PowerApps.TestEngine.Tests.Helpers
{
    public static class LoggingTestHelper
    {
        public static void SetupMock(Mock<ILogger> logger)
        {

            logger.Setup(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)));
        }

        public static void VerifyLogging(Mock<ILogger> logger, string expectedMessage, LogLevel? expectedLogLevel, Times? times = null)
        {
            Func<string, bool> messageValidationFunction = (message) => expectedMessage == null || message.CompareTo(expectedMessage) == 0;
            VerifyLogging(logger, messageValidationFunction, expectedLogLevel, times);
        }

        public static void VerifyLogging(Mock<ILogger> logger, Func<string, bool> messageValidationFunction, LogLevel? expectedLogLevel = null, Times? times = null)
        {
            times ??= Times.Once();

            Func<object, Type, bool> state = (v, t) => messageValidationFunction(v.ToString());

            logger.Verify(
                x => x.Log(
                    It.Is<LogLevel>(l => expectedLogLevel == null || l == expectedLogLevel),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => state(v, t)),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)), (Times)times);
        }
    }
}
