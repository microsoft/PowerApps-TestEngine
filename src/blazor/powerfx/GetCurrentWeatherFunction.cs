// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
using System.Reflection;

namespace Microsoft.PowerApps.TestEngine.PowerFx.Functions
{
    public class GetCurrentWeatherFunction : ReflectionFunction
    {
        public static RecordValue? Weather { get; set; } = null;

        private static RecordType resultType = RecordType.Empty()
            .Add("Condition", FormulaType.String)
            .Add("Humidity", NumberType.Number)
            .Add("Temperature", NumberType.Number)
            .Add("WindSpeed", NumberType.Number)
            .Add("Location", FormulaType.String);

        public GetCurrentWeatherFunction() : base(DPath.Root.Append(new DName("WeatherService")), "GetCurrentWeather", resultType, FormulaType.String)
        {
        }

        public RecordValue Execute(StringValue propName)
        {
            return Weather ?? ConvertToRecordValue(DefaultWeather(propName.Value));
        }

        public Weather DefaultWeather(string location)
        {
            return new Weather
            {
                Temperature = 25,
                Condition = "Sunny",
                Humidity = 50,
                WindSpeed = 10,
                Location = location
            };
        }

        public static RecordValue ConvertToRecordValue(object obj)
        {
            var fields = new List<NamedValue>();
            var properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                var value = property.GetValue(obj);
                if (value is string stringValue)
                {
                    fields.Add(new NamedValue(property.Name, FormulaValue.New(stringValue)));
                }
                if (value is int intValue)
                {
                    fields.Add(new NamedValue(property.Name, FormulaValue.New(intValue)));
                }
            }

            return FormulaValue.NewRecordFromFields(fields);
        }
    }
}