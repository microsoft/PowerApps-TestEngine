# Test Engine MCP Server Sample

This sample explains how to set up and configure Visual Studio Code to integrate with the Test Engine Model Context Protocol (MCP) provider using stdio interface using a NodeJS proxy.

## What You Need

Before you start, you'll need a few tools and permissions:
- **Power Platform Command Line Interface (CLI)**: This is a tool that lets you interact with Power Platform from your command line.
- **PowerShell**: A task automation tool from Microsoft.
- **.Net 8.0 SDK**: A software development kit needed to build and run the tests.
- **Power Platform Environment**: A space where your Plan Designer interactions and solutions exist.
- **GitHub Copilot**: Access to [GitHub Copilot](https://github.com/features/copilot)
- **Visual Studio Code**: An install of [Visual Studio Code](https://code.visualstudio.com/) to host the GitHub Copilot and the edit generated test files.
- **NodeJs**: An installation of [NodeJs](https://nodejs.org/) as the current Model Context Protocol proxy that is used to communicate with Test Engine Command Line Interface

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

8. NodeJs is [installed](https://nodejs.org/). For example on Windows you could use the following command

```pwsh
winget install nodejs
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

3. Verify that you have Power Platform command line interface (pac cli) installed

```pwsh
pac
```

4. Verify that you have Azure command line interface (az cli) installed

```pwsh
az --version
```

5. Verify that you have git installed

```pwsh
git --version
```

6. Verify you have Visual Studio Code installed

```pwsh
code --version
```

7. Verify you have NodeJs installed

```pwsh
node --version
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

4. Ensure logged out out of pac cli. This ensures you're logged out of any previous sessions.

```pwsh
pac auth clear
```

5. Login to Power Platform CLI using [pac auth](https://learn.microsoft.com/power-platform/developer/cli/reference/auth#pac-auth-create)

```pwsh
pac auth create --environment <Your environment ID>
```

6. Authenticated with Azure CLI

```pwsh
az login --use-device-code --allow-no-subscriptions
```

7. Change to MCP sample

```pwsh
cd samples\mcp
```

8. Edit the sample in your editor. For example using Visual Studio Code you can open the sample folder using the following command

```pwsh
code .
```

9. Optional: Configure your Power Platform for [Git integration](https://learn.microsoft.com/en-us/power-platform/alm/git-integration/overview)

10. Optional: Clone your Azure DevOps repository to you local machine 

11. Add the a new file named **config.json** in the same folder as RunTests.ps1. You will need to replace the value with your tenant, environment id and cloned repository information. 

  > TIP: You can obtain the environment and tenant information from your Power Apps portal by using **settings** from the main navigation var and selecting **Session Details** 

```json
{
    "tenantId": "a222222-1111-2222-3333-444455556666",
    "environmentId": "12345678-1111-2222-3333-444455556666",
    "environmentUrl": "https://contoso.crm.dynamics.com/",
    "user1Email": "test@contoso.onmicrosoft.com",
    "installPlaywright": true,
    "compile": true,
    "repository": "c:\\users\\user1\\repo"
}
```

## Run Test Engine

To Run the sample tests from PowerShell assuming the Getting started steps have been completed

```pwsh
.\Run.ps1
```

## Start Test Engine MCP Interface

In a version of Visual Studio Code that supports MCP Server agent with GitHub Copilot

1. Open Visual Studio Code

2. Open the project

3. Open Settings

   Open the settings file by navigating to File > Preferences > Settings or by pressing Ctrl + ,.

4. Edit settings.json and add the following configuration to your settings.json file to register the MCP server and enable GitHub Copilot:

```json
{
    "mcp": {
        "inputs": [],
        "servers": {
            "TestEngine": {
                "command": "node",
                "args": [".\test-engine-mcp.js", "testengine.provider.mcp.dll"],
            }
        }
    },
    "github.copilot.enable": true,
    "chat.mcp.discovery.enabled": true
}
```

5. Start the GitHub Copilot

6. Switch to [Agent mode](https://code.visualstudio.com/docs/copilot/chat/chat-agent-mode)

## Test Generation

This sample can integrate with Plan designer. In an environment that you have created a [Plan](https://learn.microsoft.com/en-us/power-apps/maker/plan-designer/plan-designer) follow these steps

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

7. Review the [Dataverse](../dataverse/README.md) on how to use the generated test yaml to test your dataverse entities.

## Power Fx Validation

1. Chat with agent using the available actions. For example after consenting to `validate-power-fx` action the following should be valid

```
If the following Power Fx valid in test engine?

Assert(1=2)
```

8. Try an invalid case 

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
