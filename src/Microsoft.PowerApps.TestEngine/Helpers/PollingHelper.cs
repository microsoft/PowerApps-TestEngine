// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
using Microsoft.Extensions.Logging;
using System;

namespace Microsoft.PowerApps.TestEngine.Helpers
{
    /// <summary>
    /// A helper class to do time checking and throw exception when timeout.
    /// </summary>
    public class PollingHelper
    {
        public static T Poll<T>(T initialValue, Func<T, bool> conditionToCheck, Func<T>? functionToCall, int timeout, ILogger logger)
        {
            ValidateTimeoutValue(timeout, logger);

            var valueToCheck = initialValue;
            DateTime startTime = DateTime.Now;

            while (conditionToCheck(valueToCheck))
            {
                if (functionToCall != null)
                {
                    valueToCheck = functionToCall();
                }

                if (IsTimeout(startTime, timeout))
                {
                    logger.LogError("Waiting timed out.");
                    logger.LogDebug("Timeout duration set to " + timeout);
                    throw new TimeoutException();
                }

                Thread.Sleep(500);
            }
            
            return valueToCheck;
        }

        public static async Task PollAsync<T>(T initialValue, Func<T, bool> conditionToCheck, Func<Task<T>>? functionToCall, int timeout, ILogger logger)
        {
            ValidateTimeoutValue(timeout, logger);

            var valueToCheck = initialValue;
            DateTime startTime = DateTime.Now;

            while (conditionToCheck(valueToCheck))
            {
                if (functionToCall != null)
                {
                    valueToCheck = await functionToCall();
                }

                if (IsTimeout(startTime, timeout))
                {
                    logger.LogError("Waiting timed out.");
                    logger.LogDebug("Timeout duration set to " + timeout);
                    throw new TimeoutException();
                }

                await Task.Delay(1000);
            }
        }

        public static async Task PollAsync<T>(T initialValue, Func<T, bool> conditionToCheck, Func<T, Task<T>>? functionToCall, int timeout, ILogger logger)
        {
            ValidateTimeoutValue(timeout, logger);

            var valueToCheck = initialValue;
            DateTime startTime = DateTime.Now;

            while (conditionToCheck(valueToCheck))
            {
                if (functionToCall != null)
                {
                    valueToCheck = await functionToCall(valueToCheck);
                }

                if (IsTimeout(startTime, timeout))
                {
                    logger.LogError("Waiting timed out.");
                    logger.LogDebug("Timeout duration set to " + timeout);
                    throw new TimeoutException();
                }

                await Task.Delay(1000);
            }
        }

        private static bool IsTimeout(DateTime startTime, int timeout)
        {
            return (DateTime.Now - startTime) > TimeSpan.FromMilliseconds(timeout);
        }

        private static void ValidateTimeoutValue(int timeout, ILogger logger)
        {
            if (timeout < 0)
            {
                logger.LogCritical("The timeout TestSetting cannot be less than zero.");
                throw new ArgumentOutOfRangeException();
            }
        }

    }
}