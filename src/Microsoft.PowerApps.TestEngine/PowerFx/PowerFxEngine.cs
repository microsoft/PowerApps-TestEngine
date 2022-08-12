// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.PowerApps;
using Microsoft.PowerApps.TestEngine.PowerApps.PowerFxModel;
using Microsoft.PowerApps.TestEngine.PowerFx.Functions;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Public;
using Microsoft.PowerFx.Types;
using Newtonsoft.Json;

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

        private RecalcEngine? Engine { get; set; }
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

        public void Setup()
        {
            var powerFxConfig = new PowerFxConfig();
            powerFxConfig.AddFunction(new SelectOneParamFunction(_powerAppFunctions, async () => await UpdatePowerFxModelAsync()));
            powerFxConfig.AddFunction(new SelectTwoParamsFunction(_powerAppFunctions, async () => await UpdatePowerFxModelAsync()));
            powerFxConfig.AddFunction(new SelectThreeParamsFunction(_powerAppFunctions, async () => await UpdatePowerFxModelAsync()));
            powerFxConfig.AddFunction(new ScreenshotFunction(_testInfraFunctions, _singleTestInstanceState, _fileSystem));
            powerFxConfig.AddFunction(new AssertWithoutMessageFunction(Logger));
            powerFxConfig.AddFunction(new AssertFunction(Logger));
            powerFxConfig.AddFunction(new SetPropertyFunction(_powerAppFunctions));
            WaitRegisterExtensions.RegisterAll(powerFxConfig, _testState.GetTimeout());

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
                throw new InvalidOperationException("Engine is null, make sure to call Setup first");
            }

            // Remove the leading = sign
            if (testSteps.StartsWith('='))
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
                Logger.LogError($"Syntax check failed. Switching to step by step");
            }

            if (goStepByStep)
            {
                // TODO: This is a temporary hack to allow for multiple screens
                // Will need to come up with a better solution
                var splitSteps = testSteps.Split(';');
                FormulaValue result = FormulaValue.NewBlank();
                foreach (var step in splitSteps)
                {
                    Logger.LogInformation($"Executing {step}");
                    result = Engine.Eval(step);
                }
                return result;
            }
            else
            {
                Logger.LogInformation($"Executing {testSteps}");
                return Engine.Eval(testSteps, null, new ParserOptions() { AllowsSideEffects = true });
            }
        }

        public async Task UpdatePowerFxModelAsync()
        {
            if (Engine == null)
            {
                Logger.LogError("Engine is null, make sure to call Setup first");
                throw new InvalidOperationException("Engine is null, make sure to call Setup first");
            }

            var controlRecordValues = await _powerAppFunctions.LoadPowerAppsObjectModelAsync();
            foreach (var control in controlRecordValues)
            {
                Engine.UpdateVariable(control.Key, control.Value);
            }
        }
    }
}
