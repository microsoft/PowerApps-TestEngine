// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerApps.TestEngine.MCP
{
    /// <summary>
    /// Provides a PowerFx function to generate Canvas App test templates.
    /// </summary>
    public class CanvasAppTestTemplateFunction : ReflectionFunction
    {
        private const string FunctionName = "GenerateCanvasAppTestTemplate";
        private static bool _recommendationAdded = false;

        public CanvasAppTestTemplateFunction()
            : base(DPath.Root, FunctionName, RecordType.Empty(), RecordType.Empty())
        {
        }

        public RecordValue Execute()
        {
            return ExecuteAsync().Result;
        }

        public async Task<RecordValue> ExecuteAsync()
        {
            // Only return the template once to avoid duplicates
            if (_recommendationAdded)
            {
                return RecordValue.NewRecordFromFields(new[]
                {
                    new NamedValue("Success", BooleanValue.New(true)),
                    new NamedValue("Message", StringValue.New("Canvas App template already added"))
                });
            }
            _recommendationAdded = true;

            var template = @"## Canvas App Test Generation Guide

This workspace contains automatically generated insight files that GitHub Copilot can use to create meaningful tests.

### Available Resources:
1. `*.test-insights.json` - Contains summarized test patterns and key Canvas App components
2. `*.ui-map.json` - Maps screen and control relationships for navigation tests
3. `canvasapp.scan.yaml` - Contains the scan rules that generated these insights

### Using Insights for Test Generation:
```powershell
# View all available test insights
Get-ChildItem -Filter *.test-insights.json -Recurse | Get-Content | ConvertFrom-Json | Format-List
```

### Test Template
Use the following YAML template as a starting point for test generation. Customize based on insights.

-----------------------
file: canvasapp.te.yaml
-----------------------

# yaml-embedded-languages: powerfx
testSuite:
  testSuiteName: Canvas App Tests
  testSuiteDescription: Validate Canvas App functionality with automated tests
  persona: User1
  appLogicalName: MyCanvasApp

  testCases:
  - testCaseName: Login Flow
    testCaseDescription: Validates that a user can log in to the app
    testSteps: |
      # Check test-insights.json for actual login screens and form names
      = Navigate(""LoginScreen"");
        SetProperty(TextInput_Username, ""Text"", ""${user1Email}"");
        SetProperty(TextInput_Password, ""Text"", ""${user1Password}"");
        Select(Button_Login);
        Assert(App.ActiveScreen.Name = ""HomeScreen"");
        
  - testCaseName: Navigation Test
    testCaseDescription: Tests the navigation between main screens
    testSteps: |
      # Check ui-map.json for screen navigation flows
      = Navigate(""HomeScreen"");
        Assert(IsVisible(Button_Settings));
        Select(Button_Settings);
        Assert(App.ActiveScreen.Name = ""SettingsScreen"");
        Select(Button_Back);
        Assert(App.ActiveScreen.Name = ""HomeScreen"");
        
  - testCaseName: Data Entry Test
    testCaseDescription: Tests form submission with validation
    testSteps: |
      # Check test-insights.json for form patterns and validation rules
      = Navigate(""NewItemScreen"");
        SetProperty(TextInput_Name, ""Text"", ""Test Item"");
        SetProperty(TextInput_Description, ""Text"", ""This is a test item created by automation"");
        SetProperty(DatePicker_DueDate, ""SelectedDate"", Today() + 7);
        
        # For validation testing, add error cases from validation patterns
        SetProperty(TextInput_Required, ""Text"", """"); # Trigger validation error
        Select(Button_Submit);
        Assert(IsVisible(Label_ValidationError));
        
        # Fix validation error and submit
        SetProperty(TextInput_Required, ""Text"", ""Required Value"");
        Select(Button_Submit);
        Assert(IsVisible(Label_SuccessMessage));
        
  - testCaseName: Search Functionality
    testCaseDescription: Tests the search feature
    testSteps: |
      # Check test-insights.json for search patterns
      = Navigate(""SearchScreen"");
        SetProperty(TextInput_Search, ""Text"", ""test"");
        Select(Button_Search);
        Assert(CountRows(Gallery_Results.AllItems) > 0);
        
        # Add edge cases for search
        SetProperty(TextInput_Search, ""Text"", """");
        Select(Button_Search);
        Assert(IsVisible(Label_EmptySearchWarning));
        
  - testCaseName: CRUD Operations
    testCaseDescription: Tests create, read, update, delete operations
    testSteps: |
      # Check test-insights.json for data sources and operations
      # Create
      = Navigate(""NewItemScreen"");
        SetProperty(TextInput_Name, ""Text"", Concatenate(""Test Item "", Now()));
        Select(Button_Submit);
        Assert(IsVisible(Label_SuccessMessage));
        
        # Read
        Navigate(""ListScreen"");
        Assert(CountRows(Gallery_Items.AllItems) > 0);
        
        # Update
        Select(Gallery_Items.FirstVisibleContainer);
        Navigate(""EditScreen"");
        SetProperty(TextInput_Name, ""Text"", Concatenate(""Updated "", Now()));
        Select(Button_Save);
        Assert(IsVisible(Label_SuccessMessage));
        
        # Delete
        Navigate(""ListScreen"");
        Select(Gallery_Items.FirstVisibleContainer);
        Select(Button_Delete);
        Assert(CountRows(Gallery_Items.AllItems) < CountRows(Gallery_Items_Before.AllItems));
    
  testSettings:
    headless: false
    locale: ""en-US""
    recordVideo: true
    extensionModules:
      enable: true
    browserConfigurations:
    - browser: Chromium

  environmentVariables:
  users:
  - personaName: User1
    emailKey: user1Email
    passwordKey: user1Password

### For more complex test scenarios:
1. Error handling tests: Check validation rules in test-insights.json
2. Edge case tests: Look for boundary conditions in form validation
3. Performance tests: Identify data-heavy screens from ui-map.json
4. Accessibility tests: Focus on identified accessible controls

Example using insight files in C# code:
```csharp
// Read test insights file to generate tests programmatically
var insightJson = File.ReadAllText(""path/to/app.test-insights.json"");
var insights = JsonSerializer.Deserialize<Dictionary<string, object>>(insightJson);

// Extract screens for navigation tests
var screens = insights[""Screens""] as Dictionary<string, object>;
var testPaths = insights[""TestPaths""] as Dictionary<string, object>;
```
";

            return RecordValue.NewRecordFromFields(new[]
            {
                new NamedValue("Success", BooleanValue.New(true)),
                new NamedValue("Template", StringValue.New(template)),
                new NamedValue("Type", StringValue.New("Canvas App Test Template")),
                new NamedValue("Priority", StringValue.New("High"))
            });
        }

        /// <summary>
        /// Resets the function state to allow recommendations again
        /// (primarily used for testing scenarios)
        /// </summary>
        public static void Reset()
        {
            _recommendationAdded = false;
        }
    }
}
