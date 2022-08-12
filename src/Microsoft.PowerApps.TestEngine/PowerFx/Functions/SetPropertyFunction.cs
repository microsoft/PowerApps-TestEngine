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
    public class SetPropertyFunctionRecord : ReflectionFunction
    {
        protected readonly IPowerAppFunctions _powerAppFunctions;
        public SetPropertyFunctionRecord(IPowerAppFunctions powerAppFunctions) : base("SetProperty", FormulaType.Blank, RecordType.Empty(), FormulaType.String, RecordType.Empty())
        {
            _powerAppFunctions = powerAppFunctions;
        }

        public BooleanValue Execute(RecordValue obj, StringValue propName, TableValue value)
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

    public class SetPropertyFunctionTable : ReflectionFunction
    {
        protected readonly IPowerAppFunctions _powerAppFunctions;
        public SetPropertyFunctionTable(IPowerAppFunctions powerAppFunctions) : base("SetProperty", FormulaType.Blank, RecordType.Empty(), FormulaType.String, TableType.Empty())
        {
            _powerAppFunctions = powerAppFunctions;
        }

        public BlankValue Execute(RecordValue obj, StringValue propName, TableValue value)
        {
            SetProperty(obj, propName, value).Wait();
            return FormulaValue.NewBlank();
        }

        private async Task SetProperty(RecordValue obj, StringValue propName, TableValue value)
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
            var controlName = obj.GetType().GetProperty("Name")?.GetValue(obj, null)?.ToString();

            var itemPath = new ItemPath()
            {
                ControlName = controlName,
                Index = null,
                ParentControl = null,
                PropertyName = (string)propName.Value
            };

            var recordType = RecordType.Empty().Add(controlName, RecordType.Empty());

            var controlTableSource = new ControlTableSource(_powerAppFunctions, itemPath, recordType);

            var powerAppControlModel = new ControlTableValue(recordType, controlTableSource, _powerAppFunctions);
            var result = await _powerAppFunctions.SetPropertyAsync(itemPath, value);

            if (!result)
            {
              throw new Exception($"Unable to set property {powerAppControlModel}");
            }
        }
    }

    public static class SetPropertyRegisterExtensions
    {
        public static void RegisterAll(this PowerFxConfig powerFxConfig, IPowerAppFunctions powerAppFunctions)
        {
            powerFxConfig.AddFunction(new SetPropertyFunctionRecord(powerAppFunctions));
            powerFxConfig.AddFunction(new SetPropertyFunctionTable(powerAppFunctions));
        }
    }
}
