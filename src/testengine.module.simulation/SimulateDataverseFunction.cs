// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace testengine.module
{
    /// <summary>
    /// This will execute Simulate request to Dataverse
    /// </summary>
    public class SimulateDataverseFunction : ReflectionFunction
    {
        private readonly ITestInfraFunctions _testInfraFunctions;
        private readonly ITestState _testState;
        private readonly ILogger _logger;

        private static readonly RecordType dataverseRequest = RecordType.Empty()
                .Add(new NamedFormulaType("Action", StringType.String))
                .Add(new NamedFormulaType("Entity", StringType.String))
                .Add(new NamedFormulaType("Then", TableType.Blank))
                .Add(new NamedFormulaType("When", TableType.Blank));

        public SimulateDataverseFunction(ITestInfraFunctions testInfraFunctions, ITestState testState, ILogger logger)
            : base(DPath.Root.Append(new DName("Experimental")), "SimulateDataverse", FormulaType.Blank, dataverseRequest)
        {
            _testInfraFunctions = testInfraFunctions;
            _testState = testState;
            _logger = logger;
        }

        public BlankValue Execute(RecordValue when)
        {
            _logger.LogInformation("------------------------------\n\n" +
                "Executing SimulateDataverse function.");

            //TODO Add Routing Async to page

            return FormulaValue.NewBlank();
        }
    }
}

