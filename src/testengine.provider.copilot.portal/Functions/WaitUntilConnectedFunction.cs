// Copyright (c) Microsoft Corporation.
// Licensed under the MIT licens

using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
using testengine.provider.copilot.portal.services;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Microsoft.PowerApps.TestEngine.Providers.Functions
{
    public class WaitUntilConnectedFunction : ReflectionFunction
    {
        private readonly ITestInfraFunctions _testInfraFunctions;
        private readonly ITestState _testState;
        private readonly ILogger _logger;
        private readonly IMessageProvider _provider;
        private IWorkerService _workerService;

        public WaitUntilConnectedFunction(ITestInfraFunctions testInfraFunctions, ITestState testState, ILogger logger, IMessageProvider provider, IWorkerService workerService)
            : base(DPath.Root.Append(new DName("Experimental")), "WaitUntilConnected", FormulaType.Blank)
        {
            _testInfraFunctions = testInfraFunctions;
            _testState = testState;
            _logger = logger;
            _provider = provider;
            _workerService = workerService;
        }

        public BlankValue Execute()
        {
            ExecuteAsync().Wait();
            return FormulaValue.NewBlank();
        }

        public async Task ExecuteAsync()
        {
            var timeout = _testState.GetTimeout();
            _logger.LogInformation("Start Wait");

            await _workerService.WaitUntilCompleteAsync(CheckCondition, _testState.GetTimeout());
        }

        private void CheckCondition(object? state)
        {
            if (!string.IsNullOrEmpty(_provider.ConversationId) && state is TimerState timerState)
            {
                timerState.Tcs.TrySetResult(true);
            }
        }
    }
}
