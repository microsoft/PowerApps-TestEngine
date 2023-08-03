// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.PowerApps.TestEngine.Helpers
{
    /// <summary>
    /// A helper class to do time checking and throw exception when timeout.
    /// </summary>
    public class PollingHelper
    {
        public static T Poll<T>(T value, Func<T, bool> conditionToCheck, Func<T> functionToCall, int timeout, ILogger logger, string errorMessage = "")
        {
            ValidateTimeoutValue(timeout, logger);
            DateTime startTime = DateTime.Now;

            while (conditionToCheck(value))
            {
                if (functionToCall != null)
                {
                    value = functionToCall();
                }

                CheckIfTimedOut(startTime, timeout, logger, errorMessage);
                Thread.Sleep(500);
            }

            return value;
        }

        // Will not print an error message
        public static async Task PollAsync<T>(T value, Func<T, bool> conditionToCheck, Func<Task<T>> functionToCall, int timeout, ILogger logger)
        {
            ValidateTimeoutValue(timeout, logger);
            DateTime startTime = DateTime.Now;

            while (conditionToCheck(value))
            {
                if (functionToCall != null)
                {
                    value = await functionToCall();
                }

                CheckIfTimedOut(startTime, timeout, logger);
                await Task.Delay(1000);
            }
        }

        // Will not print an error message
        public static async Task PollAsync<T>(T value, Func<T, bool> conditionToCheck, Func<T, Task<T>> functionToCall, int timeout, ILogger logger)
        {
            ValidateTimeoutValue(timeout, logger);
            DateTime startTime = DateTime.Now;

            while (conditionToCheck(value))
            {
                if (functionToCall != null)
                {
                    value = await functionToCall(value);
                }

                CheckIfTimedOut(startTime, timeout, logger);
                await Task.Delay(1000);
            }
        }

        public static async Task PollAsync<T>(T value, Func<T, bool> conditionToCheck, Func<Task<T>> functionToCall, int timeout, ILogger logger, string errorMessage = "")
        {
            ValidateTimeoutValue(timeout, logger);
            DateTime startTime = DateTime.Now;

            while (conditionToCheck(value))
            {
                if (functionToCall != null)
                {
                    value = await functionToCall();
                }

                CheckIfTimedOut(startTime, timeout, logger, errorMessage);
                await Task.Delay(1000);
            }
        }

        public static async Task PollAsync<T>(T value, Func<T, bool> conditionToCheck, Func<T, Task<T>> functionToCall, int timeout, ILogger logger, string errorMessage = "")
        {
            ValidateTimeoutValue(timeout, logger);
            DateTime startTime = DateTime.Now;

            while (conditionToCheck(value))
            {
                if (functionToCall != null)
                {
                    value = await functionToCall(value);
                }

                CheckIfTimedOut(startTime, timeout, logger, errorMessage);
                await Task.Delay(1000);
            }
        }

        // Will not give an exception message
        private static void CheckIfTimedOut(DateTime startTime, int timeout, ILogger logger)
        {
            if ((DateTime.Now - startTime) > TimeSpan.FromMilliseconds(timeout))
            {
                logger.LogDebug("Timeout duration was set to " + timeout);

                throw new TimeoutException();
            }
        }

        private static void CheckIfTimedOut(DateTime startTime, int timeout, ILogger logger, string errorMessage = "")
        {
            if ((DateTime.Now - startTime) > TimeSpan.FromMilliseconds(timeout))
            {
                logger.LogDebug("Timeout duration was set to " + timeout);
                logger.LogDebug("Make sure the function & property you're using is supported by TestEngine.");

                if (string.IsNullOrEmpty(errorMessage))
                {
                    logger.LogError("Waiting timed out.");
                    throw new TimeoutException();
                }
                else
                {
                    logger.LogError(errorMessage);
                    throw new TimeoutException(errorMessage);
                }
            }
        }

        private static void ValidateTimeoutValue(int timeout, ILogger logger)
        {
            if (timeout < 0)
            {
                logger.LogError("The timeout TestSetting cannot be less than zero.");
                throw new ArgumentOutOfRangeException();
            }
        }
    }
}
