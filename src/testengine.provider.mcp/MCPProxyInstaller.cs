// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.PowerApps.TestEngine.Providers
{
    public class MCPProxyInstaller
    {
        private readonly IProcessRunner? _processRunner;
        private readonly ILogger? _logger;

        public Action<string,string> WriteFile = (file, content) => File.WriteAllText(file, content);

        public MCPProxyInstaller()
        {

        }

        public MCPProxyInstaller(IProcessRunner processRunner, ILogger logger)
        {
            _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
            _logger = logger ?? throw new ArgumentNullException(nameof(_logger));
        }

        private void RunDotNetToolInstall(string workingDirectory)
        {
            var exitCode = _processRunner.Run("donet", "tool install -g testengine.server.mcp", workingDirectory);
           
            if (exitCode != 0)
            {
                throw new InvalidOperationException($"npm install failed");
            }
        }
    }
}
