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
    internal class ApiSendTextFunction : ReflectionFunction
    {
        private readonly ICopilotApiService _service;
        private readonly ILogger _logger;

        public ApiSendTextFunction(ICopilotApiService service, ILogger logger)
            : base(DPath.Root.Append(new DName("Experimental")), "SendText", FormulaType.Blank, FormulaType.String)
        {
            _service = service;
            _logger = logger;
        }

        public BlankValue Execute(StringValue text)
        {
            ExecuteAsync(text).Wait();

            return FormulaValue.NewBlank();
        }

        public async Task ExecuteAsync(StringValue text)
        {
            _logger.LogDebug($"Sent {text.Value}");
            string json = "{\"type\": \"message\",\"from\": {\"id\": \"user1\"},\"text\": \"" + text.Value + "\"}";

            await _service.SendMessageOrEventAsync(json);
        }
    }
}
