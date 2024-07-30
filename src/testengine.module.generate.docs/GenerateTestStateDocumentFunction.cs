// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace testengine.module
{
    /// <summary>
    /// This will Generate Test State Document
    /// </summary>
    public class GenerateTestStateDocumentFunction : ReflectionFunction
    {
        private readonly ITestInfraFunctions _testInfraFunctions;
        private readonly ITestState _testState;
        private readonly ILogger _logger;
        private readonly PowerFxConfig _config;

        public GenerateTestStateDocumentFunction(ITestInfraFunctions testInfraFunctions, ITestState testState, ILogger logger, PowerFxConfig config)
            : base(DPath.Root.Append(new DName("TestEngine")), "GenerateTestStateDocument", FormulaType.Blank, StringType.String)
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
                var state = _testState.GetPowerFxState();

                output.WriteLine("Functions");
#pragma warning disable CS0618 // Type or member is obsolete
                // TODO: Determin SymbolTable alternatice
                foreach ( var function in state.Config.FunctionInfos )
#pragma warning restore CS0618 // Type or member is obsolete
                {
                    output.WriteLine( function.Name );
                }

                output.WriteLine("Variables");
                foreach ( var variable in state.Symbols.SymbolNames )
                {
                    output.WriteLine(variable.Name);

                    if (variable.Type is RecordType)
                    {
                        var record = variable.Type as RecordType;
                        if (record != null)
                        {
                            foreach (var item in record.FieldNames)
                            {
                                output.WriteLine($"  > {item}");
                            }
                        }
                    }
                }

                output.WriteLine();
            }
            return FormulaValue.NewBlank();
        }
    }
}

