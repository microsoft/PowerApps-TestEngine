// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using testengine.provider.copilot.portal.services;

namespace Microsoft.PowerApps.TestEngine.Providers.Services
{
    public class DirectLineApiService : ICopilotApiService
    {
        private readonly IHttpClientWrapper _httpClientWrapper = null;

        private readonly ILogger _logger = null;

        public string Secret { get; set; } = String.Empty;

        public Uri BotFrameworkUrl { get; set; } = null;

        private string? Token { get; set; } = string.Empty;

        public string? ConversationId { get; set; } = string.Empty;

        public string Watermark { get; set; } = string.Empty;

        public DirectLineApiService() 
        {
            _httpClientWrapper = new HttpClientWrapper(new HttpClient());
           
        }

        public DirectLineApiService(IHttpClientWrapper httpClientWrapper, ILogger logger)
        {
            _httpClientWrapper = httpClientWrapper;
            _logger = logger;
        }

        public virtual async Task Setup()
        {
            _httpClientWrapper.SetAuthorizationHeader("Bearer", Secret);
            HttpResponseMessage response = await _httpClientWrapper.PostAsync(new Uri(BotFrameworkUrl, new Uri("/v3/directline/tokens/generate", UriKind.Relative)).ToString(), null);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var root = JsonDocument.Parse(json).RootElement;
                if (root.TryGetProperty("token", out JsonElement token))
                {
                    Token = token.GetString();
                }
                if (root.TryGetProperty("conversationId", out JsonElement conversationid))
                {
                    ConversationId = conversationid.GetString();
                }

                return;
            }
            throw new InvalidDataException($"Error: {response.StatusCode} - {response.ReasonPhrase}\nResponse:\n{await response.Content.ReadAsStringAsync()}");
        }

        public virtual async Task InitiateConversationAsync()
        {
            _httpClientWrapper.SetAuthorizationHeader("Bearer", Token);
            HttpResponseMessage response = await _httpClientWrapper.PostAsync(new Uri(BotFrameworkUrl, new Uri("/v3/directline/conversations", UriKind.Relative)).ToString(), null);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var root = JsonDocument.Parse(json).RootElement;
                if (root.TryGetProperty("conversationId", out JsonElement conversationid))
                {
                    ConversationId = conversationid.GetString();
                }
                return;
            }
            throw new InvalidDataException($"Error: {response.StatusCode} - {response.ReasonPhrase}\nResponse:\n{await response.Content.ReadAsStringAsync()}");
        }

        public virtual async Task<string> SendMessageOrEventAsync(string body)
        {
            _httpClientWrapper.SetAuthorizationHeader("Bearer", Token);
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await _httpClientWrapper.PostAsync(new Uri(BotFrameworkUrl, new Uri("/v3/directline/conversations/" + ConversationId + "/activities", UriKind.Relative)).ToString(), content);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            return $"Error: {response.StatusCode} - {response.ReasonPhrase}\nResponse:\n{await response.Content.ReadAsStringAsync()}";
        }

        public virtual async Task<bool> GetResponseAsync(IMessageProvider provider)
        {
            bool newMessages = false;
            _httpClientWrapper.SetAuthorizationHeader("Bearer", Token);
            var watermarkQuery = string.Empty;
            if (!string.IsNullOrEmpty(Watermark))
            {
                watermarkQuery = "?watermark=" + Uri.EscapeDataString(Watermark);
            }
            var response = await _httpClientWrapper.GetAsync(new Uri(BotFrameworkUrl, new Uri("/v3/directline/conversations/" + ConversationId + "/activities" + watermarkQuery, UriKind.Relative)).ToString());

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var responseJson = JObject.Parse(responseContent);
                var activities = responseJson["activities"];
                foreach (var activity in activities)
                {
                    var message = activity.ToString();
                    provider.Messages.Enqueue(message);
                    newMessages = true;
                }

                if (responseJson.ContainsKey("watermark"))
                {
                    Watermark = responseJson["watermark"].ToString();
                }
            }
            else if ((int)response.StatusCode == 429)
            {
                // Status code 429 - Too Many Requests
                if (response.Headers.TryGetValues("Retry-After", out var retryAfterValues))
                {
                    var retryAfter = retryAfterValues.FirstOrDefault();
                    if (int.TryParse(retryAfter, out var retryAfterSeconds))
                    {
                        _logger.LogInformation($"Rate limited. Retrying after {retryAfterSeconds} seconds.");
                        await Task.Delay(retryAfterSeconds * 1000);
                    }
                    else
                    {
                        _logger.LogInformation("Rate limited. Retrying after default 2 seconds.");
                        await Task.Delay(2000);
                    }
                }
                else
                {
                    _logger.LogInformation("Rate limited. Retrying after default 2 seconds.");
                    await Task.Delay(2000);
                }
            }
            return newMessages;
        }
    }
}
