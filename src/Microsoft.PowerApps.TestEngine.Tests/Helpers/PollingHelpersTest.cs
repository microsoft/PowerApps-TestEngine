// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerApps.TestEngine.Helpers;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.Helpers
{
    public class PollingHelpersTests
    {
        private bool testValue = false;
        Func<bool, bool> conditionToCheck = x => !x;
        Func<bool> functionToCall = () => {
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
            Assert.True(PollingHelper.Poll(false, conditionToCheck, functionToCall, true, 2000));
        }

        [Fact]
        public void PollingTimeoutTest()
        {
            Assert.Throws<TimeoutException>(() => PollingHelper.Poll(false, conditionToCheck, functionToCall, true, 1000));
        }

        [Fact]
        public void PollingAsyncSucceedsTest()
        {
            PollingHelper.PollAsync(false, conditionToCheck, () => functionToCallAsync(), true, 2000);
        }

        [Fact]
        public void PollingAsyncTimeoutTest()
        {
            Assert.ThrowsAsync<TimeoutException>(() => PollingHelper.PollAsync(false, conditionToCheck, () => functionToCallAsync(), true, 1000));
        }

        [Fact]
        public void PollingThrowsOnInvalidArgumentsTest()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => PollingHelper.Poll(false, conditionToCheck, functionToCall, true, -1000));
        }

        [Fact]
        public void PollingAsyncThrowsOnInvalidArgumentsTest()
        {
            Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => PollingHelper.PollAsync(false, conditionToCheck, () => functionToCallAsync(), true, -1000));
        }

    }
}