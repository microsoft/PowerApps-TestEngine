# Test Engine MCP Server

The Test Engine Model Context Protocol (MCP) Server is a .NET tool designed to provide a server implementation for the Model Context Protocol (MCP). This tool is currently in preview, and its features and APIs are subject to change.

## Features

- Validate Power Fx expressions.
- Retrieve a list of Plan Designer plans.
- Fetch details for specific plans.

## Installation

You can install the tool globally using the following command:

```PowerShell
dotnet tool install -g testengine.server.mcp --add-source <path-to-nupkgs> --version 0.1.9-preview
```

NOTE: You wil need to replace <path-to-nupkgs> wth the path on your system where the nuget package

## Usage

Once installed, you can run the server from a MCP Host like Visual Studio Code and a MCP Client like GitHub Copilot. For example using Visual Studio user settings.json file

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

- **Validate Power Fx Expression**: Validate a Power Fx expression for use in a test file using the ValidatePowerFx tool.
- **Get Plan List**: Retrieve a list of available Power Platform [plan designer](https://learn.microsoft.com/en-us/power-apps/maker/plan-designer/plan-designer) stored in using the GetPlanList tool.
- **Get Plan Details**: Fetch details for a specific plan using the GetPlanDetails tool.

## Development

To build and test the project locally:

1. Clone the repository.
2. Navigate to the project directory.
3. Build the project for your platform.

```PowerShell
dotnet build -c Debug 
```

4. Package the solution

```
dotnet pack -c Debug --output ./nupkgs 
```

4. Globally install you package

```PowerShell
dotnet tool install testengine.server.mcp -g --add-source ./nupkgs --version 0.1.9-preview
```

## Uninstall

Before you upgrade a version of the MCP Server ensure you stop any running Service. Once the service stopped uninstall the existing version

```
dotnet tool uninstall testengine.server.mcp -g
```

## License

This project is licensed under the [MIT License](.\LICENSE).
