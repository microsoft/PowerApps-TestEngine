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
    public class AssertFunction : ReflectionFunction
    {

        public AssertFunction() : base("Assert", FormulaType.Boolean, FormulaType.Boolean, FormulaType.String)
        {
        }

        public BooleanValue Execute(BooleanValue result, StringValue message)
        {
            if (!result.Value)
            {
                throw new AssertionFailureException(message.Value);
            }

            return BooleanValue.New(true);
        }
    }
}