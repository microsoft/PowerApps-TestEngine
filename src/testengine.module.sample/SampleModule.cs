using Microsoft.PowerFx.Types;
using Microsoft.PowerFx;
using System.ComponentModel.Composition;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Modules;
using Microsoft.PowerApps.TestEngine.PowerApps;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.Playwright;

namespace testengine.module.sample
{
    

    [Export(typeof(ITestEngineModule))]
    public class TestEngineSampleModule : ITestEngineModule
    {
        public void ExtendBrowserContextOptions(BrowserNewContextOptions options, TestSettings settings)
        {

        }

        public void RegisterPowerFxFunction(PowerFxConfig config, ITestInfraFunctions testInfraFunctions, IPowerAppFunctions powerAppFunctions, ISingleTestInstanceState singleTestInstanceState,  ITestState testState, IFileSystem fileSystem) {
            ILogger logger = singleTestInstanceState.GetLogger();
            config.AddFunction(new SampleFunction());
            logger.LogInformation("Registered Sample()");
        }

        public async Task RegisterNetworkRoute(ITestState testState, ISingleTestInstanceState singleTestInstanceState, IFileSystem fileSystem, IPage Page, NetworkRequestMock mock) {
            return;
        }
    }
}
