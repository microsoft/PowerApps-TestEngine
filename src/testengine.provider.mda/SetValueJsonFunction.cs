// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
using Newtonsoft.Json;

namespace testengine.provider.mda
{

    /// <summary>
    /// This will allow the value of a Model Driven Application control to be assigned
    /// </summary>
    /// <remarks>
    /// The <seealso cref="GetValueFunction"/> can be used to get the current value
    /// </remarks>
    public class SetValueJsonFunction : ReflectionFunction
    {
        private readonly ITestInfraFunctions _testInfraFunctions;
        private readonly ILogger _logger;

        public SetValueJsonFunction(ITestInfraFunctions testInfraFunctions, ILogger logger)
            : base(DPath.Root.Append(new DName("Preview")), "SetValue", BlankType.Blank, RecordType.Empty(), StringType.String)
        {
            _testInfraFunctions = testInfraFunctions;
            _logger = logger;
        }

        public BlankValue Execute(RecordValue item, StringValue json)
        {
            return ExecuteAsync(item, json).Result;
        }

        public async Task<BlankValue> ExecuteAsync(RecordValue item, StringValue json)
        {
            if (!string.IsNullOrEmpty(json.Value) && json.Value.StartsWith("["))
            {
                var items = JsonConvert.DeserializeObject<List<IDictionary<string, object>>>(json.Value);
                var records = new List<RecordValue>();

                foreach (var record in items)
                {
                    var fields = new List<NamedValue>();
                    foreach (var key in record.Keys)
                    {
                        fields.Add(CreateNewField(key, record[key]));
                    }
                    records.Add(RecordValue.NewRecordFromFields(fields));
                }

                if (records.Count > 0)
                {
                    var function = new SetValueFunction(_testInfraFunctions, _logger);
                    function.ExecuteAsync(item, TableValue.NewTable(records.First().Type, records)).Wait();
                }

            }
            return BlankValue.NewBlank();
        }

        private NamedValue CreateNewField(string name, object v)
        {
            if (v is string stringValue)
            {
                return new NamedValue(name, FormulaValue.New(stringValue));
            }

            if (v is DateTime dataTimeValue)
            {
                return new NamedValue(name, FormulaValue.New(dataTimeValue));
            }

            if (v is int intValue)
            {
                return new NamedValue(name, FormulaValue.New(intValue));
            }

            if (v is decimal decimalValue)
            {
                return new NamedValue(name, FormulaValue.New(decimalValue));
            }

            throw new NotImplementedException($"Json field type {v.GetType()} for {name}");
        }
    }
}

