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
using Microsoft.Extensions.Logging;

namespace Microsoft.PowerApps.TestEngine.PowerFx.Functions
{
    /// <summary>
    /// This will allow you to set properties for controls
    /// </summary>
    public class SetPropertyFunction : ReflectionFunction
    {
        protected readonly IPowerAppFunctions _powerAppFunctions;
        private readonly ILogger _logger;
        
        public SetPropertyFunction(IPowerAppFunctions powerAppFunctions, FormulaType formulaType, ILogger logger) : base("SetProperty", FormulaType.Blank, new RecordType(), FormulaType.String, formulaType)
        {
            _powerAppFunctions = powerAppFunctions;
            _logger = logger;
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

    public class SetPropertyFunctionNumber : SetPropertyFunction
    {
        public SetPropertyFunctionNumber(IPowerAppFunctions powerAppFunctions, ILogger logger) : base(powerAppFunctions, FormulaType.Number, logger)
        {
        }

        public BlankValue Execute(RecordValue obj, StringValue propName, NumberValue value)
        {
            SetProperty(obj, propName, value).Wait();
            return FormulaValue.NewBlank();
        }
    }

    public class SetPropertyFunctionString : SetPropertyFunction
    {
        public SetPropertyFunctionString(IPowerAppFunctions powerAppFunctions, ILogger logger) : base(powerAppFunctions, FormulaType.String, logger)
        {
        }

        public BlankValue Execute(RecordValue obj, StringValue propName, StringValue value)
        {
            SetProperty(obj, propName, value).Wait();
            return FormulaValue.NewBlank();
        }
    }

    public class SetPropertyFunctionBoolean : SetPropertyFunction
    {
        public SetPropertyFunctionBoolean(IPowerAppFunctions powerAppFunctions, ILogger logger) : base(powerAppFunctions, FormulaType.Boolean, logger)
        {
        }

        public BlankValue Execute(RecordValue obj, StringValue propName, BooleanValue value)
        {
            SetProperty(obj, propName, value).Wait();
            return FormulaValue.NewBlank();
        }
    }

    public class SetPropertyFunctionDate : SetPropertyFunction
    {
        public SetPropertyFunctionDate(IPowerAppFunctions powerAppFunctions, ILogger logger) : base(powerAppFunctions, FormulaType.Date, logger)
        {
        }

        public BlankValue Execute(RecordValue obj, StringValue propName, DateValue value)
        {
            SetProperty(obj, propName, value).Wait();
            return FormulaValue.NewBlank();
        }
    }

    public class SetPropertyFunctionRecord : SetPropertyFunction
    {
        public SetPropertyFunctionRecord(IPowerAppFunctions powerAppFunctions, ILogger logger) : base(powerAppFunctions, new RecordType(), logger)
        {
        }

        public BlankValue Execute(RecordValue obj, StringValue propName, RecordValue value)
        {
            SetProperty(obj, propName, value).Wait();
            return FormulaValue.NewBlank();
        }
    }

    public class SetPropertyFunctionTable : SetPropertyFunction
    {
        public SetPropertyFunctionTable(IPowerAppFunctions powerAppFunctions, ILogger logger) : base(powerAppFunctions, new TableType(), logger)
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
        public static void RegisterAll(this PowerFxConfig powerFxConfig, IPowerAppFunctions powerAppFunctions, ILogger logger)
        {
        powerFxConfig.AddFunction(new SetPropertyFunctionNumber(powerAppFunctions, logger));
        powerFxConfig.AddFunction(new SetPropertyFunctionString(powerAppFunctions, logger));
        powerFxConfig.AddFunction(new SetPropertyFunctionBoolean(powerAppFunctions, logger));
        powerFxConfig.AddFunction(new SetPropertyFunctionDate(powerAppFunctions, logger));
        powerFxConfig.AddFunction(new SetPropertyFunctionRecord(powerAppFunctions, logger));
        powerFxConfig.AddFunction(new SetPropertyFunctionTable(powerAppFunctions, logger));
        }
    }
}
