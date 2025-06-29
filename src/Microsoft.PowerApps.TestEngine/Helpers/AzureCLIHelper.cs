// Copyright(c) Microsoft Corporation.
// Licensed under the MIT license.using Microsoft.Extensions.Log

using System;
using System.Diagnostics;
using System.Text;

namespace Microsoft.PowerApps.TestEngine.Helpers
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
                Arguments = $"account get-access-token --resource {location.ToString()} --query \"accessToken\" --output tsv",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = ProcessStart(processStartInfo))
            {
                string token = ReadOutputForAccessTokenAsync(process).Result;
                return token;
            }
        }

        public async Task<string> GetAccessTokenAsync(Uri location)
        {
            var azPath = FindAzureCli();
            if (string.IsNullOrEmpty(azPath))
            {
                throw new InvalidOperationException("Azure CLI not found.");
            }

            // Create a temporary file to store the output
            var tempFilePath = Path.GetTempFileName();

            try
            {
                var azApp = azPath + ExecutableSuffix();
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "pwsh",
                    Arguments = $"-WindowStyle Hidden -Command \"az account get-access-token --resource {location.ToString()} --query \\\"accessToken\\\" --output tsv > \\\"{tempFilePath}\\\"\"",
                    UseShellExecute = true,        // Required for file redirection
                    CreateNoWindow = true
                };

                using (var process = new Process { StartInfo = processStartInfo })
                {
                    process.Start();

                    // Wait for the process to complete
                    process.WaitForExit();

                    // Monitor the file for the access token
                    var token = File.ReadAllText(tempFilePath);
                    return token;
                }
            }
            finally
            {
                // Clean up the temporary file
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
        }

        private async Task<string> ReadOutputForAccessTokenAsync(IProcessWrapper process)
        {
            return ParseAccessToken(process.StandardOutput);
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
