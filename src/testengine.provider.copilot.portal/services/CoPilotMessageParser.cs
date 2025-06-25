// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Text.Json;

namespace testengine.provider.copilot.portal.services
{
    /// <summary>
    /// Parser for Copilot message responses
    /// </summary>
    public static class CoPilotMessageParser
    {
        /// <summary>
        /// Parse messages from Copilot Studio API response
        /// </summary>
        /// <param name="responseBody">The JSON response body</param>
        /// <returns>Collection of parsed messages</returns>
        public static IEnumerable<string> ParseMessages(string responseBody)
        {
            var messages = new List<string>();
            
            try
            {
                if (string.IsNullOrEmpty(responseBody))
                {
                    return messages;
                }

                var jsonDocument = JsonDocument.Parse(responseBody);
                
                // Parse the response based on the expected Copilot Studio API format
                if (jsonDocument.RootElement.TryGetProperty("value", out var valueElement))
                {
                    if (valueElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in valueElement.EnumerateArray())
                        {
                            if (item.TryGetProperty("text", out var textElement))
                            {
                                var text = textElement.GetString();
                                if (!string.IsNullOrEmpty(text))
                                {
                                    messages.Add(text);
                                }
                            }
                            
                            // Also check for message content in other formats
                            if (item.TryGetProperty("content", out var contentElement))
                            {
                                var content = contentElement.GetString();
                                if (!string.IsNullOrEmpty(content))
                                {
                                    messages.Add(content);
                                }
                            }
                        }
                    }
                }
                else if (jsonDocument.RootElement.TryGetProperty("text", out var directTextElement))
                {
                    var text = directTextElement.GetString();
                    if (!string.IsNullOrEmpty(text))
                    {
                        messages.Add(text);
                    }
                }
                else
                {
                    // Fallback: treat the entire response as a message
                    messages.Add(responseBody);
                }
            }
            catch (JsonException)
            {
                // If JSON parsing fails, treat the entire response as a single message
                messages.Add(responseBody);
            }
            
            return messages;
        }
    }
}
