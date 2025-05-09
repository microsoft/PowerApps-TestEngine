// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace testengine.provider.mcp
{
    /// <summary>
    /// Parses YAML and converts it into a Power Fx record.
    /// Handles objects (Record), arrays (Table), and scalars.
    /// </summary>
    public class ParseYamlFunction : ReflectionFunction
    {
        private static readonly RecordType _inputType = RecordType.Empty()
            .Add("Yaml", StringType.String);

        public ParseYamlFunction()
            : base(DPath.Root.Append(new DName("Preview")), "ParseYaml", RecordType.Empty(), StringType.String)
        {
        }

        public RecordValue Execute(StringValue input)
        {
            return ExecuteAsync(input).Result;
        }

        public async Task<RecordValue> ExecuteAsync(StringValue input)
        {
            // Extract the YAML string from the input record
            if (input == null || string.IsNullOrWhiteSpace(input.Value))
            {
                throw new ArgumentException("The Yaml must contain a valid YAML string.");
            }

            // Parse the YAML string into a .NET object
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var yamlObject = deserializer.Deserialize<object>(input.Value);

            var result = ConvertToPowerFxValue(yamlObject);

            if (result is RecordValue)
            {
                return (RecordValue)result;
            }

            throw new InvalidDataException();
        }

        private FormulaValue ConvertToPowerFxValue(object yamlObject)
        {
            if (yamlObject is IDictionary<object, object> dictionary)
            {
                // Handle objects (Record)
                var fields = dictionary.Select(kvp =>
                    new NamedValue(
                        kvp.Key.ToString(),
                        ConvertToPowerFxValue(kvp.Value)
                    )
                ).ToList();

                return RecordValue.NewRecordFromFields(fields);
            }
            else if (yamlObject is IEnumerable<object> list)
            {
                // Handle arrays (Table)
                var records = list.Select(item =>
                    ConvertToPowerFxValue(item) as RecordValue
                ).ToList();

                if (records.Count > 0)
                {
                    var recordType = records.First().Type;
                    return TableValue.NewTable(recordType, records);
                }
                else
                {
                    return TableValue.NewTable(RecordType.Empty());
                }
            }
            else if (yamlObject is string str)
            {
                // Handle scalar (String)
                return StringValue.New(str);
            }
            else if (yamlObject is int intValue)
            {
                // Handle scalar (Number - Integer)
                return NumberValue.New(intValue);
            }
            else if (yamlObject is double doubleValue)
            {
                // Handle scalar (Number - Double)
                return NumberValue.New(doubleValue);
            }
            else if (yamlObject is bool boolValue)
            {
                // Handle scalar (Boolean)
                return BooleanValue.New(boolValue);
            }
            else
            {
                // Handle unknown types as strings
                return StringValue.New(yamlObject?.ToString() ?? string.Empty);
            }
        }
    }
}
