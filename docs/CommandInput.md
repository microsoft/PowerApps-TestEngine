# PowerAppsTestEngine.exe Inputs

The exe can take in inputs in the form of `config.json` (or `config.dev.json`) or command line input parameters

## Parameters

| Parameter | Details |
| -- | -- |
| EnvironmentId | Environment that the Power App you are testing is located in. For more information about environments, please view [this](https://docs.microsoft.com/en-us/power-platform/admin/environments-overview) |
| TenantId | Tenant that the Power App is located in. |
| TestPlanFile | Path to the test plan that you wish to run |
| OutputDirectory | Path to folder the test results will be placed. Optional. If this is not provided, it will be placed in the `TestOutput` folder. |
| LogLevel | Level for logging (Folllows [this](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.loglevel?view=dotnet-plat-ext-6.0)). Optional. If this is not provided, Information level logs and higher will be logged |
| QueryParams | Specify query parameters to be added to the powerapps URL. |

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
| -l | LogLevel |
| -q | QueryParams |

## What is the difference between the settings passed in via command line/config.json vs settings located inside the YAML?

The settings passed in via command line or config.json are settings that either start off the test (link to the test plan) or they are settings that are likely to change due to the environment the app being test in is located.

Settings located in the YAML should be able to be "imported" with the solution, so another person could take the solution and corresponding test plan and use the two of them without any modifications.

Example: environment id changes if the app is imported to a another tenant/environment, and so it is located as a command line or config.json setting.

## Test plan conversion
The exe can also be used to convert an msapp's test json to a Test Engine yaml test plan.
This is done by passing `convert` as an input followed by a path that leads to the test json.

```bash
#using the exe
PowerAppsTestEngine.exe convert "path/to/test.json"

#using dotnet run
dotnet run convert "path/to/test.json"
```
The output is yaml test plan in the same directory as the test json.
