// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerApps.TestEngine.Helpers;
using Microsoft.PowerApps.TestEngine.PowerApps;
using Microsoft.PowerApps.TestEngine.PowerApps.PowerFxModel;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Public.Values;

namespace Microsoft.PowerApps.TestEngine.PowerFx.Functions
{
    private readonly IPowerAppFunctions _powerAppFunctions;
    /// <summary>
    /// This will allow you to set TextInput.Text
    /// </summary>
    public SetPropertyFunction(IPowerAppFunctions powerAppFunctions, RecordType recordType) : base("SetProperty", FormulaType.Blank, recordType, FormulaType.String, FormulaType.String)
    {
        _powerAppFunctions = powerAppFunctions;
    }

    public BlankValue Execute(RecordValue obj, StringValue propName, StringValue value)
    {
        SetProperty(obj, propName, value);
        return FormulaValue.NewBlank();
    }

    private async Task SetProperty(RecordValue obj, StringValue propName, StringValue value)
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
        var result = await _powerAppFunctions.SetPropertyValue(controlModel.GetItemPath(propName), value);

        if (!result)
        {
            throw new Exception($"Unable to set property {controlModel.Name}");
        }
    }
}
