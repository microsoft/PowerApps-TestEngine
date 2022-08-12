using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PowerApps.TestEngine.Reporting
{
    public class TestLoggerScope : IDisposable
    {
        private readonly string _scope;
        private readonly Action _scopeDisposalHandler;

        public TestLoggerScope(string scope, Action scopeDisposalHandler)
        {
            _scope = scope;
            _scopeDisposalHandler = scopeDisposalHandler;
        }

        public string GetScopeString()
        {
            return _scope;
        }

        public void Dispose()
        {
            _scopeDisposalHandler();
        }
    }
}
