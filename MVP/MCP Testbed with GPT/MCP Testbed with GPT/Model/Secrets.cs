using Azure.AI.OpenAI;
using Azure;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Connectors.Ollama;
using System;
using OpenAI;

namespace OpenAITestGenerator.Model
{
internal class Secrets
{
        /// <summary>
        /// Determines whether the Azure OpenAI API should be used.
        /// </summary>
        //public static bool UsesAzureOpenAI => false;

        public static string OpenAiModel
        {
            get
            {
                return "gpt-4o";
            }
        }

        // public static string OllamaModel => "llama3.2:latest";

        public static string? ApiKey
        {
            get
            {
                return Environment.GetEnvironmentVariable("OpenAIBearerToken");
            }
        }

        public static Lazy<IChatCompletionService> ChatCompletionService
        {
            get
            {
                if (string.IsNullOrEmpty(ApiKey))
                {
                    throw new ArgumentException("The OpenAI API key must be set.");
                }
                return new Lazy<IChatCompletionService>(new OpenAIChatCompletionService(
                            modelId: OpenAiModel,
                            apiKey: ApiKey!));
            }
        }

        //public static Lazy<OpenAIClient> OpenAIClientXXX
        //{
        //    get
        //    {
        //        var apikey = ApiKey;
        //        string OpenAIEndpoint = "https://api.openai.com/v1/chat/completions";

        //        return new Lazy<OpenAIClient>(new OpenAIClient(apikey!));
        //    }
        //}
    }
}
