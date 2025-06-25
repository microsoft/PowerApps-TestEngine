// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Concurrent;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Agents.CopilotStudio.Client;
using Microsoft.Agents.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Helpers;
using Microsoft.PowerApps.TestEngine.Providers.Functions;
using Microsoft.PowerApps.TestEngine.Providers.PowerFxModel;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerApps.TestEngine.Providers
{
    /// <summary>
    /// Test Engine Provider for interacting with Microsoft Copilot Studio directly
    /// </summary>
    [Export(typeof(ITestWebProvider))]
    public class CopilotProvider : ITestWebProvider, IExtendedPowerFxProvider
    {
        private CopilotClient? _copilotClient;
        private IHost? _host;
        private IServiceProvider? _serviceProvider;
        private string? _conversationId;
        private CancellationTokenSource? _cancellationTokenSource;
        public ITestInfraFunctions? TestInfraFunctions { get; set; }
        public ISingleTestInstanceState? SingleTestInstanceState { get; set; }
        public ITestState? TestState { get; set; }
        public ITestProviderState? ProviderState { get; set; }
        public IFileSystem? FileSystem { get; set; }

        public Func<string, string> GetToken { get; set; } = (url) => new AzureCliHelper().GetAccessToken(new Uri(url));

        // MSAL constants for token caching
        private static readonly string _keyChainServiceName = "copilot_studio_client_app";
        private static readonly string _keyChainAccountName = "copilot_studio_client";

        /// <summary>
        /// Acquire token using MSAL with interactive prompt or cache
        /// </summary>
        /// <param name="scopes">The scopes to request</param>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="clientId">The client ID (optional, uses default if not provided)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Access token</returns>
        private async Task<string> GetTokenUsingMsalAsync(string[] scopes, string tenantId, string? clientId = null, CancellationToken cancellationToken = default)
        {
            // Use a default client ID if none provided (this should be configured for your environment)
            var appClientId = clientId ?? Environment.GetEnvironmentVariable("ENTRA_CLIENT_ID"); // Microsoft CLI client ID as fallback

            if (string.IsNullOrEmpty(appClientId))
            {
                throw new InvalidOperationException("Client ID is required for Copilot Studio authentication.");
            }

            // Setup a Public Client application for authentication
            IPublicClientApplication app = PublicClientApplicationBuilder.Create(appClientId)
                .WithAuthority(AadAuthorityAudience.AzureAdMyOrg)
                .WithTenantId(tenantId)
                .WithRedirectUri("http://localhost")
                .Build();

            string currentDir = Path.Combine(FileSystem?.GetDefaultRootTestEngine() ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "mcs_client_console");

            if (!Directory.Exists(currentDir))
            {
                Directory.CreateDirectory(currentDir);
            }

            StorageCreationPropertiesBuilder storageProperties = new("TokenCache", currentDir);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                storageProperties.WithLinuxUnprotectedFile();
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                storageProperties.WithMacKeyChain(_keyChainServiceName, _keyChainAccountName);
            }

            MsalCacheHelper tokenCacheHelper = await MsalCacheHelper.CreateAsync(storageProperties.Build());
            tokenCacheHelper.RegisterCache(app.UserTokenCache);

            IAccount? account = (await app.GetAccountsAsync()).FirstOrDefault();

            AuthenticationResult authResponse;
            try
            {
                authResponse = await app.AcquireTokenSilent(scopes, account).ExecuteAsync(cancellationToken);
            }
            catch (MsalUiRequiredException)
            {
                authResponse = await app.AcquireTokenInteractive(scopes).ExecuteAsync(cancellationToken);
            }

            return authResponse.AccessToken;
        }

        /// <summary>
        /// Messages collected during the conversation
        /// </summary>
        public ConcurrentQueue<IActivity> Messages { get; private set; } = new ConcurrentQueue<IActivity>();

        /// <summary>
        /// JSON messages observed as part of the test session
        /// </summary>
        public ConcurrentQueue<string> JsonMessages { get; private set; } = new ConcurrentQueue<string>();

        public string Name { get { return "copilot"; } }
        public string[] Namespaces => new string[] { "Preview" };
        public string CheckTestEngineObject => String.Empty;
        public bool ProviderExecute => false;

        /// <summary>
        /// Current conversation ID
        /// </summary>
        public string? ConversationId 
        { 
            get => _conversationId;
            set => _conversationId = value;
        }
        
        public CopilotProvider()
        {
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public CopilotProvider(ITestInfraFunctions? testInfraFunctions, ISingleTestInstanceState? singleTestInstanceState, ITestState? testState)
        {
            this.TestInfraFunctions = testInfraFunctions;
            this.SingleTestInstanceState = singleTestInstanceState;
            this.TestState = testState;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Initialize the Copilot Studio client
        /// </summary>
        private async Task InitializeCopilotClientAsync()
        {
            var logger = SingleTestInstanceState?.GetLogger();
            logger?.LogInformation("Initializing Copilot Studio client...");

            var builder = Host.CreateApplicationBuilder();            // Get configuration from test tate
            var agentName = TestState?.GetTestSuiteDefinition().AppLogicalName;
            var environmentId = TestState?.GetEnvironment();
            var tenantId = TestState?.GetTenant();
            var token = await GetTokenUsingMsalAsync(new[] { "https://api.powerplatform.com/.default" }, tenantId ?? "common");
            var environmentUrl = TestState?.GetDomain();

            if (string.IsNullOrEmpty(environmentUrl))
                throw new InvalidOperationException("domain related for Dataverse Url is required for Copilot Studio provider");

            if (string.IsNullOrEmpty(agentName))
                throw new InvalidOperationException("AppLogicalName is required for Copilot Studio provider");

            var agent = await LookupAgentByName(environmentUrl, agentName);

            if (string.IsNullOrEmpty(environmentId))
                throw new InvalidOperationException("EnvironmentId is required for Copilot Studio provider");
            if (string.IsNullOrEmpty(token))
                throw new InvalidOperationException("Token is required for Copilot Studio provider");

            // Create settings for Copilot Studio client
            var settings = new CopilotStudioClientSettings
            {
                AgentId = agent.AgentId,
                EnvironmentId = environmentId,
                TenantId = tenantId,
                Token = token
            };

            // Add HTTP client with token handler
            builder.Services.AddHttpClient("copilot")
                .ConfigurePrimaryHttpMessageHandler(() => new TokenHandler(settings, logger));

            // Add services
            builder.Services
                .AddSingleton(settings)
                .AddSingleton<ILoggerFactory>(provider => LoggerFactory.Create(builder => builder.AddConsole()))
                .AddTransient<CopilotClient>((s) =>
                {
                    ConnectionSettings connectionsSettings = new ConnectionSettings() { EnvironmentId = environmentId };

                    var clientLogger = s.GetRequiredService<ILoggerFactory>().CreateLogger<CopilotClient>();
                    return new CopilotClient(connectionsSettings, s.GetRequiredService<IHttpClientFactory>(), clientLogger, "copilot");
                });

            _host = builder.Build();
            _serviceProvider = _host.Services;
            _copilotClient = _serviceProvider.GetRequiredService<CopilotClient>();

            _copilotClient.Settings.SchemaName = agent.SchemaName;

            logger?.LogInformation("Copilot Studio client initialized successfully");
        }

        public async Task<AgentSettings> LookupAgentByName(string environmentUrl, string agentName)
        {
            var url = new Uri(environmentUrl);

            var dataverseUrl = "https://" + url.Host;

            var apiUrl = new Uri("https://" + url.Host + "/api/data/v9.2/bots?$filter=name eq '" + Uri.EscapeUriString(agentName) + "'&$select=botid,schemaname", UriKind.Absolute);

            var client = new HttpClient(new TokenHandler(new CopilotStudioClientSettings { Token = GetToken(dataverseUrl) }, SingleTestInstanceState?.GetLogger()))
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
        /// Token handler for HTTP requests
        /// </summary>
        private class TokenHandler : DelegatingHandler
        {
            private readonly CopilotStudioClientSettings _settings;
            private readonly Extensions.Logging.ILogger? _logger;

            public TokenHandler(CopilotStudioClientSettings settings, Extensions.Logging.ILogger? logger)
            {
                _settings = settings;
                _logger = logger;
                InnerHandler = new HttpClientHandler();
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                if (!string.IsNullOrEmpty(_settings.Token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.Token);
                }

                return await base.SendAsync(request, cancellationToken);
            }
        }

        public class AgentSettings
        {
            public string? AgentId { get; set; }
            public string? SchemaName { get; set; }
        }

        /// <summary>
        /// Settings for Copilot Studio client
        /// </summary>
        private class CopilotStudioClientSettings
        {
            public string? AgentId { get; set; }
            public string? EnvironmentId { get; set; }
            public string? TenantId { get; set; }
            public string? Token { get; set; }
        }
        
        private async Task<T> GetPropertyValueFromControlAsync<T>(ItemPath itemPath)
        {
            if (itemPath.PropertyName == "ConversationId" && _conversationId is T)
            {
                return (T)Convert.ChangeType(_conversationId, typeof(T));
            }
            
            throw new NotImplementedException($"Property '{itemPath.PropertyName}' not implemented for async access");
        }

        public T GetPropertyValueFromControl<T>(ItemPath itemPath)
        {
            if (itemPath.PropertyName == "ConversationId" && _conversationId != null)
            {
                return (T)Convert.ChangeType(_conversationId, typeof(T));
            }
            
            throw new InvalidOperationException($"Property '{itemPath.PropertyName}' not found.");
        }

        /// <summary>
        /// Start a conversation with the Copilot Studio agent
        /// </summary>
        public async Task<bool> StartConversationAsync()
        {
            if (_copilotClient == null)
            {
                await InitializeCopilotClientAsync();
            }

            var logger = SingleTestInstanceState?.GetLogger();
            
            try
            {
                logger?.LogInformation("Starting conversation with Copilot Studio agent...");
                
                await foreach (var activity in _copilotClient!.StartConversationAsync(
                    emitStartConversationEvent: true, 
                    cancellationToken: _cancellationTokenSource!.Token))
                {
                    if (activity != null)
                    {
                        Messages.Enqueue(activity);
                        JsonMessages.Enqueue(JsonSerializer.Serialize(activity));
                        _conversationId = activity.Conversation?.Id;
                        
                        logger?.LogDebug($"Received activity: {activity.Type} - {activity.Text}");
                    }
                }
                
                logger?.LogInformation($"Conversation started successfully. ConversationId: {_conversationId}");
                return true;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to start conversation with Copilot Studio agent");
                return false;
            }
        }

        /// <summary>
        /// Send a message to the Copilot Studio agent
        /// </summary>
        public async Task<bool> SendMessageAsync(string message)
        {
            if (_copilotClient == null)
            {
                throw new InvalidOperationException("Copilot client not initialized. Call StartConversationAsync first.");
            }

            var logger = SingleTestInstanceState?.GetLogger();
            
            try
            {
                logger?.LogInformation($"Sending message: {message}");
                
                await foreach (var activity in _copilotClient.AskQuestionAsync(
                    message, 
                    _conversationId, 
                    _cancellationTokenSource!.Token))
                {
                    if (activity != null)
                    {
                        Messages.Enqueue(activity);
                        JsonMessages.Enqueue(JsonSerializer.Serialize(activity));
                        
                        logger?.LogDebug($"Received response: {activity.Type} - {activity.Text}");
                    }
                }
                
                return true;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, $"Failed to send message: {message}");
                return false;
            }
        }

        /// <summary>
        /// Get the latest messages from the conversation
        /// </summary>
        public IEnumerable<IActivity> GetLatestMessages(int count = 10)
        {
            var messages = new List<IActivity>();
            var tempQueue = new Queue<IActivity>();
            
            // Dequeue all messages to a temporary queue
            while (Messages.TryDequeue(out var message))
            {
                tempQueue.Enqueue(message);
            }
            
            // Take the last 'count' messages
            var messageArray = tempQueue.ToArray();
            var startIndex = Math.Max(0, messageArray.Length - count);
            
            for (int i = startIndex; i < messageArray.Length; i++)
            {
                messages.Add(messageArray[i]);
                Messages.Enqueue(messageArray[i]); // Put back in the queue
            }
            
            // Put remaining messages back
            for (int i = 0; i < startIndex; i++)
            {
                Messages.Enqueue(messageArray[i]);
            }
            
            return messages;
        }
        
        public async Task<bool> CheckIsIdleAsync()
        {
            // Copilot is always ready for the next message
            return true;
        }

        /// <summary>
        /// Load object model - not required for Copilot provider as state is managed through conversation
        /// </summary>
        private async Task<Dictionary<string, ControlRecordValue>> LoadObjectModelAsyncHelper(Dictionary<string, ControlRecordValue> controlDictionary)
        {
            try
            {
                return controlDictionary;
            }
            catch (Exception ex)
            {
                ExceptionHandlingHelper.CheckIfOutDatedPublishedApp(ex, SingleTestInstanceState?.GetLogger());
                throw;
            }
        }

        /// <summary>
        /// Not required for Copilot Studio interaction
        /// </summary>
        private async Task<string> GetPowerAppsTestEngineObject()
        {
            return "{}";
        }

        /// <summary>
        /// Setup and initialize the Copilot Studio client
        /// </summary>
        public async Task CheckProviderAsync()
        {
            try
            {
                var logger = SingleTestInstanceState?.GetLogger();
                logger?.LogInformation("Checking Copilot provider...");
                
                // Initialize the client if not already done
                if (_copilotClient == null)
                {
                    await InitializeCopilotClientAsync();
                }
                
                logger?.LogInformation("Copilot provider check completed successfully");
            }
            catch (Exception ex)
            {
                SingleTestInstanceState?.GetLogger()?.LogError(ex, "Error during Copilot provider check");
                throw;
            }
        }        /// <summary>
        /// Load empty object model as Copilot state is managed through conversation
        /// </summary>
        public async Task<Dictionary<string, ControlRecordValue>> LoadObjectModelAsync()
        {
            var controlDictionary = new Dictionary<string, ControlRecordValue>();
            return controlDictionary;
        }

        /// <summary>
        /// Not currently implemented - could be used for adaptive cards interaction
        /// </summary>
        public async Task<bool> SelectControlAsync(ItemPath itemPath, string filePath = null)
        {
            // TODO: Implement for adaptive cards if needed
            return true;
        }

        /// <summary>
        /// Not currently implemented - could be used to update Copilot variables
        /// </summary>
        public async Task<bool> SetPropertyAsync(ItemPath itemPath, FormulaValue value)
        {
            // TODO: Implement for Copilot variable updates if needed
            return true;
        }

        /// <summary>
        /// Not used as Copilot doesn't have traditional controls
        /// </summary>
        public int GetItemCount(ItemPath itemPath)
        {
            return 0;
        }

        /// <summary>
        /// Get debug information about the current conversation state
        /// </summary>
        public async Task<object> GetDebugInfo()
        {
            try
            {
                return new Dictionary<string, object>
                {
                    ["ConversationId"] = _conversationId ?? "Not set",
                    ["MessageCount"] = Messages.Count,
                    ["ClientInitialized"] = _copilotClient != null,
                    ["LatestMessages"] = GetLatestMessages(5).Select(m => new { m.Type, m.Text, m.Id }).ToArray()
                };
            }
            catch (Exception ex)
            {
                return new Dictionary<string, object>
                {
                    ["Error"] = ex.Message
                };
            }
        }

        /// <summary>
        /// Check if the Copilot client is ready
        /// </summary>
        public async Task<bool> TestEngineReady()
        {
            try
            {
                if (_copilotClient == null)
                {
                    await InitializeCopilotClientAsync();
                }
                return _copilotClient != null;
            }
            catch (Exception ex)
            {
                SingleTestInstanceState?.GetLogger()?.LogError(ex, "Error checking if Copilot client is ready");
                return false;
            }
        }

        /// <summary>
        /// Generate URL - not applicable for Copilot provider
        /// </summary>
        public string GenerateTestUrl(string domain, string additionalQueryParams)
        {
            return "about:blank";
        }
        
        /// <summary>
        /// Configure PowerFx with Copilot-specific functions
        /// </summary>
        public void ConfigurePowerFx(PowerFxConfig powerFxConfig)
        {
            var logger = SingleTestInstanceState?.GetLogger();
            logger?.LogInformation("Configuring PowerFx for Copilot provider");
            
            // Add Copilot-specific functions
            powerFxConfig.AddFunction(new CopilotConnectFunction(this, logger));
            powerFxConfig.AddFunction(new CopilotSendMessageFunction(this, logger));
            powerFxConfig.AddFunction(new CopilotGetMessagesFunction(this, logger));
            powerFxConfig.AddFunction(new CopilotWaitForResponseFunction(this, TestInfraFunctions, TestState, logger));
        }

        /// <summary>
        /// Configure PowerFx engine with Copilot state
        /// </summary>
        public void ConfigurePowerFxEngine(RecalcEngine engine)
        {
            // Add Copilot state record
            engine.UpdateVariable("Copilot", new CopilotStateRecordValue(this));
        }

        /// <summary>
        /// Setup context and initialize Copilot client
        /// </summary>
        public async Task SetupContext()
        {
            var logger = SingleTestInstanceState?.GetLogger();
            logger?.LogInformation("Setting up Copilot context...");
            
            try
            {
                await InitializeCopilotClientAsync();
                logger?.LogInformation("Copilot context setup completed");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to setup Copilot context");
                throw;
            }
        }

        /// <summary>
        /// Execute PowerFx - not implemented for this provider
        /// </summary>
        public FormulaValue ExecutePowerFx(string steps, CultureInfo culture)
        {
            return BlankValue.NewBlank();
        }        /// <summary>
        /// Setup provider with dependencies
        /// </summary>
        public void Setup(PowerFxConfig powerFxConfig, ITestInfraFunctions testInfraFunctions, ISingleTestInstanceState singleTestInstanceState, ITestState testState, IFileSystem fileSystem)
        {
            TestInfraFunctions = testInfraFunctions;
            SingleTestInstanceState = singleTestInstanceState;
            TestState = testState;
            FileSystem = fileSystem;
            
            var logger = singleTestInstanceState?.GetLogger();
            logger?.LogInformation("Copilot provider setup completed");
        }

        /// <summary>
        /// Dispose of resources
        /// </summary>
        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _host?.Dispose();
        }
    }

    /// <summary>
    /// Copilot state record for PowerFx
    /// </summary>
    public class CopilotStateRecordValue : RecordValue
    {
        private readonly CopilotProvider _provider;

        public CopilotStateRecordValue(CopilotProvider provider) : base(RecordType.Empty())
        {
            _provider = provider;
        }

        protected override bool TryGetField(FormulaType fieldType, string fieldName, out FormulaValue result)
        {
            result = fieldName switch
            {
                "ConversationId" => FormulaValue.New(_provider.ConversationId ?? ""),
                "MessageCount" => FormulaValue.New(_provider.Messages.Count),
                "IsConnected" => FormulaValue.New(_provider.ConversationId != null),
                _ => FormulaValue.NewBlank()
            };
            
            return true;
        }
    }
}
