// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerApps.TestEngine.MCP
{
    /// <summary>
    /// Provides functions for storing and retrieving scan state to avoid token limits
    /// </summary>
    public static class ScanStateManager
    {
        private static readonly Dictionary<string, Dictionary<string, object>> _stateCache = new Dictionary<string, Dictionary<string, object>>();        /// <summary>
                                                                                                                                                          /// Function that saves insights to a state file during scanning
                                                                                                                                                          /// </summary>
        public class SaveInsightFunction : ReflectionFunction
        {
            private const string FunctionName = "SaveInsight";
            private readonly IFileSystem _fileSystem;
            private readonly ILogger _logger;
            private readonly string _workspacePath;

            public SaveInsightFunction(IFileSystem fileSystem, ILogger logger, string workspacePath)
                : base(DPath.Root, FunctionName, RecordType.Empty(), BooleanType.Boolean)
            {
                _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
                _workspacePath = workspacePath ?? throw new ArgumentNullException(nameof(workspacePath));
            }

            public BooleanValue Execute(RecordValue insight)
            {
                try
                {
                    var categoryValue = insight.GetField("Category");
                    var keyValue = insight.GetField("Key");
                    var appPathValue = insight.GetField("AppPath");
                    var valueValue = insight.GetField("Value");
                    if (
                        categoryValue is StringValue stringCategoryValue &&
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

                        // Save to file periodically (every 10 insights)
                        if (state.Count % 10 == 0)
                        {
                            SaveStateToFile(appPath, category, state);
                        }

                        return BooleanValue.New(true);
                    }

                    return BooleanValue.New(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error saving insight: {ex.Message}");
                    return BooleanValue.New(false);
                }
            }

            private void SaveStateToFile(string appPath, string category, Dictionary<string, object> state)
            {
                try
                {
                    // Use workspace path as base directory instead of app directory
                    string directory = _workspacePath;
                    string filename = $"{Path.GetFileName(appPath)}_{category}.scan-state.json";
                    string filePath = Path.Combine(directory, filename);

                    string json = JsonSerializer.Serialize(state, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

                    _fileSystem.WriteTextToFile(filePath, json);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error saving state file: {ex.Message}");
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
        /// Function that persists all cached insights to disk
        /// </summary>
        public class FlushInsightsFunction : ReflectionFunction
        {
            private const string FunctionName = "FlushInsights";
            private readonly IFileSystem _fileSystem;
            private readonly ILogger _logger;
            private readonly string _workspacePath;

            public FlushInsightsFunction(IFileSystem fileSystem, ILogger logger, string workspacePath)
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
                        string directory = _workspacePath; // Use workspace path as the base directory
                        string appName = Path.GetFileName(appPath);

                        // Save all categories for this app
                        foreach (var entry in _stateCache)
                        {
                            if (entry.Key.StartsWith(appName))
                            {
                                string category = entry.Key.Substring(appName.Length + 1);
                                string filename = $"{appName}_{category}.scan-state.json";
                                string filePath = Path.Combine(directory, filename);

                                string json = JsonSerializer.Serialize(entry.Value, new JsonSerializerOptions
                                {
                                    WriteIndented = true
                                });

                                _fileSystem.WriteTextToFile(filePath, json);
                            }
                        }

                        // Generate a test insights summary file
                        GenerateTestInsightsSummary(directory, appName);

                        return BooleanValue.New(true);
                    }

                    return BooleanValue.New(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error flushing insights: {ex.Message}");
                    return BooleanValue.New(false);
                }
            }
            private void GenerateTestInsightsSummary(string directory, string appName)
            {
                try
                {
                    // Gather all relevant insights for test generation
                    var testInsights = new Dictionary<string, object>
                    {
                        ["Screens"] = GetCategoryData(appName, "Screens"),
                        ["Navigation"] = GetCategoryData(appName, "Navigation"),
                        ["DataSources"] = GetCategoryData(appName, "DataSources"),
                        ["Controls"] = GetCategoryData(appName, "Controls"),
                        ["TestPaths"] = GetCategoryData(appName, "TestPaths"),
                        ["Validation"] = GetCategoryData(appName, "Validation"),
                        ["Properties"] = GetCategoryData(appName, "Properties"),
                        ["TestPatterns"] = IdentifyTestPatterns()
                    };

                    // Add metadata to help GitHub Copilot understand the insights
                    testInsights["Metadata"] = new Dictionary<string, object>
                    {
                        ["AppName"] = appName,
                        ["GeneratedAt"] = DateTime.Now.ToString("o"),
                        ["FormatVersion"] = "1.0",
                        ["Usage"] = new Dictionary<string, object>
                        {
                            ["Description"] = "This file contains test insights for GitHub Copilot to generate automated tests",
                            ["Recommendations"] = new[]
                            {
                                "Use the 'TestPatterns' section for identifying high-priority test scenarios",
                                "Reference 'Screens' and 'Navigation' for test flow mapping",
                                "Check 'DataSources' for CRUD test requirements",
                                "Explore 'Validation' for edge case and error tests",
                                "Generate at least one test case per screen in 'Screens'",
                                "Ensure each navigation pattern has test coverage"
                            }
                        },
                        // Track key metrics to help GitHub Copilot assess complexity
                        ["Metrics"] = new Dictionary<string, object>
                        {
                            ["ScreenCount"] = CountItems(testInsights["Screens"] as Dictionary<string, object>),
                            ["DataSourceCount"] = CountItems(testInsights["DataSources"] as Dictionary<string, object>),
                            ["ControlCount"] = CountItems(testInsights["Controls"] as Dictionary<string, object>),
                            ["NavigationFlowCount"] = CountItems(testInsights["Navigation"] as Dictionary<string, object>),
                            ["FormValidationCount"] = CountItems(testInsights["Validation"] as Dictionary<string, object>)
                        }
                    };

                    // Generate test recommendations based on app complexity
                    testInsights["TestRecommendations"] = GenerateTestRecommendations(testInsights);

                    string json = JsonSerializer.Serialize(testInsights, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

                    string filePath = Path.Combine(directory, $"{appName}.test-insights.json");
                    _fileSystem.WriteTextToFile(filePath, json);

                    // Create a README file to explain the insights
                    string readmePath = Path.Combine(directory, "TEST-INSIGHTS-README.md");
                    string readme = GenerateTestInsightsReadme(appName);
                    _fileSystem.WriteTextToFile(readmePath, readme);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error generating test insights: {ex.Message}");
                }
            }

            private int CountItems(Dictionary<string, object> dict)
            {
                return dict?.Count ?? 0;
            }

            private Dictionary<string, object> GenerateTestRecommendations(Dictionary<string, object> insights)
            {
                var recommendations = new Dictionary<string, object>();
                var metrics = (insights["Metadata"] as Dictionary<string, object>)["Metrics"] as Dictionary<string, object>;

                // Calculate recommended test case counts based on app complexity
                int screenCount = Convert.ToInt32(metrics["ScreenCount"]);
                int dataSourceCount = Convert.ToInt32(metrics["DataSourceCount"]);
                int validationCount = Convert.ToInt32(metrics["FormValidationCount"]);

                // Basic recommendation is at least 1 test per screen
                int basicTestCount = Math.Max(5, screenCount);

                // Scale up for complex apps
                int recommendedTestCount = basicTestCount;
                if (dataSourceCount > 0)
                {
                    recommendedTestCount += dataSourceCount * 2; // CRUD operations need multiple tests
                }
                if (validationCount > 0)
                {
                    recommendedTestCount += validationCount; // Validation needs happy/sad path tests
                }

                recommendations["RecommendedTestCaseCount"] = recommendedTestCount;
                recommendations["MinimumTestCaseCount"] = basicTestCount;
                recommendations["OptimalTestCoverage"] = new Dictionary<string, object>
                {
                    ["UINavigation"] = screenCount,
                    ["DataOperations"] = dataSourceCount * 2,
                    ["FormValidation"] = validationCount,
                    ["ErrorHandling"] = validationCount,
                    ["EdgeCases"] = Math.Max(validationCount, 3)
                };

                // Generate specific test case suggestions
                var testCaseSuggestions = new List<Dictionary<string, object>>();

                // Always recommend a login test if applicable
                if (HasLoginScreen(insights))
                {
                    testCaseSuggestions.Add(new Dictionary<string, object>
                    {
                        ["Name"] = "Authentication Test",
                        ["Description"] = "Verify user can login with valid credentials and cannot login with invalid credentials",
                        ["Priority"] = "High",
                        ["Type"] = "Authentication",
                        ["ScreenPattern"] = "LoginScreen"
                    });
                }

                // Always recommend basic navigation
                testCaseSuggestions.Add(new Dictionary<string, object>
                {
                    ["Name"] = "Main Navigation Flow",
                    ["Description"] = "Verify user can navigate between main app screens",
                    ["Priority"] = "High",
                    ["Type"] = "Navigation",
                    ["ScreenPattern"] = "All main screens"
                });

                // Add data operations if applicable
                if (dataSourceCount > 0)
                {
                    testCaseSuggestions.Add(new Dictionary<string, object>
                    {
                        ["Name"] = "CRUD Operations",
                        ["Description"] = "Test Create, Read, Update, Delete operations on main data sources",
                        ["Priority"] = "High",
                        ["Type"] = "Data",
                        ["DataSources"] = GetDataSourceNames(insights)
                    });
                }

                // Add form validation if applicable
                if (validationCount > 0)
                {
                    testCaseSuggestions.Add(new Dictionary<string, object>
                    {
                        ["Name"] = "Form Validation",
                        ["Description"] = "Test form validation with valid and invalid inputs",
                        ["Priority"] = "Medium",
                        ["Type"] = "Validation",
                        ["ValidationRules"] = "Check validation section for specific rules"
                    });
                }

                recommendations["TestCaseSuggestions"] = testCaseSuggestions;
                return recommendations;
            }

            private bool HasLoginScreen(Dictionary<string, object> insights)
            {
                var testPatterns = insights["TestPatterns"] as Dictionary<string, object>;
                if (testPatterns != null &&
                    testPatterns.TryGetValue("LoginScreens", out object loginScreens) &&
                    loginScreens is List<object> loginScreensList)
                {
                    return loginScreensList.Count > 0;
                }
                return false;
            }

            private List<string> GetDataSourceNames(Dictionary<string, object> insights)
            {
                var result = new List<string>();
                var dataSources = insights["DataSources"] as Dictionary<string, object>;

                if (dataSources != null)
                {
                    foreach (var source in dataSources)
                    {
                        if (source.Value is Dictionary<string, object> sourceDict &&
                            sourceDict.TryGetValue("DataSource", out object dataSourceName))
                        {
                            string name = dataSourceName.ToString();
                            if (!result.Contains(name))
                            {
                                result.Add(name);
                            }
                        }
                    }
                }

                return result;
            }

            private string GenerateTestInsightsReadme(string appName)
            {
                return @$"# Test Insights for {appName}

## Overview
This directory contains automatically generated test insights for {appName}. These files help GitHub Copilot generate effective automated tests.

## Files
- `{appName}.test-insights.json` - Contains key app components and test patterns
- `{appName}.ui-map.json` - Maps screens and controls for navigation testing
- `{appName}*.scan-state.json` - (Optional) Detailed app scanning data

## Using These Files with GitHub Copilot

### For Test Generation
Use GitHub Copilot to generate tests by:

1. Opening `{appName}.test-insights.json` to understand app structure
2. Create a new test file (e.g., `canvasapp.te.yaml`)
3. Ask GitHub Copilot: ""Generate a comprehensive test suite for this Canvas App based on the test insights file""

### Key File Sections

In the test-insights.json file:

- **Screens** - All app screens for navigation tests
- **Navigation** - Screen navigation patterns
- **DataSources** - Data operations (Create, Read, Update, Delete)
- **TestPatterns** - Identified patterns for test generation
- **Validation** - Form validation rules for testing boundary cases
- **TestRecommendations** - Suggested test cases and coverage

### Example Prompts for GitHub Copilot

- ""Generate login test cases using the test insights file""
- ""Create CRUD test operations for all data sources in the app""
- ""Write form validation tests with both valid and invalid inputs""
- ""Generate navigation tests covering all screens in the app""
- ""Create edge case tests for form validation rules""

## Best Practices

1. Start with high-priority test cases from TestRecommendations
2. Ensure each screen has at least one navigation test
3. Cover both happy path and error cases for data operations
4. Include tests for form validation rules
5. Test edge cases identified in the insights

## Maintaining Tests

As the app evolves:

1. Re-run the scan to update insight files
2. Compare new insights with previous test coverage
3. Update tests to cover new functionality
4. Remove obsolete tests for removed features
";
            }

            private object GetCategoryData(string appName, string category)
            {
                string key = $"{appName}_{category}";
                if (_stateCache.TryGetValue(key, out Dictionary<string, object> data))
                {
                    return data;
                }

                return new Dictionary<string, object>();
            }

            private object IdentifyTestPatterns()
            {
                var patterns = new Dictionary<string, object>();

                // Analyze navigation flows to identify common paths
                if (_stateCache.TryGetValue("Navigation", out Dictionary<string, object> navigationData))
                {
                    var flows = new List<object>();
                    var visited = new HashSet<string>();

                    foreach (var nav in navigationData.Values)
                    {
                        if (nav is Dictionary<string, object> navDict &&
                            navDict.TryGetValue("Source", out object source) &&
                            navDict.TryGetValue("Target", out object target))
                        {
                            string path = $"{source}->{target}";
                            if (!visited.Contains(path))
                            {
                                visited.Add(path);
                                flows.Add(new Dictionary<string, object>
                                {
                                    ["From"] = source,
                                    ["To"] = target,
                                    ["TestPriority"] = "High"
                                });
                            }
                        }
                    }

                    patterns["NavigationFlows"] = flows;
                }

                // Identify data operations that need testing
                if (_stateCache.TryGetValue("DataOperations", out Dictionary<string, object> dataOps))
                {
                    var dataTests = new List<object>();

                    foreach (var op in dataOps.Values)
                    {
                        if (op is Dictionary<string, object> opDict &&
                            opDict.TryGetValue("Type", out object type) &&
                            opDict.TryGetValue("Control", out object control))
                        {
                            dataTests.Add(new Dictionary<string, object>
                            {
                                ["Operation"] = type,
                                ["Control"] = control,
                                ["TestPriority"] = type.ToString() == "Create" || type.ToString() == "Update" ? "High" : "Medium"
                            });
                        }
                    }

                    patterns["DataTests"] = dataTests;
                }

                return patterns;
            }
        }

        /// <summary>
        /// Function that generates a UI map for navigation testing
        /// </summary>
        public class GenerateUIMapFunction : ReflectionFunction
        {
            private const string FunctionName = "GenerateUIMap";
            private readonly IFileSystem _fileSystem;
            private readonly ILogger _logger;
            private readonly string _workspacePath;

            public GenerateUIMapFunction(IFileSystem fileSystem, ILogger logger, string workspacePath)
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
                        string directory = _workspacePath; // Use workspace path as base directory
                        string appName = Path.GetFileName(appPath);

                        // Get screens and controls
                        var screens = new Dictionary<string, object>();
                        var screenKey = $"{appName}_Screens";
                        var controlsKey = $"{appName}_Controls";
                        var navigationKey = $"{appName}_Navigation";
                        var validationKey = $"{appName}_Validation";

                        Dictionary<string, object> navigationData = null;
                        Dictionary<string, object> validationData = null;

                        _stateCache.TryGetValue(navigationKey, out navigationData);
                        _stateCache.TryGetValue(validationKey, out validationData);

                        if (_stateCache.TryGetValue(screenKey, out Dictionary<string, object> screenData) &&
                            _stateCache.TryGetValue(controlsKey, out Dictionary<string, object> controlData))
                        {
                            // Create a map of screens and their controls
                            foreach (var screen in screenData)
                            {
                                var screenControls = new List<object>();
                                var screenButtons = new List<object>();
                                var screenInputs = new List<object>();
                                var screenValidation = new List<object>();

                                foreach (var control in controlData.Values)
                                {
                                    if (control is Dictionary<string, object> controlDict &&
                                        controlDict.TryGetValue("Parent", out object parent) &&
                                        parent.ToString() == screen.Key)
                                    {
                                        screenControls.Add(control);

                                        // Categorize controls by type for easier test generation
                                        if (controlDict.TryGetValue("ControlType", out object controlType))
                                        {
                                            string type = controlType.ToString();

                                            if (type.Equals("Button", StringComparison.OrdinalIgnoreCase))
                                            {
                                                screenButtons.Add(control);
                                            }
                                            else if (type.Equals("TextInput", StringComparison.OrdinalIgnoreCase))
                                            {
                                                screenInputs.Add(control);
                                            }
                                        }
                                    }
                                }

                                // Find validation rules for this screen
                                if (validationData != null)
                                {
                                    foreach (var validation in validationData.Values)
                                    {
                                        if (validation is Dictionary<string, object> validationDict &&
                                            validationDict.TryGetValue("Control", out object validationControl))
                                        {
                                            string controlName = validationControl.ToString();

                                            // Check if control belongs to this screen
                                            var matchingControls = screenControls.Cast<Dictionary<string, object>>()
                                                .Where(c => c["Name"].ToString() == controlName);

                                            if (matchingControls.Any())
                                            {
                                                screenValidation.Add(validation);
                                            }
                                        }
                                    }
                                }

                                // Find navigation flows from this screen
                                var screenNavigations = new List<object>();
                                if (navigationData != null)
                                {
                                    foreach (var navigation in navigationData.Values)
                                    {
                                        if (navigation is Dictionary<string, object> navDict &&
                                            navDict.TryGetValue("Source", out object source) &&
                                            source.ToString() == screen.Key)
                                        {
                                            screenNavigations.Add(navigation);
                                        }
                                    }
                                }

                                screens[screen.Key] = new Dictionary<string, object>
                                {
                                    ["Name"] = screen.Key,
                                    ["Controls"] = screenControls,
                                    ["Buttons"] = screenButtons,
                                    ["Inputs"] = screenInputs,
                                    ["ValidationRules"] = screenValidation,
                                    ["NavigationFlows"] = screenNavigations,
                                    ["TestGenerationHints"] = GenerateScreenTestHints(
                                        screen.Key,
                                        screenButtons.Count,
                                        screenInputs.Count,
                                        screenValidation.Count,
                                        screenNavigations.Count
                                    )
                                };
                            }

                            // Generate navigation map
                            var navigationMap = new Dictionary<string, List<string>>();
                            if (navigationData != null)
                            {
                                foreach (var screen in screenData.Keys)
                                {
                                    navigationMap[screen] = new List<string>();
                                }

                                foreach (var nav in navigationData.Values)
                                {
                                    if (nav is Dictionary<string, object> navDict &&
                                        navDict.TryGetValue("Source", out object source) &&
                                        navDict.TryGetValue("Target", out object target))
                                    {
                                        string sourceScreen = source.ToString();
                                        string targetScreen = target.ToString();

                                        if (navigationMap.ContainsKey(sourceScreen) && !navigationMap[sourceScreen].Contains(targetScreen))
                                        {
                                            navigationMap[sourceScreen].Add(targetScreen);
                                        }
                                    }
                                }
                            }

                            // Create test flow suggestions
                            var testFlows = new List<Dictionary<string, object>>();

                            // Basic flow covering all screens
                            var screensList = screenData.Keys.ToList();
                            if (screensList.Count > 0)
                            {
                                var basicFlow = new Dictionary<string, object>
                                {
                                    ["Name"] = "Complete App Flow",
                                    ["Description"] = "Navigation flow covering all main screens",
                                    ["Priority"] = "High",
                                    ["Screens"] = screensList,
                                };
                                testFlows.Add(basicFlow);
                            }

                            // Create GitHub Copilot guidance
                            var testGuidance = new Dictionary<string, object>
                            {
                                ["ForGitHubCopilot"] = new Dictionary<string, object>
                                {
                                    ["Description"] = "UI map to assist GitHub Copilot in generating comprehensive tests",
                                    ["Usage"] = new[]
                                    {
                                        "Use this file to understand screen structure and control relationships",
                                        "Reference 'TestGenerationHints' per screen for specialized test suggestions",
                                        "Follow the 'NavigationMap' to ensure test coverage of app flows",
                                        "Look for 'ValidationRules' to create boundary condition tests",
                                        "Use 'TestFlows' for recommended test scenarios"
                                    },
                                    ["ExamplePrompts"] = new[]
                                    {
                                        "Generate a test that navigates through all screens in the app",
                                        "Create tests for all input validation rules on ScreenName",
                                        "Write a test that follows the Complete App Flow in the UI map",
                                        "Generate a test for each button's OnSelect action on HomeScreen",
                                        "Create error handling tests for all form validations"
                                    }
                                }
                            };

                            // Save UI map to file with additional metadata
                            string json = JsonSerializer.Serialize(new Dictionary<string, object>
                            {
                                ["AppName"] = appName,
                                ["GeneratedAt"] = DateTime.Now.ToString("o"),
                                ["FormatVersion"] = "1.0",
                                ["Screens"] = screens,
                                ["NavigationMap"] = navigationMap,
                                ["TestFlows"] = testFlows,
                                ["Guidance"] = testGuidance
                            }, new JsonSerializerOptions
                            {
                                WriteIndented = true
                            });                            string filePath = Path.Combine(directory, $"{appName}.ui-map.json");
                            _fileSystem.WriteTextToFile(filePath, json, false);
                        }

                        return BooleanValue.New(true);
                    }

                    return BooleanValue.New(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error generating UI map: {ex.Message}");
                    return BooleanValue.New(false);
                }
            }

            private Dictionary<string, object> GenerateScreenTestHints(
                string screenName,
                int buttonCount,
                int inputCount,
                int validationCount,
                int navigationCount)
            {
                var hints = new Dictionary<string, object>();
                var testTypes = new List<string>();
                var priority = "Medium";

                // Determine screen type based on name and contents
                string screenType = "Standard";
                if (screenName.Contains("Login", StringComparison.OrdinalIgnoreCase) ||
                    screenName.Contains("SignIn", StringComparison.OrdinalIgnoreCase))
                {
                    screenType = "Login";
                    priority = "High";
                    testTypes.Add("Authentication");
                }
                else if (screenName.Contains("Form", StringComparison.OrdinalIgnoreCase) ||
                         screenName.Contains("New", StringComparison.OrdinalIgnoreCase) ||
                         screenName.Contains("Edit", StringComparison.OrdinalIgnoreCase))
                {
                    screenType = "Form";
                    if (validationCount > 0)
                    {
                        priority = "High";
                        testTypes.Add("Validation");
                    }
                    testTypes.Add("Data Entry");
                }
                else if (screenName.Contains("List", StringComparison.OrdinalIgnoreCase) ||
                         screenName.Contains("Gallery", StringComparison.OrdinalIgnoreCase))
                {
                    screenType = "List";
                    testTypes.Add("Data Display");
                }
                else if (screenName.Contains("Detail", StringComparison.OrdinalIgnoreCase) ||
                         screenName.Contains("View", StringComparison.OrdinalIgnoreCase))
                {
                    screenType = "Details";
                    testTypes.Add("Data Display");
                }
                else if (screenName.Contains("Search", StringComparison.OrdinalIgnoreCase))
                {
                    screenType = "Search";
                    testTypes.Add("Search");
                    priority = "High";
                }
                else if (screenName.Contains("Setting", StringComparison.OrdinalIgnoreCase))
                {
                    screenType = "Settings";
                    testTypes.Add("Configuration");
                }
                else if (screenName.Contains("Home", StringComparison.OrdinalIgnoreCase) ||
                         screenName.Contains("Main", StringComparison.OrdinalIgnoreCase) ||
                         screenName.Contains("Dashboard", StringComparison.OrdinalIgnoreCase))
                {
                    screenType = "Home";
                    priority = "High";
                    testTypes.Add("Navigation");
                }

                // Always add navigation tests if there are navigation flows
                if (navigationCount > 0 && !testTypes.Contains("Navigation"))
                {
                    testTypes.Add("Navigation");
                }

                // Add UI interaction tests if there are buttons or inputs
                if (buttonCount > 0 || inputCount > 0)
                {
                    testTypes.Add("UI Interaction");
                }

                // Generate example test cases
                var testCases = new List<string>();

                if (testTypes.Contains("Authentication"))
                {
                    testCases.Add("Test valid login credentials");
                    testCases.Add("Test invalid login credentials");
                    testCases.Add("Test empty login form submission");
                }

                if (testTypes.Contains("Validation"))
                {
                    testCases.Add($"Test form validation rules on {screenName}");
                    testCases.Add("Test boundary conditions for numeric inputs");
                    testCases.Add("Test required field validation");
                }

                if (testTypes.Contains("Data Entry"))
                {
                    testCases.Add("Test form submission with valid data");
                    if (validationCount > 0)
                    {
                        testCases.Add("Test form recovery after validation errors");
                    }
                }

                if (testTypes.Contains("Navigation"))
                {
                    testCases.Add($"Test navigation from {screenName} to other screens");
                    if (navigationCount > 1)
                    {
                        testCases.Add($"Test multiple navigation paths from {screenName}");
                    }
                }

                if (testTypes.Contains("Search"))
                {
                    testCases.Add("Test search with valid search terms");
                    testCases.Add("Test search with no results");
                    testCases.Add("Test empty search submission");
                }

                if (testTypes.Contains("Data Display"))
                {
                    testCases.Add("Test data display with multiple records");
                    testCases.Add("Test data display with no records");
                }

                // Provide test structure guidance
                hints["ScreenType"] = screenType;
                hints["TestPriority"] = priority;
                hints["TestTypes"] = testTypes;
                hints["SuggestedTestCases"] = testCases;
                hints["TestCodeExamples"] = GenerateTestExamples(screenName, screenType, inputCount > 0, buttonCount > 0);

                return hints;
            }

            private Dictionary<string, string> GenerateTestExamples(
                string screenName, string screenType, bool hasInputs, bool hasButtons)
            {
                var examples = new Dictionary<string, string>();

                // Generate basic navigation test
                examples["BasicNavigation"] = $@"= Navigate(""{screenName}"");
  Assert(App.ActiveScreen.Name = ""{screenName}"");";

                // Generate type-specific tests
                switch (screenType)
                {
                    case "Login":
                        examples["SuccessfulLogin"] = $@"= Navigate(""{screenName}"");
  SetProperty(TextInput_Username, ""Text"", ""${{user1Email}}"");
  SetProperty(TextInput_Password, ""Text"", ""${{user1Password}}"");
  Select(Button_Login);
  Assert(App.ActiveScreen.Name <> ""{screenName}"");";

                        examples["FailedLogin"] = $@"= Navigate(""{screenName}"");
  SetProperty(TextInput_Username, ""Text"", ""invalid@example.com"");
  SetProperty(TextInput_Password, ""Text"", ""wrongpassword"");
  Select(Button_Login);
  Assert(IsVisible(Label_LoginError));
  Assert(App.ActiveScreen.Name = ""{screenName}"");";
                        break;

                    case "Form":
                        if (hasInputs && hasButtons)
                        {
                            examples["FormSubmission"] = $@"= Navigate(""{screenName}"");
  SetProperty(TextInput_Field1, ""Text"", ""Test Value"");
  SetProperty(TextInput_Field2, ""Text"", ""Another Test"");
  Select(Button_Submit);
  Assert(IsVisible(Label_Success) Or App.ActiveScreen.Name <> ""{screenName}"");";

                            examples["FormValidation"] = $@"= Navigate(""{screenName}"");
  // Leave required field empty
  SetProperty(TextInput_Field1, ""Text"", """");
  SetProperty(TextInput_Field2, ""Text"", ""Test"");
  Select(Button_Submit);
  // Check validation error appears
  Assert(IsVisible(Label_ValidationError));
  // Fix the error and resubmit
  SetProperty(TextInput_Field1, ""Text"", ""Valid data"");
  Select(Button_Submit);
  Assert(Not(IsVisible(Label_ValidationError)));";
                        }
                        break;

                    case "List":
                        examples["ListView"] = $@"= Navigate(""{screenName}"");
  // Test with data
  Assert(CountRows(Gallery_Items.AllItems) > 0);
  // Select an item
  Select(Gallery_Items.FirstVisibleContainer);
  // Verify detail screen opens
  Assert(App.ActiveScreen.Name <> ""{screenName}"");";
                        break;

                    case "Search":
                        examples["SearchTest"] = $@"= Navigate(""{screenName}"");
  // Search for results
  SetProperty(TextInput_Search, ""Text"", ""test"");
  Select(Button_Search);
  Assert(CountRows(Gallery_Results.AllItems) > 0);
  // Test empty search
  SetProperty(TextInput_Search, ""Text"", """");
  Select(Button_Search);
  Assert(IsVisible(Label_EmptySearchWarning));";
                        break;

                    default:
                        if (hasButtons)
                        {
                            examples["ButtonInteraction"] = $@"= Navigate(""{screenName}"");
  // Verify button is visible
  Assert(IsVisible(Button_Action));
  // Press the button
  Select(Button_Action);
  // Verify action happened (screen changed or control appeared)
  Assert(App.ActiveScreen.Name <> ""{screenName}"" Or IsVisible(Label_ActionResult));";
                        }
                        break;
                }

                return examples;
            }
        }
    }
}
