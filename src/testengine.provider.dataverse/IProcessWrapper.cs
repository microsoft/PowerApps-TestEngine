// Copyright(c) Microsoft Corporation.
// Licensed under the MIT license.

namespace testengine.provider.dataverse
{
    public interface IProcessWrapper : IDisposable
    {
        string StandardOutput { get; }
        void WaitForExit();
    }
}
