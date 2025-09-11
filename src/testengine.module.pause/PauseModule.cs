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
            // Initialize from provided settings if available
            UpdatePreviewNamespaceProperty(settings);
        }

        public void RegisterPowerFxFunction(PowerFxConfig config, ITestInfraFunctions testInfraFunctions, ITestWebProvider testWebProvider, ISingleTestInstanceState singleTestInstanceState, ITestState testState, IFileSystem fileSystem)
        {
            // Initialize the property from YAML via TestState
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
                // In unit tests with strict mocks GetTestSettings may not be setup; ignore and proceed with null
                testSettings = null;
            }
            UpdatePreviewNamespaceProperty(testSettings);

            ILogger logger = singleTestInstanceState.GetLogger();
            
            // Register the Pause function in root namespace (always available as Pause())
            config.AddFunction(new PauseFunction(testInfraFunctions, testState, logger, this));
            
            logger.LogInformation($"Registered Pause() function. Preview namespace enabled in YAML: {IsPreviewNamespaceEnabled}");
        }

        public async Task RegisterNetworkRoute(ITestState state, ISingleTestInstanceState singleTestInstanceState, IFileSystem fileSystem, IPage Page, NetworkRequestMock mock)
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// Updates the IsPreviewNamespaceEnabled property based on YAML settings.
        /// This method reads the YAML configuration to determine if Preview namespace is enabled.
        /// </summary>
        private void UpdatePreviewNamespaceProperty(TestSettings settings)
        {
            if (settings?.ExtensionModules?.AllowPowerFxNamespaces != null)
            {
                // Check if "Preview" is explicitly listed in YAML allowPowerFxNamespaces
                bool wasEnabled = IsPreviewNamespaceEnabled;
                IsPreviewNamespaceEnabled = settings.ExtensionModules.AllowPowerFxNamespaces.Contains("Preview");
                
                // Log changes for debugging purposes
                if (wasEnabled != IsPreviewNamespaceEnabled)
                {
                    Console.WriteLine($"PauseModule: IsPreviewNamespaceEnabled changed from {wasEnabled} to {IsPreviewNamespaceEnabled}");
                }
            }
            else
            {
                IsPreviewNamespaceEnabled = false;
            }
        }
    }
}
