// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

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
    /// This will pause the current test and allow the user to interact with the browser and inspect state when headless mode is false
    /// </summary>
    public class SetOptionsFunction : ReflectionFunction
    {
        private readonly ITestInfraFunctions _testInfraFunctions;
        private readonly ILogger _logger;
        private static readonly TableType _optionsTable = TableType.Empty()
            .Add("Name", StringType.String)
            .Add("Value", StringType.Number);

        public SetOptionsFunction(ITestInfraFunctions testInfraFunctions, ILogger logger)
            : base(DPath.Root.Append(new DName("Preview")), "SetOptions", BlankType.Blank, RecordType.Empty(), _optionsTable)
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
                 "Executing SetOptions function.");

            var controlModel = (ControlRecordValue)item;

            List<int> items = new List<int>();

            foreach (var row in obj.Rows)
            {
                var value = row.Value.GetField("Value").AsDecimal().ToString();
                items.Add((int)double.Parse(value));
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
    }
}

