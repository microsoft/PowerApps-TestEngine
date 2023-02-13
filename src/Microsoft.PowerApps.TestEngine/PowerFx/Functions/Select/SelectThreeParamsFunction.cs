// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Helpers;
using Microsoft.PowerApps.TestEngine.PowerApps;
using Microsoft.PowerApps.TestEngine.PowerApps.PowerFxModel;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerApps.TestEngine.PowerFx.Functions
{
    /// <summary>
    /// This is the same functionality as the Power Apps Select function With 3 Params
    /// https://docs.microsoft.com/en-us/power-apps/maker/canvas-apps/functions/function-select
    /// </summary>
    public class SelectThreeParamsFunction : ReflectionFunction
    {
        private readonly IPowerAppFunctions _powerAppFunctions;
        private readonly Func<Task> _updateModelFunction;
        protected readonly ILogger _logger;

        public SelectThreeParamsFunction(IPowerAppFunctions powerAppFunctions, Func<Task> updateModelFunction, ILogger logger) : base("Select", FormulaType.Blank, RecordType.Empty(), FormulaType.Number, RecordType.Empty())
        {
            _powerAppFunctions = powerAppFunctions;
            _updateModelFunction = updateModelFunction;
            _logger = logger;
        }

        public BlankValue Execute(RecordValue obj, NumberValue rowOrColumn, RecordValue childObj)
        {
            SelectAsync(obj, rowOrColumn, childObj).Wait();

            return FormulaValue.NewBlank();
        }

        private async Task SelectAsync(RecordValue obj, NumberValue rowOrColumn, RecordValue childObj)
        {
            _logger.LogInformation("------------------------------\n\n" +
                "Executing Select function.");

            NullCheckHelper.NullCheck(obj, rowOrColumn, childObj, _logger);

            var parentControlName = obj.GetType().GetProperty("Name")?.GetValue(obj, null)?.ToString();
            var childControlName = childObj.GetType().GetProperty("Name")?.GetValue(childObj, null)?.ToString();

            var parentItemPath = new ItemPath()
            {
                ControlName = parentControlName,
                Index = ((int)rowOrColumn.Value) - 1,
                ParentControl = null,
                PropertyName = "AllItems"
            };
            var itemPath = new ItemPath()
            {
                ControlName = childControlName,
                Index = null,
                ParentControl = parentItemPath,
                PropertyName = null
            };

            var recordType = RecordType.Empty().Add(childControlName, RecordType.Empty());
            var powerAppControlModel = new ControlRecordValue(recordType, _powerAppFunctions, childControlName, parentItemPath);
            var result = await _powerAppFunctions.SelectControlAsync(powerAppControlModel.GetItemPath());

            if (!result)
            {
                _logger.LogTrace($"Control name: {powerAppControlModel.Name}");
                _logger.LogError("Unable to select control");
                throw new Exception();
            }

            await _updateModelFunction();

            _logger.LogInformation("Successfully finished executing Select function.");
        }
    }
}
