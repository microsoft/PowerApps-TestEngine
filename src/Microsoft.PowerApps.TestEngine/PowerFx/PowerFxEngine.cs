﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Dynamic;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Helpers;
using Microsoft.PowerApps.TestEngine.PowerApps;
using Microsoft.PowerApps.TestEngine.PowerFx.Functions;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerApps.TestEngine.PowerFx
{
    /// <summary>
    /// Wrapper for Power FX interpreter
    /// </summary>
    public class PowerFxEngine : IPowerFxEngine
    {
        private readonly ITestInfraFunctions _testInfraFunctions;
        private readonly IPowerAppFunctions _powerAppFunctions;
        private readonly IFileSystem _fileSystem;
        private readonly ISingleTestInstanceState _singleTestInstanceState;
        private readonly ITestState _testState;
        private int _retryLimit = 2;

        private RecalcEngine Engine { get; set; }
        private ILogger Logger { get { return _singleTestInstanceState.GetLogger(); } }

        public PowerFxEngine(ITestInfraFunctions testInfraFunctions,
                             IPowerAppFunctions powerAppFunctions,
                             ISingleTestInstanceState singleTestInstanceState,
                             ITestState testState,
                             IFileSystem fileSystem)
        {
            _testInfraFunctions = testInfraFunctions;
            _powerAppFunctions = powerAppFunctions;
            _singleTestInstanceState = singleTestInstanceState;
            _testState = testState;
            _fileSystem = fileSystem;
        }

        public void Setup(CultureInfo locale)
        {
            var powerFxConfig = new PowerFxConfig(locale);

            powerFxConfig.AddFunction(new SelectOneParamFunction(_powerAppFunctions, async () => await UpdatePowerFxModelAsync(), Logger));
            powerFxConfig.AddFunction(new SelectTwoParamsFunction(_powerAppFunctions, async () => await UpdatePowerFxModelAsync(), Logger));
            powerFxConfig.AddFunction(new SelectThreeParamsFunction(_powerAppFunctions, async () => await UpdatePowerFxModelAsync(), Logger));
            powerFxConfig.AddFunction(new ScreenshotFunction(_testInfraFunctions, _singleTestInstanceState, _fileSystem, Logger));
            powerFxConfig.AddFunction(new AssertWithoutMessageFunction(Logger));
            powerFxConfig.AddFunction(new AssertFunction(Logger));
            powerFxConfig.AddFunction(new SetPropertyFunction(_powerAppFunctions, Logger));
            WaitRegisterExtensions.RegisterAll(powerFxConfig, _testState.GetTimeout(), Logger);

            Engine = new RecalcEngine(powerFxConfig);
        }

        public async Task ExecuteWithRetryAsync(string testSteps)
        {
            int currentRetry = 0;
            FormulaValue result = FormulaValue.NewBlank();

            while (currentRetry <= _retryLimit)
            {
                try
                {
                    result = Execute(testSteps);
                    break;
                }
                catch (Exception e) when (e.Message.Contains("locale"))
                {
                    Logger.LogDebug($"Got {e.Message} in attempt No.{currentRetry + 1} to run");
                    currentRetry++;
                    if (currentRetry > _retryLimit)
                    {
                        // Re-throw the exception. 
                        throw;
                    }

                    // Wait to retry the operation.
                    Thread.Sleep(1000);
                    await UpdatePowerFxModelAsync();
                }
            }
        }

        public FormulaValue Execute(string testSteps)
        {
            if (Engine == null)
            {
                Logger.LogError("Engine is null, make sure to call Setup first");
                throw new InvalidOperationException();
            }

            // Remove the leading = sign
            if (testSteps.StartsWith("="))
            {
                testSteps = testSteps.Remove(0, 1);
            }

            var goStepByStep = false;
            // Check if the syntax is correct
            var checkResult = Engine.Check(testSteps, null, new ParserOptions() { AllowsSideEffects = true });
            if (!checkResult.IsSuccess)
            {
                // If it isn't, we have to go step by step as the object model isn't fully loaded
                goStepByStep = true;
                Logger.LogDebug($"Syntax check failed. Now attempting to execute lines step by step");
            }

            if (goStepByStep)
            {
                // TODO: This is a temporary hack to allow for multiple screens
                // Will need to come up with a better solution
                var splitSteps = testSteps.Split(';');
                FormulaValue result = FormulaValue.NewBlank();
                foreach (var step in splitSteps)
                {
                    Logger.LogTrace($"Attempting:{step.Replace("\n", "").Replace("\r", "")}");
                    result = Engine.Eval(step, null, new ParserOptions() { AllowsSideEffects = true });
                }
                return result;
            }
            else
            {
                Logger.LogTrace($"Attempting:\n\n{{\n{testSteps}}}");
                return Engine.Eval(testSteps, null, new ParserOptions() { AllowsSideEffects = true });
            }
        }

        public async Task UpdatePowerFxModelAsync()
        {
            if (Engine == null)
            {
                Logger.LogError("Engine is null, make sure to call Setup first");
                throw new InvalidOperationException();
            }

            await _powerAppFunctions.CheckAndHandleIfLegacyPlayerAsync();
            await PollingHelper.PollAsync<bool>(false, (x) => !x, () => _powerAppFunctions.CheckIfAppIsIdleAsync(), _testState.GetTestSettings().Timeout, _singleTestInstanceState.GetLogger(), "Something went wrong when Test Engine tried to get App status.");

            var controlRecordValues = await _powerAppFunctions.LoadPowerAppsObjectModelAsync();
            foreach (var control in controlRecordValues)
            {
                Engine.UpdateVariable(control.Key, control.Value);
            }
        }

        public IPowerAppFunctions GetPowerAppFunctions()
        {
            return _powerAppFunctions;
        }
    }
}
