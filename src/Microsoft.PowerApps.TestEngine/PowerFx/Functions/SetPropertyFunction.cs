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
        protected readonly ILogger _logger;
        
        public SetPropertyFunction(IPowerAppFunctions powerAppFunctions, FormulaType formulaType, ILogger logger) : base("SetProperty", FormulaType.Blank, new RecordType(), FormulaType.String, formulaType)
        {
            _powerAppFunctions = powerAppFunctions;
            _logger = logger;
        }

        protected async Task SetProperty(RecordValue obj, StringValue propName, FormulaValue value)
        {
            NullCheckHelper.NullCheck(obj, propName, value, _logger);

            var controlModel = (ControlRecordValue)obj;
            var result = await _powerAppFunctions.SetPropertyAsync(controlModel.GetItemPath(propName.Value), value);

            if (!result)
            {
                _logger.LogError("Unable to set property with SetProperty function.");
                _logger.LogDebug("Error occurred on DataType of type " + value.GetType());
                _logger.LogTrace("Property name: " + propName);
                _logger.LogTrace("Attempting to set property to: " + value);

                throw new Exception();
            }

            _logger.LogDebug("Successfully finished executing SetProperty function.");
        }

    }

    public class SetPropertyFunctionNumber : SetPropertyFunction
    {
        public SetPropertyFunctionNumber(IPowerAppFunctions powerAppFunctions, ILogger logger) : base(powerAppFunctions, FormulaType.Number, logger)
        {
        }

        public BlankValue Execute(RecordValue obj, StringValue propName, NumberValue value)
        {
            _logger.LogDebug("Now executing SetProperty function.");
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
            _logger.LogDebug("Now executing SetProperty function.");
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
            _logger.LogDebug("Now executing SetProperty function.");
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
            _logger.LogDebug("Now executing SetProperty function.");
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
            _logger.LogDebug("Now executing SetProperty function.");
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
            _logger.LogDebug("Now executing SetProperty function.");
            SetProperty(obj, propName, value).Wait();
            return FormulaValue.NewBlank();
        }

        private async Task SetProperty(RecordValue obj, StringValue propName, TableValue value)
        {
            NullCheckHelper.NullCheck(obj, propName, value, _logger);

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
                _logger.LogError("Unable to set property with SetProperty function.");
                _logger.LogDebug("Error occurred on DataType of type " + value.GetType());
                _logger.LogTrace("Property name: " + propName);
                _logger.LogTrace("Property attempted being set to: " + value);
                _logger.LogTrace(powerAppControlModel.ToString());

                throw new Exception();
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
