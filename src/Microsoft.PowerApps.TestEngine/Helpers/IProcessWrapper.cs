// Copyright(c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Diagnostics;

namespace Microsoft.PowerApps.TestEngine.Helpers
{
    public interface IProcessWrapper : IDisposable
    {
        Process Process { get; }

        string StandardOutput { get; }

        void WaitForExit();
    }
}
