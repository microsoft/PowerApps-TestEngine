// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerApps.TestEngine.PowerFx.Functions
{
    /// <summary>
    /// The Assert function takes in a Power FX expression that should evaluate to a boolean value.
    /// If the value returned is false, the test will fail.
    /// </summary>
    public class AssertWithoutMessageFunction : ReflectionFunction
    {

        public AssertWithoutMessageFunction() : base("Assert", FormulaType.Boolean, FormulaType.Boolean)
        {
        }

        public BooleanValue Execute(BooleanValue result)
        {
            if (!result.Value)
            {
                throw new InvalidOperationException();
            }

            return BooleanValue.New(true);
        }
    }
}