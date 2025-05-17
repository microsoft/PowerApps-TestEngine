// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.PowerApps.TestEngine.MCP.Visitor;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;
using Newtonsoft.Json.Linq;

namespace Microsoft.PowerApps.TestEngine.MCP
{
    /// <summary>
    /// Main workspace visitor class that traverses a directory structure and processes files
    /// according to provided scan rules. This class coordinates the scanning process and 
    /// delegates specific file processing to specialized methods.
    /// </summary>
    public class WorkspaceVisitor
    {
        /// <summary>
        /// Constant for the current node variable name in PowerFx expressions.
        /// This is used when evaluating scan rule conditions.
        /// </summary>
        public const string CurrentNodeVariableName = "Current";

        private readonly IFileSystem _fileSystem;
        private readonly string _workspacePath;
        private readonly ScanReference _scanReference;
        private readonly IRecalcEngine _recalcEngine;
        private readonly ILogger _logger;
        private readonly List<string> _visitedPaths;
        private readonly Dictionary<string, List<string>> _contextMap;
        private readonly List<Fact> _facts;

        /// <summary>
        /// Creates a new instance of WorkspaceVisitor.
        /// </summary>
        /// <param name="fileSystem">The file system interface to use for file operations</param>
        /// <param name="workspacePath">The root workspace path to scan</param>
        /// <param name="scanReference">The scan configuration with rules to apply</param>
        /// <param name="recalcEngine">The PowerFx recalc engine for evaluating expressions</param>
        /// <param name="logger">Optional logger (defaults to ConsoleLogger if not provided)</param>
        /// <exception cref="ArgumentNullException">Thrown if required parameters are null</exception>
        public WorkspaceVisitor(IFileSystem fileSystem, string workspacePath, ScanReference scanReference,
                               IRecalcEngine recalcEngine, ILogger logger = null)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _workspacePath = workspacePath ?? throw new ArgumentNullException(nameof(workspacePath));
            _scanReference = scanReference ?? throw new ArgumentNullException(nameof(scanReference));
            _recalcEngine = recalcEngine ?? throw new ArgumentNullException(nameof(recalcEngine));
            _logger = logger ?? new ConsoleLogger();
            _visitedPaths = new List<string>();
            _contextMap = new Dictionary<string, List<string>>();
            _facts = new List<Fact>();
        }

        /// <summary>
        /// Starts the workspace visit process.
        /// </summary>
        /// <exception cref="DirectoryNotFoundException">Thrown if the workspace path doesn't exist</exception>
        public void Visit()
        {
            if (!_fileSystem.Exists(_workspacePath))
            {
                throw new DirectoryNotFoundException($"The workspace path does not exist: {_workspacePath}");
            }

            // Process OnStart rules before starting directory traversal
            OnStart();

            // Process all directories and files recursively
            VisitDirectory(_workspacePath, "Root");
            
            // Process OnEnd rules after all files have been processed
            OnEnd();
        }

        /// <summary>
        /// Processes OnStart rules when the scan begins.
        /// </summary>
        protected virtual void OnStart()
        {
            if (_scanReference.OnStart == null)
            {
                return;
            }

            // Create a workspace node to represent the starting point
            var workspaceNode = new DirectoryNode
            {
                Name = Path.GetFileName(_workspacePath),
                Path = "",
                FullPath = "",
                Type = NodeType.Directory
            };

            // Set the current node in the RecalcEngine
            SetCurrentNodeInRecalcEngine(workspaceNode);

            // Process each OnStart rule
            foreach (var rule in _scanReference.OnStart)
            {
                try
                {
                    // Check if the 'when' condition evaluates to true
                    if (string.IsNullOrEmpty(rule.When) || EvaluateWhenCondition(rule.When))
                    {
                        // Execute the 'then' clause
                        ExecuteThenClause(rule.Then, workspaceNode);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error processing OnStart rule: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Processes OnEnd rules when the scan completes.
        /// </summary>
        protected virtual void OnEnd()
        {
            if (_scanReference.OnEnd == null)
            {
                return;
            }

            // Create a workspace node to represent the ending point
            var workspaceNode = new DirectoryNode
            {
                Name = Path.GetFileName(_workspacePath),
                Path = "",
                FullPath = "",
                Type = NodeType.Directory
            };

            // Set the current node in the RecalcEngine
            SetCurrentNodeInRecalcEngine(workspaceNode);

            // Process each OnEnd rule
            foreach (var rule in _scanReference.OnEnd)
            {
                try
                {
                    // Check if the 'when' condition evaluates to true
                    if (string.IsNullOrEmpty(rule.When) || EvaluateWhenCondition(rule.When))
                    {
                        // Execute the 'then' clause
                        ExecuteThenClause(rule.Then, workspaceNode);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error processing OnEnd rule: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Visits a directory and processes all its files and subdirectories.
        /// </summary>
        /// <param name="directoryPath">The path of the directory to visit</param>
        /// <param name="parentPath">The hierarchical parent path</param>
        private void VisitDirectory(string directoryPath, string parentPath)
        {
            if (_visitedPaths.Contains(directoryPath))
            {
                return;
            }

            _visitedPaths.Add(directoryPath);

            // Process this directory
            var relativePath = GetRelativePath(_workspacePath, directoryPath);
            var fullPath = Path.Combine(parentPath, Path.GetFileName(directoryPath));

            // Create directory node
            var directoryNode = new DirectoryNode
            {
                Name = Path.GetFileName(directoryPath),
                Path = relativePath,
                FullPath = fullPath,
                Type = NodeType.Directory
            };

            // Call OnDirectory rules
            OnDirectory(directoryNode);

            // Only continue if directory has context or if there are no OnDirectory rules
            var hasContext = _contextMap.ContainsKey(directoryPath);
            var hasOnDirectoryRules = _scanReference.OnDirectory != null && _scanReference.OnDirectory.Any();

            // Process all subdirectories
            foreach (var subdirectory in _fileSystem.GetDirectories(directoryPath))
            {
                VisitDirectory(subdirectory, fullPath);
            }

            // Process all files in this directory
            foreach (var filePath in _fileSystem.GetFiles(directoryPath))
            {
                VisitFile(filePath, fullPath);
            }
        }

        /// <summary>
        /// Visits a file and processes its contents based on file type.
        /// </summary>
        /// <param name="filePath">The path of the file to visit</param>
        /// <param name="parentPath">The hierarchical parent path</param>
        private void VisitFile(string filePath, string parentPath)
        {
            if (_visitedPaths.Contains(filePath))
            {
                return;
            }

            _visitedPaths.Add(filePath);

            var relativePath = GetRelativePath(_workspacePath, filePath);
            var fullPath = Path.Combine(parentPath, Path.GetFileName(filePath));

            // Create file node
            var fileNode = new FileNode
            {
                Name = Path.GetFileName(filePath),
                Path = relativePath,
                FullPath = fullPath,
                Extension = Path.GetExtension(filePath).TrimStart('.').ToLowerInvariant(),
                Type = NodeType.File
            };

            // Call OnFile rules
            OnFile(fileNode);

            // Check if file has context
            var hasContext = _contextMap.ContainsKey(fileNode.FullPath);

            if (hasContext)
            {
                // Process file contents based on file type
                var extension = Path.GetExtension(filePath).ToLowerInvariant();
                var fileContent = _fileSystem.ReadAllText(filePath);

                if (extension == ".yaml" || extension == ".yml")
                {
                    ProcessYamlFile(filePath, fileContent, fullPath);
                }
                else if (extension == ".json")
                {
                    ProcessJsonFile(filePath, fileContent, fullPath);
                }
                else if (string.IsNullOrEmpty(extension) && fileContent.TrimStart().StartsWith("{"))
                {
                    // Check if the file might be JSON despite not having an extension
                    try
                    {
                        ProcessJsonFile(filePath, fileContent, fullPath);
                    }
                    catch (Exception)
                    {
                        // Not valid JSON, skip processing
                    }
                }
            }
        }

        /// <summary>
        /// Processes a YAML file by deserializing its contents and visiting its objects.
        /// </summary>
        /// <param name="filePath">The path of the YAML file</param>
        /// <param name="fileContent">The content of the YAML file</param>
        /// <param name="parentPath">The hierarchical parent path</param>
        protected virtual void ProcessYamlFile(string filePath, string fileContent, string parentPath)
        {
            try
            {
                var deserializer = new YamlDotNet.Serialization.DeserializerBuilder()
                    .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention.Instance)
                    .Build();

                var yamlObject = deserializer.Deserialize<Dictionary<object, object>>(fileContent);
                if (yamlObject != null)
                {
                    VisitYamlObject(yamlObject, filePath, parentPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing YAML file {filePath}: {ex.Message}");
            }
        }

        /// <summary>
        /// Processes a JSON file by parsing its contents and visiting its objects.
        /// </summary>
        /// <param name="filePath">The path of the JSON file</param>
        /// <param name="fileContent">The content of the JSON file</param>
        /// <param name="parentPath">The hierarchical parent path</param>
        protected virtual void ProcessJsonFile(string filePath, string fileContent, string parentPath)
        {
            try
            {
                var jsonObject = JObject.Parse(fileContent);
                VisitJsonObject(jsonObject, filePath, parentPath);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing JSON file {filePath}: {ex.Message}");
            }
        }

        /// <summary>
        /// Visits a YAML object and processes its properties.
        /// </summary>
        /// <param name="yamlObject">The YAML object to visit</param>
        /// <param name="filePath">The path of the file containing the YAML object</param>
        /// <param name="parentPath">The hierarchical parent path</param>
        /// <param name="propertyName">Optional name of the property containing this object</param>
        private void VisitYamlObject(Dictionary<object, object> yamlObject, string filePath, string parentPath, string propertyName = "")
        {
            // Create an object node
            var objectNode = new ObjectNode
            {
                Name = propertyName,
                Path = filePath,
                FullPath = parentPath,
                Type = NodeType.Object
            };

            // Call OnObject rules
            OnObject(objectNode);

            // Visit each property
            foreach (var property in yamlObject)
            {
                var propertyKey = property.Key.ToString();
                var fullPropertyPath = string.IsNullOrEmpty(parentPath) ? propertyKey : $"{parentPath}.{propertyKey}";

                // Create a property node
                var propertyNode = new PropertyNode
                {
                    Name = propertyKey,
                    Path = filePath,
                    FullPath = fullPropertyPath,
                    Type = NodeType.Property,
                    Value = property.Value?.ToString()
                };

                // Call OnProperty rules
                OnProperty(propertyNode);

                // Recursively process nested objects
                if (property.Value is Dictionary<object, object> nestedObject)
                {
                    VisitYamlObject(nestedObject, filePath, fullPropertyPath, propertyKey);
                }
                else if (property.Value is List<object> listValue)
                {
                    foreach (var item in listValue)
                    {
                        if (item is Dictionary<object, object> listItem)
                        {
                            VisitYamlObject(listItem, filePath, fullPropertyPath, propertyKey);
                        }
                    }
                }

                // Check for functions in property values if it's a string
                if (property.Value is string stringValue)
                {
                    CheckForFunctions(stringValue, filePath, fullPropertyPath);
                }
            }
        }

        /// <summary>
        /// Visits a JSON object and processes its properties.
        /// </summary>
        /// <param name="jsonToken">The JSON token to visit</param>
        /// <param name="filePath">The path of the file containing the JSON object</param>
        /// <param name="parentPath">The hierarchical parent path</param>
        /// <param name="propertyName">Optional name of the property containing this object</param>
        private void VisitJsonObject(JToken jsonToken, string filePath, string parentPath, string propertyName = "")
        {
            if (jsonToken is JObject jsonObject)
            {
                // Create an object node
                var objectNode = new ObjectNode
                {
                    Name = propertyName,
                    Path = filePath,
                    FullPath = parentPath,
                    Type = NodeType.Object
                };

                // Call OnObject rules
                OnObject(objectNode);

                // Visit each property
                foreach (var property in jsonObject.Properties())
                {
                    var propertyKey = property.Name;
                    var fullPropertyPath = string.IsNullOrEmpty(parentPath) ? propertyKey : $"{parentPath}.{propertyKey}";

                    // Create a property node
                    var propertyNode = new PropertyNode
                    {
                        Name = propertyKey,
                        Path = filePath,
                        FullPath = fullPropertyPath,
                        Type = NodeType.Property,
                        Value = property.Value?.ToString()
                    };

                    // Call OnProperty rules
                    OnProperty(propertyNode);

                    // Recursively process nested objects and arrays
                    VisitJsonObject(property.Value, filePath, fullPropertyPath, propertyKey);

                    // Check for functions in property values if it's a string
                    if (property.Value is JValue jValue && jValue.Type == JTokenType.String)
                    {
                        CheckForFunctions(jValue.Value.ToString(), filePath, fullPropertyPath);
                    }
                }
            }
            else if (jsonToken is JArray jsonArray)
            {
                for (int i = 0; i < jsonArray.Count; i++)
                {
                    var fullArrayItemPath = $"{parentPath}[{i}]";
                    VisitJsonObject(jsonArray[i], filePath, fullArrayItemPath);
                }
            }
        }

        /// <summary>
        /// Checks a text value for function calls using PowerFx parsing with regex fallback.
        /// </summary>
        /// <param name="text">The text to check for functions</param>
        /// <param name="filePath">The path of the file containing the text</param>
        /// <param name="parentPath">The hierarchical parent path</param>
        private void CheckForFunctions(string text, string filePath, string parentPath)
        {
            try
            {
                // Use RecalcEngine to parse the expression and get the AST
                var parseResult = _recalcEngine.Parse(text, new ParserOptions
                {
                    AllowsSideEffects = true,
                    // TODO: Set Culture to ensure consistent parsing
                    Culture = new CultureInfo("en-US")
                });

                if (parseResult.IsSuccess)
                {
                    // Get the ParsedExpression from the result
                    var parsedExpression = parseResult.Root;

                    // Visit the AST to find function calls
                    var functionVisitor = new FunctionCallVisitor();
                    parsedExpression.Accept(functionVisitor);

                    // Process found functions
                    foreach (var function in functionVisitor.FoundFunctions)
                    {
                        var functionNode = new FunctionNode
                        {
                            Name = function,
                            Path = filePath,
                            FullPath = parentPath,
                            Type = NodeType.Function
                        };

                        // Call OnFunction rules
                        OnFunction(functionNode);
                    }
                }
            }
            catch (Exception ex)
            {
                // Fallback to regex-based detection if parsing fails
                try
                {
                    // Pattern to match function calls: word followed by opening parenthesis
                    // This captures function names like "FunctionName(" or "Function.Name("
                    var functionPattern = @"(\w+(?:\.\w+)*)\s*\(";
                    var matches = Regex.Matches(text, functionPattern);

                    foreach (Match match in matches)
                    {
                        if (match.Groups.Count > 1)
                        {
                            var functionName = match.Groups[1].Value;

                            var functionNode = new FunctionNode
                            {
                                Name = functionName,
                                Path = filePath,
                                FullPath = parentPath,
                                Type = NodeType.Function
                            };

                            // Call OnFunction rules
                            OnFunction(functionNode);
                        }
                    }
                }
                catch (Exception regexEx)
                {
                    _logger.LogError($"Error in regex fallback detection: {regexEx.Message}");
                }

                _logger.LogWarning($"Error parsing expression: {ex.Message}. Used regex-based fallback detection.");
            }
        }

        /// <summary>
        /// Processes a directory node using the rules in the scan configuration.
        /// </summary>
        /// <param name="node">The directory node to process</param>
        protected virtual void OnDirectory(DirectoryNode node)
        {
            if (_scanReference.OnDirectory == null)
            {
                return;
            }

            // Set up the node in RecalcEngine using CurrentNodeVariableName
            SetCurrentNodeInRecalcEngine(node);

            foreach (var rule in _scanReference.OnDirectory)
            {
                try
                {
                    // Evaluate the When condition using the CurrentNodeVariableName variable
                    if (EvaluateWhenCondition(rule.When))
                    {
                        // The When condition evaluated to true, execute the Then clause
                        ExecuteThenClause(rule.Then, node);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error evaluating rule for directory {node.Path}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Processes a file node using the rules in the scan configuration.
        /// </summary>
        /// <param name="node">The file node to process</param>
        protected virtual void OnFile(FileNode node)
        {
            if (_scanReference.OnFile == null)
            {
                return;
            }

            // Set up the node in RecalcEngine using CurrentNodeVariableName
            SetCurrentNodeInRecalcEngine(node);

            foreach (var rule in _scanReference.OnFile)
            {
                try
                {
                    // Evaluate the When condition using the CurrentNodeVariableName variable
                    if (EvaluateWhenCondition(rule.When))
                    {
                        _contextMap.TryAdd(node.FullPath, new List<string> { });

                        // The When condition evaluated to true, execute the Then clause
                        ExecuteThenClause(rule.Then, node);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error evaluating rule for file {node.Path}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Processes an object node using the rules in the scan configuration.
        /// </summary>
        /// <param name="node">The object node to process</param>
        protected virtual void OnObject(ObjectNode node)
        {
            if (_scanReference.OnObject == null)
            {
                return;
            }

            // Set up the node in RecalcEngine using CurrentNodeVariableName
            SetCurrentNodeInRecalcEngine(node);

            foreach (var rule in _scanReference.OnObject)
            {
                try
                {
                    // Evaluate the When condition using the CurrentNodeVariableName variable
                    if (EvaluateWhenCondition(rule.When))
                    {
                        // The When condition evaluated to true, execute the Then clause
                        ExecuteThenClause(rule.Then, node);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error evaluating rule for object {node.FullPath}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Processes a property node using the rules in the scan configuration.
        /// </summary>
        /// <param name="node">The property node to process</param>
        protected virtual void OnProperty(PropertyNode node)
        {
            if (_scanReference.OnProperty == null)
            {
                return;
            }

            // Set up the node in RecalcEngine using CurrentNodeVariableName
            SetCurrentNodeInRecalcEngine(node);

            foreach (var rule in _scanReference.OnProperty)
            {
                try
                {
                    // Evaluate the When condition using the CurrentNodeVariableName variable
                    if (EvaluateWhenCondition(rule.When))
                    {
                        // The When condition evaluated to true, execute the Then clause
                        ExecuteThenClause(rule.Then, node);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error evaluating rule for property {node.FullPath}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Processes a function node using the rules in the scan configuration.
        /// </summary>
        /// <param name="node">The function node to process</param>
        protected virtual void OnFunction(FunctionNode node)
        {
            if (_scanReference.OnFunction == null)
            {
                return;
            }

            // Set up the node in RecalcEngine using CurrentNodeVariableName
            SetCurrentNodeInRecalcEngine(node);

            foreach (var rule in _scanReference.OnFunction)
            {
                try
                {
                    // Evaluate the When condition using the CurrentNodeVariableName variable
                    if (EvaluateWhenCondition(rule.When))
                    {
                        // The When condition evaluated to true, execute the Then clause
                        ExecuteThenClause(rule.Then, node);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error evaluating rule for function {node.Name}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Evaluates a PowerFx "When" condition with the current node context.
        /// </summary>
        /// <param name="whenExpression">The PowerFx expression to evaluate</param>
        /// <returns>True if the condition evaluates to true, false otherwise</returns>
        private bool EvaluateWhenCondition(string whenExpression)
        {
            // Evaluate the expression using the CurrentNodeVariableName variable
            var result = _recalcEngine.Eval(whenExpression, new ParserOptions { AllowsSideEffects = true });

            if (result is BooleanValue boolResult)
            {
                return boolResult.Value;
            }

            return false;
        }        /// <summary>
                 /// Executes a PowerFx "Then" clause with the current node context.
                 /// </summary>
                 /// <param name="thenClause">The PowerFx expression to execute</param>
                 /// <param name="node">The node being processed</param>
        private void ExecuteThenClause(string thenClause, Node node)
        {
            // Evaluate the expression using the RecalcEngine
            try
            {
                _recalcEngine.Eval(thenClause, new ParserOptions { AllowsSideEffects = true });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error executing clause '{thenClause}': {ex.Message}");
            }
        }

        /// <summary>
        /// Converts a Node object to a PowerFx RecordValue for use in expressions.
        /// </summary>
        /// <param name="node">The node to convert</param>
        /// <returns>A RecordValue representing the node</returns>
        private RecordValue NodeToRecord(Node node)
        {
            var fields = new List<NamedValue>();

            // Add common properties
            fields.Add(new NamedValue("Name", FormulaValue.New(node.Name ?? string.Empty)));
            fields.Add(new NamedValue("Path", FormulaValue.New(node.Path ?? string.Empty)));
            fields.Add(new NamedValue("FullPath", FormulaValue.New(node.FullPath ?? string.Empty)));
            fields.Add(new NamedValue("Type", FormulaValue.New(node.Type.ToString())));

            // Add specific properties based on node type
            if (node is FileNode fileNode)
            {
                fields.Add(new NamedValue("Extension", FormulaValue.New(fileNode.Extension ?? string.Empty)));
            }
            else if (node is PropertyNode propertyNode)
            {
                fields.Add(new NamedValue("Value", FormulaValue.New(propertyNode.Value ?? string.Empty)));
            }

            return RecordValue.NewRecordFromFields(fields);
        }

        /// <summary>
        /// Gets a path relative to the workspace root.
        /// </summary>
        /// <param name="basePath">The base workspace path</param>
        /// <param name="fullPath">The full path to convert to a relative path</param>
        /// <returns>The path relative to the workspace root</returns>
        private string GetRelativePath(string basePath, string fullPath)
        {
            if (fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
            {
                var relativePath = fullPath.Substring(basePath.Length).TrimStart('\\', '/');
                return relativePath;
            }
            return fullPath;
        }

        /// <summary>
        /// Escapes special characters in a string.
        /// </summary>
        /// <param name="input">The string to escape</param>
        /// <returns>The escaped string</returns>
        private string EscapeString(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            return input.Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
        }

        /// <summary>
        /// Sets up the current node in the RecalcEngine for use in expressions.
        /// </summary>
        /// <param name="node">The node to set as the current node</param>
        private void SetCurrentNodeInRecalcEngine(Node node)
        {
            var nodeRecord = NodeToRecord(node);
            _recalcEngine.UpdateVariable(CurrentNodeVariableName, nodeRecord);
        }
    }
}
