// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Text.RegularExpressions;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerApps.TestEngine.MCP
{
    /// <summary>
    /// Provides PowerFx functions specific to Canvas App analysis.
    /// </summary>
    public static class CanvasAppScanFunctions
    {
        /// <summary>
        /// Identifies common UI patterns in Canvas Apps
        /// </summary>
        public class IdentifyUIPatternFunction : ReflectionFunction
        {
            private const string FunctionName = "IdentifyUIPattern";

            public IdentifyUIPatternFunction()
                : base(DPath.Root, FunctionName, RecordType.Empty(), StringType.String)
            {
            }

            public StringValue Execute(RecordValue controlInfo)
            {
                try
                {
                    string controlType = string.Empty;
                    string controlName = string.Empty;

                    // Access properties from the record value directly
                    var typeValue = controlInfo.GetField("Type");
                    if (typeValue != null && typeValue is StringValue stringTypeValue)
                    {
                        controlType = stringTypeValue.Value;
                    }

                    var nameValue = controlInfo.GetField("Name");
                    if (nameValue != null && nameValue is StringValue stringNameValue)
                    {
                        controlName = stringNameValue.Value;
                    }

                    // Identify common UI patterns based on control type and naming patterns
                    if (Regex.IsMatch(controlName, ".*Screen$", RegexOptions.IgnoreCase))
                    {
                        return StringValue.New("Screen");
                    }
                    else if (controlType.Equals("button", StringComparison.OrdinalIgnoreCase) ||
                             Regex.IsMatch(controlName, ".*btn.*|.*button.*", RegexOptions.IgnoreCase))
                    {
                        return StringValue.New("Button");
                    }
                    else if (Regex.IsMatch(controlType, "text|input", RegexOptions.IgnoreCase) ||
                             Regex.IsMatch(controlName, ".*text.*|.*input.*", RegexOptions.IgnoreCase))
                    {
                        return StringValue.New("TextInput");
                    }
                    else if (Regex.IsMatch(controlType, "gallery|collection", RegexOptions.IgnoreCase) ||
                             Regex.IsMatch(controlName, ".*gallery.*|.*list.*", RegexOptions.IgnoreCase))
                    {
                        return StringValue.New("Gallery");
                    }
                    else if (Regex.IsMatch(controlType, "form", RegexOptions.IgnoreCase) ||
                             Regex.IsMatch(controlName, ".*form.*", RegexOptions.IgnoreCase))
                    {
                        return StringValue.New("Form");
                    }
                    else if (Regex.IsMatch(controlType, "dropdown|combo", RegexOptions.IgnoreCase) ||
                             Regex.IsMatch(controlName, ".*dropdown.*|.*combo.*", RegexOptions.IgnoreCase))
                    {
                        return StringValue.New("Dropdown");
                    }
                    else if (Regex.IsMatch(controlType, "toggle|checkbox", RegexOptions.IgnoreCase) ||
                             Regex.IsMatch(controlName, ".*toggle.*|.*check.*", RegexOptions.IgnoreCase))
                    {
                        return StringValue.New("Toggle");
                    }
                    else if (Regex.IsMatch(controlType, "date", RegexOptions.IgnoreCase) ||
                             Regex.IsMatch(controlName, ".*date.*|.*calendar.*", RegexOptions.IgnoreCase))
                    {
                        return StringValue.New("DatePicker");
                    }
                    else
                    {
                        return StringValue.New("Other");
                    }
                }
                catch (Exception)
                {
                    return StringValue.New("Unknown");
                }
            }
        }

        /// <summary>
        /// Detects navigation patterns in Canvas Apps
        /// </summary>
        public class DetectNavigationPatternFunction : ReflectionFunction
        {
            private const string FunctionName = "DetectNavigationPattern";

            public DetectNavigationPatternFunction()
                : base(DPath.Root, FunctionName, StringType.String, StringType.String)
            {
            }

            public StringValue Execute(StringValue formula)
            {
                try
                {
                    string formulaText = formula.Value;

                    if (string.IsNullOrEmpty(formulaText))
                    {
                        return StringValue.New("Unknown");
                    }

                    // Detect navigation patterns
                    if (Regex.IsMatch(formulaText, @"Navigate\s*\(\s*[\w""']+\s*,\s*[\w""']+", RegexOptions.IgnoreCase))
                    {
                        return StringValue.New("ScreenNavigation");
                    }
                    else if (Regex.IsMatch(formulaText, @"Back\s*\(", RegexOptions.IgnoreCase))
                    {
                        return StringValue.New("BackNavigation");
                    }
                    else if (Regex.IsMatch(formulaText, @"NewForm\s*\(|EditForm\s*\(|ViewForm\s*\(", RegexOptions.IgnoreCase))
                    {
                        return StringValue.New("FormNavigation");
                    }
                    else if (Regex.IsMatch(formulaText, @"Launch\s*\(", RegexOptions.IgnoreCase))
                    {
                        return StringValue.New("ExternalNavigation");
                    }
                    else if (Regex.IsMatch(formulaText, @"SubmitForm\s*\(", RegexOptions.IgnoreCase))
                    {
                        return StringValue.New("FormSubmission");
                    }
                    else
                    {
                        return StringValue.New("Other");
                    }
                }
                catch (Exception)
                {
                    return StringValue.New("Unknown");
                }
            }
        }

        /// <summary>
        /// Analyzes Canvas App formulas to detect data operations
        /// </summary>
        public class AnalyzeDataOperationFunction : ReflectionFunction
        {
            private const string FunctionName = "AnalyzeDataOperation";

            public AnalyzeDataOperationFunction()
                : base(DPath.Root, FunctionName, StringType.String, StringType.String)
            {
            }

            public StringValue Execute(StringValue formula)
            {
                try
                {
                    string formulaText = formula.Value;

                    if (string.IsNullOrEmpty(formulaText))
                    {
                        return StringValue.New("Unknown");
                    }

                    // Detect data operations
                    if (Regex.IsMatch(formulaText, @"Patch\s*\(", RegexOptions.IgnoreCase))
                    {
                        return StringValue.New("Update");
                    }
                    else if (Regex.IsMatch(formulaText, @"Remove\s*\(|RemoveIf\s*\(", RegexOptions.IgnoreCase))
                    {
                        return StringValue.New("Delete");
                    }
                    else if (Regex.IsMatch(formulaText, @"Collect\s*\(", RegexOptions.IgnoreCase))
                    {
                        return StringValue.New("Create");
                    }
                    else if (Regex.IsMatch(formulaText, @"Filter\s*\(|Search\s*\(|LookUp\s*\(", RegexOptions.IgnoreCase))
                    {
                        return StringValue.New("Query");
                    }
                    else if (Regex.IsMatch(formulaText, @"Sort\s*\(|SortByColumns\s*\(", RegexOptions.IgnoreCase))
                    {
                        return StringValue.New("Sort");
                    }
                    else if (Regex.IsMatch(formulaText, @"Sum\s*\(|Average\s*\(|Min\s*\(|Max\s*\(|Count\s*\(", RegexOptions.IgnoreCase))
                    {
                        return StringValue.New("Aggregate");
                    }
                    else
                    {
                        return StringValue.New("Other");
                    }
                }
                catch (Exception)
                {
                    return StringValue.New("Unknown");
                }
            }
        }
    }
}
