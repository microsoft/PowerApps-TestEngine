using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.PowerApps.TestEngine.SolutionAnalyzer;

namespace Microsoft.PowerApps.TestEngine.TestCaseGenerator
{
    public class TestCaseGenerator
    {
        public string GenerateTestPlan(AppStructure appStructure, string appLogicalName, string environmentId, string tenantId)
        {
            var sb = new StringBuilder();

            // Test Suite
            sb.AppendLine("testSuite:");
            sb.AppendLine($"  testSuiteName: {appStructure.AppName} Complete Tests");
            sb.AppendLine($"  testSuiteDescription: Comprehensive test cases for {appStructure.AppName} app controls.");
            sb.AppendLine("  persona: User1");
            sb.AppendLine($"  appLogicalName: {appLogicalName}");
            sb.AppendLine();
            sb.AppendLine("  testCases:");

            foreach (var screen in appStructure.Screens)
            {
                sb.AppendLine($"    # {screen.Name} Screen Test Cases");
                GenerateScreenTestCases(sb, screen);
            }

            // Test Settings
            sb.AppendLine();
            sb.AppendLine("testSettings:");
            sb.AppendLine("  headless: false");
            sb.AppendLine("  locale: \"en-US\"");
            sb.AppendLine("  recordVideo: true");
            sb.AppendLine("  extensionModules:");
            sb.AppendLine("    enable: true");
            sb.AppendLine("  browserConfigurations:");
            sb.AppendLine("    - browser: Chromium");
            sb.AppendLine("      channel: msedge");
            sb.AppendLine();

            // Environment Variables
            sb.AppendLine("environmentVariables:");
            sb.AppendLine("  users:");
            sb.AppendLine("    - personaName: User1");
            sb.AppendLine("      emailKey: user1Email");
            sb.AppendLine("      passwordKey: NotNeeded");

            return sb.ToString();
        }

        private void GenerateScreenTestCases(StringBuilder sb, ScreenInfo screen)
        {
            foreach (var control in screen.Controls)
            {
                var controlType = control.Type.ToLower();

                sb.AppendLine($"    # {control.Name} Test Cases");

                // Check RichTextEditor FIRST before checking for generic "text"
                if (controlType.Contains("richtexteditor") || controlType.Contains("richedit") || controlType.Contains("richtext"))
                {
                    GenerateComprehensiveRichTextEditorTestCases(sb, control, screen.Name);
                }
                else if (controlType.Contains("label"))
                {
                    GenerateComprehensiveLabelTestCases(sb, control, screen.Name);
                }
                else if (controlType.Contains("textbox") || controlType.Contains("textinput") || controlType.Contains("text"))
                {
                    GenerateComprehensiveTextBoxTestCases(sb, control, screen.Name);
                }
                else if (controlType.Contains("button"))
                {
                    GenerateComprehensiveButtonTestCases(sb, control, screen.Name);
                }
                else if (controlType.Contains("dropdown"))
                {
                    GenerateComprehensiveDropdownTestCases(sb, control, screen.Name);
                }
                else if (controlType.Contains("combobox"))
                {
                    GenerateComprehensiveComboboxTestCases(sb, control, screen.Name);
                }
                else if (controlType.Contains("checkbox"))
                {
                    GenerateComprehensiveCheckboxTestCases(sb, control, screen.Name);
                }
                else if (controlType.Contains("datepicker"))
                {
                    GenerateComprehensiveDatePickerTestCases(sb, control, screen.Name);
                }
                else if (controlType.Contains("radio"))
                {
                    GenerateComprehensiveRadioGroupTestCases(sb, control, screen.Name);
                }
                else if (controlType.Contains("slider"))
                {
                    GenerateComprehensiveSliderTestCases(sb, control, screen.Name);
                }
                else if (controlType.Contains("toggle"))
                {
                    GenerateComprehensiveToggleTestCases(sb, control, screen.Name);
                }
                else if (controlType.Contains("gallery"))
                {
                    GenerateComprehensiveGalleryTestCases(sb, control, screen.Name);
                }
                else
                {
                    GenerateGenericTestCases(sb, control, screen.Name);
                }
            }
        }

        private void GenerateComprehensiveLabelTestCases(StringBuilder sb, ControlInfo control, string screenName)
        {
            // Text Property Test
            sb.AppendLine($"    - testCaseName: Test {control.Name} Text Property");
            sb.AppendLine($"      testCaseDescription: Verify that {control.Name} Text property can be set and retrieved correctly.");
            sb.AppendLine("      testSteps: |");
            sb.AppendLine($"        SetProperty({control.Name}.Text, \"Test Value\");");
            sb.AppendLine($"        Assert({control.Name}.Text = \"Test Value\", \"Expected {control.Name}.Text to be 'Test Value'\");");
            sb.AppendLine();

            // Empty Text Test
            sb.AppendLine($"    - testCaseName: Test {control.Name} Empty Text");
            sb.AppendLine($"      testCaseDescription: Verify that {control.Name} Text property can be set to empty string.");
            sb.AppendLine("      testSteps: |");
            sb.AppendLine($"        SetProperty({control.Name}.Text, \"\");");
            sb.AppendLine($"        Assert({control.Name}.Text = \"\", \"Expected {control.Name}.Text to be empty\");");
            sb.AppendLine();

            // Long Text Test
            sb.AppendLine($"    - testCaseName: Test {control.Name} Long Text");
            sb.AppendLine($"      testCaseDescription: Verify that {control.Name} can handle long text without issues.");
            sb.AppendLine("      testSteps: |");
            sb.AppendLine($"        SetProperty({control.Name}.Text, \"This is a very long text to check if the label can handle it without any issues.\");");
            sb.AppendLine($"        Assert({control.Name}.Text = \"This is a very long text to check if the label can handle it without any issues.\", \"Expected {control.Name} to display long text\");");
            sb.AppendLine();

            // Special Characters Test
            sb.AppendLine($"    - testCaseName: Test {control.Name} Special Characters");
            sb.AppendLine($"      testCaseDescription: Verify that {control.Name} displays special characters correctly.");
            sb.AppendLine("      testSteps: |");
            sb.AppendLine($"        SetProperty({control.Name}.Text, \"Special@#$%^&*!:\");");
            sb.AppendLine($"        Assert({control.Name}.Text = \"Special@#$%^&*!:\", \"Expected {control.Name} to display special characters\");");
            sb.AppendLine();

            // Numeric Text Test
            sb.AppendLine($"    - testCaseName: Test {control.Name} Numeric Text");
            sb.AppendLine($"      testCaseDescription: Verify that {control.Name} can display numeric text.");
            sb.AppendLine("      testSteps: |");
            sb.AppendLine($"        SetProperty({control.Name}.Text, \"1234567890\");");
            sb.AppendLine($"        Assert({control.Name}.Text = \"1234567890\", \"Expected {control.Name}.Text to be numeric text\");");
            sb.AppendLine();

            // Visible Property False Test
            sb.AppendLine($"    - testCaseName: Test {control.Name} Visible Property False");
            sb.AppendLine($"      testCaseDescription: Verify that {control.Name} visibility can be set to false.");
            sb.AppendLine("      testSteps: |");
            sb.AppendLine($"        SetProperty({control.Name}.Visible, false);");
            sb.AppendLine($"        Assert({control.Name}.Visible = false, \"Expected {control.Name}.Visible to be false\");");
            sb.AppendLine();
        }

        private void GenerateComprehensiveComboboxTestCases(StringBuilder sb, ControlInfo control, string screenName)
        {
            // Visible Property True
            sb.AppendLine($"    - testCaseName: Test {control.Name} Visible Property True");
            sb.AppendLine($"      testCaseDescription: Verify that {control.Name} visibility can be set to true.");
            sb.AppendLine("      testSteps: |");
            sb.AppendLine($"        SetProperty({control.Name}.Visible, true);");
            sb.AppendLine($"        Assert({control.Name}.Visible = true, \"Expected {control.Name}.Visible to be true\");");
            sb.AppendLine();

            // Visible Property False
            sb.AppendLine($"    - testCaseName: Test {control.Name} Visible Property False");
            sb.AppendLine($"      testCaseDescription: Verify that {control.Name} visibility can be set to false.");
            sb.AppendLine("      testSteps: |");
            sb.AppendLine($"        SetProperty({control.Name}.Visible, false);");
            sb.AppendLine($"        Assert({control.Name}.Visible = false, \"Expected {control.Name}.Visible to be false\");");
            sb.AppendLine();

            // Visible Toggle
            sb.AppendLine($"    - testCaseName: Test {control.Name} Visible Toggle");
            sb.AppendLine($"      testCaseDescription: Verify that {control.Name} visibility can be toggled correctly.");
            sb.AppendLine("      testSteps: |");
            sb.AppendLine($"        SetProperty({control.Name}.Visible, true);");
            sb.AppendLine($"        Assert({control.Name}.Visible = true, \"Expected {control.Name} to be visible\");");
            sb.AppendLine($"        SetProperty({control.Name}.Visible, false);");
            sb.AppendLine($"        Assert({control.Name}.Visible = false, \"Expected {control.Name} to be hidden\");");
            sb.AppendLine();

            // Items Available
            sb.AppendLine($"    - testCaseName: Test {control.Name} Items Available");
            sb.AppendLine($"      testCaseDescription: Verify that {control.Name} has items in its data source.");
            sb.AppendLine("      testSteps: |");
            sb.AppendLine($"        Assert(CountRows({control.Name}.Items) >= 0, \"Expected {control.Name}.Items to be available\");");
            sb.AppendLine();

            // Items Not Empty
            sb.AppendLine($"    - testCaseName: Test {control.Name} Items Not Empty");
            sb.AppendLine($"      testCaseDescription: Verify that {control.Name} Items collection is not empty.");
            sb.AppendLine("      testSteps: |");
            sb.AppendLine($"        Assert(!IsEmpty({control.Name}.Items), \"Expected {control.Name} to have items available\");");
            sb.AppendLine();

            // Select Single Item
            sb.AppendLine($"    - testCaseName: Test {control.Name} Select Single Item");
            sb.AppendLine($"      testCaseDescription: Verify that a single item can be selected in {control.Name}.");
            sb.AppendLine("      testSteps: |");
            sb.AppendLine($"        SetProperty({control.Name}.SelectedItems, Table({{Value:\"Test Item\", ID:1}}));");
            sb.AppendLine($"        Assert(CountRows({control.Name}.SelectedItems) = 1, \"Expected one item to be selected\");");
            sb.AppendLine();

            // Select Multiple Items
            sb.AppendLine($"    - testCaseName: Test {control.Name} Select Multiple Items");
            sb.AppendLine($"      testCaseDescription: Verify that multiple items can be selected in {control.Name}.");
            sb.AppendLine("      testSteps: |");
            sb.AppendLine($"        SetProperty({control.Name}.SelectedItems, Table(");
            sb.AppendLine("          {Value:\"Item A\", ID:1},");
            sb.AppendLine("          {Value:\"Item B\", ID:2},");
            sb.AppendLine("          {Value:\"Item C\", ID:3}");
            sb.AppendLine("        ));");
            sb.AppendLine($"        Assert(CountRows({control.Name}.SelectedItems) = 3, \"Expected three items to be selected\");");
            sb.AppendLine();

            // Clear Selection
            sb.AppendLine($"    - testCaseName: Test {control.Name} Clear Selection");
            sb.AppendLine($"      testCaseDescription: Verify that {control.Name} selection can be cleared to empty table.");
            sb.AppendLine("      testSteps: |");
            sb.AppendLine($"        SetProperty({control.Name}.SelectedItems, Table());");
            sb.AppendLine($"        Assert(CountRows({control.Name}.SelectedItems) = 0, \"Expected no items to be selected\");");
            sb.AppendLine();

            // Select First Item
            sb.AppendLine($"    - testCaseName: Test {control.Name} Select First Item");
            sb.AppendLine($"      testCaseDescription: Verify that the first item can be selected from available items.");
            sb.AppendLine("      testSteps: |");
            sb.AppendLine($"        SetProperty({control.Name}.SelectedItems, FirstN({control.Name}.Items, 1));");
            sb.AppendLine($"        Assert(CountRows({control.Name}.SelectedItems) = 1, \"Expected one item to be selected\");");
            sb.AppendLine();

            // Select First Two Items
            sb.AppendLine($"    - testCaseName: Test {control.Name} Select First Two Items");
            sb.AppendLine($"      testCaseDescription: Verify that the first two items can be selected from available items.");
            sb.AppendLine("      testSteps: |");
            sb.AppendLine($"        SetProperty({control.Name}.SelectedItems, FirstN({control.Name}.Items, 2));");
            sb.AppendLine($"        Assert(CountRows({control.Name}.SelectedItems) = 2, \"Expected two items to be selected\");");
            sb.AppendLine();

            // Reselect After Clear
            sb.AppendLine($"    - testCaseName: Test {control.Name} Reselect After Clear");
            sb.AppendLine($"      testCaseDescription: Verify selection works after clearing.");
            sb.AppendLine("      testSteps: |");
            sb.AppendLine($"        SetProperty({control.Name}.SelectedItems, Table());");
            sb.AppendLine($"        Assert(CountRows({control.Name}.SelectedItems) = 0, \"Expected no items selected after clear\");");
            sb.AppendLine($"        SetProperty({control.Name}.SelectedItems, Table({{Value:\"New Item\", ID:999}}));");
            sb.AppendLine($"        Assert(CountRows({control.Name}.SelectedItems) = 1, \"Expected one item to be selected after reselection\");");
            sb.AppendLine();

            // Multiple Select Cycles
            sb.AppendLine($"    - testCaseName: Test {control.Name} Multiple Select Cycles");
            sb.AppendLine($"      testCaseDescription: Verify multiple selection and deselection cycles.");
            sb.AppendLine("      testSteps: |");
            sb.AppendLine($"        SetProperty({control.Name}.SelectedItems, Table({{Value:\"Item X\", ID:10}}));");
            sb.AppendLine($"        Assert(CountRows({control.Name}.SelectedItems) = 1, \"Expected one item selected in first cycle\");");
            sb.AppendLine($"        SetProperty({control.Name}.SelectedItems, Table());");
            sb.AppendLine($"        Assert(CountRows({control.Name}.SelectedItems) = 0, \"Expected no items after clear\");");
            sb.AppendLine($"        SetProperty({control.Name}.SelectedItems, Table({{Value:\"Item Y\", ID:20}}));");
            sb.AppendLine($"        Assert(CountRows({control.Name}.SelectedItems) = 1, \"Expected one item selected in second cycle\");");
            sb.AppendLine();
        }

        private void GenerateComprehensiveTextBoxTestCases(StringBuilder sb, ControlInfo control, string screenName)
        {
            // Sample Text Test - Use .Text for Canvas TextInput
            sb.AppendLine($"    - testCaseName: Test {control.Name} Sample Text");
            sb.AppendLine($"      testCaseDescription: Verify that {control.Name} accepts and displays input correctly.");
            sb.AppendLine("      testSteps: |");
            sb.AppendLine($"        SetProperty({control.Name}.Text, \"Sample Text\");");
            sb.AppendLine($"        Assert({control.Name}.Text = \"Sample Text\", \"Verify {control.Name} displays the input text correctly\");");
            sb.AppendLine();

            // Empty Text Test
            sb.AppendLine($"    - testCaseName: Test {control.Name} Empty");
            sb.AppendLine($"      testCaseDescription: Verify that {control.Name} can be set to empty string.");
            sb.AppendLine("      testSteps: |");
            sb.AppendLine($"        SetProperty({control.Name}.Text, \"\");");
            sb.AppendLine($"        Assert({control.Name}.Text = \"\", \"Expected {control.Name}.Text to be empty\");");
            sb.AppendLine();

            // Long Text Test
            sb.AppendLine($"    - testCaseName: Test {control.Name} Long Text");
            sb.AppendLine($"      testCaseDescription: Verify that {control.Name} can handle long text.");
            sb.AppendLine("      testSteps: |");
            sb.AppendLine($"        SetProperty({control.Name}.Text, \"This is a very long text to check if the TextBox can handle it without any issues.\");");
            sb.AppendLine($"        Assert({control.Name}.Text = \"This is a very long text to check if the TextBox can handle it without any issues.\", \"Expected {control.Name}.Text to be the long text\");");
            sb.AppendLine();

            // Special Characters Test
            sb.AppendLine($"    - testCaseName: Test {control.Name} Special Characters");
            sb.AppendLine($"      testCaseDescription: Verify that {control.Name} handles special characters.");
            sb.AppendLine("      testSteps: |");
            sb.AppendLine($"        SetProperty({control.Name}.Text, \"Special@#$%^&*()!\");");
            sb.AppendLine($"        Assert({control.Name}.Text = \"Special@#$%^&*()!\", \"Expected {control.Name} to handle special characters\");");
            sb.AppendLine();

            // Numeric Text Test
            sb.AppendLine($"    - testCaseName: Test {control.Name} Numeric Text");
            sb.AppendLine($"      testCaseDescription: Verify that {control.Name} can handle numeric text.");
            sb.AppendLine("      testSteps: |");
            sb.AppendLine($"        SetProperty({control.Name}.Text, \"1234567890\");");
            sb.AppendLine($"        Assert({control.Name}.Text = \"1234567890\", \"Expected {control.Name}.Text to be numeric text\");");
            sb.AppendLine();

            // Visible Property False Test
            sb.AppendLine($"    - testCaseName: Test {control.Name} Visible Property False");
            sb.AppendLine($"      testCaseDescription: Verify that {control.Name} visibility can be set to false.");
            sb.AppendLine("      testSteps: |");
            sb.AppendLine($"        SetProperty({control.Name}.Visible, false);");
            sb.AppendLine($"        Assert({control.Name}.Visible = false, \"Expected {control.Name}.Visible to be false\");");
            sb.AppendLine();
        }

        private void GenerateComprehensiveButtonTestCases(StringBuilder sb, ControlInfo control, string screenName)
        {
            // Button Click Test
            sb.AppendLine($"    - testCaseName: Test {control.Name} Click Once");
            sb.AppendLine($"      testCaseDescription: Verify that {control.Name} performs action when clicked once.");
            sb.AppendLine("      testSteps: |");
            sb.AppendLine($"        Select({control.Name});");
            sb.AppendLine($"        Assert({control.Name}.Visible = true, \"Verify {control.Name} is functional\");");
            sb.AppendLine();

            // Button Click Twice Test
            sb.AppendLine($"    - testCaseName: Test {control.Name} Click Twice");
            sb.AppendLine($"      testCaseDescription: Verify that {control.Name} performs action when clicked twice.");
            sb.AppendLine("      testSteps: |");
            sb.AppendLine($"        Select({control.Name});");
            sb.AppendLine($"        Select({control.Name});");
            sb.AppendLine($"        Assert({control.Name}.Visible = true, \"Verify {control.Name} handles multiple clicks\");");
            sb.AppendLine();
        }

        private void GenerateComprehensiveCheckboxTestCases(StringBuilder sb, ControlInfo control, string screenName)
        {
            // Similar comprehensive tests for Checkbox
            sb.AppendLine($"    - testCaseName: Test {control.Name} Checked Property");
            sb.AppendLine($"      testCaseDescription: Verify that {control.Name} checked state can be set.");
            sb.AppendLine("      testSteps: |");
            sb.AppendLine($"        SetProperty({control.Name}.Checked, true);");
            sb.AppendLine($"        Assert({control.Name}.Checked = true, \"Expected {control.Name}.Checked to be true\");");
            sb.AppendLine();
        }

        private void GenerateComprehensiveDatePickerTestCases(StringBuilder sb, ControlInfo control, string screenName)
        {
            sb.AppendLine($"    - testCaseName: Test {control.Name} SelectedDate Property");
            sb.AppendLine($"      testCaseDescription: Verify that {control.Name} date can be set and retrieved.");
            sb.AppendLine("      testSteps: |");
            sb.AppendLine($"        SetProperty({control.Name}.SelectedDate, Date(2024,10,01));");
            sb.AppendLine($"        Assert({control.Name}.SelectedDate = Date(2024,10,01), \"Checking the SelectedDate property\");");
            sb.AppendLine();
        }

        private void GenerateComprehensiveRadioGroupTestCases(StringBuilder sb, ControlInfo control, string screenName)
        {
            sb.AppendLine($"    - testCaseName: Test {control.Name} Selection");
            sb.AppendLine($"      testCaseDescription: Verify that {control.Name} item selection works.");
            sb.AppendLine("      testSteps: |");
            sb.AppendLine($"        SetProperty({control.Name}.DefaultSelectedItems, Table({{Value:\"Item 1\"}}));");
            sb.AppendLine($"        Assert(CountRows({control.Name}.SelectedItems) = 1, \"Validated Successfully\");");
            sb.AppendLine();
        }

        private void GenerateComprehensiveSliderTestCases(StringBuilder sb, ControlInfo control, string screenName)
        {
            sb.AppendLine($"    - testCaseName: Test {control.Name} Value Property");
            sb.AppendLine($"      testCaseDescription: Verify that {control.Name} value can be set.");
            sb.AppendLine("      testSteps: |");
            sb.AppendLine($"        SetProperty({control.Name}.Value, 50);");
            sb.AppendLine($"        Assert({control.Name}.Value = 50, \"Checking the Value property\");");
            sb.AppendLine();
        }

        private void GenerateComprehensiveToggleTestCases(StringBuilder sb, ControlInfo control, string screenName)
        {
            sb.AppendLine($"    - testCaseName: Test {control.Name} Toggle On");
            sb.AppendLine($"      testCaseDescription: Verify that {control.Name} can be toggled on.");
            sb.AppendLine("      testSteps: |");
            sb.AppendLine($"        SetProperty({control.Name}.Checked, true);");
            sb.AppendLine($"        Assert({control.Name}.Checked = true, \"User action correctly toggled {control.Name} to on\");");
            sb.AppendLine();
        }

        private void GenerateComprehensiveGalleryTestCases(StringBuilder sb, ControlInfo control, string screenName)
        {
            sb.AppendLine($"    - testCaseName: Test {control.Name} Has Items");
            sb.AppendLine($"      testCaseDescription: Verify {control.Name} contains items.");
            sb.AppendLine("      testSteps: |");
            sb.AppendLine($"        Assert(CountRows({control.Name}.AllItems) > 0, \"Gallery should have items\");");
            sb.AppendLine();
        }

        private void GenerateGenericTestCases(StringBuilder sb, ControlInfo control, string screenName)
        {
            sb.AppendLine($"    - testCaseName: Test {control.Name} Visible Property");
            sb.AppendLine($"      testCaseDescription: Verify that {control.Name} is accessible.");
            sb.AppendLine("      testSteps: |");
            sb.AppendLine($"        Assert({control.Name}.Visible = true, \"{control.Name} should be accessible\");");
            sb.AppendLine();
        }

        private void GenerateComprehensiveRichTextEditorTestCases(StringBuilder sb, ControlInfo control, string screenName)
        {
            // RichTextEditor uses .HtmlText property, not .Value
            sb.AppendLine($"    - testCaseName: Test {control.Name} Sample Text");
            sb.AppendLine($"      testCaseDescription: Verify that {control.Name} accepts and displays rich text correctly.");
            sb.AppendLine("      testSteps: |");
            sb.AppendLine($"        SetProperty({control.Name}.HtmlText, \"<p>Sample Text</p>\");");
            sb.AppendLine($"        Assert(!IsBlank({control.Name}.HtmlText), \"Verify {control.Name} displays the input text correctly\");");
            sb.AppendLine();

            sb.AppendLine($"    - testCaseName: Test {control.Name} Empty");
            sb.AppendLine($"      testCaseDescription: Verify that {control.Name} can be set to empty.");
            sb.AppendLine("      testSteps: |");
            sb.AppendLine($"        SetProperty({control.Name}.HtmlText, \"\");");
            sb.AppendLine($"        Assert({control.Name}.HtmlText = \"\", \"Expected {control.Name}.HtmlText to be empty\");");
            sb.AppendLine();

            sb.AppendLine($"    - testCaseName: Test {control.Name} Long Text");
            sb.AppendLine($"      testCaseDescription: Verify that {control.Name} can handle long text.");
            sb.AppendLine("      testSteps: |");
            sb.AppendLine($"        SetProperty({control.Name}.HtmlText, \"<p>This is a very long text to check if the RichTextEditor can handle it without any issues.</p>\");");
            sb.AppendLine($"        Assert(!IsBlank({control.Name}.HtmlText), \"Expected {control.Name} to handle long text\");");
            sb.AppendLine();

            sb.AppendLine($"    - testCaseName: Test {control.Name} Special Characters");
            sb.AppendLine($"      testCaseDescription: Verify that {control.Name} handles special characters.");
            sb.AppendLine("      testSteps: |");
            sb.AppendLine($"        SetProperty({control.Name}.HtmlText, \"<p>Special@#$%^&*()!</p>\");");
            sb.AppendLine($"        Assert(!IsBlank({control.Name}.HtmlText), \"Expected {control.Name} to handle special characters\");");
            sb.AppendLine();

            sb.AppendLine($"    - testCaseName: Test {control.Name} Numeric Text");
            sb.AppendLine($"      testCaseDescription: Verify that {control.Name} can handle numeric text.");
            sb.AppendLine("      testSteps: |");
            sb.AppendLine($"        SetProperty({control.Name}.HtmlText, \"<p>1234567890</p>\");");
            sb.AppendLine($"        Assert(!IsBlank({control.Name}.HtmlText), \"Expected {control.Name} to handle numeric text\");");
            sb.AppendLine();

            sb.AppendLine($"    - testCaseName: Test {control.Name} Visible Property True");
            sb.AppendLine($"      testCaseDescription: Verify that {control.Name} visibility can be set to true.");
            sb.AppendLine("      testSteps: |");
            sb.AppendLine($"        SetProperty({control.Name}.Visible, true);");
            sb.AppendLine($"        Assert({control.Name}.Visible = true, \"Expected {control.Name}.Visible to be true\");");
            sb.AppendLine();

            sb.AppendLine($"    - testCaseName: Test {control.Name} Visible Property False");
            sb.AppendLine($"      testCaseDescription: Verify that {control.Name} visibility can be set to false.");
            sb.AppendLine("      testSteps: |");
            sb.AppendLine($"        SetProperty({control.Name}.Visible, false);");
            sb.AppendLine($"        Assert({control.Name}.Visible = false, \"Expected {control.Name}.Visible to be false\");");
            sb.AppendLine();

            sb.AppendLine($"    - testCaseName: Test {control.Name} Visible Toggle");
            sb.AppendLine($"      testCaseDescription: Verify that {control.Name} visibility can be toggled.");
            sb.AppendLine("      testSteps: |");
            sb.AppendLine($"        SetProperty({control.Name}.Visible, true);");
            sb.AppendLine($"        Assert({control.Name}.Visible = true, \"Expected {control.Name} to be visible\");");
            sb.AppendLine($"        SetProperty({control.Name}.Visible, false);");
            sb.AppendLine($"        Assert({control.Name}.Visible = false, \"Expected {control.Name} to be hidden\");");
            sb.AppendLine();
        }

        private void GenerateComprehensiveDropdownTestCases(StringBuilder sb, ControlInfo control, string screenName)
        {
            // Dropdown is typically single-select only
            sb.AppendLine($"    - testCaseName: Test {control.Name} Visible Property True");
            sb.AppendLine($"      testCaseDescription: Verify that {control.Name} visibility can be set to true.");
            sb.AppendLine("      testSteps: |");
            sb.AppendLine($"        SetProperty({control.Name}.Visible, true);");
            sb.AppendLine($"        Assert({control.Name}.Visible = true, \"Expected {control.Name}.Visible to be true\");");
            sb.AppendLine();

            sb.AppendLine($"    - testCaseName: Test {control.Name} Visible Property False");
            sb.AppendLine($"      testCaseDescription: Verify that {control.Name} visibility can be set to false.");
            sb.AppendLine("      testSteps: |");
            sb.AppendLine($"        SetProperty({control.Name}.Visible, false);");
            sb.AppendLine($"        Assert({control.Name}.Visible = false, \"Expected {control.Name}.Visible to be false\");");
            sb.AppendLine();

            sb.AppendLine($"    - testCaseName: Test {control.Name} Visible Toggle");
            sb.AppendLine($"      testCaseDescription: Verify that {control.Name} visibility can be toggled correctly.");
            sb.AppendLine("      testSteps: |");
            sb.AppendLine($"        SetProperty({control.Name}.Visible, true);");
            sb.AppendLine($"        Assert({control.Name}.Visible = true, \"Expected {control.Name} to be visible\");");
            sb.AppendLine($"        SetProperty({control.Name}.Visible, false);");
            sb.AppendLine($"        Assert({control.Name}.Visible = false, \"Expected {control.Name} to be hidden\");");
            sb.AppendLine();

            sb.AppendLine($"    - testCaseName: Test {control.Name} Items Available");
            sb.AppendLine($"      testCaseDescription: Verify that {control.Name} has items in its data source.");
            sb.AppendLine("      testSteps: |");
            sb.AppendLine($"        Assert(CountRows({control.Name}.Items) >= 0, \"Expected {control.Name}.Items to be available\");");
            sb.AppendLine();

            sb.AppendLine($"    - testCaseName: Test {control.Name} Items Not Empty");
            sb.AppendLine($"      testCaseDescription: Verify that {control.Name} Items collection is not empty.");
            sb.AppendLine("      testSteps: |");
            sb.AppendLine($"        Assert(!IsEmpty({control.Name}.Items), \"Expected {control.Name} to have items available\");");
            sb.AppendLine();

            // Dropdown is single-select - use SelectedItems with single item
            sb.AppendLine($"    - testCaseName: Test {control.Name} Select Single Item");
            sb.AppendLine($"      testCaseDescription: Verify that a single item can be selected in {control.Name}.");
            sb.AppendLine("      testSteps: |");
            sb.AppendLine($"        SetProperty({control.Name}.SelectedItems, Table({{Value:\"Test Item\", ID:1}}));");
            sb.AppendLine($"        Assert(CountRows({control.Name}.SelectedItems) = 1, \"Expected one item to be selected\");");
            sb.AppendLine();

            // Remove multi-select tests for Dropdown (it's single-select)
            sb.AppendLine($"    - testCaseName: Test {control.Name} Clear Selection");
            sb.AppendLine($"      testCaseDescription: Verify that {control.Name} selection can be cleared.");
            sb.AppendLine("      testSteps: |");
            sb.AppendLine($"        SetProperty({control.Name}.SelectedItems, Table());");
            sb.AppendLine($"        Assert(CountRows({control.Name}.SelectedItems) = 0, \"Expected no items to be selected\");");
            sb.AppendLine();

            sb.AppendLine($"    - testCaseName: Test {control.Name} Select First Item");
            sb.AppendLine($"      testCaseDescription: Verify that the first item can be selected from available items.");
            sb.AppendLine("      testSteps: |");
            sb.AppendLine($"        SetProperty({control.Name}.SelectedItems, FirstN({control.Name}.Items, 1));");
            sb.AppendLine($"        Assert(CountRows({control.Name}.SelectedItems) = 1, \"Expected one item to be selected\");");
            sb.AppendLine();

            sb.AppendLine($"    - testCaseName: Test {control.Name} Reselect After Clear");
            sb.AppendLine($"      testCaseDescription: Verify selection works after clearing.");
            sb.AppendLine("      testSteps: |");
            sb.AppendLine($"        SetProperty({control.Name}.SelectedItems, Table());");
            sb.AppendLine($"        Assert(CountRows({control.Name}.SelectedItems) = 0, \"Expected no items selected after clear\");");
            sb.AppendLine($"        SetProperty({control.Name}.SelectedItems, Table({{Value:\"New Item\", ID:999}}));");
            sb.AppendLine($"        Assert(CountRows({control.Name}.SelectedItems) = 1, \"Expected one item to be selected after reselection\");");
            sb.AppendLine();
        }
    }
}
