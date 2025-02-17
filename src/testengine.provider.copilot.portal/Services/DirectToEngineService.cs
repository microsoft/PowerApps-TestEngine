// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Agents.CopilotStudio.Client;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using Microsoft.Agents.Protocols.Primitives;
using System.Text.Json;

namespace testengine.provider.copilot.portal.services
{
    public class DirectToEngineService : ICopilotApiService
    {
        
        public string? ConversationId { get; set; }

        public string BotIdentifier { get; set; } = string.Empty;

        public string EnvironmentId { get; set; } = string.Empty;

        public string Token { get; set; } = string.Empty;

        private CopilotClient _copilotClient = null;

        private IMessageProvider _provider = null;

        private JsonSerializerOptions _options = new JsonSerializerOptions() { PropertyNamingPolicy = new LowerCaseNamingPolicy() };

        public DirectToEngineService(IMessageProvider provider)
        {
            _provider = provider;

        }

        public Task<bool> GetResponseAsync(IMessageProvider provider)
        {
            return Task.FromResult(true);
        }

        public async Task Setup()
        {
            var serviceProvider = new ServiceCollection()
                .AddHttpClient()
                .BuildServiceProvider();

            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            
            var settings = new ConnectionSettings(null)
            {
                BotIdentifier = BotIdentifier,
                EnvironmentId = EnvironmentId,
                CopilotBotType = Microsoft.Agents.CopilotStudio.Client.Discovery.BotType.Published
            };
            _copilotClient = new CopilotClient(settings, httpClientFactory, (string name) => { return Task.FromResult(Token); }, logger, "mcs");
        }

        public async Task InitiateConversationAsync()
        {
            _ = Task.Run(async () =>
            {
                var cancellationToken = new CancellationToken();

                await foreach (Activity act in _copilotClient.StartConversationAsync(emitStartConversationEvent: true, cancellationToken: cancellationToken))
                {
                    if (string.IsNullOrEmpty(ConversationId) && act.Conversation != null) {
                        _provider.ConversationId = act.Conversation.Id;
                        ConversationId = _provider.ConversationId;
                    }
                    _provider.Messages.Enqueue(JsonSerializer.Serialize(act, _options));
                }
            });
        }

        public async Task<string> SendMessageOrEventAsync(string body)
        {
            if (ConversationId != null)
            {
                await foreach ( var activity in _copilotClient.AskQuestionAsync(body, ConversationId, CancellationToken.None))
                {
                    _provider.Messages.Enqueue(JsonSerializer.Serialize(activity, _options));
                }
            }

            return string.Empty;
        }
    }
}
