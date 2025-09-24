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

namespace testengine.module
{
    [Export(typeof(ITestEngineModule))]
    public class PauseModule : ITestEngineModule
    {
        /// <summary>
        /// Indicates whether Preview namespace is enabled in YAML testSettings.extensionModules.allowPowerFxNamespaces.
        /// </summary>
        public virtual bool IsPreviewNamespaceEnabled { get; private set; } = false;

        public void ExtendBrowserContextOptions(BrowserNewContextOptions options, TestSettings settings)
        {
            UpdatePreviewNamespaceProperty(settings);
        }

        public void RegisterPowerFxFunction(PowerFxConfig config, ITestInfraFunctions testInfraFunctions, ITestWebProvider testWebProvider, ISingleTestInstanceState singleTestInstanceState, ITestState testState, IFileSystem fileSystem)
        {
            TestSettings testSettings = null;
            try
            {
                if (testState != null)
                {
                    testSettings = testState.GetTestSettings();
                }
            }
            catch
            {
                testSettings = null;
            }
            UpdatePreviewNamespaceProperty(testSettings);

            ILogger logger = singleTestInstanceState.GetLogger();

            // Only register Pause() function if Preview namespace is enabled
            if (IsPreviewNamespaceEnabled)
            {
                config.AddFunction(new PauseFunction(testInfraFunctions, testState, logger));
                logger.LogInformation("Registered Pause()");
            }
            else
            {
                logger.LogInformation("Skip registering Pause() - Preview namespace not enabled");
            }
        }

        public async Task RegisterNetworkRoute(ITestState state, ISingleTestInstanceState singleTestInstanceState, IFileSystem fileSystem, IPage Page, NetworkRequestMock mock)
        {
            await Task.CompletedTask;
        }

        private void UpdatePreviewNamespaceProperty(TestSettings settings)
        {
            IsPreviewNamespaceEnabled = settings?.ExtensionModules?.AllowPowerFxNamespaces?.Contains("Preview") ?? false;
        }
    }
}
