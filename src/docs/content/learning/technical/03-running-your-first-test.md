---
title: 03 - Running Your First Test
---

Welcome, early adopters! If you're eager to try the current version of Power Platform Automated Testing, you're in the right place. This module will help you get started with the necessary components to explore automated testing.

### Prerequisites

To get started, you'll need the following components:
1. **Ability to Clone Repository**: Use Git command line or GitHub Desktop to clone the repository.
2. **PowerShell**: Ensure you have PowerShell installed on your machine.
3. **Power Platform Command Line Tools**: Install the Power Platform Command Line tools to interact with your Power Platform environment.
4. **.NET 8.0 SDK**: Download and install the .NET 8.0 SDK to build the code.

> NOTEL These instructions apply to the [inner ring](../context/ring-deployment-model.md) using a build from source strategy. As these changes are included in the [pac test run](https://learn.microsoft.com/power-platform/developer/cli/reference/test) many of these steps are not required.

This can seem daunting, but the following steps will help you with this process assuming you are using Microsoft Windows as your local operating system.

These install steps use winget for installation.

### Ability to Clone Repository

To install a GitHub client, you can follow these steps. You can also follow Download GitHub Desktop to download the application.

1. If you don't already have Git installed, you can install it using `winget`:

    ```pwsh
    winget install --id Git.Git -e --source winget
    ```

2. Alternatively, you can use GitHub Desktop. Install it using `winget`:

    ```pwsh
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

### PowerShell

PowerShell is typically pre-installed on Windows. For these instructions, we assume PowerShell Core. You can also follow instructions on Installing PowerShell for different operating systems.

1. PowerShell Core can be installed using `winget`:

    ```cmd
    winget install --id Microsoft.PowerShell --source winget
    ```

2. **Verification**: Run the following command in PowerShell Core to check the version:

    ```bash
    pwsh --version
    ```

### .NET 8.0 SDK

You can install the .NET 8.0 SDK using the following commands in Microsoft Windows. You can also follow Download .NET 8.0 for different operating systems and install steps.

1. Download and install the .NET 8.0 SDK using `winget`:

    ```bash
    winget install Microsoft.DotNet.SDK.8 
    ```

2. **Verification**: Verify the installation by running:

    ```pwsh
    dotnet --version
    ```

3. Close and reopen your terminal to ensure the new components are in the system path.

### Power Platform Command Line Interface (CLI)

You can install the Power Platform CLI using the following commands in Microsoft Windows. You can also follow Install Microsoft Power Platform CLI for different operating systems and install types.

1. Install Power Platform Command Line Interface (CLI) using dotnet command line interface:

    ```pwsh
    dotnet tool install --global Microsoft.PowerApps.CLI.Tool
    ```

2. **Verification**: Verify the installation by running:

    ```pwsh
    pac -v
    ```

3. Close and reopen your terminal to ensure the new components are in the system path.

## Getting Started

Have the prerequisites installed and verified? If so, let's look at some samples to get started. For each sample, reference the README.md for more information.

### Sample Configuration Settings

Each of the samples will need a **config.json** file in the folder using the following format. This file should be placed in each sample's folder at the same level as `RunTest.ps1`.

1. Create conig.json in **`samples\buttonclicker`** folder

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

2. To complete this file:

    - Open Power Apps.
    - Navigate to the Environment you want to test.
    - On the command bar, select Settings (gear).
    - Select Session details.
    - Copy the Environment Id and Tenant Id to the config.json file you have created.
    - Update user1Email to your test user email address.

    Note: Some samples will not require the customPage setting.

## Git Branch Notes

A particular sample can be present in a feaure branch. As changes are reviewed and updated, they will move to main and the Power Platform pac test run command. To use any feature branch, checkout the branch:

#### For example to checkout integration branch run: 
```pwsh
git checkout integration
```

### Simplest Example: Clicking the Button of a Canvas App

1. Copy your completed config.json file into the folder in samples\buttonclicker using [README](https://github.com/microsoft/PowerApps-TestEngine/blob/integration/samples/buttonclicker/README.md)

2. Run the test:

    ```pwsh
    pwsh -File RunTests.ps1
    ```

3. When the browser opens login to the deployed Power App

## Summary

In this section, you learned how to set up the necessary components to run your first automated test using configuration for the sample settings and running a simple test by clicking a button in a Canvas App. By following these steps, you are now equipped to explore automated testing in Power Platform.
