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
        private readonly LogLevel _engineLoggingLevel;
        public List<string> Logs { get; set; } = new List<string>();
        public List<string> DebugLogs { get; set; } = new List<string>();

        public TestLogger(IFileSystem fileSystem, LogLevel engineLoggingLevel)
        {
            _fileSystem = fileSystem;
            _engineLoggingLevel = engineLoggingLevel;
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

            if (_engineLoggingLevel <= LogLevel.Debug) {
                _fileSystem.WriteTextToFile(Path.Combine(directoryPath, "debugLogs.txt"), DebugLogs.ToArray());
            }
        }

        public void Log<TState>(LogLevel messageLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var logString = $"[{messageLevel}] - [{eventId}]: {formatter(state, exception)}{Environment.NewLine}";

             if(messageLevel >= _engineLoggingLevel){

                if (messageLevel > LogLevel.Debug)
                {
                    Logs.Add(logString);
                }

                DebugLogs.Add(logString);
                Console.Out.WriteLine(logString);
            }
        }
    }
}
