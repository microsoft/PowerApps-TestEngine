// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.PowerFx.Functions;
using Microsoft.PowerApps.TestEngine.Tests.Helpers;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerFx.Types;
using Moq;
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
            LoggingTestHelper.VerifyLogging(MockLogger, message, LogLevel.Trace, Times.Once());
        }

        [Fact]
        public void AssertFunctionSucceedsOnFalseTest()
        {
            LoggingTestHelper.SetupMock(MockLogger);
            var assertFunction = new AssertFunction(MockLogger.Object);
            var message = "This should fail";
            Assert.Throws<AssertionFailureException>(() => assertFunction.Execute(BooleanValue.New(false), StringValue.New(message)));
            LoggingTestHelper.VerifyLogging(MockLogger, "Assert failed. Property is not equal to the specified value.", LogLevel.Error, Times.Once());
        }
    }
}
