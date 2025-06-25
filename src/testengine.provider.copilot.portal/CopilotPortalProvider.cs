// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Concurrent;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Helpers;
using Microsoft.PowerApps.TestEngine.Providers.Functions;
using Microsoft.PowerApps.TestEngine.Providers.PowerFxModel;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;
using testengine.provider.copilot.portal.services;

namespace Microsoft.PowerApps.TestEngine.Providers
{    /// <summary>
    /// Test Engine Provider for interacting with the Test window of Microsoft Copilot Portal conversational agent
    /// </summary>
    [Export(typeof(ITestWebProvider))]
    public class CopilotPortalProvider : IExtendedPowerFxProvider, IMessageProvider
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

        public string[] Namespaces => new string[] { "Preview" };

        public IWorkerService MessageWorker { get; set; } = null;
        public IWorkerService ActionWorker { get; set; } = null;

        public Func<string, string> GetToken { get; set; } = (url) => new AzureCliHelper().GetAccessToken(new Uri(url));

        public AgentSettings AgentSettings { get; set; }

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
        /// Load Object model by default as additional state will be created in Copilot record
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
        /// <param name="filePath">The physical file path for image file</param>
        /// <returns></returns>
        public async Task<bool> SelectControlAsync(ItemPath itemPath, string filePath = null)
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

            //// TODO: Other sovereign cloud
            //switch (domain)
            //{
            //    case "gcc":
            //        // Source: https://learn.microsoft.com/en-us/microsoft-copilot-studio/requirements-licensing-gcc
            //        domain = "https://gcc.powerva.microsoft.us";
            //        break;
            //    case "gcchigh":
            //        // Source: https://learn.microsoft.com/en-us/microsoft-copilot-studio/requirements-licensing-gcc
            //        domain = "http://high.powerva.microsoft.us";
            //        break;
            //}

            //if (string.IsNullOrEmpty(domain))
            //{
                // Assume global commerical cloud maker base url
            var portalDomain = "https://copilotstudio.microsoft.com";
            //}

            var testSuiteDefinition = SingleTestInstanceState.GetTestSuiteDefinition();
            if (testSuiteDefinition == null)
            {
                SingleTestInstanceState.GetLogger().LogError("Test definition must be specified.");
                throw new InvalidOperationException();
            }

            var appLogicalName = testSuiteDefinition.AppLogicalName;

            AgentSettings = LookupAgentByName(domain, appLogicalName).Result;

            var appId = AgentSettings.AgentId;

            if (string.IsNullOrEmpty(appId))
            {
                SingleTestInstanceState.GetLogger().LogError("App Id must be defined.");
                throw new InvalidOperationException();
            }

            BaseEnviromentUrl = $"{portalDomain}/environments/{environment}/bots/{appId}/overview";

            TestState.SetDomain(domain);

            return BaseEnviromentUrl;
        }

        public async Task<AgentSettings> LookupAgentByName(string environmentUrl, string agentName)
        {
            var url = new Uri(environmentUrl);

            var dataverseUrl = "https://" + url.Host;

            var apiUrl = new Uri("https://" + url.Host + "/api/data/v9.2/bots?$filter=name eq '" + Uri.EscapeUriString(agentName) + "'&$select=botid,schemaname", UriKind.Absolute);

            var token = GetToken(dataverseUrl);

            var client = new HttpClient(new TokenHandler(token, SingleTestInstanceState?.GetLogger()))
            {
                BaseAddress = apiUrl
            };

            var response = await client.GetStringAsync(apiUrl);

            // Parse the response to extract the agent ID
            var jsonResponse = JsonDocument.Parse(response);
            if (jsonResponse.RootElement.TryGetProperty("value", out var agents) && agents.GetArrayLength() > 0)
            {
                return new AgentSettings
                {
                    AgentId = agents[0].GetProperty("botid").GetString() ?? string.Empty,
                    SchemaName = agents[0].GetProperty("schemaname").GetString() ?? string.Empty
                };
            }
            throw new InvalidOperationException($"Agent '{agentName}' not found in environment '{environmentUrl}'.");
        }

        /// <summary>
        /// Configure Network Listener to observe test conversation messages
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task SetupContext()
        {
            var context = TestInfraFunctions.GetContext();

            await context.RouteAsync($"**/*", async route =>
            {
                var url = route.Request.Url;

                if (route.Request.Method == "POST" && url.Contains(AgentSettings?.AgentId) && url.Contains("test/conversations"))
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
            var logger = SingleTestInstanceState.GetLogger();
            powerFxConfig.AddFunction(new WaitUntilConnectedFunction(TestInfraFunctions, TestState, logger, this));
            powerFxConfig.AddFunction(new CopilotSendMessageFunction(TestInfraFunctions, TestState, SingleTestInstanceState.GetLogger()));
            powerFxConfig.AddFunction(new WaitUntilMessageFunction(TestInfraFunctions, TestState, logger, this));
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

        public FormulaValue ExecutePowerFx(string steps, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Json messages observed as part of the test session
        /// </summary>
        public ConcurrentQueue<string> Messages { get; private set; } = new ConcurrentQueue<string>();
        
        public bool ProviderExecute
        {
            get { return false; }
        }

        /// <summary>
        /// Setup method for RecalcEngine
        /// </summary>
        /// <param name="powerFxConfig"></param>
        /// <param name="testInfraFunctions"></param>
        /// <param name="singleTestInstanceState"></param>
        /// <param name="testState"></param>
        /// <param name="fileSystem"></param>
        public void Setup(PowerFxConfig powerFxConfig, ITestInfraFunctions testInfraFunctions, ISingleTestInstanceState singleTestInstanceState, ITestState testState, IFileSystem fileSystem)
        {
            // TODO: Setup RecalcEngine for copilot portal provider
        }

        private string _conversationId = String.Empty;

        public string? ConversationId
        {
            get
            {
                return _conversationId;
            }

            set => _conversationId = value != null ? value : String.Empty;
        }
    }

    public class AgentSettings
    {
        public string? AgentId { get; set; }
        public string? SchemaName { get; set; }
    }

    /// <summary>
    /// Token handler for HTTP requests
    /// </summary>
    public class TokenHandler : DelegatingHandler
    {
        private readonly Extensions.Logging.ILogger? _logger;
        private readonly string _token;

        public TokenHandler(string token, Extensions.Logging.ILogger? logger)
        {
            _token = token;
            _logger = logger;
            InnerHandler = new HttpClientHandler();
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(_token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
