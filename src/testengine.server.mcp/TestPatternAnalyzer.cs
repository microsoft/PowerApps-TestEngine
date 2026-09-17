// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerApps.TestEngine.MCP
{
    /// <summary>
    /// Functions that analyze Canvas App patterns to suggest specific test cases
    /// </summary>
    public static class TestPatternAnalyzer
    {
        private static readonly HashSet<string> _analyzedScreens = new HashSet<string>();
        private static readonly HashSet<string> _analyzedFlows = new HashSet<string>();
        private static readonly HashSet<string> _analyzedForms = new HashSet<string>();

        /// <summary>
        /// Identifies test patterns for login screens
        /// </summary>
        public class DetectLoginScreenFunction : ReflectionFunction
        {
            private const string FunctionName = "DetectLoginScreen";

            public DetectLoginScreenFunction()
                : base(DPath.Root, FunctionName, RecordType.Empty(), BooleanType.Boolean)
            {
            }

            public BooleanValue Execute(RecordValue screenInfo)
            {
                try
                {
                    var nameValue = screenInfo.GetField("Name");
                    if (nameValue is StringValue stringNameValue)
                    {
                        string name = stringNameValue.Value;

                        // Skip if already analyzed
                        if (_analyzedScreens.Contains(name))
                        {
                            return BooleanValue.New(false);
                        }

                        _analyzedScreens.Add(name);

                        // Check screen name patterns for login screens
                        var loginScreenPatterns = new[]
                        {
                            "login", "signin", "sign in", "log in", "auth", "authenticate"
                        };

                        foreach (var pattern in loginScreenPatterns)
                        {
                            if (name.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                return BooleanValue.New(true);
                            }
                        }

                        // Check for login controls in the screen
                        var controlsValue = screenInfo.GetField("Controls");
                        if (controlsValue is TableValue tableValue)
                        {
                            bool hasUsernameField = false;
                            bool hasPasswordField = false;
                            bool hasLoginButton = false;

                            foreach (var row in tableValue.Rows)
                            {
                                var controlName = row.Value.GetField("Name");
                                if (controlName is StringValue controlNameStr)
                                {
                                    string controlNameValue = controlNameStr.Value.ToLowerInvariant();

                                    if (controlNameValue.Contains("user") || controlNameValue.Contains("email") ||
                                        controlNameValue.Contains("login") || controlNameValue.Contains("name"))
                                    {
                                        hasUsernameField = true;
                                    }

                                    if (controlNameValue.Contains("pass") || controlNameValue.Contains("pwd"))
                                    {
                                        hasPasswordField = true;
                                    }

                                    if ((controlNameValue.Contains("login") || controlNameValue.Contains("signin") ||
                                         controlNameValue.Contains("submit")) &&
                                        (controlNameValue.Contains("button") || controlNameValue.Contains("btn")))
                                    {
                                        hasLoginButton = true;
                                    }
                                }
                            }

                            return BooleanValue.New(hasUsernameField && hasPasswordField && hasLoginButton);
                        }
                    }

                    return BooleanValue.New(false);
                }
                catch (Exception)
                {
                    return BooleanValue.New(false);
                }
            }
        }

        /// <summary>
        /// Identifies CRUD operations on a data source
        /// </summary>
        public class DetectCrudOperationsFunction : ReflectionFunction
        {
            private const string FunctionName = "DetectCrudOperations";

            public DetectCrudOperationsFunction()
                : base(DPath.Root, FunctionName, RecordType.Empty(), RecordType.Empty())
            {
            }

            public RecordValue Execute(RecordValue dataSource)
            {
                try
                {
                    string sourceName = string.Empty;
                    var operations = new Dictionary<string, bool>
                    {
                        ["Create"] = false,
                        ["Read"] = false,
                        ["Update"] = false,
                        ["Delete"] = false
                    };

                    var nameValue = dataSource.GetField("Name");
                    if (nameValue is StringValue stringNameValue)
                    {
                        sourceName = stringNameValue.Value;
                    }

                    var opsValue = dataSource.GetField("Operations");
                    if (opsValue is TableValue tableValue)
                    {
                        foreach (var row in tableValue.Rows)
                        {
                            var typeValue = row.Value.GetField("Type");
                            if (typeValue is StringValue typeStr)
                            {
                                string type = typeStr.Value;

                                switch (type.ToLowerInvariant())
                                {
                                    case "patch":
                                    case "collect":
                                    case "submit":
                                        operations["Create"] = true;
                                        break;
                                    case "lookup":
                                    case "filter":
                                    case "search":
                                        operations["Read"] = true;
                                        break;
                                    case "update":
                                    case "updateif":
                                        operations["Update"] = true;
                                        break;
                                    case "remove":
                                    case "removeif":
                                    case "clear":
                                        operations["Delete"] = true;
                                        break;
                                }
                            }
                        }
                    }

                    // Create a record of the operations for this data source
                    var fields = new List<NamedValue>
                    {
                        new NamedValue("DataSource", StringValue.New(sourceName)),
                        new NamedValue("HasCreate", BooleanValue.New(operations["Create"])),
                        new NamedValue("HasRead", BooleanValue.New(operations["Read"])),
                        new NamedValue("HasUpdate", BooleanValue.New(operations["Update"])),
                        new NamedValue("HasDelete", BooleanValue.New(operations["Delete"])),
                        new NamedValue("IsCrud", BooleanValue.New(
                            operations["Create"] && operations["Read"] &&
                            operations["Update"] && operations["Delete"]))
                    };

                    return RecordValue.NewRecordFromFields(fields);
                }
                catch (Exception)
                {
                    return RecordValue.NewRecordFromFields(new[]
                    {
                        new NamedValue("DataSource", StringValue.New(string.Empty)),
                        new NamedValue("HasCreate", BooleanValue.New(false)),
                        new NamedValue("HasRead", BooleanValue.New(false)),
                        new NamedValue("HasUpdate", BooleanValue.New(false)),
                        new NamedValue("HasDelete", BooleanValue.New(false)),
                        new NamedValue("IsCrud", BooleanValue.New(false))
                    });
                }
            }
        }

        /// <summary>
        /// Identifies form submission patterns
        /// </summary>
        public class DetectFormPatternFunction : ReflectionFunction
        {
            private const string FunctionName = "DetectFormPattern";

            public DetectFormPatternFunction()
                : base(DPath.Root, FunctionName, RecordType.Empty(), RecordType.Empty())
            {
            }

            public RecordValue Execute(RecordValue formInfo)
            {
                try
                {
                    string formId = string.Empty;
                    string formName = string.Empty;
                    string formType = "Unknown";
                    bool hasValidation = false;
                    bool hasSubmission = false;

                    var idValue = formInfo.GetField("Id");
                    if (idValue is StringValue stringIdValue)
                    {
                        formId = stringIdValue.Value;
                    }

                    // Skip if already analyzed
                    if (_analyzedForms.Contains(formId))
                    {
                        return CreateFormRecord(formId, formName, formType, hasValidation, hasSubmission);
                    }

                    _analyzedForms.Add(formId);

                    var nameValue = formInfo.GetField("Name");
                    if (nameValue is StringValue stringNameValue)
                    {
                        formName = stringNameValue.Value;
                    }

                    // Determine form type
                    var propsValue = formInfo.GetField("Properties");
                    if (propsValue is TableValue propsTable)
                    {
                        // Check for common form control properties
                        foreach (var prop in propsTable.Rows)
                        {
                            var propName = prop.Value.GetField("Name");
                            var propValue = prop.Value.GetField("Value");
                            if (
                                propName is StringValue propNameStr &&
                                propValue is StringValue propValueStr)
                            {
                                string name = propNameStr.Value;
                                string value = propValueStr.Value;

                                // Check for mode property to identify form type
                                if (name.Equals("Mode", StringComparison.OrdinalIgnoreCase))
                                {
                                    if (value.IndexOf("new", StringComparison.OrdinalIgnoreCase) >= 0)
                                    {
                                        formType = "Create";
                                    }
                                    else if (value.IndexOf("edit", StringComparison.OrdinalIgnoreCase) >= 0)
                                    {
                                        formType = "Edit";
                                    }
                                    else if (value.IndexOf("view", StringComparison.OrdinalIgnoreCase) >= 0)
                                    {
                                        formType = "View";
                                    }
                                }

                                // Check for validation formula
                                if (name.Equals("Valid", StringComparison.OrdinalIgnoreCase) ||
                                    name.Contains("Validation"))
                                {
                                    hasValidation = true;
                                }

                                // Check for submission handler
                                if (name.Equals("OnSuccess", StringComparison.OrdinalIgnoreCase) ||
                                    name.Equals("OnSubmit", StringComparison.OrdinalIgnoreCase))
                                {
                                    hasSubmission = true;
                                }
                            }
                        }
                    }

                    // If form type still unknown, try to determine from name
                    if (formType == "Unknown")
                    {
                        if (formName.IndexOf("new", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            formName.IndexOf("create", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            formType = "Create";
                        }
                        else if (formName.IndexOf("edit", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 formName.IndexOf("update", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            formType = "Edit";
                        }
                        else if (formName.IndexOf("view", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 formName.IndexOf("display", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            formType = "View";
                        }
                    }

                    return CreateFormRecord(formId, formName, formType, hasValidation, hasSubmission);
                }
                catch (Exception)
                {
                    return CreateFormRecord(string.Empty, string.Empty, "Unknown", false, false);
                }
            }

            private RecordValue CreateFormRecord(string id, string name, string type, bool hasValidation, bool hasSubmission)
            {
                return RecordValue.NewRecordFromFields(new[]
                {
                    new NamedValue("FormId", StringValue.New(id)),
                    new NamedValue("FormName", StringValue.New(name)),
                    new NamedValue("FormType", StringValue.New(type)),
                    new NamedValue("HasValidation", BooleanValue.New(hasValidation)),
                    new NamedValue("HasSubmission", BooleanValue.New(hasSubmission)),
                    new NamedValue("TestPriority", StringValue.New(
                        hasValidation && hasSubmission ? "High" :
                        hasValidation || hasSubmission ? "Medium" : "Low"))
                });
            }
        }

        /// <summary>
        /// Generates test case recommendations based on detected patterns
        /// </summary>
        public class GenerateTestCaseRecommendationsFunction : ReflectionFunction
        {
            private const string FunctionName = "GenerateTestCaseRecommendations";

            public GenerateTestCaseRecommendationsFunction()
                : base(DPath.Root, FunctionName, RecordType.Empty(), TableType.Empty())
            {
            }

            public TableValue Execute(RecordValue appInfo)
            {
                try
                {
                    var testCases = new List<RecordValue>();

                    // App basic info
                    string appName = string.Empty;

                    var nameValue = appInfo.GetField("Name");
                    if (nameValue is StringValue stringNameValue)
                    {
                        appName = stringNameValue.Value;
                    }


                    // Process CRUD operations
                    var dataSourcesValue = appInfo.GetField("DataSources");
                    if (dataSourcesValue is TableValue dataSourcesTable)
                    {
                        foreach (var source in dataSourcesTable.Rows)
                        {
                            var sourceName = source.Value.GetField("Name");
                            if (sourceName is StringValue sourceNameStr)
                            {
                                var hasCreate = source.Value.GetField("HasCreate");
                                if (hasCreate is BooleanValue hasCreateBool &&
                                    hasCreateBool.Value)
                                {
                                    testCases.Add(CreateCrudTestCase(appName, "Create", sourceNameStr.Value));
                                }

                                var hasRead = source.Value.GetField("HasRead");
                                if (hasRead is BooleanValue hasReadBool &&
                                    hasReadBool.Value)
                                {
                                    testCases.Add(CreateCrudTestCase(appName, "Read", sourceNameStr.Value));
                                }

                                var hasUpdate = source.Value.GetField("HasUpdate");
                                if (hasUpdate is BooleanValue hasUpdateBool &&
                                    hasUpdateBool.Value)
                                {
                                    testCases.Add(CreateCrudTestCase(appName, "Update", sourceNameStr.Value));
                                }

                                var hasDelete = source.Value.GetField("HasDelete");
                                if (hasDelete is BooleanValue hasDeleteBool &&
                                    hasDeleteBool.Value)
                                {
                                    testCases.Add(CreateCrudTestCase(appName, "Delete", sourceNameStr.Value));
                                }
                            }
                        }
                    }

                    // Process forms
                    var formsValue = appInfo.GetField("Forms");
                    if (formsValue is TableValue formsTable)
                    {
                        foreach (var form in formsTable.Rows)
                        {

                            var formType = form.Value.GetField("FormName");
                            var formName = form.Value.GetField("FormType");
                            var hasValidation = form.Value.GetField("HasValidation");

                            if (formName is StringValue formNameStr &&
                                formType is StringValue formTypeStr &&
                                hasValidation is BooleanValue hasValidationBool)
                            {
                                testCases.Add(CreateFormTestCase(
                                    appName,
                                    formNameStr.Value,
                                    formTypeStr.Value,
                                    hasValidationBool.Value));
                            }
                        }
                    }

                    return TableValue.NewTable(RecordType.Empty(), testCases);
                }
                catch (Exception)
                {
                    return TableValue.NewTable(RecordType.Empty(), new List<RecordValue>());
                }
            }

            private RecordValue CreateLoginTestCase(string appName, string screenName)
            {
                var testCaseId = $"Login_{Guid.NewGuid().ToString().Substring(0, 8)}";

                var testSteps = new StringBuilder();
                testSteps.AppendLine($"= Navigate(\"{screenName}\");");
                testSteps.AppendLine("  SetProperty(TextInput_Username, \"Text\", \"${{user1Email}}\");");
                testSteps.AppendLine("  SetProperty(TextInput_Password, \"Text\", \"${{user1Password}}\");");
                testSteps.AppendLine("  Select(Button_Login);");
                testSteps.AppendLine("  // Happy path: successful login");
                testSteps.AppendLine("  Assert(App.ActiveScreen.Name <> \"" + screenName + "\");");

                return RecordValue.NewRecordFromFields(new[]
                {
                    new NamedValue("TestCaseId", StringValue.New(testCaseId)),
                    new NamedValue("TestCaseName", StringValue.New("Login Flow")),
                    new NamedValue("TestCaseDescription", StringValue.New($"Validates that a user can log in to {appName} successfully")),
                    new NamedValue("TestPriority", StringValue.New("High")),
                    new NamedValue("TestCategory", StringValue.New("Authentication")),
                    new NamedValue("TestSteps", StringValue.New(testSteps.ToString()))
                });
            }

            private RecordValue CreateCrudTestCase(string appName, string operation, string dataSource)
            {
                var testCaseId = $"{operation}_{dataSource}_{Guid.NewGuid().ToString().Substring(0, 8)}";

                var testSteps = new StringBuilder();

                switch (operation)
                {
                    case "Create":
                        testSteps.AppendLine("= Navigate(\"NewItemScreen\");");
                        testSteps.AppendLine("  // Fill form fields with test data");
                        testSteps.AppendLine("  SetProperty(TextInput_Title, \"Text\", \"Test Item\");");
                        testSteps.AppendLine("  SetProperty(TextInput_Description, \"Text\", \"Test description\");");
                        testSteps.AppendLine("  Select(Button_Submit);");
                        testSteps.AppendLine("  // Verify creation succeeded");
                        testSteps.AppendLine("  Assert(IsVisible(Label_Success));");
                        break;
                    case "Read":
                        testSteps.AppendLine("= Navigate(\"SearchScreen\");");
                        testSteps.AppendLine("  SetProperty(TextInput_Search, \"Text\", \"Test\");");
                        testSteps.AppendLine("  Select(Button_Search);");
                        testSteps.AppendLine("  // Verify search results");
                        testSteps.AppendLine("  Assert(CountRows(Gallery_Results.AllItems) > 0);");
                        break;
                    case "Update":
                        testSteps.AppendLine("= Navigate(\"ListScreen\");");
                        testSteps.AppendLine("  // Select first item in gallery");
                        testSteps.AppendLine("  Select(Gallery_Items.FirstVisibleContainer);");
                        testSteps.AppendLine("  // Edit the item");
                        testSteps.AppendLine("  SetProperty(TextInput_Title, \"Text\", Concatenate(\"Updated \", Now()));");
                        testSteps.AppendLine("  Select(Button_Save);");
                        testSteps.AppendLine("  // Verify update succeeded");
                        testSteps.AppendLine("  Assert(IsVisible(Label_Success));");
                        break;
                    case "Delete":
                        testSteps.AppendLine("= Navigate(\"ListScreen\");");
                        testSteps.AppendLine("  // Count items before deletion");
                        testSteps.AppendLine("  Set(itemCountBefore, CountRows(Gallery_Items.AllItems));");
                        testSteps.AppendLine("  // Select and delete first item");
                        testSteps.AppendLine("  Select(Gallery_Items.FirstVisibleContainer);");
                        testSteps.AppendLine("  Select(Button_Delete);");
                        testSteps.AppendLine("  // Confirm deletion in dialog");
                        testSteps.AppendLine("  Select(Button_ConfirmDelete);");
                        testSteps.AppendLine("  // Verify item was deleted");
                        testSteps.AppendLine("  Assert(CountRows(Gallery_Items.AllItems) < itemCountBefore);");
                        break;
                }

                return RecordValue.NewRecordFromFields(new[]
                {
                    new NamedValue("TestCaseId", StringValue.New(testCaseId)),
                    new NamedValue("TestCaseName", StringValue.New($"{operation} {dataSource}")),
                    new NamedValue("TestCaseDescription", StringValue.New($"Tests {operation.ToLowerInvariant()} operation on {dataSource}")),
                    new NamedValue("TestPriority", StringValue.New("Medium")),
                    new NamedValue("TestCategory", StringValue.New("Data")),
                    new NamedValue("TestSteps", StringValue.New(testSteps.ToString()))
                });
            }

            private RecordValue CreateFormTestCase(string appName, string formName, string formType, bool hasValidation)
            {
                var testCaseId = $"Form_{formType}_{Guid.NewGuid().ToString().Substring(0, 8)}";

                var testSteps = new StringBuilder();

                switch (formType)
                {
                    case "Create":
                        testSteps.AppendLine($"= Navigate(\"{formName}\");");
                        testSteps.AppendLine("  // Fill form fields with test data");
                        testSteps.AppendLine("  SetProperty(TextInput_Field1, \"Text\", \"Test Data\");");

                        if (hasValidation)
                        {
                            // Add validation test
                            testSteps.AppendLine("  // Test validation - missing required field");
                            testSteps.AppendLine("  SetProperty(TextInput_Field2, \"Text\", \"\");");
                            testSteps.AppendLine("  Select(Button_Submit);");
                            testSteps.AppendLine("  // Verify validation error shows");
                            testSteps.AppendLine("  Assert(IsVisible(Label_ValidationError));");
                            testSteps.AppendLine("  // Fix validation issue");
                            testSteps.AppendLine("  SetProperty(TextInput_Field2, \"Text\", \"Valid Data\");");
                        }

                        testSteps.AppendLine("  // Submit the form");
                        testSteps.AppendLine("  Select(Button_Submit);");
                        testSteps.AppendLine("  // Verify submission successful");
                        testSteps.AppendLine("  Assert(IsVisible(Label_Success));");
                        break;

                    case "Edit":
                        testSteps.AppendLine("= Navigate(\"ListScreen\");");
                        testSteps.AppendLine("  // Select first item to edit");
                        testSteps.AppendLine("  Select(Gallery_Items.FirstVisibleContainer);");
                        testSteps.AppendLine($"  // Verify edit form loaded");
                        testSteps.AppendLine($"  Assert(App.ActiveScreen.Name = \"{formName}\");");
                        testSteps.AppendLine("  // Modify field");
                        testSteps.AppendLine("  SetProperty(TextInput_Field1, \"Text\", Concatenate(\"Updated \", Now()));");
                        testSteps.AppendLine("  // Submit changes");
                        testSteps.AppendLine("  Select(Button_Submit);");
                        testSteps.AppendLine("  // Verify update successful");
                        testSteps.AppendLine("  Assert(IsVisible(Label_Success));");
                        break;

                    case "View":
                        testSteps.AppendLine("= Navigate(\"ListScreen\");");
                        testSteps.AppendLine("  // Select first item to view");
                        testSteps.AppendLine("  Select(Gallery_Items.FirstVisibleContainer);");
                        testSteps.AppendLine($"  // Verify view form loaded");
                        testSteps.AppendLine($"  Assert(App.ActiveScreen.Name = \"{formName}\");");
                        testSteps.AppendLine("  // Verify readonly state");
                        testSteps.AppendLine("  Assert(!IsEnabled(TextInput_Field1));");
                        testSteps.AppendLine("  // Test navigation to edit");
                        testSteps.AppendLine("  If(IsVisible(Button_Edit), Select(Button_Edit));");
                        break;
                }

                return RecordValue.NewRecordFromFields(new[]
                {
                    new NamedValue("TestCaseId", StringValue.New(testCaseId)),
                    new NamedValue("TestCaseName", StringValue.New($"{formType} Form Test")),
                    new NamedValue("TestCaseDescription", StringValue.New($"Tests the {formType.ToLowerInvariant()} form functionality")),
                    new NamedValue("TestPriority", StringValue.New(hasValidation ? "High" : "Medium")),
                    new NamedValue("TestCategory", StringValue.New("Forms")),
                    new NamedValue("TestSteps", StringValue.New(testSteps.ToString()))
                });
            }
        }

        /// <summary>
        /// Reset state of all analyzers
        /// </summary>
        public static void Reset()
        {
            _analyzedScreens.Clear();
            _analyzedFlows.Clear();
            _analyzedForms.Clear();
        }
    }
}
