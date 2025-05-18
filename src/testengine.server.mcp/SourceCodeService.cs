// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.MCP;
using Microsoft.PowerApps.TestEngine.MCP.PowerFx;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

public class SourceCodeService
{
    private readonly RecalcEngine? _recalcEngine;
    private readonly WorkspaceVisitorFactory? _visitorFactory;
    private readonly Microsoft.PowerApps.TestEngine.Config.TestSettings? _testSettings;

    public Func<IFileSystem> FileSystemFactory { get; set; } = () => new FileSystem();

    private IFileSystem? _fileSystem;
    private Microsoft.Extensions.Logging.ILogger? _logger;
    private string _basePath = String.Empty;

    /// <summary>
    /// Empty constructor for the SourceCodeService class for unit test 
    /// </summary>
    public SourceCodeService()
    {

    }

    public SourceCodeService(RecalcEngine recalcEngine, Microsoft.Extensions.Logging.ILogger logger) : this(recalcEngine, new WorkspaceVisitorFactory(new FileSystem(), logger), logger, null, String.Empty)
    {

    }

    public SourceCodeService(RecalcEngine recalcEngine, WorkspaceVisitorFactory visitorFactory, Microsoft.Extensions.Logging.ILogger? logger)
        : this(recalcEngine, visitorFactory, logger, null, String.Empty)
    {
    }

    public SourceCodeService(RecalcEngine recalcEngine, WorkspaceVisitorFactory visitorFactory, Microsoft.Extensions.Logging.ILogger? logger, Microsoft.PowerApps.TestEngine.Config.TestSettings? testSettings, string basePath)
    {
        _recalcEngine = recalcEngine ?? throw new ArgumentNullException(nameof(recalcEngine));
        _visitorFactory = visitorFactory ?? throw new ArgumentNullException(nameof(visitorFactory));
        _logger = logger;
        _testSettings = testSettings;
        _basePath = basePath;
    }

    /// <summary>
    /// Loads the solution source code from the repository path defined in the environment variable.
    /// </summary>
    /// <param name="workspaceRequest">The request to apply to workspace to get recommendations and facts to assist the Agent</param>
    /// <returns>A dictionary representation of the workspace</returns>
    public virtual object LoadSolutionFromSourceControl(WorkspaceRequest workspaceRequest)
    {
        string workspace = workspaceRequest.Location;
        string powerFx = workspaceRequest.PowerFx;
        string[] scans = workspaceRequest.Scans;

        // Check if the workspace path is valid
        if (string.IsNullOrEmpty(workspace))
        {
            return CreateRecommendation("Open a workspace to load the solution.");
        }

        // Construct the solution path
        if (_fileSystem == null)
        {
            _fileSystem = FileSystemFactory();
        }

        // Check if the solution path exists
        if (!_fileSystem.Exists(workspace))
        {
            return CreateRecommendation($"Solution not found at path {workspace}. Ensure the repository is loaded in your MCP Host");
        }

        // Load the solution source code
        LoadSolutionSourceCode(workspace);        // Process scans if specified
        if (_recalcEngine != null && scans != null && scans.Length > 0)
        {
            // Create factory if not already provided
            var visitorFactory = _visitorFactory ?? new WorkspaceVisitorFactory(_fileSystem, _logger);
            ProcessScans(workspace, scans, visitorFactory);
        }

        if (!string.IsNullOrEmpty(powerFx))
        {
            if (powerFx.StartsWith("="))
            {
                powerFx = powerFx.Substring(1); // Remove the leading '='
            }

            // Load the Power Fx code if provided
            _recalcEngine.Eval(powerFx, options: new ParserOptions { AllowsSideEffects = true });
        }

        // Convert to dictionary and return
        return ToDictionary();
    }

    /// <summary>
    /// Creates a recommendation object.
    /// </summary>
    /// <param name="message">The recommendation message.</param>
    /// <returns>A recommendation object.</returns>
    private object CreateRecommendation(string message)
    {
        return new List<Recommendation>
        {
            new Recommendation
            {
                Id = Guid.NewGuid().ToString(),
                Type = "SourceControl",
                Suggestion = message,
                Priority = "High"
            }
        };
    }

    /// <summary>
    /// Loads the source code of a Power Platform solution from the specified folder into strongly typed collections.
    /// </summary>
    /// <param name="solutionPath">The path to the root folder of the Power Platform solution.</param>
    private void LoadSolutionSourceCode(string solutionPath)
    {
        if (string.IsNullOrWhiteSpace(solutionPath))
        {
            throw new ArgumentException("Solution path cannot be null or empty.", nameof(solutionPath));
        }

        if (!_fileSystem.Exists(solutionPath))
        {
            throw new DirectoryNotFoundException($"The specified solution path does not exist: {solutionPath}");
        }

        // Initialize collections for strongly typed objects
        var files = new List<SourceFile>();
        var canvasApps = new List<CanvasApp>();
        var workflows = new List<Workflow>();
        var entities = new List<DataverseEntity>();
        var facts = new List<Fact>();
        var recommendations = new List<Recommendation>();

        // Walk through the folders and classify files
        foreach (var filePath in _fileSystem.GetFiles(solutionPath))
        {
            var relativePath = GetRelativePath(solutionPath, filePath);
            var fileId = GenerateUniqueId(relativePath);
            var fileContent = _fileSystem.ReadAllText(filePath);
            var fileExtension = Path.GetExtension(filePath).ToLower();
            var name = Path.GetFileNameWithoutExtension(filePath);

            // Add file to the Files collection
            var file = new SourceFile
            {
                Id = fileId,
                Path = relativePath,
                RawContent = fileContent,
                IncludeInModel = false
            };
            files.Add(file);

            // Classify and process files based on their type
            switch (fileExtension)
            {
                case ".json":
                    if (relativePath.Contains("modernflows"))
                    {
                        string description = name.Substring(0, name.IndexOf('-'));
                        workflows.Add(CreateWorkflow(fileContent, fileId, filePath));
                    }
                    break;
                case ".yaml":
                case ".yml":
                    if (name == "canvasapp")
                    {
                        canvasApps.Add(CreateCanvasApp(fileContent, fileId, filePath));
                    }

                    if (name == "entity")
                    {
                        entities.Add(CreateEntity(fileContent, fileId, filePath));
                    }
                    break;
                default:
                    if (_visitorFactory != null)
                    {
                        // If the factory exists, attempt to process all file types
                        // The visitor implementations will handle what they support
                    }
                    else
                    {
                        throw new NotSupportedException($"Unsupported file type: {fileExtension}");
                    }
                    break;
            }
        }

        // Reset states for recommendation functions
        DataverseTestTemplateFunction.Reset();
        CanvasAppTestTemplateFunction.Reset();
        TestPatternAnalyzer.Reset();

        // Register custom PowerFx functions for recommendations
        if (_recalcEngine != null)
        {
            // Template generation functions
            _recalcEngine.Config.AddFunction(new DataverseTestTemplateFunction());
            _recalcEngine.Config.AddFunction(new CanvasAppTestTemplateFunction());

            // Helper functions for adding facts and recommendations
            _recalcEngine.Config.AddFunction(new AddFactFunction(_recalcEngine));            // Canvas App specific analysis functions
            _recalcEngine.Config.AddFunction(new CanvasAppScanFunctions.IdentifyUIPatternFunction());
            _recalcEngine.Config.AddFunction(new CanvasAppScanFunctions.DetectNavigationPatternFunction());
            _recalcEngine.Config.AddFunction(new CanvasAppScanFunctions.AnalyzeDataOperationFunction());

            // State management functions (for handling large apps)
            _recalcEngine.Config.AddFunction(new ScanStateManager.SaveInsightFunction(_fileSystem, _logger, solutionPath));
            _recalcEngine.Config.AddFunction(new ScanStateManager.FlushInsightsFunction(_fileSystem, _logger, solutionPath));
            _recalcEngine.Config.AddFunction(new ScanStateManager.GenerateUIMapFunction(_fileSystem, _logger, solutionPath));

            // Enhanced insight management with the new wrapper
            _recalcEngine.Config.AddFunction(new SaveInsightWrapper(_fileSystem, _logger, solutionPath));

            // Test pattern analyzers
            _recalcEngine.Config.AddFunction(new TestPatternAnalyzer.DetectLoginScreenFunction());
            _recalcEngine.Config.AddFunction(new TestPatternAnalyzer.DetectCrudOperationsFunction());
            _recalcEngine.Config.AddFunction(new TestPatternAnalyzer.DetectFormPatternFunction());
            _recalcEngine.Config.AddFunction(new TestPatternAnalyzer.GenerateTestCaseRecommendationsFunction());
        }

        // Load collections into the RecalcEngine context
        AddVariable("Files", files, () => new SourceFile().ToRecord().Type);
        AddVariable("CanvasApps", canvasApps, () => new CanvasApp().ToRecord().Type);
        AddVariable("Workflows", workflows, () => new Workflow().ToRecord().Type);
        AddVariable("Entities", entities, () => new DataverseEntity().ToRecord().Type);
        AddVariable("Facts", facts, () => new Fact().ToRecord().Type);
        AddVariable("Recommendations", recommendations, () => new Recommendation().ToRecord().Type);
    }

    private void AddVariable(string variable, IEnumerable<RecordObject> collection, Func<RecordType> type)
    {
        _recalcEngine.UpdateVariable(variable, (collection.Count<RecordObject>() == 0) ? TableValue.NewTable(type()) : TableValue.NewTable(type(), collection.Select(f => f.ToRecord())));
    }

    private CanvasApp? CreateCanvasApp(string rawContent, string fileId, string filePath)
    {
        // Parse the rawContent YAML into a .NET object
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var yamlObject = deserializer.Deserialize<Dictionary<string, object>>(rawContent);

        Dictionary<object, object> canvasAppDict = new Dictionary<object, object>();

        if (yamlObject != null)
        {
            // Extract the CanvasApp section
            if (!yamlObject.TryGetValue("CanvasApp", out var canvasAppData) || canvasAppData is not Dictionary<object, object>)
            {
                throw new InvalidDataException("Invalid CanvasApp YAML structure.");
            }
            canvasAppDict = (Dictionary<object, object>)canvasAppData;
        }

        // Create a new CanvasApp object
        var newApp = new CanvasApp
        {
            Id = Guid.NewGuid().ToString(),
            FileId = fileId,
            Name = canvasAppDict.TryGetValue("Name", out var name) ? name.ToString() : string.Empty,
            Type = "CanvasApp",
            RawContext = rawContent,
            ModelContext = ExtractModelContext(rawContent),
            Connections = ExtractConnections(canvasAppDict)
        };

        // Populate additional properties
        newApp.Facts = new List<Fact>
        {
            new Fact { Key = "AppVersion", Value = canvasAppDict.TryGetValue("AppVersion", out var appVersion) ? appVersion.ToString() : string.Empty },
            new Fact { Key = "Status", Value = canvasAppDict.TryGetValue("Status", out var status) ? status.ToString() : string.Empty },
            new Fact { Key = "DisplayName", Value = canvasAppDict.TryGetValue("DisplayName", out var displayName) ? displayName.ToString() : string.Empty },
            new Fact { Key = "BackgroundColor", Value = canvasAppDict.TryGetValue("BackgroundColor", out var backgroundColor) ? backgroundColor.ToString() : string.Empty },
            new Fact { Key = "IsCustomizable", Value = canvasAppDict.TryGetValue("IsCustomizable", out var isCustomizable) ? isCustomizable.ToString() : string.Empty },
            new Fact { Key = "IntroducedVersion", Value = canvasAppDict.TryGetValue("IntroducedVersion", out var introducedVersion) ? introducedVersion.ToString() : string.Empty }
        };

        // Add facts for database references if available
        if (canvasAppDict.TryGetValue("DatabaseReferences", out var databaseReferences) && databaseReferences is string databaseReferencesJson)
        {
            var databaseReferencesDict = DeserializeJson<Dictionary<string, object>>(databaseReferencesJson);
            foreach (var reference in databaseReferencesDict)
            {
                newApp.Facts.Add(new Fact
                {
                    Id = Guid.NewGuid().ToString(),
                    Key = reference.Key,
                    Value = reference.Value.ToString()
                });
            }
        }

        return newApp;
    }

    private List<string> ExtractConnections(Dictionary<object, object> canvasAppDict)
    {
        if (canvasAppDict.TryGetValue("ConnectionReferences", out var connectionReferences) && connectionReferences is string connectionReferencesJson)
        {
            var connectionReferencesDict = DeserializeJson<Dictionary<string, object>>(connectionReferencesJson);
            return connectionReferencesDict.Keys.ToList();
        }

        return new List<string>();
    }

    private T? DeserializeJson<T>(string json)
    {
        return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
    }

    private Workflow CreateWorkflow(string rawContent, string fileId, string filePath)
    {
        return new Workflow
        {
            Id = Guid.NewGuid().ToString(),
            FileId = fileId,
            Name = ExtractName(rawContent),
            Type = "Workflow",
            RawContext = rawContent,
            ModelContext = ExtractModelContext(rawContent)
        };
    }

    private DataverseEntity CreateEntity(string rawContent, string fileId, string filePath)
    {
        return new DataverseEntity
        {
            Id = Guid.NewGuid().ToString(),
            FileId = fileId,
            Name = ExtractName(rawContent),
            Type = "Entity",
            RawContext = rawContent,
            ModelContext = ExtractModelContext(rawContent)
        };
    }

    private string ExtractName(string rawContent)
    {
        // Placeholder for extracting the name from the raw content
        return "ExtractedName"; // Simplified for now
    }

    private string ExtractModelContext(string rawContent)
    {
        // Placeholder for extracting model-specific context
        return string.Empty; // Simplified for now
    }

    private string GetRelativePath(string basePath, string fullPath)
    {
        return fullPath.Substring(basePath.Length).TrimStart('\\');
    }

    private string GenerateUniqueId(string input)
    {
        using (var sha256 = SHA256.Create())
        {
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }

    /// <summary>
    /// Processes the requested scans using the WorkspaceVisitorFactory
    /// </summary>
    /// <param name="workspacePath">The workspace path to scan</param>
    /// <param name="scans">The scan configurations to apply</param>
    /// <param name="visitorFactory">The WorkspaceVisitorFactory to use</param>
    private void ProcessScans(string workspacePath, string[] scans, WorkspaceVisitorFactory visitorFactory)
    {
        if (_recalcEngine == null)
        {
            return;
        }

        // Reset states for recommendation functions
        DataverseTestTemplateFunction.Reset();


        // Register custom PowerFx functions for recommendations
        _recalcEngine.Config.AddFunction(new DataverseTestTemplateFunction());
        _recalcEngine.Config.AddFunction(new AddFactFunction(_recalcEngine));

        // Build a list of recommendations if needed
        List<string> recommendations = new List<string>();

        foreach (var scanConfigName in scans)
        {
            try
            {
                // Create a scan reference from the scan configuration
                var scanReference = new Microsoft.PowerApps.TestEngine.Config.ScanReference();
                bool scanFound = false;

                // If we have test settings, try to find the requested scan by name
                if (_testSettings != null && _testSettings.Scans.Any())
                {
                    // Special case: when "all" is specified, process all available scans
                    if (string.Equals(scanConfigName, "all", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger?.LogInformation("Processing all available scans");
                        foreach (var scan in _testSettings.Scans)
                        {
                            ProcessSingleScan(workspacePath, scan, visitorFactory);
                        }
                        return; // Done processing all scans
                    }

                    // Look for a scan with a matching name
                    var matchedScan = _testSettings.Scans.FirstOrDefault(s =>
                        string.Equals(s.Name, scanConfigName, StringComparison.OrdinalIgnoreCase));

                    if (matchedScan != null)
                    {
                        _logger?.LogInformation($"Found matching scan: {matchedScan.Name}");
                        scanReference = matchedScan;
                        scanFound = true;
                    }
                }

                if (!scanFound)
                {
                    // No matching scan found, add a recommendation
                    string recommendation = $"No scan configuration found for '{scanConfigName}'. ";

                    if (_testSettings != null && _testSettings.Scans.Any())
                    {
                        recommendation += "Available scans: " + string.Join(", ", _testSettings.Scans.Select(s => s.Name));
                    }
                    else
                    {
                        recommendation += "No scans are defined in TestSettings.";
                    }

                    recommendations.Add(recommendation);
                    _logger?.LogWarning(recommendation);
                    continue;
                }

                ProcessSingleScan(workspacePath, scanReference, visitorFactory);
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error processing scan '{scanConfigName}': {ex.Message}";
                recommendations.Add(errorMessage);
                _logger?.LogError(ex, errorMessage);
            }
        }

        // If we have recommendations, add them to the recalc engine
        if (recommendations.Count > 0)
        {
            foreach (string recommendation in recommendations)
            {
                _recalcEngine.Eval($"AddRecommendation(\"{recommendation.Replace("\"", "\\\"")}\")");
            }
        }
    }

    private void ProcessSingleScan(string workspacePath, Microsoft.PowerApps.TestEngine.Config.ScanReference scanReference, WorkspaceVisitorFactory visitorFactory)
    {
        if (_recalcEngine == null)
        {
            return;
        }
        _logger?.LogInformation($"Processing scan: {scanReference.Name} from location: {scanReference.Location}");

        // Load scan configuration from file if a location is specified
        Microsoft.PowerApps.TestEngine.MCP.Visitor.ScanReference visitorScanRef;        // Load scan configuration from the location if specified
        if (!string.IsNullOrEmpty(scanReference.Location))
        {
            // Ensure file system is initialized
            if (_fileSystem == null)
            {
                _fileSystem = FileSystemFactory();
            }
            // Attempt to load the scan configuration from the location
            string configFilePath = scanReference.Location;
            if (!Path.IsPathRooted(configFilePath))
            {
                // If the path is not rooted, determine the base path using TestSettings file location
                configFilePath = Path.Combine(_basePath, configFilePath);
                _logger?.LogInformation($"Non-rooted path detected. Using combined path: {configFilePath} with base path: {_basePath}");
            }

            // Store the original location for reference
            scanReference.Location = configFilePath;

            if (_fileSystem.FileExists(configFilePath))
            {
                try
                {
                    string configContent = _fileSystem.ReadAllText(configFilePath);
                    // Parse the configuration content and create a visitor scan reference
                    var deserializer = new DeserializerBuilder()
                        .WithNamingConvention(CamelCaseNamingConvention.Instance)
                        .Build();

                    visitorScanRef = deserializer.Deserialize<Microsoft.PowerApps.TestEngine.MCP.Visitor.ScanReference>(configContent);

                    // Keep the original name if it wasn't specified in the config file
                    if (string.IsNullOrEmpty(visitorScanRef.Name))
                    {
                        visitorScanRef.Name = scanReference.Name;
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"Failed to load scan configuration from {configFilePath}: {ex.Message}");
                    // Create default scan reference with just the name
                    visitorScanRef = new Microsoft.PowerApps.TestEngine.MCP.Visitor.ScanReference
                    {
                        Name = scanReference.Name
                    };
                }
            }
            else
            {
                _logger?.LogWarning($"Scan configuration file not found: {configFilePath}");
                // Create default scan reference with just the name
                visitorScanRef = new Microsoft.PowerApps.TestEngine.MCP.Visitor.ScanReference
                {
                    Name = scanReference.Name
                };
            }
        }
        else
        {
            // Create a new visitor scan reference with just the name
            visitorScanRef = new Microsoft.PowerApps.TestEngine.MCP.Visitor.ScanReference
            {
                Name = scanReference.Name
            };
        }

        // Create a visitor configuration based on the scan reference
        var visitor = visitorFactory.Create(workspacePath, visitorScanRef, _recalcEngine);

        // Execute the visitor to process the scan
        visitor.Visit();
    }

    /// <summary>
    /// Converts the RecalcEngine variables into YAML documents.
    /// </summary>
    /// <returns>A dictionary where the key is the section name and the value is the YAML representation.</returns>
    public Dictionary<string, string> ConvertToYaml()
    {
        var yamlDocuments = new Dictionary<string, string>();

        // Serialize each section into YAML
        yamlDocuments["Files"] = ConvertSectionToYaml("Files");
        yamlDocuments["CanvasApps"] = ConvertSectionToYaml("CanvasApps");
        yamlDocuments["Workflows"] = ConvertSectionToYaml("Workflows");
        yamlDocuments["Entities"] = ConvertSectionToYaml("Entities");
        yamlDocuments["Facts"] = ConvertSectionToYaml("Facts");
        yamlDocuments["Recommendations"] = ConvertSectionToYaml("Recommendations");

        return yamlDocuments;
    }

    private string ConvertSectionToYaml(string variableName)
    {
        // Retrieve the table from RecalcEngine
        var table = _recalcEngine.GetValue(variableName) as TableValue;
        if (table == null)
        {
            return string.Empty;
        }

        // Filter rows where IncludeInModel is true and exclude the IncludeInModel field
        var filteredRows = table.Rows
            .Where(row => row.Value is RecordValue record && record.GetField("IncludeInModel") is BooleanValue include && include.Value)
            .Select(row => ConvertRecordToDictionary(row.Value as RecordValue, "IncludeInModel", "RawContext"));

        // Serialize to YAML
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        return serializer.Serialize(filteredRows);
    }

    private Dictionary<string, object> ConvertRecordToDictionary(RecordValue record, params string[] exclude)
    {
        var dictionary = new Dictionary<string, object>();

        foreach (var field in record.Fields)
        {
            if (!exclude.Any(item => item.ToLower() == field.Name.ToLower()))
            {
                if (field.Value is TableValue tableValue)
                {
                    var rows = new List<Dictionary<string, object>>();
                    foreach (var row in tableValue.Rows.ToArray())
                    {
                        rows.Add(ConvertRecordToDictionary(row.Value, exclude));
                    }
                    dictionary[field.Name] = rows;
                }
                else
                {
                    dictionary[field.Name] = field.Value.ToObject(); ;
                }
            }
        }

        return dictionary;
    }

    /// <summary>
    /// Converts the RecalcEngine variables into a dictionary representation.
    /// </summary>
    /// <returns>A dictionary where the key is the section name and the value is the dictionary representation of the section.</returns>
    public Dictionary<string, List<Dictionary<string, object>>> ToDictionary()
    {
        var sectionDictionaries = new Dictionary<string, List<Dictionary<string, object>>>();

        // Convert each section into a dictionary and add it to the outer dictionary
        sectionDictionaries["Files"] = ConvertSectionToDictionary("Files");
        sectionDictionaries["CanvasApps"] = ConvertSectionToDictionary("CanvasApps");
        sectionDictionaries["Workflows"] = ConvertSectionToDictionary("Workflows");
        sectionDictionaries["Entities"] = ConvertSectionToDictionary("Entities");
        sectionDictionaries["Facts"] = ConvertSectionToDictionary("Facts");
        sectionDictionaries["Recommendations"] = ConvertSectionToDictionary("Recommendations");

        return sectionDictionaries;
    }

    private List<Dictionary<string, object>> ConvertSectionToDictionary(string variableName)
    {
        // Retrieve the table from RecalcEngine
        var table = _recalcEngine.GetValue(variableName) as TableValue;
        if (table == null)
        {
            return new List<Dictionary<string, object>>();
        }

        // Filter rows where IncludeInModel is true
        return table.Rows
            .Where(row => row.Value is RecordValue record && record.GetField("IncludeInModel") is BooleanValue include && include.Value)
            .Select(row => ConvertRecordToDictionary(row.Value as RecordValue, "Id", "IncludeInModel", "RawContext"))
            .ToList();
    }
}

public class SourceFile : RecordObject
{
    public string? Id { get; set; } = String.Empty;
    public string? Path { get; set; } = String.Empty;
    public string? RawContent { get; set; } = string.Empty;

    override public RecordValue ToRecord()
    {
        return RecordValue.NewRecordFromFields(new List<NamedValue>
        {
            new NamedValue("Id", FormulaValue.New(Id)),
            new NamedValue("Path", FormulaValue.New(Path)),
            new NamedValue("RawContent", FormulaValue.New(RawContent ?? String.Empty)),
            new NamedValue("IncludeInModel", FormulaValue.New(IncludeInModel)),
            new NamedValue("Facts", ConvertFactsToTable()),
            new NamedValue("Recommendations", ConvertRecommendationsToTable()),
        });
    }


}

public class CanvasApp : ContextObject
{
    public string? FileId { get; set; } = String.Empty;
    public List<string> Connections { get; set; } = new List<string>();
}

public class Workflow : ContextObject
{
    public string? FileId { get; set; }
}

public class DataverseEntity : ContextObject
{
    public string? FileId { get; set; }
}

public abstract class RecordObject
{
    public bool IncludeInModel { get; set; } = false;

    public List<Fact> Facts { get; set; } = new List<Fact>();

    public List<Recommendation> Recommendations { get; set; } = new List<Recommendation>();

    public TableValue ConvertFactsToTable()
    {
        // Convert the list of Facts into a TableValue
        return TableValue.NewTable(
            new Fact().ToRecord().Type, // Define the type of the table
            Facts.Select(fact => fact.ToRecord()) // Convert each Fact to a RecordValue
        );
    }

    public TableValue ConvertRecommendationsToTable()
    {
        // Convert the list of Recommendations into a TableValue
        return TableValue.NewTable(
            new Recommendation().ToRecord().Type, // Define the type of the table
            Recommendations.Select(recommendation => recommendation.ToRecord()) // Convert each Recommendation to a RecordValue
        );
    }

    public abstract RecordValue ToRecord();
}

public abstract class ContextObject : RecordObject
{
    public string? Id { get; set; } = String.Empty;
    public string? Name { get; set; } = String.Empty;
    public string? Type { get; set; } = String.Empty;
    public string? RawContext { get; set; } = String.Empty;
    public string? ModelContext { get; set; } = String.Empty;

    public override RecordValue ToRecord()
    {
        return RecordValue.NewRecordFromFields(new List<NamedValue>
        {
            new NamedValue("Id", FormulaValue.New(Id)),
            new NamedValue("Name", FormulaValue.New(Name)),
            new NamedValue("Type", FormulaValue.New(Type)),
            new NamedValue("RawContext", FormulaValue.New(RawContext)),
            new NamedValue("ModelContext", FormulaValue.New(ModelContext)),
            new NamedValue("IncludeInModel", FormulaValue.New(IncludeInModel)),
            new NamedValue("Facts", ConvertFactsToTable()),
            new NamedValue("Recommendations", ConvertRecommendationsToTable()),
            new NamedValue("IncludeInModel", FormulaValue.New(IncludeInModel))
        });
    }
}

public class Fact : RecordObject
{
    public string? Id { get; set; } = String.Empty;
    public string? Key { get; set; } = String.Empty;
    public string? Value { get; set; } = String.Empty;

    override public RecordValue ToRecord()
    {
        return RecordValue.NewRecordFromFields(new List<NamedValue>
        {
            new NamedValue("Id", FormulaValue.New(Id)),
            new NamedValue("Key", FormulaValue.New(Key)),
            new NamedValue("Value", FormulaValue.New(Value)),
            new NamedValue("IncludeInModel", FormulaValue.New(IncludeInModel))
        });
    }
}

public class Recommendation : RecordObject
{
    public string? Id { get; set; } = String.Empty;
    public string? Type { get; set; } = String.Empty;
    public string? Suggestion { get; set; } = String.Empty;
    public string? Priority { get; set; } = String.Empty;

    public override RecordValue ToRecord()
    {
        return RecordValue.NewRecordFromFields(new List<NamedValue>
        {
            new NamedValue("Id", FormulaValue.New(Id)),
            new NamedValue("Type", FormulaValue.New(Type)),
            new NamedValue("Suggestion", FormulaValue.New(Suggestion)),
            new NamedValue("Priority", FormulaValue.New(Priority)),
            new NamedValue("IncludeInModel", FormulaValue.New(IncludeInModel))
        });
    }
}
