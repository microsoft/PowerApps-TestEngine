// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Public.Values;

namespace Microsoft.PowerApps.TestEngine.PowerFx.Functions
{
    /// <summary>
    /// This will allow you to set TextInput.Text
    /// </summary>
    public class SetPropertyFunction : ReflectionFunction
    {
        private readonly string _propertyValue;

        public SetPropertyFunction() 
            : base("SetProperty", FormulaType.Blank, FormulaType.String)
        {
        }

        public BlankValue Execute(UntypedObjectValue obj, StringValue propertyValue)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            if (propertyValue == null)
            {
                throw new ArgumentNullException(nameof(propertyValue));
            }

            obj = _propertyValue;

            return FormulaValue.NewBlank();
        }
    }
}
