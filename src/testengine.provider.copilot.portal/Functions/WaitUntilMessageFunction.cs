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
            : base(DPath.Root.Append(new DName("Preview")), "WaitUntilMessage", FormulaType.Boolean, FormulaType.String)
        {
            _testInfraFunctions = testInfraFunctions;
            _testState = testState;
            _logger = logger;
            _provider = provider;
        }

        public BooleanValue Execute(StringValue expectedMessage)
        {
            _logger.LogInformation($"Waiting for message: {expectedMessage.Value}");

           return ExecuteAsync(expectedMessage).GetAwaiter().GetResult();
        }

        public async Task<BooleanValue> ExecuteAsync(StringValue expectedMessage)
        {
            _logger.LogInformation($"Waiting for message: {expectedMessage.Value}");

            var page = _testInfraFunctions.Page;

            try
            {
                string sectionSelector = "section[aria-roledescription='chat history']";

                // Wait until the expected text appears in any <article> inside the section
                await page.Locator($"{sectionSelector} article").Filter(new()
                {
                    HasTextString = expectedMessage.Value
                }).WaitForAsync();

                return FormulaValue.New(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error waiting for message: {expectedMessage.Value}");
                return FormulaValue.New(false);
            }
        }
    }
}
