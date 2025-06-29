// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerApps.TestEngine.MCP.Visitor
{
    /// <summary>
    /// Console-based implementation of the ILogger interface.
    /// Writes log messages to the console with appropriate prefixes.
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        /// <summary>
        /// Logs an error message to the console with an "ERROR:" prefix.
        /// </summary>
        /// <param name="message">The error message to log</param>
        public void LogError(string message) => Console.WriteLine($"ERROR: {message}");

        /// <summary>
        /// Logs a warning message to the console with a "WARNING:" prefix.
        /// </summary>
        /// <param name="message">The warning message to log</param>
        public void LogWarning(string message) => Console.WriteLine($"WARNING: {message}");

        /// <summary>
        /// Logs an informational message to the console without a prefix.
        /// </summary>
        /// <param name="message">The informational message to log</param>
        public void LogInformation(string message) => Console.WriteLine(message);
    }
}
