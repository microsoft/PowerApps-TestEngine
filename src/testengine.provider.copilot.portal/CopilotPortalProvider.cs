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
using Microsoft.PowerApps.TestEngine.Providers.Functions;
using System.Collections.Concurrent;

namespace Microsoft.PowerApps.TestEngine.Providers
{
    /// <summary>
    /// Test Engine Provider for interacting with the Test window of Microsoft Copilot Portal conversational agent
    /// </summary>
    [Export(typeof(ITestWebProvider))]
    public class CopilotPortalProvider : IExtendedTestWebProvider, IMessageProvider
    {
        public string BaseEnviromentUrl { get; set; } = "";

        /// <summary>
        /// Validate that the key elements of the Portal have been loaded
        /// </summary>
        public string CheckTestEngineObject { get; } = "var expression = \"var element = document.getElementById('O365_MainLink_NavMenu'); if (typeof(element) != 'undefined' && element != null) { return '' } else { return 'Loading' }\";";

        public ITestInfraFunctions? TestInfraFunctions { get; set; }

        public ISingleTestInstanceState? SingleTestInstanceState { get; set; }

        public ITestState? TestState { get; set; }

        public ITestProviderState? ProviderState { get; set; }

        public string[] Namespaces => new string[] { "Experimental" };

        public CopilotPortalProvider()
        {

        }

        public CopilotPortalProvider(ITestInfraFunctions? testInfraFunctions, ISingleTestInstanceState? singleTestInstanceState, ITestState? testState)
        {
            this.TestInfraFunctions = testInfraFunctions;
            this.SingleTestInstanceState = singleTestInstanceState;
            this.TestState = testState;
        }

        public string Name { get { return "copilot.portal"; } }

        private async Task<T> GetPropertyValueFromControlAsync<T>(ItemPath itemPath)
        {
            throw new NotImplementedException();
        }

        public T GetPropertyValueFromControl<T>(ItemPath itemPath)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///  Check if the portal is ready to start the test
        /// </summary>
        /// <returns></returns>
        public async Task<bool> CheckIsIdleAsync()
        {
            try
            {
                var expression = "var element = document.getElementById('O365_MainLink_NavMenu'); if (typeof(element) != 'undefined' && element != null) { 'Idle' } else { 'Loading' }";

                var idle = (await TestInfraFunctions.RunJavascriptAsync<string>(expression)) == "Idle";

                return idle;
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
        /// Not currently required for Copilot Portal provider as Power Fx State is created from observed messages
        /// </summary>
        /// <param name="controlDictionary"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Not required for Microsoft Copilot Studio portal interaction
        /// </summary>
        /// <returns></returns>

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

        /// <summary>
        /// Not currently required for Microsoft Copilot Studio as Power Fx state and managed by observing Network changes
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Load ab emprt Object model by default as additional state will be created in Copilot record
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
            try
            {
                await TestInfraFunctions.RunJavascriptAsync<string>(@"// Create a style element
var style = document.createElement('style');
style.type = 'text/css';

// Define the CSS rules
var css = `
  [role=""alertdialog""], .ms-TeachingBubble, .ui-DialogSurface, .fai-FirstRunContent, .fui-DialogSurface__backdrop, .fui-DialogSurface {
    display: none !important;
  }
  [name=""x-pw-glass""] {
    display: none;
  }
`;
// Add the CSS rules to the style element
if (style.styleSheet) {
  style.styleSheet.cssText = css;
} else {
  style.appendChild(document.createTextNode(css));
}
document.head.appendChild(style);");
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
        /// Generate the expected Copilot studio portal for testing
        /// </summary>
        /// <param name="domain">The copilot portal url. If blank will default to commercial cloud</param>
        /// <param name="additionalQueryParams"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public string GenerateTestUrl(string domain, string additionalQueryParams)
        {
            var environment = TestState.GetEnvironment();
            if (string.IsNullOrEmpty(environment))
            {
                SingleTestInstanceState.GetLogger().LogError("Environment cannot be empty.");
                throw new InvalidOperationException();
            }

            // TODO: Other sovereign cloud
            switch (domain)
            {
                case "gcc":
                    // Source: https://learn.microsoft.com/en-us/microsoft-copilot-studio/requirements-licensing-gcc
                    domain = "https://gcc.powerva.microsoft.us";
                    break;
                case "gcchigh":
                    // Source: https://learn.microsoft.com/en-us/microsoft-copilot-studio/requirements-licensing-gcc
                    domain = "http://high.powerva.microsoft.us";
                    break;
            }

            if (string.IsNullOrEmpty(domain))
            {
                // Assume global commerical cloud maker base url
                domain = "https://copilotstudio.microsoft.com";
            }

            var testSuiteDefinition = SingleTestInstanceState.GetTestSuiteDefinition();
            if (testSuiteDefinition == null)
            {
                SingleTestInstanceState.GetLogger().LogError("Test definition must be specified.");
                throw new InvalidOperationException();
            }

            var appId = testSuiteDefinition.AppId;

            if (string.IsNullOrEmpty(appId))
            {
                SingleTestInstanceState.GetLogger().LogError("App Id must be defined.");
                throw new InvalidOperationException();
            }

            BaseEnviromentUrl = $"{domain}/environments/{environment}/bots/{appId}/overview";

            TestState.SetDomain(BaseEnviromentUrl);

            return BaseEnviromentUrl;
        }

        /// <summary>
        /// Configure Network Listener to observe test conversation messages
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task SetupContext()
        {
            var context = TestInfraFunctions.GetContext();

            var testSuiteDefinition = SingleTestInstanceState.GetTestSuiteDefinition();
            if (testSuiteDefinition == null)
            {
                SingleTestInstanceState.GetLogger().LogError("Test definition must be specified.");
                throw new InvalidOperationException();
            }

            var appId = testSuiteDefinition.AppId;

            await context.RouteAsync($"**/*", async route =>
            {
                var url = route.Request.Url;
                if (route.Request.Method == "POST" && url.Contains(appId) &&  url.Contains("test/conversations"))
                {
                    var response = await route.FetchAsync();
                    var responseBody = await response.TextAsync();
                    var json = CoPilotMessageParser.ParseMessages(responseBody);
                    var logger = SingleTestInstanceState.GetLogger();
                    foreach (var message in json)
                    {
                        Messages.Enqueue(message);
                        logger.LogInformation(message);
                    }
                }
                await route.ContinueAsync();
            });
        }

        /// <summary>
        /// Add Copilot specific functions
        /// </summary>
        /// <param name="powerFxConfig"></param>
        public void ConfigurePowerFx(PowerFxConfig powerFxConfig)
        {
            powerFxConfig.AddFunction(new SendTextFunction(TestInfraFunctions, TestState, SingleTestInstanceState.GetLogger()));
            powerFxConfig.AddFunction(new WaitUntilMessageFunction(TestInfraFunctions, TestState, SingleTestInstanceState.GetLogger(), this));
        }

        /// <summary>
        /// Add Copilot state and messages
        /// </summary>
        /// <param name="engine"></param>
        public void ConfigurePowerFxEngine(RecalcEngine engine)
        {
            engine.UpdateVariable("Copilot", new CopilotStateRecordValue(this));
        }

        public Task GetNewMessages()
        {
            // No implmentation needed as obtained from RouteAsync
            return Task.CompletedTask;
        }

        /// <summary>
        /// Json messages observed as part of the test session
        /// </summary>
        public ConcurrentQueue<string> Messages { get; private set; } = new ConcurrentQueue<string>();
    }
}
