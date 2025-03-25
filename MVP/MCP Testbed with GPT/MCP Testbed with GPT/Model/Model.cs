using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using  Microsoft.SemanticKernel.Connectors.Ollama;
using System.Text.Json;
using Microsoft.Extensions.AI;
using System.Windows;
using Microsoft.SemanticKernel.Connectors.OpenAI;

#pragma warning disable SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

namespace OpenAITestGenerator.Model
{
    internal class Model
    {
        private static Model model = new Model();
        private static string systemPromptFileName = "Assets\\SystemPrompt.txt";
        public Kernel Kernel { get; private set; } = null!;
        public static MCPClientPool ClientPool { get; private set; }

        public static Model GetModel()
        {
            return model;
        }

        public McpServerConfigurationCollection McpServerConfigurationCollection { get; private set; }
        public Model()
        {
            var builder = Kernel.CreateBuilder();
            var endpoint = new Uri("http://localhost:11434");

            var key = Secrets.ApiKey;
            var model = Secrets.OpenAiModel;

            this.Kernel = builder.Build();

            JsonSerializerOptions jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

            McpServerConfigurationCollection conf = JsonSerializer.Deserialize<McpServerConfigurationCollection>(File.ReadAllText("config.json"),
                jsonSerializerOptions)!;

            McpServerConfigurationCollection = conf;

            ClientPool = [];
            foreach (var server in conf.McpServers)
            {
                ClientPool.Add(server.Key, server.Value,
                    (parameters) =>
                    {
                        var message = $"Tool: {parameters["tool"]} ({Encoding.UTF8.GetString(JsonSerializer.SerializeToUtf8Bytes(parameters["parameters"]))})"; 
                        var result = MessageBox.Show("The Assistant wants to run a tool.\n" + message, "Tool Permission", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No, MessageBoxOptions.DefaultDesktopOnly);
                        return result == MessageBoxResult.Yes;
                    }
                );
            }

        }

        public string SystemPrompt 
        { 
            get
            {
                return File.ReadAllText(systemPromptFileName);
            }
            
            set 
            {
                File.WriteAllText(systemPromptFileName, value);
            }
        }
    }
}
