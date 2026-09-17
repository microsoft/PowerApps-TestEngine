// Copyright(c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Diagnostics;

namespace Microsoft.PowerApps.TestEngine.Helpers
{
    public class ProcessWrapper : IProcessWrapper
    {
        private readonly Process _process;

        public Process Process
        {
            get { return _process; }
        }

        public ProcessWrapper(Process process)
        {
            _process = process;
        }

        public string StandardOutput
        {
            get
            {
                using (var reader = Process.StandardOutput)
                {
                    return reader.ReadToEnd();
                }
            }
        }

        public void WaitForExit()
        {
            Process.WaitForExit();
        }

        public void Dispose()
        {
            Process.Dispose();
        }
    }
}
