using System.Net;
using System.Net.Sockets;
using OmniSharp.Extensions.LanguageServer.Server;
using Newtonsoft.Json.Linq;

namespace testengine.language.server
{
    class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// <param name="args">The command-line arguments.</param>
        static async Task Main(string[] args)
        {
            // Read the port from the configuration file, defaulting to 8080 if not defined.
            int port = GetPortFromConfig();

            // Create a TCP listener on the specified port.
            var listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            Console.WriteLine($"Listening on port {port}...");

            while (true)
            {
                // Accept incoming TCP client connections.
                var client = await listener.AcceptTcpClientAsync();
                _ = Task.Run(async () =>
                {
                    // Create and start the language server with the client stream.
                    var server = await LanguageServer.From(options => options
                        .WithInput(client.GetStream())
                        .WithOutput(client.GetStream())
                        .WithHandler<TextDocumentHandler>()
                    );

                    // Wait for the server to exit.
                    await server.WaitForExit;
                });
            }
        }

        /// <summary>
        /// Reads the port number from a configuration file.
        /// </summary>
        /// <returns>The port number specified in the configuration file, or 8080 if not specified.</returns>
        private static int GetPortFromConfig()
        {
            const string configFilePath = "config.json";
            if (File.Exists(configFilePath))
            {
                var config = JObject.Parse(File.ReadAllText(configFilePath));
                return config.Value<int?>("serverPort") ?? 8080;
            }
            return 8080;
        }
    }
}
