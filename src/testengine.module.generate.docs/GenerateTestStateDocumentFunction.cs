// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;

namespace testengine.module
{
    /// <summary>
    /// This will pause the current test and allow the user to interact with the browser and inspect state when headless mode is false
    /// </summary>
    public class GenerateTestStateDocumentFunction : ReflectionFunction
    {
        private readonly ITestInfraFunctions _testInfraFunctions;
        private readonly ITestState _testState;
        private readonly ILogger _logger;
        private readonly PowerFxConfig _config;

        public GenerateTestStateDocumentFunction(ITestInfraFunctions testInfraFunctions, ITestState testState, ILogger logger, PowerFxConfig config)
            : base("GenerateTestStateDocument", FormulaType.Blank, StringType.String)
        {
            _testInfraFunctions = testInfraFunctions;
            _testState = testState;
            _logger = logger;
            _config = config;
        }

        public BlankValue Execute(StringValue file)
        {
            using ( var output = new StreamWriter(file.Value) )
            {
                output.WriteLine("Functions");
                foreach (var function in _config.SymbolTable.FunctionNames)
                {
                    output.WriteLine(function);
                }

                output.WriteLine("Variables");
                foreach ( var symbol in _config.SymbolTable.SymbolNames)
                {
                    output.WriteLine(symbol.DisplayName.Value);
                }
                output.WriteLine();
            }
            return FormulaValue.NewBlank();
        }
    }
}

