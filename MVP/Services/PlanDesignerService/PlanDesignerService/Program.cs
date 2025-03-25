using ModelContextProtocol;
using ModelContextProtocol.Server;
using Microsoft.Extensions.Hosting;
using System.ComponentModel;
using System.Diagnostics;

namespace PlanDesignerService
{

    public class PlanDesignerService
    {
        public static async Task Main(string[] args)
        {
            var builder = Host.CreateEmptyApplicationBuilder(settings: null);
            builder.Services
                .AddMcpServer()
            .WithStdioServerTransport()
            .WithTools();

            await builder.Build().RunAsync();
        }

        [McpToolType]
        public static class EchoTool
        {
            [McpTool, Description("Gets the plan designer text for the given environment.")]
            public static async Task<string> GetPlanDesignerText(
                [Description("The environment containing the Plan design")] string environment,
                [Description("The name of the Plan design")] string planDesignName)
            {
                // Call the pac cli tool
                var info = new ProcessStartInfo()
                {
                    FileName = "pac.exe",
                    Arguments = $" --environment {environment}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                };

                var process = new Process() { StartInfo = info };
                process.Start();

                await process.WaitForExitAsync();

                // Do we need the output from the pac command?
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();

                if (process.ExitCode != 0)
                {
                    throw new Exception($"Process exited with code {process.ExitCode}: {error}");
                }

                return output;
            }
        }
    }
}
