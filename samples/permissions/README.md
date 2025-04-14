# Overview

This sample is deigned to test the behavior of Canvas app and Model Driven App (MDA) entity list and custom page. The following table provides some assumptions and example Authentication methods that you can use to validate permissions.

| Persona | Description | Authentication Method |
|---------|-------------|-----------------------|
| user1   | Assume the permissions Power App canvas app has been shared with user but no Power App license assigned | [Microsoft Authenticator](https://learn.microsoft.com/entra/identity/authentication/concept-authentication-authenticator-app)
| user2   | Assume that user account has not been shared user persona and no Power App license assigned is assigned | [Temporary Access Pass](https://learn.microsoft.com/entra/identity/authentication/howto-authentication-temporary-access-pass)

## What You Need

Before you start, you'll need a few tools and permissions:
- **Power Platform Command Line Interface (CLI)**: This is a tool that lets you interact with Power Platform from your command line.
- **PowerShell**: A task automation tool from Microsoft.
- **.Net 8.0 SDK**: A software development kit needed to build and run the tests.
- **Power Platform Environment**: A space where your Power Apps live.
- **Admin or Customizer Rights**: Permissions to make changes in your Power Platform environment.

## Prerequisites

1. Install of .Net SDK 8.0 from [Downloads](https://dotnet.microsoft.com/download/dotnet/8.0)
2. An install of PowerShell following the [Install Overview](https://learn.microsoft.com/powershell/scripting/install/installing-powershel) for your operating system
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

3. Import the solution Permissions*.zip into the environment you want to test with

4. Ensure logged out out of pac cli. This ensures you're logged out of any previous sessions.

```pwsh
pac auth clear
```

5. Login to Power Platform CLI using [pac auth](https://learn.microsoft.com/power-platform/developer/cli/reference/auth#pac-auth-create)

```pwsh
pac auth create --environment <Your environment ID>
```

5. Add the config.json in the same folder as RunTests.ps1 replacing the value with your tenant and  environment id

```json
{
    "tenantId": "a1234567-1111-2222-3333-4444555566666",
    "environmentId": "c0000001-2222-3333-5555-12345678",
    "canvasAppName": "contoso_canvas_4033c",
    "customPageName": "contoso_custom_b2441",
    "mdaName": "contoso_MDA",
    "runInstall": true,
    "installPlaywright": true,
    "userEmail1": "alans@contoso.onmicrosoft.com",
    "userEmail2": "aliciat@contoso.onmicrosoft.com"
}
```

## Run Test

To Run the sample tests from PowerShell assuming the Getting started steps have been completed

```pwsh
.\RunTests.ps1
```

## What to Expect

- **Login Prompt**: You'll be asked to log in to the Power Apps Portal for the first time
- **Test Execution**: The Test Engine will run the steps to test your Power Apps Portal, MDA and Canvas apps.
- **Cached Credentials**: If you choose "Stay Signed In," future tests will use your saved credentials.
- **Expired Credentials**: If your temporary access password has expired the test will fail. For example you could use the Entra portal to delete a Temporary Access Pass and observe that the test case should fail for persona `userEmail2`.
