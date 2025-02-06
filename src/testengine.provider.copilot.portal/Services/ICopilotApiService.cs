// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerApps.TestEngine.Providers;

namespace testengine.provider.copilot.portal.services
{
    public interface ICopilotApiService
    {
        string? ConversationId { get; }

        public Task Setup();

        public Task InitiateConversationAsync();

        public Task<string> SendMessageOrEventAsync(string body);

        public Task<bool> GetResponseAsync(IMessageProvider provider);
    }
}
