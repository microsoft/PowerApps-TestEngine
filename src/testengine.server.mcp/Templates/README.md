# Test Engine MCP Template System

The Test Engine MCP server includes a template system that allows test generators to access standardized templates for common test scenarios.

## Template Management

Templates are stored as embedded resources in the MCP server assembly. They are defined in the manifest.yaml file, which maps template names to their respective resource files.

### Manifest Structure

```yaml
template:
  TemplateName:
    Resource: FileName.md
    Description: "Template description"
```

## Available MCP Actions

The following MCP actions are available for working with templates:

### GetTemplates

Lists all available templates from the manifest.

Example response:
```json
{
  "template": {
    "AIBuilderPrompt": {
      "Resource": "AIBuilderPrompt.md",
      "Description": "Provides a template for MCP Client to allow generation of Automation test, configuration and documentation for specific AI Builder model"
    },
    "JavaScriptWebResource": {
      "Resource": "JavaScriptWebResource.md",
      "Description": "Provides a template for MCP Client to allow generation of Automation test, configuration and documentation for specific JavaScript WebResource"
    }
  }
}
```

### GetTemplate

Retrieves a specific template by name.

Parameters:
- `templateName`: Name of the template to retrieve (case-sensitive)

Example response for `GetTemplate("JavaScriptWebResource")`:
```json
{
  "name": "JavaScriptWebResource",
  "description": "Provides a template for MCP Client to allow generation of Automation test, configuration and documentation for specific JavaScript WebResource",
  "content": "# Recommendation\n\nUse the source code definition of web resource {{webresources\\filename.js}} and the sample in {{TestYamlSample}} and {{MockJsSample}} to create..."
}
```

### ListEmbeddedResources

Lists all available embedded resources in the assembly (useful for debugging).

Example response:
```json
{
  "resources": [
    "testengine.server.mcp.Templates.manifest.yaml",
    "testengine.server.mcp.Templates.JavaScriptWebResource.md",
    "testengine.server.mcp.Templates.AIBuilderPrompt.md",
    "testengine.server.mcp.Templates.ModelDrivenApplication.md",
    "testengine.server.mcp.Templates.Variables.yaml"
  ]
}
```

## Template Customization

### Variables

Templates can include variables in the format `{{VariableName}}`. These should be replaced with appropriate values by the client application when using the templates.

Variables can be resolved from:
1. A `tests\variables.yaml` file in the workspace
2. Directly from the workspace context
3. By querying the Test Engine MCP Server for the "variables.yaml" template

### Benefits of the Template System

1. **Consistency**: Standardized templates ensure consistent test generation
2. **Maintainability**: Centralized templates make updates easier
3. **Extensibility**: New template types can be added without changing the client code
