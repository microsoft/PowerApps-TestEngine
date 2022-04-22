// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.PowerApps.TestEngine.Reporting
{
    /// <summary>
    /// Logger to capture logs for test reporting
    /// </summary>
    public class TestLogger : ILogger
    {
        public List<string> Logs { get; set; } = new List<string>();
        public List<string> DebugLogs { get; set; } = new List<string>();

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void WriteToLogsToFile(string directoryPath)
        {
            File.WriteAllLines($"{directoryPath}\\logs.txt", Logs.ToArray());
            File.WriteAllLines($"{directoryPath}\\debugLogs.txt", DebugLogs.ToArray());
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Debug)
            {
                DebugLogs.Add($"{formatter(state, exception)}{Environment.NewLine}");
            }
            else
            {
                DebugLogs.Add($"{formatter(state, exception)}{Environment.NewLine}");
                Logs.Add($"{formatter(state, exception)}{Environment.NewLine}");
            }
        }
    }
}
