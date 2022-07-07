﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

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

        public SelectThreeParamsFunction(IPowerAppFunctions powerAppFunctions, Func<Task> updateModelFunction) : base("Select", FormulaType.Blank, new RecordType(), FormulaType.Number, new RecordType())
        {
            _powerAppFunctions = powerAppFunctions;
            _updateModelFunction = updateModelFunction;
        }

        public BlankValue Execute(RecordValue obj, NumberValue rowOrColumn, RecordValue childObj)
        {
            SelectAsync(obj, rowOrColumn, childObj).Wait();

            return FormulaValue.NewBlank();
        }

        private async Task SelectAsync(RecordValue obj, NumberValue rowOrColumn, RecordValue childObj)
        {
            if (obj == null)
            {
                throw new ArgumentException(nameof(obj));
            }

            if (rowOrColumn == null)
            {
                throw new ArgumentException(nameof(rowOrColumn));
            }

            if (childObj == null)
            {
                throw new ArgumentException(nameof(childObj));
            }

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

            var recordType = new RecordType().Add(childControlName, new RecordType());
            var powerAppControlModel = new ControlRecordValue(recordType, _powerAppFunctions, childControlName, parentItemPath);
            var result = await _powerAppFunctions.SelectControlAsync(powerAppControlModel.GetItemPath());

            if (!result)
            {
                throw new Exception($"Unable to select control {powerAppControlModel.Name}");
            }

            await _updateModelFunction();
        }
    }
}