// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace testengine.module
{
    /// <summary>
    /// This will execute CSharp Script (CSX) file passing IBrowserContext and ILogger
    /// </summary>
    public class PlaywrightScriptFunction : ReflectionFunction
    {
        private readonly ITestInfraFunctions _testInfraFunctions;
        private readonly ITestState _testState;
        private readonly ILogger _logger;
        private readonly IFileSystem _filesystem;

        public PlaywrightScriptFunction(ITestInfraFunctions testInfraFunctions, ITestState testState, IFileSystem filesystem, ILogger logger)
            : base(DPath.Root.Append(new DName("Experimental")), "PlaywrightScript", FormulaType.Blank, FormulaType.String)
        {
            _testInfraFunctions = testInfraFunctions;
            _testState = testState;
            _logger = logger;
            _filesystem = filesystem;
        }

        public BlankValue Execute(StringValue file)
        {
            _logger.LogInformation("------------------------------\n\n" +
                "Executing PlaywrightScript function.");

            if (!_filesystem.FileExists(file.Value))
            {
                _logger.LogError("Invalid file");
                throw new ArgumentException("Invalid file");
            }

            _logger.LogDebug("Loading file");

            var filename = GetFullFile(_testState, file.Value);
            var script = _filesystem.ReadAllText(filename);

            var hash = ComputeSha256Hash(script);

            var dllFile = Path.Combine(Path.GetDirectoryName(this.GetType().Assembly.Location), Path.GetFileNameWithoutExtension(filename) + $"-{hash}.dll");
            if (!File.Exists(dllFile))
            {
                _logger.LogDebug("Compiling file");

                ScriptOptions options = ScriptOptions.Default;
                var roslynScript = CSharpScript.Create(script, options);
                var compilation = roslynScript.GetCompilation();

                compilation = compilation.WithOptions(compilation.Options
                .WithOptimizationLevel(OptimizationLevel.Release)
                .WithOutputKind(OutputKind.DynamicallyLinkedLibrary));

                byte[] assemblyBinaryContent;
                using (var assemblyStream = new MemoryStream())
                {
                    var result = compilation.Emit(assemblyStream);
                    if (!result.Success)
                    {
                        var errors = string.Join(Environment.NewLine, result.Diagnostics.Select(x => x));
                        throw new Exception("Compilation errors: " + Environment.NewLine + errors);
                    }

                    assemblyBinaryContent = assemblyStream.ToArray();
                }
                File.WriteAllBytes(dllFile, assemblyBinaryContent);

                // Required after compile
                GC.Collect();
            }

            Assembly assembly = Assembly.LoadFile(dllFile);

            _logger.LogDebug("Run script");
            Run(assembly);

            _logger.LogInformation("Successfully finished executing PlaywrightScript function.");

            return FormulaValue.NewBlank();
        }

        static string ComputeSha256Hash(string rawData)
        {
            // Create a SHA256   
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array  
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Convert byte array to a string   
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        private string GetFullFile(ITestState testState, string filename)
        {
            if (!Path.IsPathRooted(filename))
            {
                var testResultDirectory = Path.GetDirectoryName(testState.GetTestConfigFile().FullName);
                filename = Path.Combine(testResultDirectory, filename);
            }
            return filename;
        }

        private void Run(Assembly assembly)
        {
            //Execute the script
            var types = assembly.GetTypes();

            bool found = false;
            foreach (var scriptType in types)
            {
                if (scriptType.Name.Equals("PlaywrightScript"))
                {
                    found = true;

                    var method = scriptType.GetMethod("Run", BindingFlags.Static | BindingFlags.Public);

                    var context = _testInfraFunctions.GetContext();

                    if (method == null)
                    {
                        _logger.LogError("Static Run Method not found");
                    }

                    method?.Invoke(null, new object[] { context, _logger });
                }
            }

            if (!found)
            {
                _logger.LogError("PlaywrightScript class not found");
            }
        }
    }
}

