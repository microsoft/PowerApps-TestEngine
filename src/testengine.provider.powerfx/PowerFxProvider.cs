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
    /// Functions for interacting with the Power Fx
    /// </summary>
    [Export(typeof(ITestWebProvider))]
    public class PowerFxProvider : ITestWebProvider
    {
        public ITestInfraFunctions? TestInfraFunctions { get; set; }

        public ISingleTestInstanceState? SingleTestInstanceState { get; set; }

        public ITestState? TestState { get; set; }

        public PowerFxProvider()
        {

        }

        public PowerFxProvider(ITestInfraFunctions? testInfraFunctions, ISingleTestInstanceState? singleTestInstanceState, ITestState? testState)
        {
            this.TestInfraFunctions = testInfraFunctions;
            this.SingleTestInstanceState = singleTestInstanceState;
            this.TestState = testState;
        }

        public string Name { get { return "powerfx"; } }

        public string[] Namespaces => new string[] { "Preview" };

        public ITestProviderState? ProviderState { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public string CheckTestEngineObject => "";

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
            return true;
        }

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

        public async Task<bool> SelectControlAsync(ItemPath itemPath, string filePath = null)
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
            return "about:blank";
        }
    }
}
