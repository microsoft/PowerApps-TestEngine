// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Diagnostics;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.System;

namespace Microsoft.PowerApps.TestEngine.Providers
{
    public class MCPProxyInstaller
    {
        private readonly IFileSystem? _fileSystem;
        private readonly IProcessRunner? _processRunner;
        private readonly ILogger? _logger;

        public Action<string,string> WriteFile = (file, content) => File.WriteAllText(file, content);

        public MCPProxyInstaller()
        {

        }

        public MCPProxyInstaller(IFileSystem fileSystem, IProcessRunner processRunner, ILogger logger)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
            _logger = logger ?? throw new ArgumentNullException(nameof(_logger));
        }

        public virtual void EnsureMCPProxyInstalled()
        {
            // Get the default root path for the test engine
            var rootPath = _fileSystem.GetDefaultRootTestEngine();
            var mcpPath = Path.Combine(rootPath, "mcp");

            bool installed = false;

            _logger.LogDebug($"Checking if {mcpPath} exists");

            // Check if the "mcp" directory exists
            if (!_fileSystem.Exists(mcpPath))
            {
                // Create the "mcp" directory
                _fileSystem.CreateDirectory(mcpPath);
            }

            var proxyFile = Path.Combine(mcpPath, "app.js");
            if (NeedsUpdate("proxy/app.js", proxyFile, proxyFile + ".hash"))
            {
                ExtractFile("proxy/app.js", proxyFile);
                WriteFile(proxyFile + ".hash", ComputeEmbeddedResourceHash("proxy/app.js"));
                installed = true;
            }


            proxyFile = Path.Combine(mcpPath, "package.json");
            if (_fileSystem?.Exists(proxyFile) == false)
            {
                ExtractFile("proxy/package.json", proxyFile);
                installed = true;
            }

            if (installed)
            {
                // Run npm install to install dependencies
                RunNpmInstall(mcpPath);
            }
        }


        private bool NeedsUpdate(string resourcePath, string destinationPath, string hashFilePath)
        {
            // Compute the hash of the embedded resource
            var embeddedHash = ComputeEmbeddedResourceHash(resourcePath);

            // Check if the destination file exists
            if (!_fileSystem.FileExists(destinationPath))
            {
                return true; // File does not exist, needs to be updated
            }

            // Check if the hash file exists
            if (!_fileSystem.FileExists(hashFilePath))
            {
                return true; // Hash file does not exist, needs to be updated
            }

            // Read the existing hash from the hash file
            var existingHash = _fileSystem.ReadAllText(hashFilePath);

            // Compare the hashes
            return !string.Equals(embeddedHash, existingHash, StringComparison.OrdinalIgnoreCase);
        }

        public static string ComputeEmbeddedResourceHash(string resourcePath)
        {
            // Get the current assembly
            var assembly = typeof(MCPProxyInstaller).Assembly;

            // Ensure the resource path matches the embedded resource naming convention
            var resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(name => name.EndsWith(resourcePath.Replace("/", "."), StringComparison.OrdinalIgnoreCase));

            if (resourceName == null)
            {
                throw new InvalidOperationException($"Embedded resource '{resourcePath}' not found in assembly.");
            }

            // Read the embedded resource stream
            using (var resourceStream = assembly.GetManifestResourceStream(resourceName))
            {
                if (resourceStream == null)
                {
                    throw new InvalidOperationException($"Failed to load embedded resource '{resourceName}'.");
                }

                using (var sha256 = SHA256.Create())
                {
                    var hashBytes = sha256.ComputeHash(resourceStream);
                    return BitConverter.ToString(hashBytes).Replace("-", "").ToUpperInvariant();
                }
            }
        }

        private void WriteHashFile(string resourcePath, string hashFilePath)
        {
            var hash = ComputeEmbeddedResourceHash(resourcePath);
            _fileSystem.WriteTextToFile(hashFilePath, hash, overwrite: true);
        }


        private void ExtractFile(string resourcePath, string destinationPath)
        {
            // Get the current assembly
            var assembly = typeof(MCPProxyInstaller).Assembly;

            // Ensure the resource path matches the embedded resource naming convention
            var resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(name => name.EndsWith(resourcePath.Replace("/", "."), StringComparison.OrdinalIgnoreCase));

            if (resourceName == null)
            {
                throw new InvalidOperationException($"Embedded resource '{resourcePath}' not found in assembly.");
            }

            // Read the embedded resource stream
            using (var resourceStream = assembly.GetManifestResourceStream(resourceName))
            {
                if (resourceStream == null)
                {
                    throw new InvalidOperationException($"Failed to load embedded resource '{resourceName}'.");
                }

                using (var reader = new StreamReader(resourceStream))
                {
                    var fileContent = reader.ReadToEnd();

                    // Write the content to the destination path
                    WriteFile(destinationPath, fileContent);
                }
            }
        }

        private void RunNpmInstall(string workingDirectory)
        {
            var exitCode = _processRunner.Run("npm", "install", workingDirectory);
           
            if (exitCode != 0)
            {
                throw new InvalidOperationException($"npm install failed");
            }
        }
    }
}
