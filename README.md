# Power Apps Test Engine

[![CI Build](https://github.com/microsoft/PowerApps-TestEngine/actions/workflows/build-test.yml/badge.svg)](https://github.com/microsoft/PowerApps-TestEngine/actions/workflows/build-test.yml)
> This is currently an experimental project.

Power Apps Test Engine is an open source project that provides a way for makers to run tests authored using Power FX against Canvas apps. These tests are written in our Power FX expression language.

This engine uses Playwright to orchestrate the tests.

## Getting Started

To get started with running one of the samples

### Prerequisites

- Have [.NET Core 6.0.x SDK](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) installed and make sure your `MSBuildSDKsPath` environment variable is pointing to [.NET Core 6.0.x SDK](https://dotnet.microsoft.com/en-us/download/dotnet/6.0).
- Have [PowerShell](https://docs.microsoft.com/en-us/powershell/scripting/install/installing-powershell?view=powershell-7.2) installed.

### Build locally

Run the commands below in PowerShell.

```bash
# Pull github repo
git clone https://github.com/microsoft/PowerApps-TestEngine.git

# Change to the PowerAppsTestEngine folder
cd PowerApps-TestEngine\src\PowerAppsTestEngine

# Build
dotnet build

# Install required browsers - replace <net-version> with actual output folder name, eg. net6.0.
pwsh bin\Debug\<net-version>\playwright.ps1 install
```

### Import a sample solution

Log in to Power Apps with a work or school organization account. The account used cannot have multi-factor authentication enabled for PowerApps. For Microsoft employees, you will need to create a test account.

If you need a test tenant, you can create one by visiting [this link](https://cdx.transform.microsoft.com/my-tenants).

Import a sample solution (eg. `PowerApps-TestEngine\samples\basicgallery\BasicGallery_1_0_0_2.zip`). Remember the environment that you imported the solution to. For information about solution importing, please view [this](https://docs.microsoft.com/en-us/power-apps/maker/data-platform/import-update-export-solutions). Check [Samples Introduction](https://github.com/microsoft/PowerApps-TestEngine/blob/main/samples/SamplesIntroduction.md) for more sample solutions.

### Set up the config file

Create a `config.dev.json` inside the `PowerAppsTestEngine` folder. (It should be next to the config.json.)

The contents should be a copy of config.json, like this:

```
{
  "environmentId": "",
  "tenantId": "",
  "testPlanFile": "",
  "outputDirectory": ""
}
```

Fill in the various properties:

- environmentId: Environment that you imported the solution to
  - You can get the `environmentId` by visiting [this link](https://make.powerapps.com/). Make sure to select the environment you import the solution to. In the URL, you will be able to find the `environmentId`. (Eg. https://make.powerapps.com/environments/{environmentId}/solutions/)
- tenantId: Tenant you are in
  - Select the solution imported and visit the `Details` of the solution. `tenantId` can be found in `Web link`.
- testPlanFile: Path to the `testPlan.fx.yaml` file for the sample that you wish to run. (Eg. `../../samples/basicgallery/testPlan.fx.yaml`)
- outputDirectory: Path to folder you wish the test results to be placed.

For more information about the config and the inputs to the command, please view [this link](https://github.com/microsoft/PowerApps-TestEngine/blob/main/docs/CommandInput.md).

### Setup the user environment variables

Please view [this link](https://github.com/microsoft/PowerApps-TestEngine/blob/main/docs/Yaml/Users.md). Refer to the user configuration section in your selected sample's `testPlan.fx.yaml`.

### Run test

Now you should be ready to run the test:

```
# Run test
dotnet run
```

When the run is complete, you should be able to view the test results in the folder you specified earlier.

Check [Samples Introduction](https://github.com/microsoft/PowerApps-TestEngine/blob/main/samples/SamplesIntroduction.md) for more sample solutions.

## What to do next

Option 1: Modify the `testPlan.fx.yaml` of a provided sample to run tests created on your own. You can also modify the sample PowerApp and create new tests for your updated app. Check [Power FX](https://github.com/microsoft/PowerApps-TestEngine/tree/main/docs/PowerFX) for writing functions. The sample test plan will be [here](https://github.com/microsoft/PowerApps-TestEngine/blob/main/samples/template/TestPlanTemplate.fx.yaml).

Option 2: If you are using [Test Studio](https://docs.microsoft.com/en-us/power-apps/maker/canvas-apps/test-studio), you can convert your Test Studio tests to Test Engine.

Note: Currently this is a slightly complicated process. In the near future we'll have the download button available in Test Studio to download the converted test plan.

1. Download your Power App.
2. Rename your `.msapp` file by adding `.zip` at the end
3. Unzip your zipped `.msapp` file and you will see a `AppTests` folder.
4. In the folder there is a `2.json` file. Run the following commands in command line to convert an Test Studio json to a Test Engine yaml test plan.

```bash
dotnet run convert "path\to\yourApp.msapp.zip\AppTests\2.json"
```

5. Open the yaml file generated and add the logical name or app ID of your app. The steps to get them are [here](https://github.com/microsoft/PowerApps-TestEngine#remarks).
6. Make sure you update the config file and user configurations if you are using a different tenant or environment for this app. You will need to modify `testPlanFile` with the path to the `2.fx.yaml` file for the sample that you wish to run.
7. Now you should be ready to run the test with `dotnet run`

## More about the test plan

[Yaml Format](https://github.com/microsoft/PowerApps-TestEngine/tree/main/docs/Yaml)

[Power FX](https://github.com/microsoft/PowerApps-TestEngine/tree/main/docs/PowerFX)

## Remarks

- **Working with apps within Solutions** - Test plan files for apps that are part of [solutions](https://docs.microsoft.com/en-us/power-apps/maker/data-platform/solutions-overview) are portable across environments.  For solution-based apps, the test plan refers to the target app with a logical name (the appLogicalName property) which does not change if the app moves to a different environment.
  1. Locate the App Logical name for the app
      1. In the **Solutions** tab, open the solution that contains the app
      1. Select **Apps**
      1. Note the **Name** column. It is the app logical name (Not the **Display name**)
  2. Update your test plan file
      1. Open the test plan YAML file for the app
      1. Fill in the **appLogicalName** value with the new App logical name

- **Working with apps outside of Solutions** - If you move an app that is *not* part of a solution to a new environment, you will need to manually update the test plan file to refer to the app. How to update a test plan file for a non-solution based app:

  1. Locate the App ID for the app in its new location
      1. In the **Apps** list, locate the app and open the context menu
      1. Select **Details**
      1. Note the **App ID** GUID on the Details pane
  2. Update your test plan file
      1. Open the test plan YAML file for the app
      1. Fill in the **appId** with the new App ID

## Known limitations

While work to provide full control coverage is in progress, support for the following controls are currently unavailable:

- Charts
- Media
- Timer
- Mixed Reality
- Child controls within components

## Frequently asked questions

### 1. I got timeout error. What does it mean?

You might get a timeout error due to the app taking longer than the default 30s time-limit to load. Most of the time, re-running the program can solve the problem. If this error still happens you probably want to check the recording in the test result folder to see what caused the error. If your app takes longer to load, you can also modify the timeout limit in [test settings](https://github.com/microsoft/PowerApps-TestEngine/blob/main/docs/Yaml/testSettings.md) to give it more time.

### 2. What is the difference between the settings passed in via command line/config.json vs settings located inside the YAML?

The settings passed in via command line or config.json are settings that either start off the test (link to the test plan) or they are settings that are likely to change due to the environment the app being test in is located.

Settings located in the YAML should be able to be "imported" with the solution, so another person could take the solution and corresponding test plan and use the two of them without any modifications.

Example: environmentId changes if the app is imported to a another tenant/environment, and so it is located as a command line or config.json setting.

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

This [JS folder](https://github.com/microsoft/PowerApps-TestEngine/tree/main/src/Microsoft.PowerApps.TestEngine/JS) is having refactor in big ways. Thus, we aren't yet accepting code contributions for all Javascript related part of this project until we are more stable.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft 
trademarks or logos is subject to and must follow 
[Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general).
Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship.
Any use of third-party trademarks or logos are subject to those third-party's policies.
