// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx;

namespace Microsoft.PowerApps.TestEngine.PowerFx
{
    public class PowerFxState
    {
        public PowerFxConfig Config { get; set; }

        public ReadOnlySymbolTable Symbols {  get; set; }
    }
}
