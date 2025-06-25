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
    /// Function to send text to the Copilot Portal
    /// </summary>
    public class CopilotSendMessageFunction : ReflectionFunction
    {
        private readonly ITestInfraFunctions _testInfraFunctions;
        private readonly ITestState _testState;
        private readonly ILogger _logger;

        public CopilotSendMessageFunction(ITestInfraFunctions testInfraFunctions, ITestState testState, ILogger logger)
            : base(DPath.Root.Append(new DName("Preview")), "CopilotSendMessage", FormulaType.Boolean, FormulaType.String)
        {
            _testInfraFunctions = testInfraFunctions;
            _testState = testState;
            _logger = logger;
        }

        public BooleanValue Execute(StringValue text)
        {
            _logger.LogInformation($"Sending text to Copilot Portal: {text.Value}");

            try
            {
                // Implementation to send text to the Copilot Portal
                // This would typically involve interacting with the chat input field
                var newValue = text.Value.Replace("'", "\\'");

                var script = @"
                (function () {
                    // Find the chat input field and send text
                    var inputField = document.querySelector('input[type=""text""], textarea, [contenteditable=""true""]');
                    if (inputField) {
                        if (inputField.contentEditable === 'true') {
                            inputField.textContent = '" + newValue + @"';
                            inputField.dispatchEvent(new Event('input', { bubbles: true }));
                        } else {
                            inputField.value = '" + newValue + @"';
                            inputField.dispatchEvent(new Event('input', { bubbles: true }));
                        }
                        
                        // Find and click send button
                        var sendButton = document.querySelector('button[aria-label*=""Send""], button[title*=""Send""], .send-button');
                        if (sendButton) {
                            sendButton.click();
                            return true;
                        }
                        
                        // Try Enter key as fallback
                        inputField.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));
                        return true;
                    }
                    return false;
                })();";

                var result = _testInfraFunctions.RunJavascriptAsync<bool>(script).GetAwaiter().GetResult();
                
                _logger.LogInformation($"Text sent successfully: {result}");
                return FormulaValue.New(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending text to Copilot Portal.");
                return FormulaValue.New(false);
            }
        }
    }
}
