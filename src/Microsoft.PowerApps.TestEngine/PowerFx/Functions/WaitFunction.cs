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

        public BooleanValue Execute(UntypedObjectValue obj, StringValue propName, FormulaValue val)
        {
            return FormulaValue.New(Wait(obj, propName, val));
        }

        private bool Wait(UntypedObjectValue obj, StringValue propName, FormulaValue val)
        {
            IUntypedObject untypedVal = obj.Impl;

            PowerAppControlModel controlModel = (PowerAppControlModel)untypedVal;

            if (controlModel == null)
            {
                return false;
            }

            String? propertyValue = null;
            var text = ((StringValue)val).Value;
            while (propertyValue != text)
            {
                if (!controlModel.TryGetProperty(propName.Value, out IUntypedObject result))
                {
                    return false;
                }

                PowerAppControlPropertyModel controlPropertyModel = (PowerAppControlPropertyModel)result;
                if (controlPropertyModel == null)
                {
                    return false;
                }

                propertyValue = controlPropertyModel.GetString();

                if (propertyValue != text)
                {
                    Thread.Sleep(500);
                }

            }

            return true;
        }
    }
}