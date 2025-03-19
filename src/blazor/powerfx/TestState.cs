
using System.Dynamic;
using System.Globalization;
using System.Text;
using Microsoft.PowerApps.TestEngine.PowerFx;
using System.Text.RegularExpressions;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Core.Tokens;
using System.Text.Json;
using Microsoft.PowerFx.Syntax;

public class TestState
{
    public string? App { get; private set; }
    public string? Settings { get; private set; }
    public string? Code { get; private set; }
    private RecalcEngine? _engine;
    private CultureInfo Culture = new CultureInfo("en-us");

    public TestState(string input)
    {
        ParseInput(input);
        InitializeEngine();
        ParseAppCode();
    }

    private void ParseInput(string input)
    {
        var lines = input.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
        var appContent = new StringBuilder();
        var settingsContent = new StringBuilder();
        var codeContent = new StringBuilder();

        string currentBlock = "Code";

        foreach (var line in lines)
        {
            if (line.StartsWith("// File:"))
            {
                currentBlock = "App";
                continue;
            }
            if (line.StartsWith("// Settings:"))
            {
                currentBlock = "Settings";
                continue;
            }
            if (line.StartsWith("// Test:"))
            {
                currentBlock = "Code";
                continue;
            }
            if (line.StartsWith("// Code:") && currentBlock != null)
            {
                currentBlock = "Code";
                continue;
            }

            if (currentBlock == "App")
            {
                appContent.AppendLine(line);
            }
            else if (currentBlock == "Settings")
            {
                settingsContent.AppendLine(line);
            }
            else if (currentBlock == "Code")
            {
                codeContent.AppendLine(line);
            }
        }

        App = appContent.ToString();
        Settings = settingsContent.ToString();
        Code = codeContent.ToString();
    }

    private void InitializeEngine()
    {
        var locale = "en-us";
        if ( !string.IsNullOrEmpty(Settings))
        {
            var deserializer = new DeserializerBuilder()
           .WithNamingConvention(CamelCaseNamingConvention.Instance)
           .Build();

            var settings = deserializer.Deserialize<ExpandoObject>(Settings) as IDictionary<string, object>;
            if (settings != null 
                && settings.ContainsKey("locale") 
            ) {
                locale = settings["locale"].ToString();
            }
        }
        Culture = new CultureInfo(locale);

        _engine = PowerFxEngine.Init(out var options, locale);
    }

    private void ParseAppCode()
    {
        if ( string.IsNullOrEmpty(App))
        {
            return;
        }

        var stream = new YamlStream();
        stream.Load(new StringReader(App));

        LoadYamlToEngine(stream.Documents);
    }

    private void UpdateAppCode(string controlName, string propertyName, object value)
    {
        if (string.IsNullOrEmpty(App))
        {
            return;
        }

        var stream = new YamlStream();
        stream.Load(new StringReader(App));

        UpdateYamlControlProperty(stream.Documents, controlName, propertyName, value);

        ParseAppCode();
    }

    private void UpdateYamlControlProperty(IList<YamlDocument> documents, string controlName, string propertyName, object value)
    {
        foreach (var document in documents)
        {
            if (document.RootNode is YamlMappingNode rootProperties)
            {
                foreach (var rootProperty in rootProperties)
                {
                    if (rootProperty.Key.ToString() == "Controls"
                        && rootProperty.Value is YamlMappingNode controls)
                    {
                        foreach (var control in controls)
                        {
                            if (control.Key is YamlScalarNode controlKey
                                && controlKey.Value == controlName
                                && control.Value is YamlMappingNode controlValues
                               )
                            {
                                var fields = new List<NamedValue>();
                                foreach (var property in controlValues.Children)
                                {
                                    if (property.Key.ToString() == propertyName && property.Value is YamlScalarNode valueNode)
                                    {
                                        var left = App.Substring(0, (int)valueNode.Start.Index);
                                        var strValue = "";
                                        if (value is string)
                                        {
                                            strValue = $"= \"{value}\"";
                                        }
                                        else
                                        {
                                            strValue = $"= {value}";
                                        }
                                        var right = App.Substring((int)valueNode.End.Index);
                                        App = left + strValue + right;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private void LoadYamlToEngine(IList<YamlDocument> documents)
    {
        foreach (var document in documents)
        {
            if (document.RootNode is YamlMappingNode rootProperties)
            {
                foreach (var rootProperty in rootProperties)
                {
                    HandleProperties(rootProperty.Key, rootProperty.Value);

                    HandleControls(rootProperty.Key, rootProperty.Value);
                } 
            }
        }
    }

    private void HandleProperties(YamlNode key, YamlNode value)
    {
        if (key is YamlScalarNode topPropertyName
            && topPropertyName.Value == "OnStart"
            && value is YamlScalarNode onStartPropertyValue
            && onStartPropertyValue.Value.StartsWith("=")
           )
        {
            PowerFxEngine.Execute(onStartPropertyValue.Value.Substring(1), _engine, new ParserOptions { AllowsSideEffects = true, Culture = Culture, NumberIsFloat = true });
        }

        if (key is YamlScalarNode propertyName
            && propertyName.Value == "Properties"
            && value is YamlMappingNode properties)
        {
            foreach (var property in properties)
            {
                if (property.Key is YamlScalarNode propertyKey
                    && propertyKey.Value == "OnVisible"
                    && property.Value is YamlScalarNode propertyValue
                    && propertyValue.Value != null
                    && propertyValue.Value.StartsWith("=")
                   )
                {
                    PowerFxEngine.Execute(propertyValue.Value.Substring(1), _engine, new ParserOptions { AllowsSideEffects = true, Culture = Culture, NumberIsFloat = true });
                }
            }
        }
    }

    private void HandleControls(YamlNode key, YamlNode value)
    {
        if (key is YamlScalarNode propertyName
            && propertyName.Value == "Controls"
            && value is YamlMappingNode controls)
        {
            foreach (var control in controls)
            {
                if (control.Key is YamlScalarNode controlKey
                    && control.Value is YamlMappingNode controlValues
                   )
                {
                    var fields = new List<NamedValue>();
                    foreach ( var property in controlValues.Children )
                    {
                        
                        if ( property.Key is YamlScalarNode name 
                            && property.Value is YamlScalarNode propertyValue
                            && propertyValue.Value != null
                            && propertyValue.Value.StartsWith("=")
                        )
                        {
                            var evaluatedValue = PowerFxEngine.Execute(propertyValue.Value.Substring(1), _engine, new ParserOptions { AllowsSideEffects = true, Culture = Culture, NumberIsFloat = true });
                            var rawValue = System.Text.Json.JsonDocument.Parse(evaluatedValue);
                            switch (rawValue.RootElement.ValueKind) { 
                                case JsonValueKind.String:
                                    fields.Add(new NamedValue(name.ToString(), FormulaValue.New(rawValue.RootElement.ToString())));
                                    break;
                                case JsonValueKind.Number:
                                    var numValue = rawValue.RootElement.ToString();
                                    if ( int.TryParse(numValue, out int intValue))
                                    {
                                        fields.Add(new NamedValue(name.ToString(), FormulaValue.New(intValue)));
                                    }
                                    if (double.TryParse(numValue, out double doubleValue))
                                    {
                                        fields.Add(new NamedValue(name.ToString(), FormulaValue.New(doubleValue)));
                                    }
                                    break;
                            }
                        }
                    }
                    _engine.UpdateVariable(controlKey.Value, RecordValue.NewRecordFromFields(fields));
                }
            }
        }
    }

    private RecordValue GenerateRecordFromYaml(IDictionary<string, object> yamlDict)
    {
        var fields = new List<NamedValue>();
        foreach (var kvp in yamlDict)
        {
            if (kvp.Value is string strValue)
            {
                if (strValue.StartsWith("="))
                {
                    strValue = PowerFxEngine.Execute(strValue.Substring(1), _engine, new ParserOptions { AllowsSideEffects = true, Culture = Culture, NumberIsFloat = true });
                }
                fields.Add(new NamedValue(kvp.Key, StringValue.New(strValue)));
            }
            else if (kvp.Value is int intValue)
            {
                fields.Add(new NamedValue(kvp.Key, NumberValue.New(intValue)));
            }
            // Add more types as needed
        }

        return RecordValue.NewRecordFromFields(fields.ToArray());
    }

    public string ExecuteCode()
    {
        var options = new ParserOptions { AllowsSideEffects = true, Culture = Culture, NumberIsFloat = true };
        var checkResult = _engine.Check(Code, null, options);
        var splitSteps = PowerFxHelper.ExtractFormulasSeparatedByChainingOperator(_engine, checkResult, Culture);
        string result = "";

        foreach (var step in splitSteps) {
            if (!string.IsNullOrEmpty(step.Trim()))
            {
                result = PowerFxEngine.Execute(step.Trim(), _engine, options);
                Update(step);
            }
        }

        return result;
    }

    public void Update(string code)
    {
        var results = _engine.Parse(code);

        if (results.Root is CallNode callNode 
            && callNode.Head is Identifier headIdentifier
            && headIdentifier.Name == "SetProperty"
            && callNode.Args.ChildNodes[0] is DottedNameNode name
            && name.Right is Identifier identifier)
        {
            if (callNode.Args.ChildNodes[1] is StrLitNode strValue)
            {
                UpdateAppCode(name.Left.ToString(), identifier.Name, strValue.Value);
            }

            if (callNode.Args.ChildNodes[1] is NumLitNode numValue)
            {
                UpdateAppCode(name.Left.ToString(), identifier.Name, numValue.ActualNumValue);
            }
        }
    }
}