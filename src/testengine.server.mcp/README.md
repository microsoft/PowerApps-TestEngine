# Test Engine MCP Server

> **PREVIEW NOTICE**: This feature is in preview. Preview features aren't meant for production use and may have restricted functionality. These features are available before an official release so that customers can get early access and provide feedback.

The Test Engine Model Context Protocol (MCP) Server is a .NET command line tool designed to provide a server implementation for the Model Context Protocol (MCP). This tool enables AI-assisted app testing through workspace scanning, PowerFx validation, and automated test recommendations.

## Features

- **Workspace Scanning**: Scan directories and files using an extensible visitor pattern
- **Power Fx Validation**: Validate Power Fx expressions for test files
- **App Fact Collection**: Collect app facts using the ScanStateManager pattern
- **Plan Integration**: Retrieve and get details for specific Power Platform Plan Designer plans
- **Test Recommendations**: Generate actionable test recommendations based on app structure
- **Guided Testing**: Use fact-based analysis to provide context-aware testing guidance

## Installation

You can install the tool globally using the following command:

```PowerShell
dotnet tool install -g testengine.server.mcp --add-source <path-to-nupkgs> --version 0.1.9-preview
```

NOTE: You will need to replace `<path-to-nupkgs>` with the path on your system where the nuget package is located.

## Usage

Once installed, you can run the server from an MCP Host like Visual Studio Code and an MCP Client like GitHub Copilot. For example, using Visual Studio user settings.json file:

```json
{
    "mcp": {
        "servers": {
            "TestEngine": {
                "command": "testengine.server.mcp",
                "args": [
                    "testsettings.te.yaml",
                    "https://contoso.crm.dynamics.com/"
                ]
            }
        }
    }
}
```

## Commands

### Validate Power Fx Expression
Validates a Power Fx expression for use in a test file.
```
ValidatePowerFx(powerFx: string)
```

### Get Plan List
Retrieves a list of available Power Platform [plan designer](https://learn.microsoft.com/en-us/power-apps/maker/plan-designer/plan-designer) plans.
```
GetPlanList()
```

### Get Plan Details
Fetches details for a specific plan and scans the current workspace to provide facts and recommendations.
```
GetPlanDetails(planId: string, workspacePath: string)
```

### Get Scan Types
Retrieves details for available scan types.
```
GetScanTypes()
```

### Scan Workspace
Scans a workspace with optional scan types and post-processing Power Fx steps.
```
Scan(workspacePath: string, scans: string[], powerFx: string)
```

## Scan and Recommendation Process

The MCP server combines scanning and recommendation capabilities to provide an end-to-end test generation solution:

1. **Workspace Analysis**: Scans the workspace to identify app components, structures, and patterns
2. **Fact Collection**: Gathers details about entities, relationships, business logic, and UI components
3. **Recommendation Rules**: Applies testing best practices based on collected facts
4. **Test Generation Guidance**: Provides structured guidance to the MCP Client for generating tests

### Available Scan Types

You can retrieve available scan types using the `GetScanTypes()` command. Common scan types include:

- **Code**: Analyzes source code files for patterns and structures
- **Structure**: Evaluates project organization and dependencies
- **Config**: Examines configuration files for test-relevant settings
- **Dataverse**: Identifies Dataverse entities and relationships
- **Custom**: User-defined scan types through extension points

## Workspace Visitor Pattern

The server uses a visitor pattern to scan workspaces, represented by the `WorkspaceVisitor` class. This pattern:

1. Recursively traverses directories and files
2. Processes files based on type (JSON, YAML, code files)
3. Applies scan rules at various stages (OnStart, OnDirectory, OnFile, OnObject, OnProperty, OnFunction, OnEnd)
4. Collects facts and insights using the ScanStateManager pattern

## Fact Management

The server includes a `ScanStateManager` with two key components:

1. **SaveFactFunction**: Records individual facts about app components
2. **ExportFactsFunction**: Exports collected facts to a consolidated JSON file with metrics and recommendations

## Recommendation Generation

> **PREVIEW NOTICE**: This feature is in preview. Preview features aren't meant for production use and may have restricted functionality. These features are available before an official release so that customers can get early access and provide feedback.

The MCP server aims to use collected facts to generate intelligent test recommendations in the form of:

1. **Guided Prompts**: Context-aware suggestions based on your app structure
2. **Sample References**: Links to relevant code samples that align with your testing needs
3. **Best Practices**: Tailored testing strategies based on your application components

### How Recommendations Work

The recommendation system follows this process:

1. **Fact Collection**: The server scans your workspace and collects facts about your app
2. **Analysis**: Facts are processed through recommendation rules
3. **Generation**: The system creates targeted recommendations that guide the MCP Client (like GitHub Copilot) to generate appropriate test content
4. **Delivery**: Recommendations are presented as actionable suggestions with references to sample code

### Example Recommendation Flow

```
Scan → Detect Dataverse Entity → Recommend Test Pattern → Reference Sample → Generate Test
```

### Sample Recommendation Outputs

The recommendation system generates structured guidance like:

```yaml
recommendationType: dataverse-entity-test
context:
  entityName: Account
  attributes:
    - name
    - accountnumber
    - telephone1
  relationships:
    - contacts
    - opportunities
recommendation: Generate tests that validate CRUD operations for the Account entity
sampleReference: ../samples/dataverse/entity-testing.yaml
prompt: Create a test that creates an Account record, updates its telephone number, and verifies the update was successful
```

Such recommendations help the MCP Client (like GitHub Copilot) to generate targeted, context-aware test code that follows best practices.

### Power Fx Integration for Recommendations

New Power Fx functions could be added to allow direct interaction with the recommendation system from within scanner PowerFx expressions:

```
AddRecommendation(
  { RecommendationType: Text,
  Context: Record,
  Recommendation: Text,
  SampleReference: Text,
  Prompt: Text }
): Boolean
```

This function could allow you to programmatically add recommendations during scanning:

```
// Example usage in scanner PowerFx
If(
  Contains(Facts.EntityNames, "Account"),
  AddRecommendation(
    {
        RecommandationType: "dataverse-entity-test",
        Context: {
            entityName: "Account",
            attributes: ["name", "accountnumber", "telephone1"]
        },
        Recommendation: "Generate tests that validate Account entity operations",
        SampleReference": ../samples/dataverse/entity-testing.yaml",
        Prompt: "Create a test that validates the Account entity"
  ),
  false
)
```

Additional helper functions for recommendation management:

```
GetRecommendations(): Table  // Returns all current recommendations
ClearRecommendations(): Boolean  // Clears all recommendations
FilterRecommendations(filterExpression: Text): Table  // Filters recommendations by criteria
```

## Roadmap

Here are the list of possible enhancements that could be considered based on customer feedback:

1. **Enhanced Scan Types**
   - Support for more file formats and app structures
   - Advanced code analysis for deeper insights
   - Custom scan rule definitions

2. **Improved Recommendation Engine**
   - ML-based suggestion ranking and filtering
   - Domain-specific testing pattern recommendations
   - Automated test coverage analysis

3. **Integration Enhancements**
   - Tighter integration with Power Platform development tools
   - CI/CD pipeline integration capabilities
   - Test result analytics and reporting

4. **User Experience**
   - Simplified configuration options
   - Interactive recommendation refinement
   - Visual test coverage maps

5. **Extensibility**
   - Custom recommendation rule definitions
   - Pluggable fact collectors for specialized app components
   - Extension points for third-party testing frameworks

### Extending Recommendations

The MCP server could be extended to allow for custom recommendation rules. These could be added by:

1. Creating a new class that implements the `IRecommendationRule` interface
2. Registering the rule with the recommendation engine
3. Providing sample references and prompt templates for your custom rule

Example custom rule structure:

```csharp
public class CustomEntityRule : IRecommendationRule
{
    public string RuleId => "custom-entity-validation";
    
    public bool CanApply(ScanFacts facts) 
    {
        // Logic to determine if this rule applies to the scanned facts
    }
    
    public Recommendation Generate(ScanFacts facts)
    {
        // Generate a recommendation based on the facts
        return new Recommendation
        {
            Type = "entity-validation",
            Context = /* Context from facts */,
            SampleReference = "../samples/custom/validation.yaml",
            Prompt = "Generate a test that validates..."
        };
    }
}
```

### Creating Custom Power Fx Recommendation Functions

We could consider extending the Power Fx functions available in the scanner by implementing custom functions:

```csharp
public class CustomRecommendationFunctions : IPowerFxFunctionLibrary
{
    public void RegisterFunctions(PowerFxConfig config)
    {
        // Register your custom AddRecommendation function
        config.AddFunction(new AddRecommendationFunction());
        config.AddFunction(new GetRecommendationsFunction());
        // Add more custom functions...
    }
}

// Example AddRecommendation function implementation
public class AddRecommendationFunction : ReflectionFunction
{
    public AddRecommendationFunction()
        : base("AddRecommendation", FormulaType.Boolean, 
               new[] {
                   FormulaType.String, // recommendationType
                   RecordType.Empty(),  // context
                   FormulaType.String,  // recommendation
                   FormulaType.String,  // sampleReference
                   FormulaType.String   // prompt
               })
    {
    }

    public static FormulaValue Execute(StringValue recType, RecordValue context, 
                                      StringValue recommendation, StringValue sample, 
                                      StringValue prompt, IServiceProvider services)
    {
        // Implementation to add a recommendation to the system
        // ...
        return FormulaValue.New(true);
    }
}
```

## Development

To build and test the project locally:

1. Clone the repository.
2. Navigate to the project directory.
3. Build the project for your platform:

```PowerShell
dotnet build -c Debug 
```

4. Package the solution:

```PowerShell
dotnet pack -c Debug --output ./nupkgs 
```

5. Globally install your package:

```PowerShell
dotnet tool install testengine.server.mcp -g --add-source ./nupkgs --version 0.1.9-preview
```

## Uninstall

Before you upgrade a version of the MCP Server, ensure you stop any running service. Once the service is stopped, uninstall the existing version:

```PowerShell
dotnet tool uninstall testengine.server.mcp -g
```

## License

This project is licensed under the [MIT License](.\LICENSE).
