// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.ComponentModel.Composition;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Helpers;
using Microsoft.PowerApps.TestEngine.Providers.PowerFxModel;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerApps.TestEngine.Providers
{
    /// <summary>
    /// Functions for interacting with the Example provider
    /// </summary>
    [Export(typeof(ITestWebProvider))]
    public class ExampleProvider : ITestWebProvider
    {
        /// <summary>
        /// The base url that applys to resoures for this provider
        /// </summary>
        public string BaseEnviromentUrl { get; set; } = String.Empty;

        /// <summary>
        /// Contains test infrastructure instance to interact with Playwright
        /// </summary>
        public ITestInfraFunctions? TestInfraFunctions { get; set; }

        /// <summary>
        /// Contains information around the specific 
        /// </summary>
        public ISingleTestInstanceState? SingleTestInstanceState { get; set; }

        /// <summary>
        /// Contains test configuration information
        /// </summary>
        public ITestState? TestState { get; set; }

        /// <summary>
        /// Information on the state of the provider
        /// </summary>
        public ITestProviderState? ProviderState { get; set; }

        /// <summary>
        /// Indicate if provider should listen to and react to Power Fx test steps
        /// </summary>
        private bool _listenToPowerFxChanges = false;

        public ExampleProvider()
        {

        }

        /// <summary>
        /// Creates a new instace of provider
        /// </summary>
        /// <param name="testInfraFunctions"></param>
        /// <param name="singleTestInstanceState"></param>
        /// <param name="testState"></param>
        public ExampleProvider(ITestInfraFunctions? testInfraFunctions, ISingleTestInstanceState? singleTestInstanceState, ITestState? testState)
        {
            this.TestInfraFunctions = testInfraFunctions;
            this.SingleTestInstanceState = singleTestInstanceState;
            this.TestState = testState;

            UpdateState(testState);
        }

        /// <summary>
        /// Optionall subscribe to test state events
        /// </summary>
        /// <param name="state">The state to optionally be updated</param>
        private void UpdateState(ITestState state)
        {
            if (state != null && _listenToPowerFxChanges)
            {
                state.ExecuteStepByStep = true;

                state.BeforeTestStepExecuted += async (o, e) => await TestState_BeforeTestStepExecuted(o, e);
                state.AfterTestStepExecuted += async (o, e) => await TestState_AfterTestStepExecuted(o, e);
            }
        }

        /// <summary>
        /// Unique name of this provider
        /// </summary>
        public string Name { get { return "example"; } }

        /// <summary>
        /// Optional JavaScript to check Test Engine setup in Playwright
        /// </summary>
        public string CheckTestEngineObject => "";

        // <summary>
        /// Get a property value for a defined control
        /// </summary>
        /// <param name="itemPath">The control name and property to retreive</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public T GetPropertyValueFromControl<T>(ItemPath itemPath)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Determine if the provider is ready to apply test steps
        /// </summary>
        /// <returns></returns>
        public async Task<bool> CheckIsIdleAsync()
        {
            try
            {
                // TODO: Change to alternative means of determine if portal web page is ready
                var expression = "var element = document.getElementById('O365_MainLink_Settings'); if (typeof(element) != 'undefined' && element != null) { 'Idle' } else { 'Loading' }";
                return (await TestInfraFunctions.RunJavascriptAsync<string>(expression)) == "Idle";
            }
            catch (Exception ex)
            {
                if (ex.Message?.ToString() == ExceptionHandlingHelper.PublishedAppWithoutJSSDKErrorCode)
                {
                    ExceptionHandlingHelper.CheckIfOutDatedPublishedApp(ex, SingleTestInstanceState.GetLogger());
                    throw;
                }

                SingleTestInstanceState.GetLogger().LogDebug(ex.ToString());
                return false;
            }

        }

        /// <summary>
        /// Query the provider and return located controls and properties
        /// </summary>
        /// <param name="controlDictionary"></param>
        /// <returns></returns>
        private async Task<Dictionary<string, ControlRecordValue>> LoadObjectModelAsyncHelper(Dictionary<string, ControlRecordValue> controlDictionary)
        {
            try
            {
                // TODO: Return controls found in provider implementation

                return controlDictionary;
            }

            catch (Exception ex)
            {
                ExceptionHandlingHelper.CheckIfOutDatedPublishedApp(ex, SingleTestInstanceState.GetLogger());
                throw;
            }
        }

        /// <summary>
        /// Check provider is setup
        /// </summary>
        /// <returns></returns>
        public Task CheckProviderAsync()
        {
            // TODO: Add any code to check id provider ready
            return Task.CompletedTask;
        }

        /// <summary>
        /// Load list of controls and properties from the provider
        /// </summary>
        /// <returns></returns>
        public async Task<Dictionary<string, ControlRecordValue>> LoadObjectModelAsync()
        {
            var controlDictionary = new Dictionary<string, ControlRecordValue>();

            return controlDictionary;
        }

        /// <summary>
        /// Selects a control
        /// </summary>
        /// <param name="itemPath">The control to select</param>
        /// <returns></returns>
        public async Task<bool> SelectControlAsync(ItemPath itemPath)
        {
            // TODO
            return true;
        }

        /// <summary>
        /// Set the value for a control and property
        /// </summary>
        /// <param name="itemPath">The control to be updated</param>
        /// <returns></returns>
        public async Task<bool> SetPropertyAsync(ItemPath itemPath, FormulaValue value)
        {
            // TODO
            return true;
        }

        /// <summary>
        /// Get the total number items that match the item path
        /// </summary>
        /// <param name="itemPath">The path to search for</param>
        /// <returns></returns>
        public int GetItemCount(ItemPath itemPath)
        {
            return 0;
        }

        /// <summary>
        /// Return debug information to help debug errors that occur with the provider
        /// </summary>
        /// <returns></returns>
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
        /// Check if the provider is ready for the Test Engine to execute tests
        /// </summary>
        /// <returns></returns>
        public async Task<bool> TestEngineReady()
        {
            try
            {

                // To support webplayer version without ready function 
                // return true for this without interrupting the test run
                return true;
            }
            catch (Exception ex)
            {

                // If the error returned is anything other than PublishedAppWithoutJSSDKErrorCode capture that and throw
                SingleTestInstanceState.GetLogger().LogDebug(ex.ToString());
                throw;
            }
        }

        /// <summary>
        /// Generate a test url based on the user defined domain and query string
        /// </summary>
        /// <param name="domain">The optional domain to be applied. WIll be defaults if not empty</param>
        /// <param name="additionalQueryParams">Optional additional query parameters</param>
        /// <returns></returns>
        public string GenerateTestUrl(string domain, string additionalQueryParams)
        {
            if (string.IsNullOrEmpty(domain))
            { 
                domain = "about:blank";
            }

            //TODO: Determine
            //var environment = TestState.GetEnvironment();
            //if (string.IsNullOrEmpty(environment))
            //{
            //    SingleTestInstanceState.GetLogger().LogError("Environment cannot be empty.");
            //    throw new InvalidOperationException();
            //}

            //TODO: Determain if base use is affected by region
            // TODO: Other sovereign cloud url
            switch (domain)
            {
                case "gcc":
                    //domain = "https://example.us";
                    domain = "about:blank";
                    break;
                case "gcchigh":
                    //domain = "https://example.high.us";
                    domain = "about:blank";
                    break;
                case "dod":
                    //domain = "https://example.platform.us";
                    domain = "about:blank";
                    break;
            }

            BaseEnviromentUrl = $"{domain}";

            TestState.SetDomain(BaseEnviromentUrl);

            return BaseEnviromentUrl;
        }

        private async Task TestState_BeforeTestStepExecuted(object? sender, TestStepEventArgs e)
        {
            // TODO: Actions to apply after Power Fx updates
            
        }

        private async Task TestState_AfterTestStepExecuted(object? sender, TestStepEventArgs e)
        {
            // TODO: Actions to apply after Power Fx updates
        }
    }
}
