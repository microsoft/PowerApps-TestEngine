using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Text.Json;
using testengine.plugin.copilot;
using testengine.provider.copilot.portal.services;
using Microsoft.JSInterop;

namespace copilot.test
{
    [SupportedOSPlatform("browser")]
    public partial class TestEngine
    {
        private static EvaluateTestSteps _steps = new EvaluateTestSteps();

        [JSExport]
        public static async Task Init(string config)
        {
            Dictionary<string, string> settings = JsonSerializer.Deserialize<Dictionary<string, string>>(config);
            _steps.EnvironmentId = settings["environmentId"];
            var token = settings["token"];
            await _steps.ExecuteAsync(settings["botIdentifier"], "", "Experimental.Connect()", token);
        }

        [JSExport]
        public static string ConversationId()
        {
            return _steps.ConversationId;
        }

        [JSExport]
        public static string[] GetMessages()
        {
            return _steps.Messages;
        }

        [JSExport]
        public static void SetMessages(string[] messages)
        {
            _steps.Messages = messages;
        }

        [JSExport]
        public static async Task<string> ExecuteAsync(string config, string code)
        {
            var serviceProvider = new ServiceCollection()
               .AddScoped<IJSRuntime, JSRuntime>()
               .BuildServiceProvider();

            Dictionary<string, string> settings = JsonSerializer.Deserialize<Dictionary<string, string>>(config);
            _steps.EnvironmentId = settings["environmentId"];
            _steps.WorkerService = new SingleThreadedWorkerService();
            if (settings.ContainsKey("messages"))
            {
                _steps.Messages = JsonSerializer.Deserialize<string[]>(settings["messages"]);
            }
            var token = settings["token"];

            return await _steps.ExecuteAsync(settings["botIdentifier"], settings["conversationId"], code, token);
        }
    }
}
