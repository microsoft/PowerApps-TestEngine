# PowerAppsTestEngine.exe Inputs

The exe can take in inputs in the form of `config.json` (or `config.dev.json`) or command line input parameters

## Parameters

| Parameter | Details |
| -- | -- |
| EnvironmentId | Environment that the Power App you are testing is located in. For more information about environments, please view [this](https://docs.microsoft.com/en-us/power-platform/admin/environments-overview) |
| TenantId | Tenant that the Power App is located in. |
| TestPlanFile | Path to the test plan that you wish to run |
| OutputDirectory | Path to folder the test results will be placed. Optional. If this is not provided, it will be placed in the `TestOutput` folder. |

## Config.json

Please view the checked in `config.json` file for the latest format.

Use a `config.dev.json` to prevent accidentally checking in personal info.

### Command line

Command line parameters override anything specified in `config.json`

| Switch | Parameter |
| -- | -- |
| -i | TestPlanFile |
| -e | EnvironmentId |
| -t | TenantId |
| -o | OutputDirectory |