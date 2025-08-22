// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerApps.TestEngine.PowerFx.Functions
{
    /// <summary>
    /// This will pause the current test and allow the user to interact with the browser and inspect state when headless mode is false
    /// </summary>
    public class PauseFunction : ReflectionFunction
    {
        private readonly ITestInfraFunctions _testInfraFunctions;
        private readonly ITestState _testState;
        private readonly ILogger _logger;

        public PauseFunction(ITestInfraFunctions testInfraFunctions, ITestState testState, ILogger logger)
            : base("Pause", FormulaType.Blank)
        {
            _testInfraFunctions = testInfraFunctions;
            _testState = testState;
            _logger = logger;
        }

        public BlankValue Execute()
        {
            _logger.LogInformation("------------------------------\n\n" +
                 "Executing Pause function.");

            var testSettings = _testState.GetTestSettings();

            if (!IsPreviewEnabledInOriginalConfig(testSettings))
            {
                _logger.LogInformation("Pause function is disabled - Preview namespace not explicitly enabled in YAML configuration.");
                return FormulaValue.NewBlank();
            }

            if (!testSettings.Headless)
            {
                var page = _testInfraFunctions.GetContext().Pages.First();
                page.PauseAsync().Wait();
                _logger.LogInformation("Successfully finished executing Pause function.");
            }
            else
            {
                _logger.LogInformation("Skip Pause function as in headless mode.");
            }

            return FormulaValue.NewBlank();
        }

        private bool IsPreviewEnabledInOriginalConfig(TestSettings testSettings)
        {
            return testSettings?.ExtensionModules?.Parameters?.ContainsKey("enableCorePause") == true &&
                   testSettings.ExtensionModules.Parameters["enableCorePause"]?.ToString().ToLower() == "true";
        }
    }
}
