// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.PowerFx.Functions;
using Microsoft.PowerApps.TestEngine.Tests.Helpers;
using Microsoft.PowerFx.Core.Public.Values;
using Moq;
using System;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.PowerFx.Functions
{
    public class AssertFunctionTests
    {
        private Mock<ILogger> MockLogger;

        public AssertFunctionTests()
        {
            MockLogger = new Mock<ILogger>(MockBehavior.Strict);
        }

        [Fact]
        public void AssertFunctionSucceedsOnTrueTest()
        {
            LoggingTestHelper.SetupMock(MockLogger);
            var assertFunction = new AssertFunction(MockLogger.Object);
            var message = "This should succeed";
            var result = assertFunction.Execute(BooleanValue.New(true), StringValue.New(message));
            Assert.IsType<BlankValue>(result);
            LoggingTestHelper.VerifyLogging(MockLogger, message, LogLevel.Information, Times.Once());
        }

        [Fact]
        public void AssertFunctionSucceedsOnFalseTest()
        {
            LoggingTestHelper.SetupMock(MockLogger);
            var assertFunction = new AssertFunction(MockLogger.Object);
            var message = "This should fail";
            Assert.Throws<InvalidOperationException>(() => assertFunction.Execute(BooleanValue.New(false), StringValue.New(message)));
            LoggingTestHelper.VerifyLogging(MockLogger, $"Assert failed: {message}", LogLevel.Error, Times.Once());
        }
    }
}
