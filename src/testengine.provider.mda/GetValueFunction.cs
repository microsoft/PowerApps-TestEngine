// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Providers.PowerFxModel;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
using Newtonsoft.Json.Linq;

namespace testengine.provider.mda
{
    /// <summary>
    /// This will pause the current test and allow the user to interact with the browser and inspect state when headless mode is false
    /// </summary>
    public class GetValueFunction : ReflectionFunction
    {
        private readonly ITestInfraFunctions _testInfraFunctions;
        private readonly ILogger _logger;

        public GetValueFunction(ITestInfraFunctions testInfraFunctions, ILogger logger)
            : base(DPath.Root.Append(new DName("Preview")), "GetValue", FormulaType.UntypedObject, RecordType.Empty())
        {
            _testInfraFunctions = testInfraFunctions;
            _logger = logger;
        }

        public FormulaValue Execute(RecordValue obj)
        {
            return ExecuteAsync(obj).Result;
        }

        public async Task<FormulaValue> ExecuteAsync(RecordValue obj)
        {
            _logger.LogInformation("------------------------------\n\n" +
                 "Executing GetValue function.");

            var controlModel = (ControlRecordValue)obj;

            var page = _testInfraFunctions.GetContext().Pages.First();
            var json = await page.EvaluateAsync<string>($"JSON.stringify(Xrm.Page.ui.formContext.getAttribute('{controlModel.Name}').getValue())");

            var value = JToken.Parse(json);

            if (value is JArray jArray)
            {
                var options = await page.EvaluateAsync<string>($"JSON.stringify(Xrm.Page.ui.formContext.getAttribute('{controlModel.Name}').getOptions())");

                if (options.StartsWith("["))
                {
                    var optionArray = JArray.Parse(options);

                    var filteredOptions = optionArray
                     .Where(option => jArray.Any(val => val.ToString() == option.Value<int>("value").ToString()))
                     .ToList();

                    var filteredJArray = JArray.FromObject(filteredOptions);
                    return ConvertToUntypedObjectValue(filteredJArray);
                }
            }

            return ConvertToUntypedObjectValue(value);
        }

        private FormulaValue ConvertToUntypedObjectValue(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Integer:
                    return NumberValue.New((int)token);

                case JTokenType.Float:
                    return NumberValue.New((double)token);

                case JTokenType.String:
                    return StringValue.New((string)token);

                case JTokenType.Boolean:
                    return BooleanValue.New((bool)token);

                case JTokenType.Null:
                    return BlankValue.NewBlank();

                case JTokenType.Object:
                    var fields = new List<NamedValue>();
                    foreach (var property in (JObject)token)
                    {
                        fields.Add(new NamedValue(property.Key, ConvertToUntypedObjectValue(property.Value)));
                    }
                    return RecordValue.NewRecordFromFields(fields);

                case JTokenType.Array:
                    var records = new List<RecordValue>();
                    foreach (var item in (JArray)token)
                    {
                        var recordItem = ConvertToUntypedObjectValue(item) as RecordValue;
                        if (recordItem != null)
                        {
                            records.Add(recordItem);
                        }
                    }

                    if (records.Count == 0)
                    {
                        return BlankValue.NewBlank();
                    }

                    var firstRecord = records.First();
                    return TableValue.NewTable(firstRecord.Type, records);

                default:
                    throw new InvalidOperationException($"Unsupported JTokenType: {token.Type}");
            }

        }
    }
}

