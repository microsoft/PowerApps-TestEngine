// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Helpers;
using Microsoft.PowerApps.TestEngine.PowerFx.Functions;
using Microsoft.PowerApps.TestEngine.Providers;
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
        private readonly ITestInfraFunctions TestInfraFunctions;
        private readonly ITestWebProvider _testWebProvider;
        private readonly IFileSystem _fileSystem;
        private readonly ISingleTestInstanceState SingleTestInstanceState;
        private readonly ITestState TestState;
        private int _retryLimit = 2;

        private RecalcEngine Engine { get; set; }
        private ILogger Logger { get { return SingleTestInstanceState.GetLogger(); } }

        public PowerFxEngine(ITestInfraFunctions testInfraFunctions,
                             ITestWebProvider testWebProvider,
                             ISingleTestInstanceState singleTestInstanceState,
                             ITestState testState,
                             IFileSystem fileSystem)
        {
            TestInfraFunctions = testInfraFunctions;
            _testWebProvider = testWebProvider;
            SingleTestInstanceState = singleTestInstanceState;
            TestState = testState;
            _fileSystem = fileSystem;
        }

        public void Setup()
        {
            var powerFxConfig = new PowerFxConfig(Features.PowerFxV1);

            var vals = new SymbolValues();
            var symbols = (SymbolTable)vals.SymbolTable;
            symbols.EnableMutationFunctions();
            powerFxConfig.SymbolTable = symbols;

            // Enabled to allow ability to set variable and collection state that can be used with providers and as test variables
            powerFxConfig.EnableSetFunction();

            powerFxConfig.AddFunction(new SelectOneParamFunction(_testWebProvider, async () => await UpdatePowerFxModelAsync(), Logger));
            powerFxConfig.AddFunction(new SelectTwoParamsFunction(_testWebProvider, async () => await UpdatePowerFxModelAsync(), Logger));
            powerFxConfig.AddFunction(new SelectThreeParamsFunction(_testWebProvider, async () => await UpdatePowerFxModelAsync(), Logger));
            powerFxConfig.AddFunction(new ScreenshotFunction(TestInfraFunctions, SingleTestInstanceState, _fileSystem, Logger));
            powerFxConfig.AddFunction(new AssertWithoutMessageFunction(Logger));
            powerFxConfig.AddFunction(new AssertFunction(Logger));
            powerFxConfig.AddFunction(new SetPropertyFunction(_testWebProvider, Logger));

            var settings = TestState.GetTestSettings();
            if (settings != null && settings.ExtensionModules != null && settings.ExtensionModules.Enable)
            {
                if (TestState.GetTestEngineModules().Count == 0)
                {
                    Logger.LogError("Extension enabled, none loaded");
                }
                foreach (var module in TestState.GetTestEngineModules())
                {

                    module.RegisterPowerFxFunction(powerFxConfig, TestInfraFunctions, _testWebProvider, SingleTestInstanceState, TestState, _fileSystem);
                }
            }
            else
            {
                if (TestState.GetTestEngineModules().Count > 0)
                {
                    Logger.LogInformation("Extension loaded but not enabled");
                }
            }

            WaitRegisterExtensions.RegisterAll(powerFxConfig, TestState.GetTimeout(), Logger);

            Engine = new RecalcEngine(powerFxConfig);

            var symbolValues = new SymbolValues(powerFxConfig.SymbolTable);
            foreach (var val in powerFxConfig.SymbolTable.SymbolNames.ToList())
            {
                // TODO
                if (powerFxConfig.SymbolTable.TryLookupSlot(val.Name, out ISymbolSlot slot))
                {
                    Engine.UpdateVariable(val.Name, symbolValues.Get(slot));
                    powerFxConfig.SymbolTable.RemoveVariable(val.Name);
                }
            }
        }

        public async Task ExecuteWithRetryAsync(string testSteps, CultureInfo culture)
        {
            int currentRetry = 0;
            FormulaValue result = FormulaValue.NewBlank();

            while (currentRetry <= _retryLimit)
            {
                try
                {
                    result = Execute(testSteps, culture);
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

        public FormulaValue Execute(string testSteps, CultureInfo culture)
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

            var goStepByStep = TestState.ExecuteStepByStep;

            // Check if the syntax is correct
            var checkResult = Engine.Check(testSteps, null, GetPowerFxParserOptions(culture));
            if (!checkResult.IsSuccess)
            {
                // If it isn't, we have to go step by step as the object model isn't fully loaded
                goStepByStep = true;
                Logger.LogDebug($"Syntax check failed. Now attempting to execute lines step by step");
            }

            if (goStepByStep)
            {
                var splitSteps = PowerFxHelper.ExtractFormulasSeparatedByChainingOperator(Engine, checkResult, culture);
                FormulaValue result = FormulaValue.NewBlank();

                int stepNumber = 0;

                foreach (var step in splitSteps)
                {
                    TestState.OnBeforeTestStepExecuted(new TestStepEventArgs { TestStep = step, StepNumber = stepNumber, Engine = Engine });

                    Logger.LogTrace($"Attempting:{step.Replace("\n", "").Replace("\r", "")}");
                    result = Engine.Eval(step, null, new ParserOptions() { AllowsSideEffects = true, Culture = culture, NumberIsFloat = true });

                    TestState.OnAfterTestStepExecuted(new TestStepEventArgs { TestStep = step, Result = result, StepNumber = stepNumber, Engine = Engine });
                    stepNumber++;
                }
                return result;
            }
            else
            {
                var values = new SymbolValues();
                Logger.LogTrace($"Attempting:\n\n{{\n{testSteps}}}");
                TestState.OnBeforeTestStepExecuted(new TestStepEventArgs { TestStep = testSteps, StepNumber = null, Engine = Engine });
                var result = Engine.Eval(testSteps, null, new ParserOptions() { AllowsSideEffects = true, Culture = culture, NumberIsFloat = true });
                TestState.OnAfterTestStepExecuted(new TestStepEventArgs { TestStep = testSteps, Result = result, StepNumber = 1 });
                return result;
            }
        }

        public async Task UpdatePowerFxModelAsync()
        {
            if (Engine == null)
            {
                Logger.LogError("Engine is null, make sure to call Setup first");
                throw new InvalidOperationException();
            }

            if (!PowerAppIntegrationEnabled)
            {
                return;
            }

            await PollingHelper.PollAsync<bool>(false, (x) => !x, () => _testWebProvider.CheckIsIdleAsync(), TestState.GetTestSettings().Timeout, SingleTestInstanceState.GetLogger(), "Something went wrong when Test Engine tried to get App status.");

            var controlRecordValues = await _testWebProvider.LoadObjectModelAsync();
            foreach (var control in controlRecordValues)
            {
                Engine.UpdateVariable(control.Key, control.Value);
            }
        }

        private static ParserOptions GetPowerFxParserOptions(CultureInfo culture)
        {
            // Currently support for decimal is in progress for PowerApps
            // Power Fx by default treats number as decimal. Hence setting NumberIsFloat config to true in our case

            // TODO: Evuate culture evaluate across languages
            return new ParserOptions() { AllowsSideEffects = true, Culture = culture, NumberIsFloat = true };
        }

        public ITestWebProvider GetWebProvider()
        {
            return _testWebProvider;
        }

        public async Task RunRequirementsCheckAsync()
        {
            await _testWebProvider.CheckProviderAsync();
            await _testWebProvider.TestEngineReady();
        }

        public bool PowerAppIntegrationEnabled { get; set; } = true;
    }
}
