// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.ComponentModel.Composition;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Helpers;
using Microsoft.PowerApps.TestEngine.Providers.PowerFxModel;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerApps.TestEngine.Providers
{
    /// <summary>
    /// Functions for interacting with the Power App Portal
    /// </summary>
    [Export(typeof(ITestWebProvider))]
    public class PowerAppPortalProvider : ITestWebProvider
    {
        public string BaseEnviromentUrl { get; set; } = "";

        public string CheckTestEngineObject { get; } = "var expression = \"var element = document.getElementById('O365_MainLink_Settings'); if (typeof(element) != 'undefined' && element != null) { element.click(); var settings = document.getElementById('sessionDetails-help-menu-item'); settings.click() } else { return 'Loading' }\";";

        public ITestInfraFunctions? TestInfraFunctions { get; set; }

        public ISingleTestInstanceState? SingleTestInstanceState { get; set; }

        public ITestState? TestState { get; set; }

        public ITestProviderState? ProviderState { get; set; }

        public string[] Namespaces => new string[] { "Preview" };

        public PowerAppPortalProvider()
        {

        }

        public PowerAppPortalProvider(ITestInfraFunctions? testInfraFunctions, ISingleTestInstanceState? singleTestInstanceState, ITestState? testState)
        {
            this.TestInfraFunctions = testInfraFunctions;
            this.SingleTestInstanceState = singleTestInstanceState;
            this.TestState = testState;
        }

        public string Name { get { return "powerapps.portal"; } }

        private async Task<T> GetPropertyValueFromControlAsync<T>(ItemPath itemPath)
        {
            throw new NotImplementedException();
        }

        public T GetPropertyValueFromControl<T>(ItemPath itemPath)
        {
            throw new NotImplementedException();
        }


        public async Task<bool> CheckIsIdleAsync()
        {
            try
            {
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

        private async Task<Dictionary<string, ControlRecordValue>> LoadObjectModelAsyncHelper(Dictionary<string, ControlRecordValue> controlDictionary)
        {
            try
            {
                // TODO

                return controlDictionary;
            }

            catch (Exception ex)
            {
                ExceptionHandlingHelper.CheckIfOutDatedPublishedApp(ex, SingleTestInstanceState.GetLogger());
                throw;
            }
        }

        private async Task<string> GetPowerAppsTestEngineObject()
        {
            var result = "true";

            try
            {
                // TODO: Get object model
                //result = await TestInfraFunctions.RunJavascriptAsync<string>(CheckTestEngineObject);
                return "{}";
            }
            catch (NullReferenceException) { }

            return result;
        }

        public async Task CheckProviderAsync()
        {
            try
            {
                // See if using legacy player
                try
                {
                    // TODO: Update as needed
                    //await PollingHelper.PollAsync<string>("undefined", (x) => x.ToLower() == "undefined", () => GetPowerAppsTestEngineObject(), TestState.GetTestSettings().Timeout, SingleTestInstanceState.GetLogger());
                }
                catch (TimeoutException)
                {
                    // TODO
                }
            }
            catch (Exception ex)
            {
                SingleTestInstanceState.GetLogger().LogDebug(ex.ToString());
            }
        }

        public async Task<Dictionary<string, ControlRecordValue>> LoadObjectModelAsync()
        {
            var controlDictionary = new Dictionary<string, ControlRecordValue>();

            return controlDictionary;
        }

        public async Task<bool> SelectControlAsync(ItemPath itemPath)
        {
            // TODO
            return true;
        }

        public async Task<bool> SetPropertyAsync(ItemPath itemPath, FormulaValue value)
        {
            // TODO
            return true;
        }


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

        public string GenerateTestUrl(string domain, string additionalQueryParams)
        {
            var environment = TestState.GetEnvironment();
            if (string.IsNullOrEmpty(environment))
            {
                SingleTestInstanceState.GetLogger().LogError("Environment cannot be empty.");
                throw new InvalidOperationException();
            }

            if (string.IsNullOrEmpty(domain))
            {
                // Assume global commerical cloud maker base url
                domain = "https://make.powerapps.com";
            }

            BaseEnviromentUrl = $"{domain}/environments/{environment}";

            TestState.SetDomain(BaseEnviromentUrl);

            return BaseEnviromentUrl + "/home?source=testengine";
        }
    }
}
