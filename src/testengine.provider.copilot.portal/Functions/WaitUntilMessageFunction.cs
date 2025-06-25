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
    /// Function to wait until a specific message appears in the Copilot Portal
    /// </summary>
    public class WaitUntilMessageFunction : ReflectionFunction
    {
        private readonly ITestInfraFunctions _testInfraFunctions;
        private readonly ITestState _testState;
        private readonly ILogger _logger;
        private readonly CopilotPortalProvider _provider;

        public WaitUntilMessageFunction(ITestInfraFunctions testInfraFunctions, ITestState testState, ILogger logger, CopilotPortalProvider provider)
            : base(DPath.Root.Append(new DName("Preview")), "WaitUntilMessage", FormulaType.Boolean, RecordType.Empty(), FormulaType.String)
        {
            _testInfraFunctions = testInfraFunctions;
            _testState = testState;
            _logger = logger;
            _provider = provider;
        }

        public BooleanValue Execute(StringValue expectedMessage)
        {
            _logger.LogInformation($"Waiting for message: {expectedMessage.Value}");

            try
            {
                var timeout = _testState.GetTestSettings().Timeout;
                var startTime = DateTime.Now;

                while ((DateTime.Now - startTime).TotalMilliseconds < timeout)
                {
                    // Check if the expected message appears in the messages queue
                    var messages = _provider.Messages.ToArray();
                      foreach (var message in messages)
                    {
                        if (message.IndexOf(expectedMessage.Value, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            _logger.LogInformation($"Found expected message: {expectedMessage.Value}");
                            return FormulaValue.New(true);
                        }
                    }

                    Thread.Sleep(500); // Wait 500ms before checking again
                }

                _logger.LogWarning($"Timeout waiting for message: {expectedMessage.Value}");
                return FormulaValue.New(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error waiting for message: {expectedMessage.Value}");
                return FormulaValue.New(false);
            }
        }
    }
}
