// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Types;
using Microsoft.PowerFx;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Reflection;
using Microsoft.PowerFx.Core.Utils;

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
            : base(DPath.Root.Append(new DName("TestEngine")), "PlaywrightScript", FormulaType.Blank, FormulaType.String)
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

            if ( !_filesystem.IsValidFilePath(file.Value) )
            {
                _logger.LogError("Invalid file");
                throw new ArgumentException("Invalid file");
            }

            _logger.LogDebug("Loading file");

            var filename = GetFullFile(_testState, file.Value);
            var script = _filesystem.ReadAllText(filename);

            byte[] assemblyBinaryContent;

            _logger.LogDebug("Compiling file");

            ScriptOptions options = ScriptOptions.Default;
            var roslynScript = CSharpScript.Create(script, options);
            var compilation = roslynScript.GetCompilation();

            compilation = compilation.WithOptions(compilation.Options
            .WithOptimizationLevel(OptimizationLevel.Release)
            .WithOutputKind(OutputKind.DynamicallyLinkedLibrary));

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

            GC.Collect();

            Assembly assembly = Assembly.Load(assemblyBinaryContent);

            _logger.LogDebug("Run script");
            Run(assembly);

            _logger.LogInformation("Successfully finished executing PlaywrightScript function.");

            return FormulaValue.NewBlank();
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
            foreach ( var scriptType in types )
            {
                if ( scriptType.Name.Equals("PlaywrightScript") )
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

            if ( !found ) {
                 _logger.LogError("PlaywrightScript class not found");
            }
        }
    }
}

