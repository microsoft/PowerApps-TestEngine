// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerApps.TestEngine.PowerApps;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Public.Values;

namespace Microsoft.PowerApps.TestEngine.PowerFx.Functions
{
    /// <summary>
    /// This will wait for the property of the control to equal the specified value.
    /// TODO: Future intended function is of this format: `Wait(boolean expression)`. This is pending some improvements in Power FX to be available.
    /// </summary>
    public class WaitFunction : ReflectionFunction
    {
        public WaitFunction() : base("Wait", FormulaType.Blank, FormulaType.UntypedObject, FormulaType.String, FormulaType.String)
        {
        }

        public BlankValue Execute(UntypedObjectValue obj, StringValue propName, FormulaValue valueToCheck)
        {
            Wait(obj, propName, valueToCheck);
            return FormulaValue.NewBlank();
        }

        private void Wait(UntypedObjectValue obj, StringValue propName, FormulaValue valueToCheck)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            if (propName == null)
            {
                throw new ArgumentNullException(nameof(propName));
            }

            if (valueToCheck == null)
            {
                throw new ArgumentNullException(nameof(valueToCheck));
            }

            IUntypedObject untypedVal = obj.Impl;

            var controlModel = (PowerAppControlModel)untypedVal;
            string? propertyValue = null;

            // TODO handle non strings?
            var text = ((StringValue)valueToCheck).Value;
            while (propertyValue != text)
            {
                if (!controlModel.TryGetProperty(propName.Value, out IUntypedObject result))
                {
                    throw new InvalidOperationException($"Property does not exist {propName.Value}");
                }

                var controlPropertyModel = (PowerAppControlPropertyModel)result;
                propertyValue = controlPropertyModel.GetString();

                if (propertyValue != text)
                {
                    // TODO: Do we want to timeout after some amount of time?
                    Thread.Sleep(500);
                }

            }
        }
    }
}