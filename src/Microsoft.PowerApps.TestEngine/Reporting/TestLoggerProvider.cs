// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.PowerApps.TestEngine.Reporting
{
    /// <summary>
    /// Logger provider for TestLogger
    /// </summary>
    public class TestLoggerProvider : ILoggerProvider
    {
        public static Dictionary<string, TestLogger> TestLoggers { get; set; } = new Dictionary<string, TestLogger>();

        public ILogger CreateLogger(string categoryName)
        {
            var logger = new TestLogger();
            TestLoggers.Add(categoryName, logger);
            return logger;
        }

        public void Dispose()
        {
        }
    }
}
