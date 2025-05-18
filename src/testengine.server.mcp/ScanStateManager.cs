// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerApps.TestEngine.MCP
{
    /// <summary>
    /// State manager for collecting app facts and generating test recommendations
    /// </summary>
    public static class ScanStateManager
    {
        private static readonly Dictionary<string, Dictionary<string, object>> _stateCache = new Dictionary<string, Dictionary<string, object>>();

        /// <summary>
        /// Collects individual app facts during scanning
        /// </summary>
        public class SaveFactFunction : ReflectionFunction
        {
            private const string FunctionName = "SaveFact";
            private readonly IFileSystem _fileSystem;
            private readonly ILogger _logger;
            private readonly string _workspacePath;

            public SaveFactFunction(IFileSystem fileSystem, ILogger logger, string workspacePath)
                : base(DPath.Root, FunctionName, RecordType.Empty(), BooleanType.Boolean)
            {
                _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
                _workspacePath = workspacePath ?? throw new ArgumentNullException(nameof(workspacePath));
            }

            public BooleanValue Execute(RecordValue factRecord)
            {
                try
                {
                    var categoryValue = factRecord.GetField("Category");
                    var keyValue = factRecord.GetField("Key");
                    var appPathValue = factRecord.GetField("AppPath");
                    var valueValue = factRecord.GetField("Value");
                    
                    if (categoryValue is StringValue stringCategoryValue &&
                        keyValue is StringValue stringKeyValue &&
                        appPathValue is StringValue stringAppPathValue)
                    {
                        string category = stringCategoryValue.Value;
                        string key = stringKeyValue.Value;
                        string appPath = stringAppPathValue.Value;

                        // Use app path as part of state key to separate different apps
                        string stateKey = $"{Path.GetFileName(appPath)}_{category}";

                        // Initialize state dictionary if it doesn't exist
                        if (!_stateCache.TryGetValue(stateKey, out Dictionary<string, object> state))
                        {
                            state = new Dictionary<string, object>();
                            _stateCache[stateKey] = state;
                        }

                        // Convert FormulaValue to C# object
                        object value = ConvertFormulaValueToObject(valueValue);

                        // Store in cache
                        state[key] = value;

                        return BooleanValue.New(true);
                    }

                    return BooleanValue.New(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error saving fact: {ex.Message}");
                    return BooleanValue.New(false);
                }
            }

            private object ConvertFormulaValueToObject(FormulaValue value)
            {
                switch (value)
                {
                    case StringValue stringValue:
                        return stringValue.Value;
                    case NumberValue numberValue:
                        return numberValue.Value;
                    case BooleanValue booleanValue:
                        return booleanValue.Value;
                    case RecordValue recordValue:
                        var record = new Dictionary<string, object>();
                        foreach (var field in recordValue.Fields)
                        {
                            record[field.Name] = ConvertFormulaValueToObject(field.Value);
                        }
                        return record;
                    case TableValue tableValue:
                        var list = new List<object>();
                        foreach (var row in tableValue.Rows)
                        {
                            list.Add(ConvertFormulaValueToObject(row.Value));
                        }
                        return list;
                    default:
                        return value.ToObject();
                }
            }
        }

        /// <summary>
        /// Exports collected facts with recommendations to a single file
        /// </summary>
        public class ExportFactsFunction : ReflectionFunction
        {
            private const string FunctionName = "ExportFacts";
            private readonly IFileSystem _fileSystem;
            private readonly ILogger _logger;
            private readonly string _workspacePath;

            public ExportFactsFunction(IFileSystem fileSystem, ILogger logger, string workspacePath)
                : base(DPath.Root, FunctionName, RecordType.Empty(), BooleanType.Boolean)
            {
                _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
                _workspacePath = workspacePath ?? throw new ArgumentNullException(nameof(workspacePath));
            }

            public BooleanValue Execute(RecordValue parameters)
            {
                try
                {
                    var appPathValue = parameters.GetField("AppPath");
                    if (appPathValue is StringValue stringAppPathValue)
                    {
                        string appPath = stringAppPathValue.Value;
                        string directory = _workspacePath;
                        string appName = Path.GetFileName(appPath);

                        // Create a consolidated facts file
                        var appFacts = new Dictionary<string, object>();
                        
                        // Add all facts by category
                        foreach (var entry in _stateCache)
                        {
                            if (entry.Key.StartsWith(appName))
                            {
                                string category = entry.Key.Substring(appName.Length + 1);
                                appFacts[category] = entry.Value;
                            }
                        }

                        // Add metadata
                        appFacts["Metadata"] = new Dictionary<string, object>
                        {
                            ["AppName"] = appName,
                            ["GeneratedAt"] = DateTime.Now.ToString("o"),
                            ["FormatVersion"] = "1.0"
                        };

                        // Calculate metrics
                        var metrics = new Dictionary<string, object>();
                        if (appFacts.TryGetValue("Screens", out object screens) && screens is Dictionary<string, object> screensDict)
                        {
                            metrics["ScreenCount"] = screensDict.Count;
                        }
                        
                        if (appFacts.TryGetValue("Controls", out object controls) && controls is Dictionary<string, object> controlsDict)
                        {
                            metrics["ControlCount"] = controlsDict.Count;
                        }
                        
                        if (appFacts.TryGetValue("DataSources", out object dataSources) && dataSources is Dictionary<string, object> dataSourcesDict)
                        {
                            metrics["DataSourceCount"] = dataSourcesDict.Count;
                        }

                        ((Dictionary<string, object>)appFacts["Metadata"])["Metrics"] = metrics;

                        // Add recommendations
                        appFacts["TestRecommendations"] = GenerateTestRecommendations(appFacts);

                        // Write to file
                        string json = JsonSerializer.Serialize(appFacts, new JsonSerializerOptions
                        {
                            WriteIndented = true
                        });

                        string filePath = Path.Combine(directory, $"{appName}.app-facts.json");
                        _fileSystem.WriteTextToFile(filePath, json);

                        return BooleanValue.New(true);
                    }

                    return BooleanValue.New(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error exporting facts: {ex.Message}");
                    return BooleanValue.New(false);
                }
            }

            private Dictionary<string, object> GenerateTestRecommendations(Dictionary<string, object> facts)
            {
                var recommendations = new Dictionary<string, object>();
                var testCases = new List<Dictionary<string, string>>();
                
                // Get metrics from metadata
                var metadata = facts["Metadata"] as Dictionary<string, object>;
                var metrics = metadata["Metrics"] as Dictionary<string, object>;

                // Extract counts (safely)
                int screenCount = metrics.TryGetValue("ScreenCount", out object screenCountObj) ? Convert.ToInt32(screenCountObj) : 0;
                int controlCount = metrics.TryGetValue("ControlCount", out object controlCountObj) ? Convert.ToInt32(controlCountObj) : 0;
                int dataSourceCount = metrics.TryGetValue("DataSourceCount", out object dataSourceCountObj) ? Convert.ToInt32(dataSourceCountObj) : 0;

                // Calculate basic test scope
                recommendations["MinimumTestCount"] = Math.Max(screenCount, 3);
                
                // Add screen navigation tests
                if (screenCount > 0)
                {
                    testCases.Add(new Dictionary<string, string>
                    {
                        ["Type"] = "Navigation",
                        ["Description"] = "Test basic navigation between app screens",
                        ["Priority"] = "High"
                    });
                }

                // Add data tests if app has data sources
                if (dataSourceCount > 0)
                {
                    testCases.Add(new Dictionary<string, string>
                    {
                        ["Type"] = "Data",
                        ["Description"] = "Test CRUD operations on app data sources",
                        ["Priority"] = "High"
                    });
                }

                // Add UI interaction tests if app has controls
                if (controlCount > 0)
                {
                    testCases.Add(new Dictionary<string, string>
                    {
                        ["Type"] = "UI",
                        ["Description"] = "Test UI interactions with app controls",
                        ["Priority"] = "Medium"
                    });
                }

                recommendations["RecommendedTestCases"] = testCases;
                return recommendations;
            }
        }
    }
}
