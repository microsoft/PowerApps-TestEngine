using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerApps.TestEngine.Providers
{
    public interface IProcessRunner
    {
        int Run(string fileName, string arguments, string workingDirectory);
    }
}
