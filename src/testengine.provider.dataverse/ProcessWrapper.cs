// Copyright(c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Diagnostics;

namespace testengine.provider.dataverse
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
