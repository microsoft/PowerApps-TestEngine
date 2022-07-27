// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
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
        protected readonly ILogger _logger;
        public WaitFunction(int timeout, FormulaType formulaType, ILogger logger) : base("Wait", FormulaType.Blank, new RecordType(), FormulaType.String, formulaType)
        {
            _timeout = timeout;
            _logger = logger;
        }

        protected void NullCheck(RecordValue obj, StringValue propName, FormulaValue value)
        {
            bool encounteredError = false;

            if (obj == null)
            {
                _logger.LogError("Object cannot be null.");
                encounteredError = true;
            }

            if (propName == null)
            {
                _logger.LogError("Property name cannot be null.");
                encounteredError = true;
            }
            else
            {
                _logger.LogTrace("Property name: " + propName);
            }

            if (value == null)
            {
                _logger.LogError("Property cannot be set to a null value.");
                encounteredError = true;
            }
            else
            {
                _logger.LogDebug("Error occurred on DataType of type " + value.GetType());
                _logger.LogTrace("Property attempted being set to: " + value);
            }

            if (encounteredError == true)
            {
                throw new ArgumentNullException();
            }
        }

        protected void PollingCondition<T>(Func<T, bool> conditionToCheck, Func<T>? functionToCall, int timeout)
        {
            _logger.LogInformation("Checking if condition is met, before timing out.");

            if (timeout < 0)
            {
                _logger.LogError("Timeout cannot be less than zero.");
                throw new ArgumentOutOfRangeException();
            }

            DateTime startTime = DateTime.Now;
            bool conditional = true;

            while (conditional)
            {
                if (functionToCall != null)
                {
                    conditional = conditionToCheck(functionToCall());
                }
                
                if ((DateTime.Now - startTime) > TimeSpan.FromMilliseconds(timeout))
                {
                    _logger.LogDebug("Timeout duration set to " + timeout);
                    _logger.LogError("Timed operation timed out.");
                    throw new TimeoutException();
                }

                Thread.Sleep(500);
            }
        }
    }

    public class WaitFunctionNumber : WaitFunction
    {
        public WaitFunctionNumber(int timeout, ILogger logger) : base(timeout, FormulaType.Number, logger)
        {
        }

        public BlankValue Execute(RecordValue obj, StringValue propName, NumberValue value)
        {
            _logger.LogInformation("Executing Wait function.");
            Wait(obj, propName, value);
            return FormulaValue.NewBlank();
        }

        private void Wait(RecordValue obj, StringValue propName, NumberValue value)
        {
            NullCheck(obj, propName, value);

            var controlModel = (ControlRecordValue)obj;

            PollingCondition<double>((x) => x != value.Value, () => {
                return ((NumberValue)controlModel.GetField(propName.Value)).Value;
            }, _timeout);
        }
    }

    public class WaitFunctionString : WaitFunction
    {
        public WaitFunctionString(int timeout, ILogger logger) : base(timeout, FormulaType.String, logger)
        {
        }

        public BlankValue Execute(RecordValue obj, StringValue propName, StringValue value)
        {
            _logger.LogInformation("Executing Wait function.");
            Wait(obj, propName, value);
            return FormulaValue.NewBlank();
        }

        private void Wait(RecordValue obj, StringValue propName, StringValue value)
        {
            NullCheck(obj, propName, value);

            var controlModel = (ControlRecordValue)obj;

            PollingCondition<string>((x) => x != value.Value, () => {
                return ((StringValue)controlModel.GetField(propName.Value)).Value;
            }, _timeout);
        }
    }

    public class WaitFunctionBoolean : WaitFunction
    {
        public WaitFunctionBoolean(int timeout, ILogger logger) : base(timeout, FormulaType.Boolean, logger)
        {
        }

        public BlankValue Execute(RecordValue obj, StringValue propName, BooleanValue value)
        {
            _logger.LogInformation("Executing Wait function.");
            Wait(obj, propName, value);
            return FormulaValue.NewBlank();
        }

        private void Wait(RecordValue obj, StringValue propName, BooleanValue value)
        {
            NullCheck(obj, propName, value);

            var controlModel = (ControlRecordValue)obj;

            PollingCondition<bool>((x) => x != value.Value, () => {
                return ((BooleanValue)controlModel.GetField(propName.Value)).Value;
            }, _timeout);
        }
    }

    /* TODO: When .Date and .DateTime not ambiguous, uncomment
     * Currently waiting on PowerFX 'DateTime' and 'Date' types to be less ambiguous, so that both can be used
    public class WaitFunctionDateTime : WaitFunction
    {
        public WaitFunctionDateTime(int timeout, ILogger logger) : base(timeout, FormulaType.DateTime, logger)
        {
        }

        public BlankValue Execute(RecordValue obj, StringValue propName, DateTimeValue value)
        {
            _logger.LogInformation("Executing Wait function.");
            Wait(obj, propName, value);
            return FormulaValue.NewBlank();
        }

        private void Wait(RecordValue obj, StringValue propName, DateTimeValue value)
        {
            NullCheck(obj, propName, value);

            var controlModel = (ControlRecordValue)obj;

            PollingCondition<DateTime>((x) => x != value.Value, () => {
                return ((DateTimeValue)controlModel.GetField(propName.Value)).Value;
            }, _timeout);
        }
    }
    */

    public class WaitFunctionDate : WaitFunction
    {
        public WaitFunctionDate(int timeout, ILogger logger) : base(timeout, FormulaType.Date, logger)
        {
        }

        public BlankValue Execute(RecordValue obj, StringValue propName, DateValue value)
        {
            _logger.LogInformation("Executing Wait function.");
            Wait(obj, propName, value);
            return FormulaValue.NewBlank();
        }

        private void Wait(RecordValue obj, StringValue propName, DateValue value)
        {
            NullCheck(obj, propName, value);

            var controlModel = (ControlRecordValue)obj;
            
            PollingCondition<DateTime>((x) => x != value.Value, () => {
                return ((DateValue)controlModel.GetField(propName.Value)).Value;
            }, _timeout);
        }
    }

    public static class WaitRegisterExtensions
    {
        public static void RegisterAll(this PowerFxConfig powerFxConfig, int timeout, ILogger logger)
        {
            powerFxConfig.AddFunction(new WaitFunctionNumber(timeout, logger));
            powerFxConfig.AddFunction(new WaitFunctionString(timeout, logger));
            powerFxConfig.AddFunction(new WaitFunctionBoolean(timeout, logger));
            // TODO: When .Date and .DateTime not ambiguous, uncomment
            // powerFxConfig.AddFunction(new WaitFunctionDateTime(timeout));
            powerFxConfig.AddFunction(new WaitFunctionDate(timeout, logger));
        }
    }
}
