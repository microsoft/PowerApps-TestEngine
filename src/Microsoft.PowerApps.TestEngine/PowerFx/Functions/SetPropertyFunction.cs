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
        private readonly IPowerAppFunctions _powerAppFunctions;

        public SetPropertyFunction(IPowerAppFunctions powerAppFunctions, FormulaType formulaType) : base("SetProperty", FormulaType.Blank, new RecordType(), FormulaType.String, formulaType)
        {
            _powerAppFunctions = powerAppFunctions;
        }

        public static void RegisterAll(IPowerAppFunctions powerAppFunctions)
        {
            SetPropertyFunction(powerAppFunctions, FormulaType.String);
            SetPropertyFunction(powerAppFunctions, FormulaType.Number);
            SetPropertyFunction(powerAppFunctions, FormulaType.Boolean);
            SetPropertyFunction(powerAppFunctions, FormulaType.Date);
            //Record
            //Table
        }

        public BlankValue Execute(RecordValue obj, StringValue propName, NumberValue value)
        {
            SetProperty(obj, propName, value).Wait();
            return FormulaValue.NewBlank();
        }

        public BlankValue Execute(RecordValue obj, StringValue propName, StringValue value)
        {
            SetProperty(obj, propName, value).Wait();
            return FormulaValue.NewBlank();
        }

        public BlankValue Execute(RecordValue obj, StringValue propName, BooleanValue value)
        {
            SetProperty(obj, propName, value).Wait();
            return FormulaValue.NewBlank();
        }

        public BlankValue Execute(RecordValue obj, StringValue propName, DateValue value)
        {
            SetProperty(obj, propName, value).Wait();
            return FormulaValue.NewBlank();
        }
        
        /*
        public BlankValue Execute(RecordValue obj, StringValue propName, RecordValue value)
        {
            SetProperty(obj, propName, value).Wait();
            return FormulaValue.NewBlank();
        }

        public BlankValue Execute(RecordValue obj, StringValue propName, TableValue value)
        {
            SetProperty(obj, propName, value).Wait();
            return FormulaValue.NewBlank();
        }
        */

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
        /*
        private async Task SetProperty(RecordValue obj, StringValue propName, RecordValue value)
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

            var controlModel = (ControlRecordValue)obj; 
            var result = await _powerAppFunctions.SetPropertyAsync(controlModel.GetItemPath(propName.Value), value);

            if (!result)
            {
                throw new Exception($"Unable to set property {controlModel.Name}");
            }
        }
        */
    }
}
