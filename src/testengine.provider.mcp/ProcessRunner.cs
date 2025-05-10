// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Diagnostics;

namespace Microsoft.PowerApps.TestEngine.Providers
{
    public class ProcessRunner : IProcessRunner
    {
        public int Run(string fileName, string arguments, string workingDirectory)
        {
            // Validate fileName
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));
            }

            if (!Path.IsPathRooted(fileName) && !IsExecutableInPath(fileName))
            {
                throw new FileNotFoundException($"The executable '{fileName}' was not found in the system PATH or as an absolute path.");
            }

            // Validate arguments
            if (arguments == null)
            {
                throw new ArgumentNullException(nameof(arguments), "Arguments cannot be null.");
            }

            // Validate workingDirectory
            if (string.IsNullOrWhiteSpace(workingDirectory))
            {
                throw new ArgumentException("Working directory cannot be null or empty.", nameof(workingDirectory));
            }

            if (!Directory.Exists(workingDirectory))
            {
                throw new DirectoryNotFoundException($"The specified working directory does not exist: {workingDirectory}");
            }

            // Initialize the process
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = true,
                    CreateNoWindow = true
                }
            };

            // Execute the process
            process.Start();
            process.WaitForExit();

            // Return the exit code
            return process.ExitCode;
        }

        private bool IsExecutableInPath(string fileName)
        {
            var paths = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator) ?? Array.Empty<string>();
            foreach (var path in paths)
            {
                var fullPath = Path.Combine(path, fileName);
                if (File.Exists(fullPath))
                {
                    return true;
                }

                if (File.Exists(fullPath + ".cmd"))
                {
                    return true;
                }

                if (File.Exists(fullPath + ".exe"))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
