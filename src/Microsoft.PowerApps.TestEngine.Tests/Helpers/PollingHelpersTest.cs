// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerApps.TestEngine.Helpers;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.Logging;
using Moq;

namespace Microsoft.PowerApps.TestEngine.Tests.Helpers
{
    public class PollingHelpersTests
    {
        private int _enoughRuntime = 5000;
        private int _notEnoughRuntime = 1000;
        private int _invalidRuntime = -1000;
        private Mock<ILogger> MockLogger;

        public PollingHelpersTests()
        {
            MockLogger = new Mock<ILogger>(MockBehavior.Loose);
        }

        private Func<bool, bool> conditionToCheck = x => !x;
        private Func<bool> functionToCall = () => {
            Thread.Sleep(1000);
            return true;
        };

        private async Task<bool> functionToCallAsync()
        {
            await Task.Delay(1000);
            return true;
        }

        [Fact]
        public void PollingSucceedsTest()
        {
            Assert.True(PollingHelper.Poll(false, conditionToCheck, functionToCall, _enoughRuntime, MockLogger.Object));
        }

        [Fact]
        public void PollingTimeoutTest()
        {
            Assert.Throws<TimeoutException>(() => PollingHelper.Poll(false, conditionToCheck, functionToCall, _notEnoughRuntime, MockLogger.Object));
        }

        [Fact]
        public async void PollingAsyncSucceedsTest()
        {
            await PollingHelper.PollAsync(false, conditionToCheck, () => functionToCallAsync(), _enoughRuntime, MockLogger.Object);
        }

        [Fact]
        public void PollingAsyncTimeoutTest()
        {
            Assert.ThrowsAsync<TimeoutException>(() => PollingHelper.PollAsync(false, conditionToCheck, () => functionToCallAsync(), _notEnoughRuntime, MockLogger.Object));
        }

        [Fact]
        public void PollingThrowsOnInvalidArgumentsTest()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => PollingHelper.Poll(false, conditionToCheck, functionToCall, _invalidRuntime, MockLogger.Object));
        }

        [Fact]
        public void PollingAsyncThrowsOnInvalidArgumentsTest()
        {
            Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => PollingHelper.PollAsync(false, conditionToCheck, () => functionToCallAsync(), _invalidRuntime, MockLogger.Object));
        }

    }
}