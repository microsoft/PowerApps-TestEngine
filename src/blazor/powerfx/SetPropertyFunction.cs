// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerApps.TestEngine.PowerFx.Functions
{
    /// <summary>
    /// The SetProperty function takes in a Record and property to update and a new value
    /// </summary>
    public class SetPropertyFunction : ReflectionFunction
    {
        public Func<TestState>? TestState { get; set; }

        public SetPropertyFunction() : base("SetProperty", FormulaType.Blank, RecordType.Empty(), FormulaType.String, FormulaType.Boolean)
        {
        }

        public BooleanValue Execute(RecordValue obj, StringValue propName, FormulaValue value)
        {
            obj.UpdateField(propName.Value, value);
           
            return BooleanValue.New(true);
        }
    }
}