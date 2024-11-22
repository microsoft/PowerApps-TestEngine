// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.ComponentModel.Composition;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Modules;
using Microsoft.PowerApps.TestEngine.Providers;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;

namespace testengine.module
{
    [Export(typeof(ITestEngineModule))]
    public class SimulationModule : ITestEngineModule
    {
        public void ExtendBrowserContextOptions(BrowserNewContextOptions options, TestSettings settings)
        {

        }

        public void RegisterPowerFxFunction(PowerFxConfig config, ITestInfraFunctions testInfraFunctions, ITestWebProvider testWebProvider, ISingleTestInstanceState singleTestInstanceState, ITestState testState, IFileSystem fileSystem)
        {
            ILogger logger = singleTestInstanceState.GetLogger();

            // TODO: Determine how Record can be used as value for action argument. Current value is evaluated as empty
            RecordValue actionValues = RecordValue.NewRecordFromFields(
                new NamedValue("Query", FormulaValue.New("query")),
                new NamedValue("Create", FormulaValue.New("create"))
            );

            var variable = config.SymbolTable.AddVariable("DataverseAction", actionValues.Type, mutable: false);

            var values = new SymbolValues(config.SymbolTable);
            values.Set(variable, actionValues);

            config.SymbolTable.CreateValues(values);

            config.AddFunction(new SimulateDataverseFunction(testInfraFunctions, testState, logger));
            logger.LogInformation("Registered Experimental.SimulateDataverse()");

            config.AddFunction(new SimulateConnectorFunction(testInfraFunctions, testState, logger));
            logger.LogInformation("Registered Experimental.SimulateConnector()");
        }

        public async Task RegisterNetworkRoute(ITestState state, ISingleTestInstanceState singleTestInstanceState, IFileSystem fileSystem, IPage Page, NetworkRequestMock mock)
        {
            await Task.CompletedTask;
        }
    }
}
