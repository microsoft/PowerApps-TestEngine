# Test Engine MCP Server Sample

> **PREVIEW NOTICE**: This feature is in preview. Preview features aren't meant for production use and may have restricted functionality. These features are available before an official release so that customers can get early access and provide feedback.

This sample explains how to set up and configure Visual Studio Code to integrate with the Test Engine Model Context Protocol (MCP) provider using stdio interface using a NodeJS proxy.

## What You Need

Before you start, you'll need a few tools and permissions:
- **Power Platform Command Line Interface (CLI)**: This is a tool that lets you interact with Power Platform from your command line.
- **PowerShell**: A task automation tool from Microsoft.
- **.Net 8.0 SDK**: A software development kit needed to build and run the tests.
- **Power Platform Environment**: A space where your Plan Designer interactions and solutions exist.
- **GitHub Copilot**: Access to [GitHub Copilot](https://github.com/features/copilot)
- **Visual Studio Code**: An install of [Visual Studio Code](https://code.visualstudio.com/) to host the GitHub Copilot and edit generated test files.

## Available MCP Server Features

The Test Engine MCP Server provides the following capabilities through GitHub Copilot in Visual Studio Code:

- **Workspace Scanning**: Scan directories and files to analyze your project structure
- **Power Fx Validation**: Validate Power Fx expressions for test files
- **App Fact Collection**: Collect app facts using the ScanStateManager pattern
- **Plan Integration**: Retrieve and get details for specific Power Platform Plan Designer plans
- **Test Recommendations**: Generate actionable test recommendations based on app structure

### Available Commands

The MCP Server implements the following commands:

1. **ValidatePowerFx**: Validates a Power Fx expression for use in a test file
2. **GetPlanList**: Retrieves a list of available Power Platform plans
3. **GetPlanDetails**: Fetches details for a specific plan and provides facts and recommendations
4. **GetScanTypes**: Retrieves details for available scan types
5. **Scan**: Scans a workspace with optional scan types and post-processing Power Fx steps

## Prerequisites

1. Install of .Net SDK 8.0 from [Downloads](https://dotnet.microsoft.com/download/dotnet/8.0). For example on windows you could use the following command

```cmd
winget install Microsoft.DotNet.SDK.8
```

2. An install of PowerShell following the [Install Overview](https://learn.microsoft.com/powershell/scripting/install/installing-powershell) for your operating system. For example on Windows you could use the following command

```cmd
winget install --id Microsoft.PowerShell --source winget
```

3. The Power Platform Command Line interface installed using the [Learn install guidance](https://learn.microsoft.com/power-platform/developer/cli/introduction?tabs=windows#install-microsoft-power-platform-cli). For example assuming you have .NET SDK installed you could use the following command

```pwsh
dotnet tool install --global Microsoft.PowerApps.CLI.Tool
```

4. A created Power Platform environment using the [Power Platform Admin Center](https://learn.microsoft.com/power-platform/admin/create-environment) or [Power Platform Command Line](https://learn.microsoft.com/power-platform/developer/cli/reference/admin#pac-admin-create)

5. Git Client has been installed. For example using [GitHub Desktop](https://desktop.github.com/download/) or the [Git application](https://git-scm.com/book/en/v2/Getting-Started-Installing-Git). For example on Windows you could use the following command

```pwsh
winget install --id Git.Git -e --source winget
```

6. The Azure CLI has been [installed](https://learn.microsoft.com/cli/azure/install-azure-cli)

```pwsh
winget install -e --id Microsoft.AzureCLI
```

7. Visual Studio Code is [installed](https://code.visualstudio.com/docs/setup/setup-overview). For example on Windows you could use the following command

```pwsh
winget install -e --id Microsoft.VisualStudioCode
```

## Verification

  > NOTE: If at any stage you find that a component is not installed, you may need to restart you command line session to verify that the component has been installed 

1. Verify you have .Net 8.0 SDK installed

```pwsh
dotnet --list-sdks
```

2. Verify you have PowerShell installed

```pwsh
pwsh --version
```

3. Verify that you have Azure command line interface (az cli) installed

```pwsh
az --version
```

4. Verify that you have git installed

```pwsh
git --version
```

5. Verify you have Visual Studio Code installed

```pwsh
code --version
```

## Getting Started

1. Clone the repository using the git application and PowerShell command line. For example using the git command line

```pwsh
git clone https://github.com/microsoft/PowerApps-TestEngine
```

2. Change to cloned folder

```pwsh
cd PowerApps-TestEngine
```

3. Checkout the working branch

```pwsh
git checkout grant-archibald-ms/mcp-606
```

4. Authenticated with Azure CLI

```pwsh
az login --allow-no-subscriptions
```

5. Change to MCP sample

```pwsh
cd samples\mcp
```

6. Optional: Configure your Power Platform for [Git integration](https://learn.microsoft.com/en-us/power-platform/alm/git-integration/overview)

   - Clone your Azure DevOps repository to you local machine 

## Install the Test Engine

1. Create config.json in the mcp sample folder.

```json
{
    "uninstall": true,
    "compile": true
}
```

2. Run the install following from PowerShell to compile and install the Test Engine MCP Server

```pwsh
.\Install.ps1
```

## Start Test Engine MCP Interface

In a version of Visual Studio Code that supports MCP Server agent with GitHub Copilot

1. Open PowerShell prompt `pwsh`

2. Change to the cloned version of Power Apps Test Engine. For example

```PowerShell
cd c:\users\<useruser>\Source\PowerApps-TestEngine
```

3. Open Visual Studio Code using 

```PowerShell
code .
```

4. Open Settings

   Open the settings file by navigating to File > Preferences > Settings or by pressing Ctrl + ,.

5. Edit settings.json and suggested json from the `Install.ps1` results to the settings.json file to register the MCP server and enable GitHub Copilot

6. Start the GitHub Copilot

7. Switch to [Agent mode](https://code.visualstudio.com/docs/copilot/chat/chat-agent-mode)

## Test Generation

> **PREVIEW NOTICE**: These test generation features are in preview. Preview features aren't meant for production use and may have restricted functionality. These features are available before an official release so that customers can get early access and provide feedback.

This sample can integrate with Plan designer. In an environment that you have created a [Plan](https://learn.microsoft.com/en-us/power-apps/maker/plan-designer/plan-designer) follow these steps:

1. Create an [empty workspace](https://code.visualstudio.com/docs/editing/workspaces/workspaces) in Visual Studio Code

2. Switch to [Agent mode](https://code.visualstudio.com/docs/copilot/chat/chat-agent-mode)

3. Chat with agent using the available actions. 

4. For example after consenting to `get-plan-list` action the following should return plans you have access to in the environment

```
Show me available plans
```

5. Get information on the first plan. You will need to consent to the `get-plan-details` action

```
Get me details on the "Contoso Plan" plan
```

6. Generate the tests using the recommended yaml template 

```
Generate tests for my Dataverse entities
```

7. Review the [Dataverse](../dataverse/README.md) documentation on how to use the generated test yaml to test your dataverse entities.

## Power Fx Validation

> **PREVIEW NOTICE**: This Power Fx validation feature is in preview. Preview features aren't meant for production use and may have restricted functionality. These features are available before an official release so that customers can get early access and provide feedback.

1. Chat with agent using the available actions. For example after consenting to `validate-power-fx` action the following should be valid

```
If the following Power Fx valid in test engine?

Assert(1=2)
```

2. Try an invalid case 

```
If the following Power Fx valid in test engine?

Assert(DoesNotExist)
```

Which will return the following 

```
The Power Fx expression Assert(DoesNotExist) is not valid in the Test Engine. The errors indicate:

'DoesNotExist' is not recognized as a valid name.
The Assert function has invalid arguments.
Let me know if you need further assistance!
```
