// Copyright(c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace testengine.user.storagestate
{
    public interface IProcessWrapper : IDisposable
    {
        string StandardOutput { get; }
        void WaitForExit();
    }
}
