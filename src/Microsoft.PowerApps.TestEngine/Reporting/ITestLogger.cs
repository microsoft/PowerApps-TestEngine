// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.PowerApps.TestEngine.Reporting
{
    /// <summary>
    /// Logger to capture logs for test reporting
    /// </summary>
    public interface ITestLogger : ILogger
    {
        /// <summary>
        /// Writes all captured logs to file
        /// </summary>
        /// <param name="directoryPath">Directory to place log files in</param>
        /// <param name="filter">Filter for filtering logs</param>
        public void WriteToLogsFile(string directoryPath, string filter);

        /// <summary>
        /// Writes all exception logs to debug file
        /// </summary>
        /// <param name="directoryPath">Directory to place log files in</param>
        /// <param name="exception">Content of the exception</param>
        public void WriteExceptionToDebugLogsFile(string directoryPath, string exception);
    }
}
