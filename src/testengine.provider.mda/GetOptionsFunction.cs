// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

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
    public class GetOptionsFunction : ReflectionFunction
    {
        private readonly ITestInfraFunctions _testInfraFunctions;
        private readonly ILogger _logger;
        private static readonly TableType _resultsTable = TableType.Empty()
            .Add("Name", StringType.String)
            .Add("Value", StringType.Number);

        public GetOptionsFunction(ITestInfraFunctions testInfraFunctions, ILogger logger)
            : base(DPath.Root.Append(new DName("Preview")), "GetOptions", _resultsTable, RecordType.Empty())
        {
            _testInfraFunctions = testInfraFunctions;
            _logger = logger;
        }

        public TableValue Execute(RecordValue obj)
        {
            return ExecuteAsync(obj).Result;
        }

        public async Task<TableValue> ExecuteAsync(RecordValue obj)
        {
            _logger.LogInformation("------------------------------\n\n" +
                 "Executing GetOptions function.");

            var controlModel = (ControlRecordValue)obj;

            var page = _testInfraFunctions.GetContext().Pages.First();
            var json = await page.EvaluateAsync<string>($"JSON.stringify(Xrm.Page.ui.formContext.getAttribute('{controlModel.Name}').getOptions())");

            var options = JArray.Parse(json);
            var records = new List<RecordValue>();

            foreach (var option in options)
            {
                var record = RecordValue.NewRecordFromFields(
                    new NamedValue(new KeyValuePair<string, FormulaValue>(
                            "Name",
                            StringValue.New(option["text"].ToString()) 
                        )),
                    new NamedValue(new KeyValuePair<string, FormulaValue>(
                            "Value",
                            NumberValue.New(option["value"].ToObject<double>())
                        ))
                    );
                records.Add(record);
            }

            return TableValue.NewTable(_resultsTable.ToRecord(), records);
        }
    }
}

