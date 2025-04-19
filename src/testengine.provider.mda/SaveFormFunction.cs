// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Helpers;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace testengine.provider.mda
{
    /// <summary>
    /// This will wait for the current form to be saved.
    /// </summary>
    /// <remarks>
    /// </remarks>
    public class SaveFormFunction : ReflectionFunction
    {
        private readonly ITestInfraFunctions _testInfraFunctions;
        private readonly ITestState? _testState;
        private readonly ILogger _logger;

        public SaveFormFunction(ITestInfraFunctions testInfraFunctions, ITestState? testState, ILogger logger)
            : base(DPath.Root.Append(new DName("Preview")), "SaveForm", BooleanType.Boolean)
        {
            _testInfraFunctions = testInfraFunctions;
            _testState = testState;
            _logger = logger;
        }

        /// <summary>
        /// Attempt to save the form
        /// </summary>
        /// <returns><c>True</c> if form successfully saved.</returns>
        public BooleanValue Execute()
        {
            _logger.LogInformation("Starting Save Form");
            return ExecuteAsync().Result;
        }

        public async Task<BooleanValue> ExecuteAsync()
        {
            await _testInfraFunctions.RunJavascriptAsync<bool>("window.saveCompleted = null; Xrm.Page.data.save().then(function() { window.saveCompleted = true; }).catch(() => window.saveCompleted = false);");

            var getValue = () => _testInfraFunctions.RunJavascriptAsync<object>("window.saveCompleted").Result;

            var result = PollingHelper.Poll<object>(null, x => x == null, getValue, _testState != null ? 3000 : _testState.GetTimeout(), _logger, "Unable to complete save");

            if (result is bool value)
            {
                return BooleanValue.New(value);
            }

            return BooleanValue.New(false);
        }
    }
}
