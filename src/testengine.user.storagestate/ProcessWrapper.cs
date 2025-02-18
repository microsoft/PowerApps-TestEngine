// Copyright(c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace testengine.user.storagestate
{
    public class ProcessWrapper : IProcessWrapper
    {
        private readonly Process _process;

        public ProcessWrapper(Process process)
        {
            _process = process;
        }

        public string StandardOutput
        {
            get
            {
                using (var reader = _process.StandardOutput)
                {
                    return reader.ReadToEnd();
                }
            }
        }

        public void WaitForExit()
        {
            _process.WaitForExit();
        }

        public void Dispose()
        {
            _process?.Dispose();
        }
    }
}
