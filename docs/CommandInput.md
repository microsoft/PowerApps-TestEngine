# PowerAppsTestEngine.exe Inputs

The executable can take in inputs defined in `config.dev.json`, or as command line input parameters.

## Parameters

| Parameter | Details |
| -- | -- |
| EnvironmentId | Environment that the Power Apps app you are testing is located in. For more information about environments, please view [this](https://docs.microsoft.com/en-us/power-platform/admin/environments-overview) |
| TenantId | Tenant that the Power Apps app is located in. |
| TestPlanFile | Path to the test plan that you wish to run |
| OutputDirectory | Path to folder the test results will be placed. Optional. If this is not provided, it will be placed in the `TestOutput` folder. |
| LogLevel | Level for logging (Folllows [this](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.loglevel?view=dotnet-plat-ext-6.0)). Optional. If this is not provided, Information level logs and higher will be logged |
| QueryParams | Specify query parameters to be added to the Power Apps URL. |
| Domain | Specify what URL domain your app uses. This is optional; if not set, it will default to 'apps.powerapps.com'. |
## Config.json

Please view the checked in `config.json` file for the latest format.

Use a `config.dev.json` to prevent accidentally checking in personal info.

### Command line

When provided, command line parameters will override anything specified in `config.dev.json`

| Switch | Parameter |
| -- | -- |
| -i | TestPlanFile |
| -e | EnvironmentId |
| -t | TenantId |
| -o | OutputDirectory |
| -l | LogLevel |
| -q | QueryParams |
| -d | Domain |
