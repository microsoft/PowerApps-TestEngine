# testSettings

This is used to define settings for the tests in the test plan

## YAML schema definition

| Property | Required | Description |
| -- | -- | -- |
| locale | Yes | The locale/culture syntax in which the test cases or test steps are written in. See [Global Support in Microsoft Power Fx](https://learn.microsoft.com/en-us/power-platform/power-fx/global) for more info. If unspecified, `CultureInfo.CurrentCulture` will be used for the locale by default for parsing the test steps. |
| browserConfigurations | Yes | A list of browser configurations to be tested. At least one browser must be specified. |
| recordVideo | No | Default is false. If set to true, a video recording of the test is captured. |
| headless | No | Default is true. If set to false, the browser will show up during test execution. |
| timeout | No | Default is 30000 milliseconds(30s). Timeout value in milliseconds. If any operation takes longer than the timeout limit, it will end the test in a failure. |
| filePath | No |  The file path to a separate yaml file with all the test settings. If provided, it will **override** all the test settings in the test plan. |

### Browser configuration

| Property | Required | Description |
| -- | -- | -- |
| browser | Yes | The browser to be launched when testing. This should match the [browsers supported by Playwright](https://playwright.dev/dotnet/docs/browsers). |
| device | No | The device to emulate when launching the browser. This should match the [devices supported by Playwright](https://playwright.dev/dotnet/docs/api/class-playwright#playwright-devices)
| screenHeight | No | The height of the screen to use when launching the browser. If specified, screenWidth must also be specified. |
| screenWidth | No | The width of the screen to use when launching the browser. If specified, screenHeight must also be specified.|
