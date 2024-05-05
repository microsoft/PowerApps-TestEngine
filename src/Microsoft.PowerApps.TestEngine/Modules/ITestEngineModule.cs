// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Providers;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;

namespace Microsoft.PowerApps.TestEngine.Modules
{
    public interface ITestEngineModule
    {
        void ExtendBrowserContextOptions(BrowserNewContextOptions options, TestSettings settings);
        void RegisterPowerFxFunction(PowerFxConfig config, ITestInfraFunctions testInfraFunctions, ITestWebProvider testWebProvider, ISingleTestInstanceState singleTestInstanceState, ITestState testState, IFileSystem fileSystem);
        Task RegisterNetworkRoute(ITestState state, ISingleTestInstanceState singleTestInstanceState, IFileSystem fileSystem, IPage Page, NetworkRequestMock mock);
    }
}
