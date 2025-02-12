// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.ComponentModel.Composition;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Helpers;
using Microsoft.PowerApps.TestEngine.Providers.PowerFxModel;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.PowerFx.Dataverse;
using testengine.provider.dataverse;
using System.Globalization;

namespace Microsoft.PowerApps.TestEngine.Providers
{
    /// <summary>
    /// Test Engine Provider for interacting with the Microsoft Dataverse via REST API
    /// </summary>
    [Export(typeof(ITestWebProvider))]
    public class DataverseProvider : IExtendedTestWebProvider
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

        private ServiceClient _organizationService = null;
        private RecalcEngine _engine = null;

        public DataverseProvider()
        {

        }

        public DataverseProvider(ITestInfraFunctions? testInfraFunctions, ISingleTestInstanceState? singleTestInstanceState, ITestState? testState, IEnvironmentVariable environment)
        {
            this.TestInfraFunctions = testInfraFunctions;
            this.SingleTestInstanceState = singleTestInstanceState;
            this.TestState = testState;
            this.Environment = environment;
        }

        public string Name { get { return "dataverse"; } }

        public bool ProviderExecute { 
            get { 
                return true;
            } 
        }

        private async Task<T> GetPropertyValueFromControlAsync<T>(ItemPath itemPath)
        {
            throw new NotImplementedException();
        }

        public T GetPropertyValueFromControl<T>(ItemPath itemPath)
        {
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
        /// Not required for Dataverse interaction
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
        /// Not currently required for Dataverse as Power Fx state and managed by interacting with REST API
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
        /// Not currently used as no properties that are added into Power Fx state
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
            Console.WriteLine("NOTE: Dataverse Provider is Experimental to should not be used for Production usage");
            var api = new Uri(TestState.GetDomain());
            _organizationService = new ServiceClient(api, (url) => Task.FromResult(new AzureCliHelper().GetAccessToken(api)));
            _organizationService.Connect();
            var names = _organizationService.GetTableDisplayNames();
        }

        /// <summary>
        /// Add Dataverse specific functbions
        /// </summary>
        /// <param name="powerFxConfig"></param>
        public void ConfigurePowerFx(PowerFxConfig powerFxConfig)
        {
        }

        /// <summary>
        /// Add Dataverse state and messages
        /// </summary>
        /// <param name="engine"></param>
        public void ConfigurePowerFxEngine(RecalcEngine engine)
        {
            _engine = engine;
            engine.EnableDelegation();
        }

        /// <summary>
        /// Execute the Power Fx with the Dataverse connection
        /// </summary>
        /// <param name="steps"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public FormulaValue ExecutePowerFx(string steps, CultureInfo culture)
        {
            var connection = SingleOrgPolicy.New(_organizationService);
            
            var config = new RuntimeConfig(connection.SymbolValues);
            config.AddDataverseExecute(_organizationService);
            var waiter = _engine.EvalAsync(steps, CancellationToken.None,runtimeConfig: config).GetAwaiter();

            while (!waiter.IsCompleted)
            {
                Thread.Sleep(1000);
            }

            return waiter.GetResult();
        }
    }
}
