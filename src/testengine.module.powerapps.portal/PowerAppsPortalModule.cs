﻿// Copyright (c) Microsoft Corporation.
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

namespace testengine.module
{
    [Export(typeof(ITestEngineModule))]
    public class PowerAppsPortalModule : ITestEngineModule
    {
        public void ExtendBrowserContextOptions(BrowserNewContextOptions options, TestSettings settings)
        {

        }

        public void RegisterPowerFxFunction(PowerFxConfig config, ITestInfraFunctions testInfraFunctions, ITestWebProvider testWebProvider, ISingleTestInstanceState singleTestInstanceState, ITestState testState, IFileSystem fileSystem)
        {
            ILogger logger = singleTestInstanceState.GetLogger();
            config.AddFunction(new GetConnectionsFunction(testInfraFunctions, testState, logger));
            logger.LogInformation("Registered TestEngine.GetConnections()");
            config.AddFunction(new ExportConnectionsFunction(testInfraFunctions, testState, logger));
            logger.LogInformation("Registered TestEngine.ExportConnections()");
            config.AddFunction(new CreateConnectionFunction(testInfraFunctions, testState, logger));
            logger.LogInformation("Registered TestEngine.CreateConnection()");
            config.AddFunction(new CheckConnectionExistsFunction(testInfraFunctions, testState, logger));
            logger.LogInformation("Registered TestEngine.CheckConnectionExists()");
            config.AddFunction(new UpdateConnectionReferencesFunction(testInfraFunctions, testState, logger));
            logger.LogInformation("Registered TestEngine.UpdateConnectionReferences()");
            config.AddFunction(new SelectSectionFunction(testInfraFunctions, testState, logger));
            logger.LogInformation("Registered TestEngine.SelectSection()");
        }

        public async Task RegisterNetworkRoute(ITestState state, ISingleTestInstanceState singleTestInstanceState, IFileSystem fileSystem, IPage Page, NetworkRequestMock mock)
        {
            await Task.CompletedTask;
        }
    }
}
