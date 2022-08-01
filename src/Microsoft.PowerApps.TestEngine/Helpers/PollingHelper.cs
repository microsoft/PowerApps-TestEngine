// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
using System;

namespace Microsoft.PowerApps.TestEngine.Helpers
{
    /// <summary>
    /// A helper class to do time checking and throw exception when timeout.
    /// </summary>
    public class PollingHelper
    {
        public static T Poll<T>(T initialValue, Func<T, bool> conditionToCheck, Func<T>? functionToCall, int timeout)
        {
            if (timeout < 0)
            {
                throw new ArgumentOutOfRangeException();
            }

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
                    throw new TimeoutException("Timed operation timed out.");
                }

                Thread.Sleep(500);
            }
            
            return valueToCheck;
        }

        // Poll created for types that cannot have an initial value, because said type cannot be null
        public static void Poll<T>(Func<T, bool> conditionToCheck, Func<T>? functionToCall, int timeout)
        {
            ValidateTimeoutValue(timeout);

            DateTime startTime = DateTime.Now;
            bool conditional = true;

            while (conditional)
            {
                if (functionToCall != null)
                {
                    conditional = conditionToCheck(functionToCall());
                }

                if (IsTimeout(startTime, timeout))
                {
                    throw new TimeoutException("Timed operation timed out.");
                }

                Thread.Sleep(500);
            }
        }

        public static async Task PollAsync<T>(T initialValue, Func<T, bool> conditionToCheck, Func<Task<T>>? functionToCall, int timeout)
        {
            ValidateTimeoutValue(timeout);

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
                    throw new TimeoutException("Timed operation timed out.");
                }

                await Task.Delay(1000);
            }
        }

        public static async Task PollAsync<T>(T initialValue, Func<T, bool> conditionToCheck, Func<T, Task<T>>? functionToCall, int timeout)
        {
            ValidateTimeoutValue(timeout);

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
                    throw new TimeoutException("Timed operation timed out.");
                }

                await Task.Delay(1000);
            }
        }

        private static bool IsTimeout(DateTime startTime, int timeout)
        {
            return (DateTime.Now - startTime) > TimeSpan.FromMilliseconds(timeout);
        }

        private static void ValidateTimeoutValue(int timeout)
        {
            if (timeout < 0)
            {
                throw new ArgumentOutOfRangeException();
            }
        }

    }
}