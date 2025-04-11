# Overview

The Power Platform Center of Excellence (CoE) starter kit is made up of a number of Power Platform low code solution elements. Among these is a model driven application that can be used to setup and upgrade the CoE Starter Kit.

This sample includes Power Apps Test Engine tests that can be used to automate and test ket elements of the expected behavior of the Setup and Upgrade Wizard

## What You Need

Before you start, you'll need a few tools and permissions:
- **Power Platform Command Line Interface (CLI)**: This is a tool that lets you interact with Power Platform from your command line.
- **PowerShell**: A task automation tool from Microsoft.
- **.Net 8.0 SDK**: A software development kit needed to build and run the tests.
- **Power Platform Environment**: A space where your Power Apps live.
- **Admin or Customizer Rights**: Permissions to make changes in your Power Platform environment.

## Prerequisites

1. Install of .Net SDK 8.0 from [Downloads](https://dotnet.microsoft.com/download/dotnet/8.0)
2. An install of PowerShell following the [Install Overview](https://learn.microsoft.com/powershell/scripting/install/installing-powershell) for your operating system
3. The Power Platform Command Line interface installed using the [Learn install guidance](https://learn.microsoft.com/power-platform/developer/cli/introduction?tabs=windows#install-microsoft-power-platform-cli)
4. A created Power Platform environment using the [Power Platform Admin Center](https://learn.microsoft.com/power-platform/admin/create-environment) or [Power Platform Command Line](https://learn.microsoft.com/power-platform/developer/cli/reference/admin#pac-admin-create)
5. Granted System Administrator or System Customizer roles as documented in [Microsoft Learn](https://learn.microsoft.compower-apps/maker/model-driven-apps/privileges-required-customization#system-administrator-and-system-customizer-security-roles)
6. Git Client has been installed. For example using [GitHub Desktop](https://desktop.github.com/download/) or the [Git application](https://git-scm.com/book/en/v2/Getting-Started-Installing-Git)
7. The CoE Starter Kit core module has been installed into the environment

## Getting Started

1. Clone the repository using the git application and PowerShell command line

```pwsh
git clone https://github.com/microsoft/PowerApps-TestEngine.git
```

2. Change to cloned folder

```pwsh
cd PowerApps-TestEngine
```

3. Ensure logged out out of pac cli. This ensures you're logged out of any previous sessions.

```pwsh
pac auth clear
```

4. Login to Power Platform CLI using [pac auth](https://learn.microsoft.com/power-platform/developer/cli/reference/auth#pac-auth-create)

```pwsh
pac auth create --environment <Your environment ID>
```

5. Add the config.json in the same folder as RunTests.ps1 replacing the value with your tenant and  environment id

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

## Run Test

To Run the sample tests from PowerShell assuming the Getting started steps have been completed

```pwsh
.\RunTests.ps1
```

## Record and Replay

To record interaction with Dataverse and generate a sample Test Engine script perform the following steps assuming the Getting started steps have been completed

1. Start record process

```pwsh
.\Record.ps1
```

2. If required login to the Power App

3. Wait for the Playwright Inspector to be displayed

4. Interact with the Setup and Upgrade Wizard

5. When ready to complete the record session press play in the Playwright Inspector

6. Open the generated **recorded.te.yaml** that includes data from recorded Dataverse and Connector calls.

## What to Expect

- **Login Prompt**: You'll be asked to log in to the Power Apps Portal.
- **Test Execution**: The Test Engine will run the steps to test your Power Apps Portal.
- **Cached Credentials**: If you choose "Stay Signed In," future tests will use your saved credentials.
- **Interactive Testing**: Commands like `Preview.Pause()` will let you pause and inspect the test steps.
- **Recorded Sessions**: Test Engine provides the ability to generate recorded video of the test session in the TestOutput folder.

## Context

This sample is an example of a "build from source" using the open source licensed version of Test Engine. Features in the the source code version can include feature not yet release as part of the ```pac test run`` command in the Power Platform Command line interface action.
