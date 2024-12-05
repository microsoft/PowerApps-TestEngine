// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Helpers;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace testengine.module
{
    /// <summary>
    /// This will execute Simulate request to Dataverse
    /// </summary>
    public class ReloadPageFunction : ReflectionFunction
    {
        private readonly ITestInfraFunctions _testInfraFunctions;
        private readonly ITestState _testState;
        private readonly ILogger _logger;

        public static string DEFAULT_OFFICE_365_CHECK = "var element = document.getElementById('O365_MainLink_NavMenu'); if (typeof(element) != 'undefined' && element != null) { 'Idle' } else { 'Loading' }";

        public ReloadPageFunction(ITestInfraFunctions testInfraFunctions, ITestState testState, ILogger logger)
            : base(DPath.Root.Append(new DName("Experimental")), "ReloadPage", FormulaType.Blank)
        {
            _testInfraFunctions = testInfraFunctions;
            _testState = testState;
            _logger = logger;
        }

        public BlankValue Execute()
        {
            ExecuteAsync().Wait();
            return FormulaValue.NewBlank();
        }

        public async Task ExecuteAsync()
        {
            _logger.LogInformation("------------------------------\n\n" +
               "Executing ReloadPage function.");

            await _testInfraFunctions.Page.ReloadAsync();

            await _testState.TestProvider.CheckProviderAsync();

            var timeout = _testState.GetTimeout();

            await PollingHelper.PollAsync(
                false,
                (bool val) =>
                {
                    var awaiter = _testState.TestProvider.TestEngineReady().GetAwaiter();
                    var value = awaiter.GetResult();
                    return value == false;
                },
                (Func<Task<bool>>)null,
                timeout,
                _logger);

            await PollingHelper.PollAsync(
               false,
               (bool val) =>
               {
                   var awaiter = _testState.TestProvider.CheckIsIdleAsync().GetAwaiter();
                   return awaiter.GetResult() == false;
               },
               (Func<Task<bool>>)null,
               timeout,
               _logger);
        }

        private async Task<bool> TestEngineIsReady()
        {
            return await _testState.TestProvider.TestEngineReady();
        }
    }
}

