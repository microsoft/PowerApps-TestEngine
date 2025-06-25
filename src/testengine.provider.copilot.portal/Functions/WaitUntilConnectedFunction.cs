// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerApps.TestEngine.Providers.Functions
{
    /// <summary>
    /// Function to wait until the Copilot Portal is connected and ready
    /// </summary>
    public class WaitUntilConnectedFunction : ReflectionFunction
    {
        private readonly ITestInfraFunctions _testInfraFunctions;
        private readonly ITestState _testState;
        private readonly ILogger _logger;
        private readonly CopilotPortalProvider _provider;

        public WaitUntilConnectedFunction(ITestInfraFunctions testInfraFunctions, ITestState testState, ILogger logger, CopilotPortalProvider provider)
            : base(DPath.Root.Append(new DName("Preview")), "WaitUntilConnected", FormulaType.Boolean)
        {
            _testInfraFunctions = testInfraFunctions;
            _testState = testState;
            _logger = logger;
            _provider = provider;
        }

        public BooleanValue Execute()
        {
            return ExecuteAsync().Result;
        }

        public async Task<BooleanValue> ExecuteAsync()
        {
            _logger.LogInformation("Waiting for Copilot Portal to be connected...");

            try
            {
                var timeout = _testState.GetTestSettings().Timeout;
                var startTime = DateTime.Now;

                while ((DateTime.Now - startTime).TotalMilliseconds < timeout)
                {
                    var isIdle = _provider.CheckIsIdleAsync().GetAwaiter().GetResult();

                    var testPanelExists = await _testInfraFunctions.RunJavascriptAsync<bool>("document.querySelector('textarea') !== null");

                    if (isIdle && testPanelExists)
                    {
                        _logger.LogInformation("Copilot Portal is connected and ready.");
                        return FormulaValue.New(true);
                    }

                    Thread.Sleep(1000); // Wait 1 second before checking again
                }

                _logger.LogWarning("Timeout waiting for Copilot Portal to be connected.");
                return FormulaValue.New(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error waiting for Copilot Portal connection.");
                return FormulaValue.New(false);
            }
        }
    }
}
