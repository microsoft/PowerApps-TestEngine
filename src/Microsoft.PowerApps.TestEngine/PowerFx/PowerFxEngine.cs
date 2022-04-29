// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.PowerApps;
using Microsoft.PowerApps.TestEngine.PowerFx.Functions;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Public;
using Microsoft.PowerFx.Core.Public.Values;

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

        private RecalcEngine? Engine { get; set; }
        private ILogger Logger { get { return _singleTestInstanceState.GetLogger(); } }

        public PowerFxEngine(ITestInfraFunctions testInfraFunctions, 
                             IPowerAppFunctions powerAppFunctions, 
                             ISingleTestInstanceState singleTestInstanceState,
                             IFileSystem fileSystem)
        {
            _testInfraFunctions = testInfraFunctions;
            _powerAppFunctions = powerAppFunctions;
            _singleTestInstanceState = singleTestInstanceState;
            _fileSystem = fileSystem;
        }

        public void Setup()
        {
            var powerFxConfig = new PowerFxConfig();
            powerFxConfig.AddFunction(new ScreenshotFunction(_testInfraFunctions, _singleTestInstanceState, _fileSystem));
            powerFxConfig.AddFunction(new WaitFunction());
            powerFxConfig.AddFunction(new SelectFunction(_powerAppFunctions));
            powerFxConfig.AddFunction(new AssertFunction(Logger));
            Engine = new RecalcEngine(powerFxConfig);
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

            Logger.LogInformation($"Executing {testSteps}");
            return Engine.Eval(testSteps, null, new ParserOptions() { AllowsSideEffects = true });
        }

        public void UpdateVariable(string name, IUntypedObject value)
        {
            if (Engine == null)
            {
                Logger.LogError("Engine is null, make sure to call Setup first");
                throw new InvalidOperationException("Engine is null, make sure to call Setup first");
            }
            else
            {
                Engine.UpdateVariable(name, FormulaValue.New(value));
            }
        }
    }
}
