---
title: Get Started Now
---

Welcome, early adopters! If you're eager to try the current version of Power Platform Automated Testing, you're in the right place. This guide will help you get started with the necessary components and provide you with two paths to explore automated testing.

> There is also a work in progress [Learning module](../learning/) to help you get started.

## Prerequisites

To get started, you'll need the following components:
1. **Ability to Clone Repository**: Use Git command line or GitHub Desktop to clone the repository.
2. **PowerShell**: Ensure you have PowerShell installed on your machine.
3. **Power Platform Command Line Tools**: Install the Power Platform Command Line tools to interact with your Power Platform environment.
4. **.NET 8.0 SDK**: Download and install the .NET 8.0 SDK to build the code.
5. **Azure CLI**: Install the Azure CLI

This can seem daunting but the following steps will help you with this process assuming you are using Microsoft Windows as your local operating system.

These install steps use [winget](https://learn.microsoft.com/windows/package-manager/winget/#install-winget) for install. 

## Ability to Clone Repository

To install a GitHub client you can follow these steps. You can also follow [Download GitHub Desktop](https://desktop.github.com/download/) to download the application

1. If you don't already have Git installed, you can install it using `winget`:

```pwsh
winget install --id Git.Git -e --source winget
```

2.  Alternatively, you can use GitHub Desktop. Install it using `winget`:

```winget
winget install --id GitHub.GitHubDesktop -e --source winget
```

3. Use Git command line or GitHub Desktop to clone the repository. For Git command line, open your terminal and run:

```pwsh
git clone https://github.com/microsoft/PowerApps-TestEngine.git
```

4. **Verification**: After cloning, navigate to the repository folder and list the contents to ensure the files are there:

```pwsh
cd PowerApps-TestEngine
```

## PowerShell

PowerShell is typically pre-installed on Windows. But for these instructions we assume PowerShell Core is assumed. You can also follow instructions on [Installing](https://learn.microsoft.com/en-us/powershell/scripting/install/installing-powershell) for different operating systems.

1. PowerShell Core can be installed using `winget`:

```cmd
winget install --id Microsoft.PowerShell --source winget
```

2. **Verification**: Run the following command in PowerShell Core to check the version:

```bash
pwsh --version
```

## .Net 8.0 SDK

You can install the .Net 8.0 SDK using the following commands in Microsoft Windows. You can also follow [Download .NET 8.0](https://dotnet.microsoft.com/download/dotnet/8.0) for different operating systems and install steps.

1.  Download and install the .NET 8.0 SDK using `winget`:

```bash
winget install Microsoft.DotNet.SDK.8 
```

2. **Verification**: Verify the installation by running:

```pswh
dotnet --version
```

3. Close and reopen your terminal to ensure the new components are in the system path.

##  Power Platform Command Line Interface (CLI)

You can install the .Net 8.0 SDK using the following commands in Microsoft Windows. We can also follow [Install Microsoft Power Platform CLI](https://learn.microsoft.com/power-platform/developer/cli/introduction#install-microsoft-power-platform-cli) for different operating systems and install types.

1. Install Power Platform Command Line Interface (CLI) using dotnet command line interface

```pwsh
dotnet tool install --global Microsoft.PowerApps.CLI.Tool
```

2. **Verification**: Verify the installation by running:

```pswh
pac -v
```

3. Close and reopen your terminal to ensure the new components are in the system path.

## Azure Command Line Interface

1. You can install the Azure Command Line Interface (CLI) using winget

```pwsh
winget install -e --id Microsoft.AzureCLI
```

2. **Verification**: Verify the installation by running:

```pswh
az -version
```

3. Login to the Azure cli with user account account access to the dataverse

```pwsh
az login --allow-no-subscriptions
```

## Getting Started

Have the prerequisites installed and verified? If so lets looks at some samples to get started. For each sample reference the README.md for more information.

### Sample Configuration Settings

Each of the samples will need a **config.json** file in the folder using the following format. This file should be placed in each samples folder as the same level as `RunTest.ps1`

```json
{
    "tenantId": "a222222-1111-2222-3333-444455556666",
    "environmentId": "12345678-1111-2222-3333-444455556666",
    "customPage": "admin_initialsetuppage_d45cf",
    "user1Email": "test@contoso.onmicrosoft.com",
    "runInstall": true,
    "installPlaywright": true
}
```

To complete this file

1. Open `https://make.powerapps.com`

2. Navigate to the Environment you want to test

3. On the command bar, select Settings (gear)

4. Select **session details**

5. Copy the Environment Id and Tenant Id to the config.json file you have created

6. Update user1Email to your test user email address.

7. Note: Some samples will not require the customPage setting

### Simplest Example: Clicking the Button of a Canvas App

For those looking for a straightforward example, you can start with a simple scenario like clicking a button in a Canvas App. This example will help you understand the basics of automated testing in the Power Platform.

1. Import the ButtonClicker_*.zip file from the cloned repository using [Import solutions](https://learn.microsoft.com/power-apps/maker/data-platform/import-update-export-solutions)

2. Copy you completed config.json file into the folder

3. Run the test

```pwsh
pwsh -File RunTests.ps1
```

### Advanced Example: CoE Starter Kit Setup and Upgrade Wizard

For a more comprehensive example, you can explore the CoE Starter Kit Setup and Upgrade wizard. This low-code solution offers a broader scope for testing and provides a deeper understanding of automated testing in the Power Platform.

### Running tests

1. Install the CoE Kit using [Get started with setup](https://learn.microsoft.com/power-platform/guidance/coe/setup)

2. Copy you completed config.json file into the folder

3. Run the test

```pwsh
pwsh -File RunTests.ps1
```

### Recording new tests

To Optionally record a new test based on your input and Dataverse and Connector calls you can

1. Launch the application in 

```pwsh
pwsh -File Record.ps1
```

5. When in record model wait until the [Playwright Inspector](https://playwright.dev/docs/debug#playwright-inspector) appears

6. Interact with the application

7. Select continue in the Playwright Inspector to complete the session

8. The test results will indicate where to look at the recorded test results
