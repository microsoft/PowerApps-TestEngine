// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.System;

namespace Microsoft.PowerApps.TestEngine.Reporting
{
    /// <summary>
    /// Logger provider for TestLogger
    /// </summary>
    public class TestLoggerProvider : ILoggerProvider
    {
        public static Dictionary<string, ITestLogger> TestLoggers { get; set; } = new Dictionary<string, ITestLogger>();
        private readonly IFileSystem _fileSystem;
        public LogLevel _engineLoggingLevel;

        public TestLoggerProvider(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public void SetEngineLoggingLevel(LogLevel engineLoggingLevel)
        {
            _engineLoggingLevel = engineLoggingLevel;
        }

        public ILogger CreateLogger(string categoryName)
        {
            if (TestLoggers.ContainsKey(categoryName))
            {
                return TestLoggers[categoryName];
            }

            var logger = new TestLogger(_fileSystem, _engineLoggingLevel);
            TestLoggers.Add(categoryName, logger);
            return logger;
        }

        public void Dispose()
        {
        }
    }
}
