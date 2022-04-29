// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.System;

namespace Microsoft.PowerApps.TestEngine.Reporting
{
    /// <summary>
    /// Logger to capture logs for test reporting
    /// </summary>
    public class TestLogger : ITestLogger
    {
        private readonly IFileSystem _fileSystem;
        public List<string> Logs { get; set; } = new List<string>();
        public List<string> DebugLogs { get; set; } = new List<string>();

        public TestLogger(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void WriteToLogsFile(string directoryPath)
        {
            if (!_fileSystem.IsValidFilePath(directoryPath))
            {
                throw new ArgumentException(nameof(directoryPath));
            }

            _fileSystem.WriteTextToFile(Path.Combine(directoryPath, "logs.txt"), Logs.ToArray());
            _fileSystem.WriteTextToFile(Path.Combine(directoryPath, "debugLogs.txt"), DebugLogs.ToArray());
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var logString = $"[{logLevel}] - [{eventId}]: {formatter(state, exception)}{Environment.NewLine}";
            if (logLevel <= LogLevel.Debug)
            {
                DebugLogs.Add(logString);
            }
            else
            {
                DebugLogs.Add(logString);
                Logs.Add(logString);
            }
        }
    }
}
