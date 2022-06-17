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
        private readonly int _timeout;
        public WaitFunction(int timeout) : base("Wait", FormulaType.Blank, new RecordType(), FormulaType.String, FormulaType.String)
        {
            _timeout = timeout;
        }

        public BlankValue Execute(RecordValue obj, StringValue propName, FormulaValue valueToCheck)
        {
            Wait(obj, propName, valueToCheck);
            return FormulaValue.NewBlank();
        }

        private void Wait(RecordValue obj, StringValue propName, FormulaValue valueToCheck)
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

            var controlModel = (ControlRecordValue)obj;
            string? propertyValue = null;

            // TODO handle non strings?
            var text = ((StringValue)valueToCheck).Value;

            PollingHelper.Poll<string>(propertyValue, (x) => x != text, () => {
                return ((StringValue)controlModel.GetField(propName.Value)).Value;
            }, _timeout);
        }
    }
}