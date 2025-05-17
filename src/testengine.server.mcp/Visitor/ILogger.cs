// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.PowerApps.TestEngine.MCP.Visitor
{
    /// <summary>
    /// Interface for logging functionality to enable testing and dependency injection.
    /// Provides methods for different log levels.
    /// </summary>
    /// <remarks>
    /// This interface enables replacing the logging functionality for testing purposes
    /// or to provide different log outputs in different environments.
    /// </remarks>
    public interface ILogger
    {
        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">The error message to log</param>
        void LogError(string message);

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The warning message to log</param>
        void LogWarning(string message);

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        /// <param name="message">The informational message to log</param>
        void LogInformation(string message);
    }
}
