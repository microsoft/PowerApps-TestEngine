// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerApps.TestEngine.PowerApps;
using Microsoft.PowerApps.TestEngine.PowerApps.PowerFxModel;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerApps.TestEngine.PowerFx.Functions
{
    /// <summary>
    /// This is the same functionality as the Power Apps Select function With a Param
    /// https://docs.microsoft.com/en-us/power-apps/maker/canvas-apps/functions/function-select
    /// </summary>
    public class SelectOneParamFunction : ReflectionFunction
    {
        private readonly IPowerAppFunctions _powerAppFunctions;
        private readonly Func<Task> _updateModelFunction;

        public SelectOneParamFunction(IPowerAppFunctions powerAppFunctions, Func<Task> updateModelFunction) : base("Select", FormulaType.Blank, RecordType.Empty())
        {
            _powerAppFunctions = powerAppFunctions;
            _updateModelFunction = updateModelFunction;
        }

        public BlankValue Execute(RecordValue obj)
        {
            SelectAsync(obj).Wait();

            return FormulaValue.NewBlank();
        }

        private async Task SelectAsync(RecordValue obj)
        {
            if (obj == null)
            {
                throw new ArgumentException(nameof(obj));
            }

            var powerAppControlModel = (ControlRecordValue)obj;
            var result = await _powerAppFunctions.SelectControlAsync(powerAppControlModel.GetItemPath());

            if (!result)
            {
                throw new Exception($"Unable to select control {powerAppControlModel.Name}");
            }

            await _updateModelFunction();
        }
    }
}