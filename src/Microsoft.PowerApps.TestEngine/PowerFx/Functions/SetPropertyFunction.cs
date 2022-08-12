// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerApps.TestEngine.Helpers;
using Microsoft.PowerApps.TestEngine.PowerApps;
using Microsoft.PowerApps.TestEngine.PowerApps.PowerFxModel;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerApps.TestEngine.PowerFx.Functions
{
    /// <summary>
    /// This will allow you to set properties for controls
    /// </summary>
    public class SetPropertyFunction : ReflectionFunction
    {
        protected readonly IPowerAppFunctions _powerAppFunctions;

        public SetPropertyFunction(IPowerAppFunctions powerAppFunctions) : base("SetProperty", FormulaType.Blank, RecordType.Empty(), FormulaType.String, FormulaType.Boolean)
        {
            _powerAppFunctions = powerAppFunctions;
        }

        public BooleanValue Execute(RecordValue obj, StringValue propName, FormulaValue value)
        {
            SetProperty(obj, propName, value).Wait();
            return FormulaValue.New(true);
        }

        protected async Task SetProperty(RecordValue obj, StringValue propName, FormulaValue value)
        {
            if (obj == null)
            {
                throw new ArgumentException(nameof(obj));
            }

            if (propName == null)
            {
                throw new ArgumentException(nameof(propName));
            }

            if (value == null)
            {
                throw new ArgumentException(nameof(value));
            }

            var controlModel = (ControlRecordValue)obj;
            var result = await _powerAppFunctions.SetPropertyAsync(controlModel.GetItemPath(propName.Value), value);

            if (!result)
            {
                throw new Exception($"Unable to set property {controlModel.Name}");
            }
        }
    }
}
