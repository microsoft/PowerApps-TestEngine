// Copyright(c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerApps.TestEngine.Helpers
{
    public interface IProcessWrapper : IDisposable
    {
        string StandardOutput { get; }
        void WaitForExit();
    }
}
