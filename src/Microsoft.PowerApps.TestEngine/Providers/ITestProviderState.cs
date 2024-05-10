using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerApps.TestEngine.Providers
{
    public interface ITestProviderState
    {
        public object GetState();
    }
}
