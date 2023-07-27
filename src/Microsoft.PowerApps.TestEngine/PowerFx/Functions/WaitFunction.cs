// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Helpers;
using Microsoft.PowerApps.TestEngine.PowerApps.PowerFxModel;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerApps.TestEngine.PowerFx.Functions
{
    /// <summary>
    /// This will wait for the property of the control to equal the specified value.
    /// </summary>
    public class WaitFunction : ReflectionFunction
    {
        protected readonly int _timeout;
        protected readonly ILogger _logger;
        public WaitFunction(int timeout, FormulaType formulaType, ILogger logger) : base("Wait", FormulaType.Blank, RecordType.Empty(), FormulaType.String, formulaType)
        {
            _timeout = timeout;
            _logger = logger;
        }

        protected void PollingCondition<T>(Func<T, bool> conditionToCheck, Func<T> functionToCall, int timeout)
        {
            _logger.LogDebug("Checking if Wait function's condition is met.");

            if (timeout < 0)
            {
                _logger.LogError("The timeout TestSetting cannot be less than zero.");
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
                    _logger.LogError("Wait function timed out.");
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
            Wait(obj, propName, value);
            return FormulaValue.NewBlank();
        }

        private void Wait(RecordValue obj, StringValue propName, NumberValue value)
        {
            _logger.LogInformation("------------------------------\n\n" +
                "Executing Wait function.");

            NullCheckHelper.NullCheck(obj, propName, value, _logger);

            var controlModel = (ControlRecordValue)obj;
            var propType = controlModel.GetField(propName.Value);

            // Handling in the case that the property is a Number type
            if (propType.GetType() == typeof(NumberValue))
            {
                PollingCondition<double>((x) => x != value.Value, () =>
                {
                    return ((NumberValue)controlModel.GetField(propName.Value)).Value;
                }, _timeout);

                _logger.LogInformation("Successfully finished executing Wait function, condition was met.");
            }
            // Otherwise, return error for improper value type.
            else
            {
                _logger.LogError($"Value isn't valid. Value of the Wait function should be {propType.Type} Type instead of Number Type");
                throw new InvalidDataException();
            }
        }
    }

    public class WaitFunctionString : WaitFunction
    {
        public WaitFunctionString(int timeout, ILogger logger) : base(timeout, FormulaType.String, logger)
        {
        }

        public BlankValue Execute(RecordValue obj, StringValue propName, StringValue value)
        {
            Wait(obj, propName, value);
            return FormulaValue.NewBlank();
        }

        private void Wait(RecordValue obj, StringValue propName, StringValue value)
        {
            _logger.LogInformation("------------------------------\n\n" +
                "Executing Wait function.");

            NullCheckHelper.NullCheck(obj, propName, value, _logger);

            var controlModel = (ControlRecordValue)obj;
            var propType = controlModel.GetField(propName.Value);

            // Handling in the case that the property is a String type
            if (propType.GetType() == typeof(StringValue))
            {

                PollingCondition<string>((x) => x != value.Value, () =>
                {
                    return ((StringValue)controlModel.GetField(propName.Value)).Value;
                }, _timeout);

                _logger.LogInformation("Successfully finished executing Wait function, condition was met.");
            }
            // Otherwise, return error for improper value type.
            else
            {
                _logger.LogError($"Value isn't valid. Value of the Wait function should be {propType.Type} Type instead of String Type");
                throw new InvalidDataException();
            }
        }
    }

    public class WaitFunctionBoolean : WaitFunction
    {
        public WaitFunctionBoolean(int timeout, ILogger logger) : base(timeout, FormulaType.Boolean, logger)
        {
        }

        public BlankValue Execute(RecordValue obj, StringValue propName, BooleanValue value)
        {
            Wait(obj, propName, value);
            return FormulaValue.NewBlank();
        }

        private void Wait(RecordValue obj, StringValue propName, BooleanValue value)
        {
            _logger.LogInformation("------------------------------\n\n" +
                "Executing Wait function.");

            NullCheckHelper.NullCheck(obj, propName, value, _logger);

            var controlModel = (ControlRecordValue)obj;
            var propType = controlModel.GetField(propName.Value);

            // Handling in the case that the property is a Boolean type
            if (propType.GetType() == typeof(BooleanValue))
            {

                PollingCondition<bool>((x) => x != value.Value, () =>
                {
                    return ((BooleanValue)controlModel.GetField(propName.Value)).Value;
                }, _timeout);

                _logger.LogInformation("Successfully finished executing Wait function, condition was met.");
            }
            // Otherwise, return error for improper value type.
            else
            {
                _logger.LogError($"Value isn't valid. Value of the Wait function should be {propType.Type} Type instead of Boolean Type");
                throw new InvalidDataException();
            }
        }
    }

    public class WaitFunctionDateTime : WaitFunction
    {
        public WaitFunctionDateTime(int timeout, ILogger logger) : base(timeout, FormulaType.DateTime, logger)
        {
        }

        public BlankValue Execute(RecordValue obj, StringValue propName, DateTimeValue value)
        {
            Wait(obj, propName, value);
            return FormulaValue.NewBlank();
        }

        private void Wait(RecordValue obj, StringValue propName, DateTimeValue value)
        {
            _logger.LogInformation("------------------------------\n\n" +
                "Executing Wait function.");

            NullCheckHelper.NullCheck(obj, propName, value, _logger);

            var controlModel = (ControlRecordValue)obj;
            var propType = controlModel.GetField(propName.Value);

            // Handling in the case that the property is a DateTime
            if (propType.GetType() == typeof(DateTimeValue))
            {
                PollingCondition<DateTime>((x) => x != value.GetConvertedValue(null), () =>
                {
                    return ((DateTimeValue)controlModel.GetField(propName.Value)).GetConvertedValue(null);
                }, _timeout);
            }
            // Otherwise, the property should be be a Date
            else
            {
                PollingCondition<DateTime>((x) => x != value.GetConvertedValue(null), () =>
                {
                    return ((DateValue)controlModel.GetField(propName.Value)).GetConvertedValue(null);
                }, _timeout);
            }

            _logger.LogInformation("Successfully finished executing Wait function, condition was met.");
        }
    }

    public class WaitFunctionDate : WaitFunction
    {
        public WaitFunctionDate(int timeout, ILogger logger) : base(timeout, FormulaType.Date, logger)
        {
        }

        public BlankValue Execute(RecordValue obj, StringValue propName, DateValue value)
        {
            Wait(obj, propName, value);
            return FormulaValue.NewBlank();
        }

        private void Wait(RecordValue obj, StringValue propName, DateValue value)
        {
            _logger.LogInformation("------------------------------\n\n" +
                "Executing Wait function.");

            NullCheckHelper.NullCheck(obj, propName, value, _logger);

            var controlModel = (ControlRecordValue)obj;
            var propType = controlModel.GetField(propName.Value);

            // Handling in the case that the property is a DateTime
            if (propType.GetType() == typeof(DateTimeValue))
            {
                PollingCondition<DateTime>((x) => x != value.GetConvertedValue(null), () =>
                {
                    return ((DateTimeValue)controlModel.GetField(propName.Value)).GetConvertedValue(null);
                }, _timeout);

                _logger.LogInformation("Successfully finished executing Wait function, condition was met.");

            }
            // Handling in the case that the property is a Date
            else if (propType.GetType() == typeof(DateValue))
            {
                PollingCondition<DateTime>((x) => x != value.GetConvertedValue(null), () =>
                {
                    return ((DateValue)controlModel.GetField(propName.Value)).GetConvertedValue(null);
                }, _timeout);

                _logger.LogInformation("Successfully finished executing Wait function, condition was met.");
            }
            // Otherwise, return error for improper value type.
            else
            {
                _logger.LogError($"Value isn't valid. Value of the Wait function should be {propType.Type} Type instead of Date Type");
                throw new InvalidDataException();
            }
        }
    }

    public static class WaitRegisterExtensions
    {
        public static void RegisterAll(this PowerFxConfig powerFxConfig, int timeout, ILogger logger)
        {
            powerFxConfig.AddFunction(new WaitFunctionNumber(timeout, logger));
            powerFxConfig.AddFunction(new WaitFunctionString(timeout, logger));
            powerFxConfig.AddFunction(new WaitFunctionBoolean(timeout, logger));
            powerFxConfig.AddFunction(new WaitFunctionDate(timeout, logger));
            powerFxConfig.AddFunction(new WaitFunctionDateTime(timeout, logger));
        }
    }
}
