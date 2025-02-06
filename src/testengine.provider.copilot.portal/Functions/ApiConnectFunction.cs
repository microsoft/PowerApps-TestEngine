// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Providers.Services;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
using testengine.provider.copilot.portal.services;

namespace Microsoft.PowerApps.TestEngine.Providers.Functions
{
    internal class ApiConnectFunction : ReflectionFunction
    {
        private readonly ICopilotApiService _directLine;
        private readonly ILogger _logger;

        public ApiConnectFunction(ICopilotApiService directLine, ILogger logger)
            : base(DPath.Root.Append(new DName("Experimental")), "Connect", FormulaType.Blank)
        {
            _directLine = directLine;
            _logger = logger;
        }

        public BlankValue Execute()
        {
            ExecuteAsync().Wait();

            return FormulaValue.NewBlank();
        }

        public async Task ExecuteAsync()
        {
            await _directLine.Setup();
            await _directLine.InitiateConversationAsync();
        }
    }
}
