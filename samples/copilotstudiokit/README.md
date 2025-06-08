# Power CAT Copilot Studio Kit Sample

The **Power CAT Copilot Studio Kit** is a comprehensive solution designed to automate the testing of custom copilots within the Power Platform. This kit primarily focuses on two main use cases: intent accuracy testing and generative answers testing.

The Power CAT Copilot Studio Kit has user-friendly Model Driven Application that we want to test so that we can ensure that it verified the features that empower makers to configure copilots and test sets. The features that will need to be tested include various types of Copilot tests, including response exact match, attachments match, topic match (which requires Dataverse enrichment), and generative answers (which requires AI Builder for response analysis and Azure Application Insights for details on why an answer was or was not generated).

## What You Need

Before you start, you'll need a few tools and permissions:
- **Power Platform Command Line Interface (CLI)**: This is a tool that lets you interact with Power Platform from your command line.
- **PowerShell**: A task automation tool from Microsoft.
- **.Net 8.0 SDK**: A software development kit needed to build and run the tests.
- **Power Platform Environment**: A space where your Power Apps live.
- **Admin or Customizer Rights**: Permissions to make changes in your Power Platform environment.

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

5. Granted System Administrator or System Customizer roles as documented in [Microsoft Learn](https://learn.microsoft.com/power-apps/maker/model-driven-apps/privileges-required-customization#system-administrator-and-system-customizer-security-roles)



7. The Azure CLI has been [installed](https://learn.microsoft.com/cli/azure/install-azure-cli)

```pwsh
winget install -e --id Microsoft.AzureCLI
```

8. A code editor like Visual Studio Code is [installed](https://code.visualstudio.com/docs/setup/setup-overview). For example on Windows you could use the following command

```pwsh
winget install -e --id Microsoft.VisualStudioCode
```

9. You have an installed version of Creator kit in the target environment. For example change the **00000000-0000-0000-0000-000000000000** to your environment id

```pwsh
pac application install --environment-id 00000000-0000-0000-0000-000000000000 --application-name CreatorKitCore
```

10. You have the Copilot Studio Kit installed in the target environment. You can use [Install instructions](https://github.com/microsoft/Power-CAT-Copilot-Studio-Kit/blob/main/INSTALLATION_INSTRUCTIONS.md)

## Verification

To ensure your environment is ready:
```powershell
.\validate.ps1
```

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

6. Optional verify you have Visual Studio Code installed

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
git checkout grant-archibald-ms/copilotstudiokit-560
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

7. Change to Copilot Studio Kit sample

```pwsh
cd samples\copilotstudiokit
```

8. Edit the sample in your editor. For example using Visual Studio Code you can open the sample folder using the following command

```pwsh
code .
```

9. Using your editor of choice add the a new file named **config.json** in the same folder as RunTests.ps1. You will need to replace the value with your tenant and environment id. 

  > TIP: You can obtain the environment and tenant information from your Power Apps portal by using **settings** from the main navigation var and selecting **Session Details** 

```json
{
    "tenantId": "a222222-1111-2222-3333-444455556666",
    "environmentId": "12345678-1111-2222-3333-444455556666",
    "appName": "Copilot Studio Kit",
    "customPage": "cat_webchatcustomizer_48d6e",
    "user1Email": "test@contoso.onmicrosoft.com",
    "userAuth": "storagestate", 
    "runInstall": true,
    "installPlaywright": true,
    "useStaticContext": false,
    "getLatest": false,
     "testScripts": {
        "customPageTestScripts":[
            "webchatcustomizer-playground.te.yaml",
            "promptadvisorstudio.te.yaml",
            "adaptivecards.te.yaml",
            "home.te.yaml"
        ]
    },
    "pages": {
        "customPage": true,
        "list": true,
        "details": true,
        "customPages": [
            "cat_webchatcustomizer_48d6e",
            "cat_adaptivecards_4476e",
            "cat_promptadvisorstudio_6e1ce",
            "cat_home_c8017"
        ],
        "entities": [
            { 
                "name": "agents",
                "entity": "cat_copilotconfiguration",
                "id": "cat_copilotconfigurationid"
            },
            { 
                "name": "testsets",
                "entity": "cat_copilottestset",
                "id": "cat_copilottestsetid"
            },
            { 
                "name": "agenttest",
                "entity": "cat_copilottest",
                "id": "cat_copilottestid"
            },
            {
                "name": "testruns",
                "entity": "cat_copilottestrun",
                "id": "cat_copilottestrunid"
            },
            {
                "name": "logs",
                "entity": "cat_copilotstudiokitlogs",
                "id": "cat_copilotstudiokitlogsid"
            },
            {
                "name":"agenttranscripts",
                "entity":"cat_agenttranscripts",
                "id":"cat_agenttranscriptsid"
            },
            {
              "name":"conversationkpi",
              "entity":"cat_copilotkpi",
              "id":"cat_copilotkpiid"
            },
            {
                "name":"conversationtranscript",
                "entity":"conversationtranscript",
                "id":"conversationtranscriptid"              
            }
        ]
    }
}
```

## Run Test

To Run the sample tests from PowerShell assuming the Getting started steps have been completed

```pwsh
.\RunTests.ps1
```

## Record and Replay

To record interaction with Dataverse and generate a sample Test Engine script perform the following steps assuming the Getting started steps have been completed

1. Start record process. Change record to true in the config.json

2. Run the test

```pwsh
.\RunTests.ps1
```

3. If required login to the Power App

4. Wait for the Playwright Inspector to be displayed

5. Interact with the Setup and Upgrade Wizard

6. When ready to complete the record session press play in the Playwright Inspector

7. Open the generated **recorded.te.yaml** that includes data from recorded Dataverse and Connector calls.

## What to Expect

- **Login Prompt**: You'll be asked to log in to the Power Apps Portal.
- **Test Execution**: The Test Engine will run the steps to test your Power Apps Portal.
- **Cached Credentials**: If you choose "Stay Signed In", future tests will use your saved credentials.
- **Interactive Testing**: Commands like `Experimental.Pause()` will let you pause and inspect the test steps.
- **Recorded Sessions**: Test Engine provides the ability to generate recorded video of the test session in the TestOutput folder.

## Context

This sample is an example of a "build from source" using the open source licensed version of Test Engine. Features in the source code version can include features not yet released as part of the `pac test run` command in the Power Platform Command line interface action.

## Security

When you look at running this sample in your environment consider the following:

- Multi Factor Authentication Requirements - The initial run is likely to need an interactive logic to obtain the single sign on cookies
- Conditional Access polices - The policies could mandate specific supported browser types and Machine Device Management policies

This sample assume the following:

- Browser type will be Microsoft Edge
- If conditional access policies require work profile you will switch to the edge work profile to obtain the single sign on cookies
- You may need to change your **config.json** file to set  `"useStaticContext": true` for the login as in private browser login may not be supported by your organization's conditional access policies
