// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace testengine.module.sample
{
    public class SampleFunction : ReflectionFunction
    {
        public SampleFunction() : base(DPath.Root.Append(new DName("Experimental")), "Sample", FormulaType.Blank)
        {
        }

        public BlankValue Execute()
        {
            Console.WriteLine("!!! SAMPLE !!!");
            return BlankValue.NewBlank();
        }
    }
}
