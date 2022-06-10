// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Types;

namespace Microsoft.PowerApps.TestEngine.Tests.PowerFx.Functions
{
    public class SomeOtherRecordValue : RecordValue
    {
        public SomeOtherRecordValue(RecordType type) : base(type)
        {
        }

        protected override bool TryGetField(FormulaType fieldType, string fieldName, out FormulaValue result)
        {
            throw new global::System.NotImplementedException();
        }
    }
}
