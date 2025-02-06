using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Text.Json;
using testengine.plugin.copilot;

namespace copilot.test
{
    [SupportedOSPlatform("browser")]
    public partial class TestEngine
    {
        [JSExport]
        public static async Task<string> ExecuteAsync(string config, string code)
        {
            var steps = new EvaluateTestSteps();

            Dictionary<string, string> settings = JsonSerializer.Deserialize<Dictionary<string, string>>(config);

            steps.EnvironmentId = settings["environmentId"];
            var token = settings["token"];

            return await steps.ExecuteAsync(settings["botIdentifier"], code, token);
        }
    }
}
