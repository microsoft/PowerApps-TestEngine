// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.ComponentModel.Composition;

namespace Microsoft.PowerApps.TestEngine.Modules
{
    /// <summary>
    /// Container for loading up MEF components supported by the CLI.
    /// </summary>
    public class MefComponents
    {
#pragma warning disable 0649 // Field 'MefModules' is never assigned to... Justification: Value set by MEF
        [ImportMany]
        public IEnumerable<Lazy<ITestEngineModule>> MefModules;
#pragma warning restore 0649
    }
}
