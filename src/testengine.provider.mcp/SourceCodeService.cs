// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Security.Cryptography;
using System.Text;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;
using Newtonsoft.Json;
using YamlDotNet.Core.Tokens;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Microsoft.PowerApps.TestEngine.Providers
{
    public class SourceCodeService
    {
        public const string ENVIRONMENT_SOLUTION_PATH = "TEST_ENGINE_SOLUTION_PATH";
        private readonly RecalcEngine? _recalcEngine;

        public Func<IFileSystem> FileSystemFactory { get; set; } = () => new FileSystem();

        public Func<IEnvironmentVariable> EnvironmentVariableFactory { get; set; } = () => new EnvironmentVariable();

        private IFileSystem? _fileSystem;
        private IEnvironmentVariable? _environmentVariable;

        public SourceCodeService()
        {

        }

        public SourceCodeService(RecalcEngine recalcEngine)
        {
            _recalcEngine = recalcEngine ?? throw new ArgumentNullException(nameof(recalcEngine));
        }

        /// <summary>
        /// Loads the solution source code from the repository path defined in the environment variable.
        /// </summary>
        /// <param name="solutionId">The ID of the solution to load.</param>
        /// <returns>A dictionary representation of the solution or a recommendation if source control integration is not enabled.</returns>
        public virtual object LoadSolutionFromSourceControl(string solutionId, string powerFx)
        {
            if (_environmentVariable == null)
            {
                _environmentVariable = EnvironmentVariableFactory();
            }

            var repoPath = _environmentVariable.GetVariable(ENVIRONMENT_SOLUTION_PATH);
            if (string.IsNullOrWhiteSpace(repoPath))
            {
                return CreateRecommendation("Set the environment variable 'TEST_ENGINE_SOLUTION_PATH' to the repository path.");
            }

            // Construct the solution path
            
            if (_fileSystem == null)
            {
                _fileSystem = FileSystemFactory();
            }

            // Check if the solution path exists
            if (!_fileSystem.Exists(repoPath))
            {
                return CreateRecommendation($"Solution not found at path {repoPath}. Ensure the repository is correctly configured.");
            }

            // Load the solution source code
            LoadSolutionSourceCode(repoPath);

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
                        throw new NotSupportedException($"Unsupported file type: {fileExtension}");
                }
            }

            // Initial starter recommendation for demonstration purposes only
            // This will be refined this based on solution data. Add Power Fx function examples that will dynamically add recommendations
            recommendations.Add(new Recommendation
            {
                Id = Guid.NewGuid().ToString(),
                IncludeInModel = true,
                Type = "Yaml Test Template",
                Suggestion = @"Use the following yaml test template to generate Dataverse Tests
-----------------------
file: entity.te.yaml
-----------------------

# yaml-embedded-languages: powerfx
testSuite:
  testSuiteName: Dataverse tests
  testSuiteDescription: Validate Power Fx can be used to run Dataverse integration tests
  persona: User1
  appLogicalName: N/A
  onTestCaseStart: |
    = ForAll(Accounts, Remove(Accounts, ThisRecord))

  testCases:
    - testCaseName: No Accounts
      testCaseDescription: Should have no accounts as onTestCaseStart removes all accounts
      testSteps: |
        = Assert(CountRows(Accounts)=0)
    - testCaseName: Insert Account
      testCaseDescription: Insert a new record into account table
      testSteps: |
        = Collect(
            Accounts,
            {
                name: ""New Account""
            }
          );
          Assert(CountRows(Accounts)=1)
    - testCaseName: Insert and Remove Account
      testCaseDescription: Insert a new record into account table and then remove
      testSteps: |
        = Collect(
            Accounts,
            {
                name: ""New Account""
            }
          );
          Assert(CountRows(Accounts)=1);
          Remove(Accounts, First(Accounts));
          Assert(CountRows(Accounts)=0)
    - testCaseName: Update Account
      testCaseDescription: Update created record
      testSteps: |
        =  Collect(
            Accounts,
            {
                name: ""New Account""
            }
          );
          Patch(
            Accounts,
            First(Accounts),
            {
                name: ""Updated Account""
            }
          );
          Assert(First(Accounts).name = ""Updated Account"");
    
testSettings:
  headless: false
  locale: ""en-US""
  recordVideo: true
  extensionModules:
    enable: true
    parameters:
      enableDataverseFunctions: true
      enableAIFunctions: true
  browserConfigurations:
    - browser: Chromium

environmentVariables:
  users:
    - personaName: User1
      emailKey: user1Email
      passwordKey: NotNeeded
",
                Priority = "High"
            });

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
}
