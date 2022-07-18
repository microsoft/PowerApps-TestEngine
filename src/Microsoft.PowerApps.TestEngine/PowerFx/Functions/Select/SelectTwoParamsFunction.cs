// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

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

        public SelectTwoParamsFunction(IPowerAppFunctions powerAppFunctions, Func<Task> updateModelFunction) : base("Select", FormulaType.Blank, new RecordType(), FormulaType.Number)
        {
            _powerAppFunctions = powerAppFunctions;
            _updateModelFunction = updateModelFunction;
        }

        public BlankValue Execute(RecordValue obj, NumberValue rowOrColumn)
        {
            SelectAsync(obj, rowOrColumn).Wait();

            return FormulaValue.NewBlank();
        }

        private async Task SelectAsync(RecordValue obj, NumberValue rowOrColumn)
        {
            if (obj == null)
            {
                throw new ArgumentException(nameof(obj));
            }

            if (rowOrColumn == null)
            {
                throw new ArgumentException(nameof(rowOrColumn));
            }

            var controlName = obj.GetType().GetProperty("Name")?.GetValue(obj, null)?.ToString();

            var itemPath = new ItemPath()
            {
                ControlName = controlName,
                Index = ((int)rowOrColumn.Value) - 1,
                ParentControl = null,
                PropertyName = "AllItems"
            };

            var recordType = new RecordType().Add(controlName, new RecordType());
            var powerAppControlModel = new ControlRecordValue(recordType, _powerAppFunctions, controlName);
            
            var result = await _powerAppFunctions.SelectControlAsync(itemPath);

            if (!result)
            {
                throw new Exception($"Unable to select control {powerAppControlModel.Name}");
            }

            await _updateModelFunction();
        }
    }
}