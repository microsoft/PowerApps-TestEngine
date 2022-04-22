# Power Apps Test Engine

> This is currently an experimental project.

Power Apps Test Engine is an open source project that provides a way for makers to run tests authored using Power FX against Canvas apps. These tests are written in our Power FX expression language.

The engine uses Playwright to orchestrate the tests.

## Getting Started

To get started with running one of the samples
```
# Pull github repo
git clone https://github.com/microsoft/PowerAppsTestEngine.git

# Change to the PowerAppsTestEngine folder
cd PowerAppsTestEngine\src\PowerAppsTestEngine

# Build
dotnet build

# Install required browsers - replace netX with actual output folder name, eg. net6.0.
pwsh bin\Debug\netX\playwright.ps1 install
```

Import the solution of the selected sample. For information about solution import, please view [this](https://docs.microsoft.com/en-us/power-apps/maker/data-platform/import-update-export-solutions).

Create a `config.dev.json` inside the `PowerAppsTestEngine` folder. (It should be next to the config.json)

The contents should be a copy of config.json, like this.
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
- tenantId: Tenant you are in
- testPlanFile: Path to the `testPlan.fx.yaml` file for the sample that you wish to run. (Eg. `samples\buttonclicker\testPlan.fx.yaml`)
- outputDirectory: Path to folder you wish the test results to be placed.

For more information about the config and the inputs to the command, please view [this](.\docs\CommandInput.md)

To setup the user environment variables, please view [this](.\docs\Yaml\Users.md). Refer to the user configuration section in your selected sample's `testPlan.fx.yaml`.

Now you should be ready to run the test
```
# Run test
dotnet run
```

When the run is complete, you should be able to view the test results in the folder you specified earlier.

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

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
