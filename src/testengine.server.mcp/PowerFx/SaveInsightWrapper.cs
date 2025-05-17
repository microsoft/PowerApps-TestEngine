// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerApps.TestEngine.MCP.PowerFx
{    /// <summary>
    /// Wrapper for SaveInsight functionality to provide a consistent interface
    /// similar to AddFact function, while enabling persistent storage of insights.
    /// </summary>
    public class SaveInsightWrapper : ReflectionFunction
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;
        private readonly string _workspacePath;
        private readonly ScanStateManager.SaveInsightFunction _saveInsightFunction;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="SaveInsightWrapper"/> class.
        /// </summary>
        /// <param name="fileSystem">The file system service.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="workspacePath">The workspace path where insights will be saved.</param>
        public SaveInsightWrapper(IFileSystem fileSystem, ILogger logger, string workspacePath) 
            : base(DPath.Root, "SaveInsight", FormulaType.Boolean, RecordType.Empty())
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _workspacePath = workspacePath ?? throw new ArgumentNullException(nameof(workspacePath));
            
            // Create the underlying SaveInsightFunction
            _saveInsightFunction = new ScanStateManager.SaveInsightFunction(fileSystem, logger, workspacePath);
        }

        /// <summary>
        /// Executes the SaveInsight function to save an insight to disk
        /// </summary>
        /// <param name="insight">The record containing insight information.</param>
        /// <returns>Boolean value indicating success or failure.</returns>
        public BooleanValue Execute(RecordValue insight)
        {
            try
            {
                // Use the ScanStateManager implementation
                return _saveInsightFunction.Execute(insight);
            }            catch (Exception ex)
            {
                _logger.LogError($"Error in SaveInsight: {ex.Message}");
                return FormulaValue.New(false);
            }
        }

        /// <summary>
        /// Immediately flushes all cached insights to disk
        /// </summary>
        /// <param name="appPath">Path to the app being analyzed</param>
        /// <returns>Boolean value indicating success or failure.</returns>
        public BooleanValue Flush(string appPath)
        {
            try
            {
                // Create and execute the FlushInsightsFunction
                var flushFunction = new ScanStateManager.FlushInsightsFunction(_fileSystem, _logger, _workspacePath);
                var flushParams = RecordValue.NewRecordFromFields(
                    new NamedValue("AppPath", FormulaValue.New(appPath))
                );
                
                return flushFunction.Execute(flushParams);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error flushing insights: {ex.Message}");
                return FormulaValue.New(false);
            }
        }
        
        /// <summary>
        /// Helper method to generate a UI map from collected insights
        /// </summary>
        /// <param name="appPath">Path to the app being analyzed</param>
        /// <returns>Boolean value indicating success or failure.</returns>
        public BooleanValue GenerateUIMap(string appPath)
        {
            try
            {
                // Create and execute the GenerateUIMapFunction
                var uiMapFunction = new ScanStateManager.GenerateUIMapFunction(_fileSystem, _logger, _workspacePath);
                var uiMapParams = RecordValue.NewRecordFromFields(
                    new NamedValue("AppPath", FormulaValue.New(appPath))
                );
                
                return uiMapFunction.Execute(uiMapParams);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating UI map: {ex.Message}");
                return FormulaValue.New(false);
            }
        }
    }
}
