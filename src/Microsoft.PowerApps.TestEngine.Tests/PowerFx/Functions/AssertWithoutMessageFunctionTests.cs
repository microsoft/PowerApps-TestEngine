// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.PowerFx.Functions;
using Microsoft.PowerApps.TestEngine.Tests.Helpers;
using Microsoft.PowerFx.Types;
using Moq;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.PowerFx.Functions
{
    public class AssertWithoutMessageFunctionTests
    {
        private Mock<ILogger> MockLogger;

        public AssertWithoutMessageFunctionTests()
        {
            MockLogger = new Mock<ILogger>(MockBehavior.Strict);
        }

        [Fact]
        public void AssertFunctionSucceedsOnTrueTest()
        {
            LoggingTestHelper.SetupMock(MockLogger);
            var assertWithoutMessageFunction = new AssertWithoutMessageFunction(MockLogger.Object);
            var result = assertWithoutMessageFunction.Execute(BooleanValue.New(true));
            Assert.IsType<BlankValue>(result);
            LoggingTestHelper.VerifyLogging(MockLogger, "Successfully finished executing Assert function.", LogLevel.Information, Times.Once());
        }

        [Fact]
        public void AssertFunctionSucceedsOnFalseTest()
        {
            LoggingTestHelper.SetupMock(MockLogger);
            var assertWithoutMessageFunction = new AssertWithoutMessageFunction(MockLogger.Object);
            Assert.Throws<InvalidOperationException>(() => assertWithoutMessageFunction.Execute(BooleanValue.New(false)));
            LoggingTestHelper.VerifyLogging(MockLogger, $"Assert failed. Property is not equal to the specified value.", LogLevel.Error, Times.Once());
        }
    }
}
