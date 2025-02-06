using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using testengine.plugin.copilot;

namespace TestEngine.Samples.Copilot;

public class ReadEvaluatePrintLoop
{
    private static string SessionToken = String.Empty;

    private static SampleConnectionSettings settings = null;

    public static async Task Main(string[] args)
    {

        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

        // Get the configuration settings for the DirectToEngine client from the appsettings.json file.
        settings = new SampleConnectionSettings(builder.Configuration.GetSection("DirectToEngineSettings"));

        Console.WriteLine("Welcome to the Test Engine Copilot REPL!");
        
        while (true)
        {
            Console.WriteLine("Type exit to quit or you can enter Power Fx test steps to run or specify the name of a file to read tests from.");
            Console.Write("> ");
            string input = Console.ReadLine();

            if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            if (File.Exists(input))
            {
                string fileContent = File.ReadAllText(input);
                await ExecuteTest(fileContent);
            }
            else
            {
                string fullInput = input;
                while (!string.IsNullOrEmpty(input) && input.EndsWith(";"))
                {
                    Console.Write("+ ");
                    input = Console.ReadLine();
                    fullInput += Environment.NewLine + input;
                }

                await ExecuteTest(fullInput);
            }
        }
    }

    private static async Task ExecuteTest(string input)
    {
        var steps = new EvaluateTestSteps();
        steps.EnvironmentId = settings.EnvironmentId;
        var token = await GetToken();

        var response = await steps.ExecuteAsync(settings.BotIdentifier, input, token);
        var file = DateTime.Now.ToString("yyyy-MM-dd-HHmmss") + ".json";
        File.WriteAllText(file, response);

        Console.WriteLine($"Results in {file}");
    }

    private static async Task<string> GetToken()
    {
        string agentKey = Environment.GetEnvironmentVariable("AgentToken");

        if (string.IsNullOrEmpty(agentKey))
        {
            if (!string.IsNullOrEmpty(SessionToken))
            {
                return SessionToken;
            }

            var handler = new TokenHandler(settings);

            var result = await handler.AuthenticateAsync();

            SessionToken = result.AccessToken;
            return SessionToken;
        }

        return agentKey;
    }
}
