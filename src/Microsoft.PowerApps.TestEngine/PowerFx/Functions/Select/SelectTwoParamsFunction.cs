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
    /// This is the same functionality as the Power Apps Select function With 2 Params
    /// https://docs.microsoft.com/en-us/power-apps/maker/canvas-apps/functions/function-select
    /// </summary>
    public class SelectTwoParamsFunction : ReflectionFunction
    {
        private readonly IPowerAppFunctions _powerAppFunctions;
        private readonly Func<Task> _updateModelFunction;
        private readonly ILogger _logger;

        public SelectTwoParamsFunction(IPowerAppFunctions powerAppFunctions, Func<Task> updateModelFunction, ILogger logger) : base("Select", FormulaType.Blank, RecordType.Empty(), FormulaType.Number)
        {
            _powerAppFunctions = powerAppFunctions;
            _updateModelFunction = updateModelFunction;
            _logger = logger;
        }

        public BlankValue Execute(RecordValue obj, NumberValue rowOrColumn)
        {
            SelectAsync(obj, rowOrColumn).Wait();

            return FormulaValue.NewBlank();
        }

        private async Task SelectAsync(RecordValue obj, NumberValue rowOrColumn)
        {
            _logger.LogInformation("------------------------------\n\n" +
                "Executing Select function.");

            NullCheckHelper.NullCheck(obj, rowOrColumn, _logger);

            var controlName = obj.GetType().GetProperty("Name")?.GetValue(obj, null)?.ToString();

            var itemPath = new ItemPath()
            {
                ControlName = controlName,
                Index = ((int)rowOrColumn.Value) - 1,
                ParentControl = null,
                PropertyName = "AllItems"
            };

            var recordType = RecordType.Empty().Add(controlName, RecordType.Empty());
            var powerAppControlModel = new ControlRecordValue(recordType, _powerAppFunctions, controlName);

            var result = await _powerAppFunctions.SelectControlAsync(itemPath);

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
