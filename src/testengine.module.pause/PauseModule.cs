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
        /// True when YAML testSettings.extensionModules.allowPowerFxNamespaces contains "Preview".
        /// Read-only externally; set during registration when TestSettings are available.
        /// </summary>
        public bool IsPreviewNamespaceEnabled { get; private set; } = false;

        public void ExtendBrowserContextOptions(BrowserNewContextOptions options, TestSettings settings)
        {
            // If called first, try to initialize from provided settings as well
            if (settings != null && settings.ExtensionModules != null && settings.ExtensionModules.AllowPowerFxNamespaces != null)
            {
                IsPreviewNamespaceEnabled = settings.ExtensionModules.AllowPowerFxNamespaces.Contains("Preview");
            }
        }

        public void RegisterPowerFxFunction(PowerFxConfig config, ITestInfraFunctions testInfraFunctions, ITestWebProvider testWebProvider, ISingleTestInstanceState singleTestInstanceState, ITestState testState, IFileSystem fileSystem)
        {
            // Initialize the read-only property from YAML via TestState
            var testSettings = testState?.GetTestSettings();
            IsPreviewNamespaceEnabled = testSettings?.ExtensionModules?.AllowPowerFxNamespaces?.Contains("Preview") == true;

            ILogger logger = singleTestInstanceState.GetLogger();
            // Pass this PauseModule instance to PauseFunction so it can check IsPreviewNamespaceEnabled
            config.AddFunction(new PauseFunction(testInfraFunctions, testState, logger, this));
            logger.LogInformation($"Registered Pause() with Preview namespace enabled: {IsPreviewNamespaceEnabled}");
        }

        public async Task RegisterNetworkRoute(ITestState state, ISingleTestInstanceState singleTestInstanceState, IFileSystem fileSystem, IPage Page, NetworkRequestMock mock)
        {
            await Task.CompletedTask;
        }
    }
}
