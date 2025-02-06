// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Concurrent;
using System.ComponentModel.Composition;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Helpers;
using Microsoft.PowerApps.TestEngine.Providers.Functions;
using Microsoft.PowerApps.TestEngine.Providers.PowerFxModel;
using Microsoft.PowerApps.TestEngine.Providers.Services;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;
using testengine.provider.copilot.portal.services;

namespace Microsoft.PowerApps.TestEngine.Providers
{
    /// <summary>
    /// Test Engine Provider for interacting with the Microsoft Copilot using DirectLine API
    /// </summary>
    [Export(typeof(ITestWebProvider))]
    public class CopilotAPIProvider : IExtendedTestWebProvider, IMessageProvider
    {
        // Not required for this provider
        public string CheckTestEngineObject => String.Empty;

        public string BaseEnviromentUrl { get; set; } = "";

        public ITestInfraFunctions? TestInfraFunctions { get; set; }

        public ISingleTestInstanceState? SingleTestInstanceState { get; set; }

        public ITestState? TestState { get; set; }

        public ITestProviderState? ProviderState { get; set; }

        public IEnvironmentVariable? Environment { get; set; } = new EnvironmentVariable();

        public string[] Namespaces => new string[] { "Experimental" };

        private ICopilotApiService _apiService = null;

        public Func<IHttpClientWrapper> GetHttpWrapper = () => { return new HttpClientWrapper(new HttpClient()); };

        public CopilotAPIProvider()
        {

        }

        public CopilotAPIProvider(ITestInfraFunctions? testInfraFunctions, ISingleTestInstanceState? singleTestInstanceState, ITestState? testState, IEnvironmentVariable environment)
        {
            this.TestInfraFunctions = testInfraFunctions;
            this.SingleTestInstanceState = singleTestInstanceState;
            this.TestState = testState;
            this.Environment = environment;
        }

        public string Name { get { return "copilot.api"; } }

        private async Task<T> GetPropertyValueFromControlAsync<T>(ItemPath itemPath)
        {
            throw new NotImplementedException();
        }

        public T GetPropertyValueFromControl<T>(ItemPath itemPath)
        {
            if (itemPath.PropertyName == "ConversationId" && _apiService.ConversationId is T)
            {
                return (T)Convert.ChangeType(value: _apiService.ConversationId, typeof(T));
            }
            throw new InvalidOperationException($"Property '{itemPath.PropertyName}' not found.");
        }

        /// <summary>
        ///  Check if the portal is ready to start the test
        /// </summary>
        /// <returns></returns>
        public async Task<bool> CheckIsIdleAsync()
        {
            return true;
        }

        /// <summary>
        /// Not currently required for Copilot Portal provider as Power Fx State is created from observed messages
        /// </summary>
        /// <param name="controlDictionary"></param>
        /// <returns></returns>
        private async Task<Dictionary<string, ControlRecordValue>> LoadObjectModelAsyncHelper(Dictionary<string, ControlRecordValue> controlDictionary)
        {
            try
            {
                return controlDictionary;
            }

            catch (Exception ex)
            {
                ExceptionHandlingHelper.CheckIfOutDatedPublishedApp(ex, SingleTestInstanceState.GetLogger());
                throw;
            }
        }

        /// <summary>
        /// Not required for Microsoft Copilot Studio portal interaction
        /// </summary>
        /// <returns></returns>

        private async Task<string> GetPowerAppsTestEngineObject()
        {
            var result = "true";

            try
            {
                return "{}";
            }
            catch (NullReferenceException) { }

            return result;
        }

        /// <summary>
        /// Not currently required for Microsoft Copilot Studio as Power Fx state and managed by interacting with DirectLine API
        /// </summary>
        /// <returns></returns>
        public async Task CheckProviderAsync()
        {

        }

        /// <summary>
        /// Load an empty Object model by default as additional state will be created in Copilot record
        /// </summary>
        /// <returns></returns>
        public async Task<Dictionary<string, ControlRecordValue>> LoadObjectModelAsync()
        {
            var controlDictionary = new Dictionary<string, ControlRecordValue>();

            return controlDictionary;
        }

        /// <summary>
        /// Not currently implemented. Could be used to select controls with adaptive cards
        /// </summary>
        /// <param name="itemPath"></param>
        /// <returns></returns>
        public async Task<bool> SelectControlAsync(ItemPath itemPath)
        {
            // TODO
            return true;
        }

        /// <summary>
        /// Not currently implemented. Could be used to update variable state of the Copilot
        /// </summary>
        /// <param name="itemPath"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task<bool> SetPropertyAsync(ItemPath itemPath, FormulaValue value)
        {
            // TODO
            return true;
        }

        /// <summary>
        /// Not currently used as no properties apart from Copilot record are added into Power Fx state
        /// </summary>
        /// <param name="itemPath"></param>
        /// <returns></returns>

        public int GetItemCount(ItemPath itemPath)
        {
            return 0;
        }

        public async Task<object> GetDebugInfo()
        {
            try
            {
                return new Dictionary<string, object>();
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Setup the initial state of the portal for testing
        /// </summary>
        /// <returns></returns>
        public async Task<bool> TestEngineReady()
        {
            return true;
        }

        /// <summary>ovider
        /// Generate empty url as not required by this pr
        /// </summary>
        /// <param name="domain">Not used</param>
        /// <param name="additionalQueryParams"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public string GenerateTestUrl(string domain, string additionalQueryParams)
        {
            return "about:blank";
        }

        /// <summary>
        /// Configure Direct Line for Configured
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        public async Task SetupContext()
        {
            Console.WriteLine("NOTE: Microsoft Copilot Studio Provider is Experimental to should not be used for Production usage");

            var context = TestInfraFunctions.GetContext();

            var apiType = TestState.GetTestSettings().ExtensionModules.Parameters.ContainsKey("ApiType") ? TestState.GetTestSettings()?.ExtensionModules?.Parameters["ApiType"] : string.Empty;

            switch (apiType) {
                case "directline":
                    var directLineService = new DirectLineApiService(GetHttpWrapper(), SingleTestInstanceState.GetLogger());

                    var variableName = TestState.GetTestSettings().ExtensionModules.Parameters.ContainsKey("AgentKey") ? TestState.GetTestSettings()?.ExtensionModules?.Parameters["AgentKey"] : "AgentKey";
                    var botFrameworkUrl = TestState.GetTestSettings().ExtensionModules.Parameters.ContainsKey("BotFrameworkUrl") ? TestState.GetTestSettings()?.ExtensionModules?.Parameters["BotFrameworkUrl"] : string.Empty;

                    if (string.IsNullOrEmpty(variableName))
                    {
                        throw new InvalidDataException("Missing Agent Key from testSettings and extensionModules");
                    }

                    directLineService.Secret = Environment.GetVariable(variableName);

                    if (string.IsNullOrEmpty(botFrameworkUrl))
                    {
                        botFrameworkUrl = "https://directline.botframework.com";
                    }

                    directLineService.BotFrameworkUrl = new Uri(botFrameworkUrl);

                    _apiService = directLineService;
                    break;
                default:
                    var directToEngineService = new DirectToEngineService(this);

                    directToEngineService.BotIdentifier = TestState.GetTestSuiteDefinition().AppId;
                    directToEngineService.EnvironmentId = TestState.GetEnvironment();
                    directToEngineService.Token = Environment.GetVariable("AgentToken");


                    _apiService = directToEngineService;
                    break;

            }


        }

        /// <summary>
        /// Add Copilot specific functbions
        /// </summary>
        /// <param name="powerFxConfig"></param>
        public void ConfigurePowerFx(PowerFxConfig powerFxConfig)
        {
            // Add 
            powerFxConfig.AddFunction(new ApiConnectFunction(_apiService, SingleTestInstanceState.GetLogger()));
            powerFxConfig.AddFunction(new ApiSendTextFunction(_apiService, SingleTestInstanceState.GetLogger()));
            powerFxConfig.AddFunction(new WaitUntilMessageFunction(TestInfraFunctions, TestState, SingleTestInstanceState.GetLogger(), this));
        }

        /// <summary>
        /// Add Copilot state and messages
        /// </summary>
        /// <param name="engine"></param>
        public void ConfigurePowerFxEngine(RecalcEngine engine)
        {
            // Add DirectLine derived state
            engine.UpdateVariable("Copilot", new CopilotStateRecordValue(this));
        }

        public async Task GetNewMessages()
        {
            await _apiService.GetResponseAsync(this);
        }

        /// <summary>
        /// Json messages observed as part of the test session
        /// </summary>
        public ConcurrentQueue<string> Messages { get; private set; } = new ConcurrentQueue<string>();
    }
}
