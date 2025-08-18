// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Linq; // ADD THIS LINE - Required for .First() method
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

        /// <summary>
        /// Gets a value indicating whether this function is a preview feature
        /// </summary>
        public bool IsPreview => true;

        public PauseFunction(ITestInfraFunctions testInfraFunctions, ITestState testState, ILogger logger)
            : base("Pause", FormulaType.Blank)  // Core function - no namespace needed
        {
            _testInfraFunctions = testInfraFunctions;
            _testState = testState;
            _logger = logger;
        }

        public BlankValue Execute()
        {
            _logger.LogInformation("------------------------------\n\n" +
                 "Executing Pause function.");

            // Check if Preview features are enabled in settings
            if (!_testState.GetTestSettings().Preview)
            {
                _logger.LogWarning("Pause function is a preview feature. Enable Preview in test settings to use this function.");
                return FormulaValue.NewBlank();
            }

            if (!_testState.GetTestSettings().Headless)
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
    }
}