// Copyright(c) Microsoft Corporation.
// Licensed under the MIT license.using Microsoft.Extensions.Log

using System.Diagnostics;

namespace testengine.provider.dataverse
{
    public class AzureCliHelper
    {
        public Func<string> ExecutableSuffix = () => (PlatformHelper.IsWindows() ? ".cmd" : string.Empty);
        public Func<ProcessStartInfo, IProcessWrapper> ProcessStart = (info) => new ProcessWrapper(Process.Start(info));

        public string GetAccessToken(Uri location)
        {
            // Find the Azure CLI executable
            var azPath = FindAzureCli();
            if (string.IsNullOrEmpty(azPath))
            {
                throw new InvalidOperationException("Azure CLI not found.");
            }

            // Run the Azure CLI command to get the access token
            var processStartInfo = new ProcessStartInfo
            {
                FileName = azPath + ExecutableSuffix(),
                Arguments = $"account get-access-token --resource {location.ToString()}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = ProcessStart(processStartInfo))
            {

                process.WaitForExit();
                var result = process.StandardOutput;

                // Parse the access token from the result
                var token = ParseAccessToken(result);
                return token;
            }
        }

        public string FindAzureCli()
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = (PlatformHelper.IsWindows() ? "where" : "which"),
                Arguments = "az",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = ProcessStart(processStartInfo))
            {
                process.WaitForExit();
                var result = process.StandardOutput;

                string[] lines = result.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                if (lines == null)
                {
                    return string.Empty;
                }

                return lines.Length > 0 ? lines.First() : string.Empty;
            }
        }

        private static string ParseAccessToken(string json)
        {
            // Simple JSON parsing to extract the access token
            var tokenStart = json.IndexOf("\"accessToken\": \"") + 16;
            var tokenEnd = json.IndexOf("\"", tokenStart);
            return json.Substring(tokenStart, tokenEnd - tokenStart);
        }
    }
}
