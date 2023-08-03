// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Helpers;
using Moq;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.Helpers
{
    public class PollingHelpersTests
    {
        private int _enoughRuntime = 6000;
        private int _notEnoughRuntime = 500;
        private int _invalidRuntime = -1000;
        private Mock<ILogger> MockLogger;

        public PollingHelpersTests()
        {
            MockLogger = new Mock<ILogger>(MockBehavior.Strict);
        }

        private Func<bool, bool> conditionToCheck = x => !x;
        private Func<bool> functionToCall = () =>
        {
            Thread.Sleep(500);
            return true;
        };

        private async Task<bool> functionToCallAsync()
        {
            await Task.Delay(500);
            return true;
        }

        [Fact]
        public void PollingSucceedsTest()
        {
            LoggingTestHelper.SetupMock(MockLogger);
            Assert.True(PollingHelper.Poll(false, conditionToCheck, functionToCall, _enoughRuntime, MockLogger.Object));
        }

        [Fact]
        public void PollingTimeoutTest()
        {
            LoggingTestHelper.SetupMock(MockLogger);
            Assert.Throws<TimeoutException>(() => PollingHelper.Poll(false, conditionToCheck, functionToCall, _notEnoughRuntime, MockLogger.Object));
        }

        [Fact]
        public async Task PollingAsyncSucceedsTest()
        {
            LoggingTestHelper.SetupMock(MockLogger);
            await PollingHelper.PollAsync(false, conditionToCheck, () => functionToCallAsync(), _enoughRuntime, MockLogger.Object);
        }

        [Fact]
        public void PollingAsyncTimeoutTest()
        {
            LoggingTestHelper.SetupMock(MockLogger);
            Assert.ThrowsAsync<TimeoutException>(() => PollingHelper.PollAsync(false, conditionToCheck, () => functionToCallAsync(), _notEnoughRuntime, MockLogger.Object));
        }

        [Fact]
        public void PollingThrowsOnInvalidArgumentsTest()
        {
            LoggingTestHelper.SetupMock(MockLogger);
            Assert.Throws<ArgumentOutOfRangeException>(() => PollingHelper.Poll(false, conditionToCheck, functionToCall, _invalidRuntime, MockLogger.Object));
        }

        [Fact]
        public void PollingAsyncThrowsOnInvalidArgumentsTest()
        {
            LoggingTestHelper.SetupMock(MockLogger);
            Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => PollingHelper.PollAsync(false, conditionToCheck, () => functionToCallAsync(), _invalidRuntime, MockLogger.Object));
        }

    }
}
