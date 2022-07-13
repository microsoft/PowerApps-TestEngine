// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerApps.TestEngine.Helpers;
using Microsoft.PowerApps.TestEngine.PowerApps;
using Microsoft.PowerApps.TestEngine.PowerApps.PowerFxModel;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerApps.TestEngine.PowerFx.Functions
{
    /// <summary>
    /// This will wait for the property of the control to equal the specified value.
    /// TODO: Future intended function is of this format: `Wait(boolean expression)`. This is pending some improvements in Power FX to be available.
    /// </summary>
    public class WaitFunction : ReflectionFunction
    {
        protected readonly int _timeout;
        public WaitFunction(int timeout, FormulaType formulaType) : base("Wait", FormulaType.Blank, new RecordType(), FormulaType.String, formulaType)
        {
            _timeout = timeout;
        }
    }

    class WaitFunctionNumber : WaitFunction
    {
        public WaitFunctionNumber(int timeout) : base(timeout, FormulaType.Number)
        {
        }

        public BlankValue Execute(RecordValue obj, StringValue propName, NumberValue value)
        {
            Wait(obj, propName, value);
            return FormulaValue.NewBlank();
        }

        private void Wait(RecordValue obj, StringValue propName, NumberValue value)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            if (propName == null)
            {
                throw new ArgumentNullException(nameof(propName));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var controlModel = (ControlRecordValue)obj;

            PollingHelper.Poll<double>((x) => x != value.Value, () => {
                return ((NumberValue)controlModel.GetField(propName.Value)).Value;
            }, _timeout);
        }
    }

    class WaitFunctionString : WaitFunction
    {
        public WaitFunctionString(int timeout) : base(timeout, FormulaType.String)
        {
        }

        public BlankValue Execute(RecordValue obj, StringValue propName, StringValue value)
        {
            Wait(obj, propName, value);
            return FormulaValue.NewBlank();
        }

        private void Wait(RecordValue obj, StringValue propName, StringValue value)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            if (propName == null)
            {
                throw new ArgumentNullException(nameof(propName));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var controlModel = (ControlRecordValue)obj;

            PollingHelper.Poll<string>((x) => x != value.Value, () => {
                return ((StringValue)controlModel.GetField(propName.Value)).Value;
            }, _timeout);
        }
    }

    class WaitFunctionBoolean : WaitFunction
    {
        public WaitFunctionBoolean(int timeout) : base(timeout, FormulaType.Boolean)
        {
        }

        public BlankValue Execute(RecordValue obj, StringValue propName, BooleanValue value)
        {
            Wait(obj, propName, value);
            return FormulaValue.NewBlank();
        }

        private void Wait(RecordValue obj, StringValue propName, BooleanValue value)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            if (propName == null)
            {
                throw new ArgumentNullException(nameof(propName));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var controlModel = (ControlRecordValue)obj;

            PollingHelper.Poll<bool>((x) => x != value.Value, () => {
                return ((BooleanValue)controlModel.GetField(propName.Value)).Value;
            }, _timeout);
        }
    }

    class WaitFunctionDate : WaitFunction
    {
        public WaitFunctionDate(int timeout) : base(timeout, FormulaType.Date)
        {
        }

        public BlankValue Execute(RecordValue obj, StringValue propName, DateValue value)
        {
            Wait(obj, propName, value);
            return FormulaValue.NewBlank();
        }

        private void Wait(RecordValue obj, StringValue propName, DateValue value)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            if (propName == null)
            {
                throw new ArgumentNullException(nameof(propName));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var controlModel = (ControlRecordValue)obj;

            PollingHelper.Poll<DateTime>((x) => x != value.Value, () => {
                return ((DateValue)controlModel.GetField(propName.Value)).Value;
            }, _timeout);
        }
    }

    public static class WaitRegisterExtensions
    {
        public static void RegisterAll(this PowerFxConfig powerFxConfig, int timeout)
        {
        powerFxConfig.AddFunction(new WaitFunctionNumber(timeout));
        powerFxConfig.AddFunction(new WaitFunctionString(timeout));
        powerFxConfig.AddFunction(new WaitFunctionBoolean(timeout));
        powerFxConfig.AddFunction(new WaitFunctionDate(timeout));
        }
    }
}