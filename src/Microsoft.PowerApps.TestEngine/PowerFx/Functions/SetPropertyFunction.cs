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

        public SetPropertyFunction(IPowerAppFunctions powerAppFunctions, FormulaType formulaType) : base("SetProperty", FormulaType.Blank, new RecordType(), FormulaType.String, formulaType)
        {
            _powerAppFunctions = powerAppFunctions;
        }
    }

    public class SetPropertyFunctionNumber : SetPropertyFunction
    {
        public SetPropertyFunctionNumber(IPowerAppFunctions powerAppFunctions) : base(powerAppFunctions, FormulaType.Number)
        {
        }

        public BlankValue Execute(RecordValue obj, StringValue propName, NumberValue value)
        {
            SetProperty(obj, propName, value).Wait();
            return FormulaValue.NewBlank();
        }

        private async Task SetProperty(RecordValue obj, StringValue propName, NumberValue value)
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

    public class SetPropertyFunctionString : SetPropertyFunction
    {
        public SetPropertyFunctionString(IPowerAppFunctions powerAppFunctions) : base(powerAppFunctions, FormulaType.String)
        {
        }

        public BlankValue Execute(RecordValue obj, StringValue propName, StringValue value)
        {
            SetProperty(obj, propName, value).Wait();
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
            var result = await _powerAppFunctions.SetPropertyAsync(controlModel.GetItemPath(propName.Value), value);

            if (!result)
            {
                throw new Exception($"Unable to set property {controlModel.Name}");
            }
        }
    }

    public class SetPropertyFunctionBoolean : SetPropertyFunction
    {
        public SetPropertyFunctionBoolean(IPowerAppFunctions powerAppFunctions) : base(powerAppFunctions, FormulaType.Boolean)
        {
        }

        public BlankValue Execute(RecordValue obj, StringValue propName, BooleanValue value)
        {
            SetProperty(obj, propName, value).Wait();
            return FormulaValue.NewBlank();
        }

        private async Task SetProperty(RecordValue obj, StringValue propName, BooleanValue value)
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

    public class SetPropertyFunctionDate : SetPropertyFunction
    {
        public SetPropertyFunctionDate(IPowerAppFunctions powerAppFunctions) : base(powerAppFunctions, FormulaType.Date)
        {
        }

        public BlankValue Execute(RecordValue obj, StringValue propName, DateValue value)
        {
            SetProperty(obj, propName, value).Wait();
            return FormulaValue.NewBlank();
        }

        private async Task SetProperty(RecordValue obj, StringValue propName, DateValue value)
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

    public class SetPropertyFunctionTable : SetPropertyFunction
    {
        public SetPropertyFunctionTable(IPowerAppFunctions powerAppFunctions) : base(powerAppFunctions, new TableType())
        {
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

            var recordType = new RecordType().Add(controlName, new RecordType());

            var controlTableSource = new ControlTableSource(_powerAppFunctions,itemPath, recordType);

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
        powerFxConfig.AddFunction(new SetPropertyFunctionNumber(powerAppFunctions));
        powerFxConfig.AddFunction(new SetPropertyFunctionString(powerAppFunctions));
        powerFxConfig.AddFunction(new SetPropertyFunctionBoolean(powerAppFunctions));
        powerFxConfig.AddFunction(new SetPropertyFunctionDate(powerAppFunctions));
        powerFxConfig.AddFunction(new SetPropertyFunctionTable(powerAppFunctions));
        }
    }
}
