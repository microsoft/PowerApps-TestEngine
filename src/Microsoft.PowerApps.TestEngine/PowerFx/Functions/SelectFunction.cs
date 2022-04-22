// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerApps.TestEngine.PowerApps;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Public.Values;

namespace Microsoft.PowerApps.TestEngine.PowerFx.Functions
{
    /// <summary>
    /// This is the same functionality as the Power Apps Select function.
    /// https://docs.microsoft.com/en-us/power-apps/maker/canvas-apps/functions/function-select
    /// </summary>
    public class SelectFunction : ReflectionFunction
    {
        private readonly IPowerAppFunctions _powerAppFunctions;

        public SelectFunction(IPowerAppFunctions powerAppFunctions) : base("Select", FormulaType.Blank, FormulaType.UntypedObject)
        {
            _powerAppFunctions = powerAppFunctions;
        }

        public BooleanValue Execute(UntypedObjectValue obj)
        {
            var select = SelectAsync(obj).GetAwaiter();
            while (!select.IsCompleted)
            {
                System.Threading.Thread.Sleep(500);
            }

            return FormulaValue.New(select.GetResult());
        }

        private async Task<bool> SelectAsync(UntypedObjectValue obj)
        {
            IUntypedObject untypedVal = obj.Impl;

            var powerAppControlModel = (PowerAppControlModel)untypedVal;

            if (powerAppControlModel == null)
            {
                return false;
            }

            return await _powerAppFunctions.SelectControlAsync(powerAppControlModel.Name, null, null);
        }
    }
}
