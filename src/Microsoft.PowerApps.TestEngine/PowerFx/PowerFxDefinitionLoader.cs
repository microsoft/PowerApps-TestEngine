// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.System;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Microsoft.PowerApps.TestEngine.PowerFx
{
    /// <summary>
    /// Helper class for loading PowerFx definitions from external files
    /// </summary>
    public class PowerFxDefinitionLoader
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;

        public PowerFxDefinitionLoader(IFileSystem fileSystem, ILogger logger)
        {
            _fileSystem = fileSystem;
            _logger = logger;
        }

        private List<string> _processed = new List<string>();

        /// <summary>
        /// Loads PowerFx definitions from a file and merges them with the provided settings
        /// </summary>
        public void LoadPowerFxDefinitionsFromFile(string filePath, Config.TestSettings settings)
        {
            try
            {
                if (_processed.Contains(filePath))
                {
                    _logger.LogWarning($"PowerFx definition file already processed: {filePath}");
                    return;
                }
                _processed.Add(filePath);

                if (string.IsNullOrEmpty(filePath) || !_fileSystem.FileExists(filePath))
                {
                    _logger.LogError($"PowerFx definition file not found: {filePath}");
                    return;
                }

                var content = _fileSystem.ReadAllText(filePath);
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();

                var definitionFile = deserializer.Deserialize<Config.TestSettings>(content);

                // Merge PowerFx test types
                if (definitionFile.PowerFxTestTypes != null)
                {
                    foreach (var type in definitionFile.PowerFxTestTypes)
                    {
                        // Check if we already have a type with the same name
                        var existingType = settings.PowerFxTestTypes.FirstOrDefault(t => t.Name == type.Name);
                        if (existingType != null)
                        {
                            // Update existing type
                            existingType.Value = type.Value;
                        }
                        else
                        {
                            // Add new type
                            settings.PowerFxTestTypes.Add(type);
                        }
                    }
                }

                // Merge PowerFx functions
                if (definitionFile.TestFunctions != null)
                {
                    foreach (var function in definitionFile.TestFunctions)
                    {
                        // Check if we already have a function with the same name
                        var existingFunction = settings.TestFunctions
                            .FirstOrDefault(f =>
                                f.Code != null &&
                                function.Code != null &&
                                f.Code.StartsWith(function.Code.Split('(')[0]));

                        if (existingFunction != null)
                        {
                            // Update existing function
                            existingFunction.Description = function.Description;
                            existingFunction.Code = function.Code;
                        }
                        else
                        {
                            // Add new function
                            settings.TestFunctions.Add(function);
                        }
                    }
                }

                // Process nested PowerFx definitions
                if (definitionFile.PowerFxDefinitions != null && definitionFile.PowerFxDefinitions.Count > 0)
                {
                    string baseDirectory = Path.GetDirectoryName(filePath);
                    ProcessNestedPowerFxDefinitions(baseDirectory, definitionFile.PowerFxDefinitions, settings);
                }

                _logger.LogInformation($"Successfully loaded PowerFx definitions from {filePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading PowerFx definitions from {filePath}: {ex.Message}");
            }
        }

        /// <summary>
        /// Processes nested PowerFx definitions
        /// </summary>
        private void ProcessNestedPowerFxDefinitions(string baseDirectory, List<Config.PowerFxDefinition> definitions, Config.TestSettings settings)
        {
            foreach (var definition in definitions)
            {
                if (string.IsNullOrEmpty(definition.Location))
                {
                    continue;
                }

                // Resolve path relative to the parent file
                string resolvedPath;
                if (Path.IsPathRooted(definition.Location))
                {
                    resolvedPath = definition.Location;
                }
                else
                {
                    resolvedPath = Path.GetFullPath(Path.Combine(baseDirectory, definition.Location));
                }

                // Load and merge the nested definition
                LoadPowerFxDefinitionsFromFile(resolvedPath, settings);
            }
        }
    }
}
