// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Reflection;
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
        public List<TestLog> Logs { get; set; } = new List<TestLog>();
        public List<TestLog> DebugLogs { get; set; } = new List<TestLog>();
        private TestLoggerScope currentScope = null;

        public TestLogger(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            if (state is string)
            {
                var stateString = state as string;
                if (string.IsNullOrEmpty(stateString))
                {
                    throw new InvalidOperationException("State cannot be an empty string");
                }

                if (currentScope != null)
                {
                    throw new InvalidOperationException("Scope is already set, only implemented one level of scopes at the moment");
                }

                currentScope = new TestLoggerScope(stateString, () => { currentScope = null; });
                return currentScope;
            }
            else
            {
                throw new InvalidOperationException("We can only accept states of type string");
            }
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void WriteToLogsFile(string directoryPath, string filter)
        {
            if (!_fileSystem.Exists(directoryPath))
            {
                var assemblyLocation = Assembly.GetExecutingAssembly().Location;
                var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
                directoryPath = Path.Combine(assemblyDirectory, "logs");
                _fileSystem.CreateDirectory(directoryPath);
            }

            // If no filter, get all logs
            // else get only the logs specified by the filter
            var filterAction = (TestLog logItem) => string.IsNullOrEmpty(filter) || logItem.ScopeFilter == filter;

            _fileSystem.WriteTextToFile(Path.Combine(directoryPath, "logs.txt"), Logs.Where(filterAction).Select(x => x.LogMessage).ToArray());
            _fileSystem.WriteTextToFile(Path.Combine(directoryPath, "debugLogs.txt"), DebugLogs.Where(filterAction).Select(x => x.LogMessage).ToArray());
        }

        public void WriteExceptionToDebugLogsFile(string directoryPath, string exception)
        {
            if (!_fileSystem.Exists(directoryPath))
            {
                throw new ArgumentException(nameof(directoryPath));
            }

            _fileSystem.WriteTextToFile(Path.Combine(directoryPath, "debugLogs.txt"), exception);
        }

        public void Log<TState>(LogLevel messageLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var logString = "";

            if (messageLevel >= LogLevel.Warning && messageLevel <= LogLevel.Critical)
            {
                if (eventId == 0)
                {
                    logString += $"[{messageLevel}]: ";
                }
                else
                {
                    logString += $"[{messageLevel}] - [{eventId}]: ";
                }
            }

            logString += $"{formatter(state, exception)}{Environment.NewLine}";

            var scopeFilter = currentScope != null ? currentScope.GetScopeString() : "";
            if (messageLevel > LogLevel.Debug)
            {
                Logs.Add(new TestLog() { LogMessage = logString, ScopeFilter = scopeFilter });
            }

            DebugLogs.Add(new TestLog() { LogMessage = logString, ScopeFilter = scopeFilter });
        }
    }
}
