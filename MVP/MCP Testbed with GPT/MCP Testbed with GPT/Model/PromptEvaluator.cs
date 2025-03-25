using Microsoft.Extensions.AI;
//using Microsoft.Extensions.AI.OpenAI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
using OpenAI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

#pragma warning disable SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
namespace OpenAITestGenerator
{
    class ModelConfiguration
    {
        public string Endpoint { get; set; }
        public string ModelId { get; set; }
        public string SystemPrompt { get; set; }
    }

    class McpServerConfiguration
    {
        public required string Command { get; set; }
        public string[] Args { get; set; } = [];
        public Dictionary<string, string> Env { get; set; } = [];
    }

    class McpServerConfigurationCollection
    {
        public Dictionary<string, McpServerConfiguration> McpServers { get; set; }
        public Dictionary<string, ModelConfiguration> Models { get; set; }
    }

    /// <summary>
    /// This class does the evaluation of the AI (for now: OpenAI) prompts.
    /// </summary>
    internal class PromptEvaluator
    {
        private string systemPrompt = string.Empty;

        // private List<ChatMessage>? history = null;
        // private ChatHistory? history = null;
        IList<ChatMessage> messages = new List<ChatMessage>();

        private IChatCompletionService? chatCompletionService;

        public PromptEvaluator(string systemPrompt)
        {
            this.systemPrompt = systemPrompt;
        }

        public void NewChat(string systemPrompt)
        {
            this.systemPrompt = systemPrompt;
            this.messages = new List<ChatMessage>();
        }

        public async Task<(string, TimeSpan)> EvaluatePromptAsync(string query)
        {
            if (systemPrompt.Length == 0)
            {
                throw new ArgumentException("The system prompt must be set.");
            }

            if (string.IsNullOrEmpty(query))
            {
                return (string.Empty, TimeSpan.Zero);
            }

            if (this.messages == null)
            {
                this.messages = new List<ChatMessage>();
            }

            var clients = Model.Model.ClientPool;
            var conf = Model.Model.GetModel().McpServerConfigurationCollection;

            var allFunctions = await clients.GetAllAIFunctionsAsync();
            var chatOptions = new ChatOptions
            {
                Tools = allFunctions,
                Temperature = 0,
                ToolMode = ChatToolMode.Auto //let the assistant choose not to use a tool if it doesn't need to
            };

            if (chatCompletionService == null)
            {
                this.chatCompletionService = Model.Secrets.ChatCompletionService.Value;
            }

            using IChatClient chatClient =
                new OpenAIClient(Environment.GetEnvironmentVariable("OpenAIBearerToken")).AsChatClient("gpt-4o")
                    .AsBuilder().UseFunctionInvocation().Build();

            this.messages.Add(new ChatMessage(ChatRole.User, query));

            var timer = Stopwatch.StartNew();

            var result = await chatClient.GetResponseAsync(this.messages, chatOptions);

            timer.Stop();

            this.messages.Add(new ChatMessage(ChatRole.Assistant, result.Text));

            return (result.Text!, timer.Elapsed);
        }
    }
}
