// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.ComponentModel.Composition;
using Microsoft.PowerApps.TestEngine.Users;

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

        [ImportMany]
        public IEnumerable<Lazy<IUserManager>> UserModules;
#pragma warning restore 0649
    }
}
