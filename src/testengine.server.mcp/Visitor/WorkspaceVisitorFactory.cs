// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.MCP.Visitor;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerFx;

namespace Microsoft.PowerApps.TestEngine.MCP
{
    /// <summary>
    /// Factory class for creating WorkspaceVisitor instances.
    /// Simplifies the creation of workspace visitors by encapsulating the dependencies.
    /// </summary>
    public class WorkspaceVisitorFactory
    {
        private readonly IFileSystem _fileSystem;
        private readonly Extensions.Logging.ILogger _logger;

        /// <summary>
        /// Creates a new instance of WorkspaceVisitorFactory.
        /// </summary>
        /// <param name="fileSystem">The file system interface to use for file operations</param>
        public WorkspaceVisitorFactory(IFileSystem fileSystem, Extensions.Logging.ILogger logger)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _logger = logger;
        }

        /// <summary>
        /// Creates a new WorkspaceVisitor with default components.
        /// </summary>
        /// <param name="workspacePath">The root workspace path to scan</param>
        /// <param name="scanReference">The scan configuration with rules to apply</param>
        /// <returns>A configured WorkspaceVisitor instance</returns>
        public WorkspaceVisitor Create(string workspacePath, ScanReference scanReference, RecalcEngine recalcEngine)
        {
            // Create a RecalcEngineAdapter using the default engine
            var recalcEngineAdapter = new RecalcEngineAdapter(recalcEngine, _logger);

            // Create a default ConsoleLogger
            var logger = new ConsoleLogger();

            // Create and return the WorkspaceVisitor
            return new WorkspaceVisitor(_fileSystem, workspacePath, scanReference, recalcEngineAdapter, logger);
        }

        /// <summary>
        /// Creates a new WorkspaceVisitor with custom components.
        /// </summary>
        /// <param name="workspacePath">The root workspace path to scan</param>
        /// <param name="scanReference">The scan configuration with rules to apply</param>
        /// <param name="recalcEngine">The PowerFx recalc engine adapter for evaluating expressions</param>
        /// <param name="logger">The logger to use for logging messages</param>
        /// <returns>A configured WorkspaceVisitor instance</returns>
        public WorkspaceVisitor Create(string workspacePath, ScanReference scanReference,
                                      IRecalcEngine recalcEngine, Visitor.ILogger logger)
        {
            return new WorkspaceVisitor(_fileSystem, workspacePath, scanReference, recalcEngine, logger);
        }
    }
}
