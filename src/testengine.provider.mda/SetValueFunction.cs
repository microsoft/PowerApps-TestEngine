// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Dynamic;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Providers.PowerFxModel;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace testengine.provider.mda
{

    /// <summary>
    /// This will allow the value of a Model Driven Application control to be assigned
    /// </summary>
    /// <remarks>
    /// The <seealso cref="GetValueFunction"/> can be used to get the current value
    /// </remarks>
    public class SetValueFunction : ReflectionFunction
    {
        private readonly ITestInfraFunctions _testInfraFunctions;
        private readonly ILogger _logger;
        private static readonly TableType _optionsTable = TableType.Empty()
            .Add("Name", StringType.String)
            .Add("Value", StringType.Number);

        public SetValueFunction(ITestInfraFunctions testInfraFunctions, ILogger logger)
            : base(DPath.Root.Append(new DName("Preview")), "SetValue", BlankType.Blank, RecordType.Empty(), _optionsTable)
        {
            _testInfraFunctions = testInfraFunctions;
            _logger = logger;
        }

        public BlankValue Execute(RecordValue item, TableValue obj)
        {
            return ExecuteAsync(item, obj).Result;
        }

        public async Task<BlankValue> ExecuteAsync(RecordValue item, TableValue obj)
        {
            _logger.LogInformation("------------------------------\n\n" +
                 "Executing SetValue function.");

            var controlModel = (ControlRecordValue)item;

            List<ExpandoObject> items = new List<ExpandoObject>();

            foreach (var row in obj.Rows)
            {
                var rowObject = new ExpandoObject();
                var rowObjectDirectory = (IDictionary<string, object>)rowObject;

                await foreach (var field in row.Value.GetFieldsAsync(CancellationToken.None))
                {
                    rowObjectDirectory.Add(field.Name, GetFieldValue(field));
                }
                items.Add(rowObject);
            }

            var values = JsonSerializer.Serialize(items);

            var page = _testInfraFunctions.GetContext().Pages.First();

            var timeout = 30000;
            var started = DateTime.Now;

            while (DateTime.Now.Subtract(started).TotalMilliseconds <= timeout)
            {
                try
                {
                    await page.EvaluateAsync<string>(@"Xrm.Page.ui.formContext.getAttribute('" + controlModel.Name + "').setValue(" + values + ")");
                    await page.EvaluateAsync<string>(@"Xrm.Page.ui.formContext.getAttribute('" + controlModel.Name + "').fireOnChange()");

                    break;
                }
                catch
                {
                    Thread.Sleep(1000);
                    break;
                }
            }

            return BlankValue.NewBlank();
        }

        private object GetFieldValue(NamedValue field)
        {
            if (field.Value.TryGetPrimitiveValue(out object value))
            {
                return value;
            }
            switch (field.Value.Type)
            {
                default:
                    throw new NotSupportedException($"Unsupported field type: {field.Value.Type}");
            }
        }
    }
}

