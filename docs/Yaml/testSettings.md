# testSettings

This is used to define settings for the tests in the test plan

## YAML schema definition

| Property | Required | Description |
| -- | -- | -- |
| browserConfigurations | Yes | A list of browser configurations to be tested. At least one browser must be specified. |
| recordVideo | No | Default is false. If set to true, a video recording of the test is captured |
| enablePowerFxOverlay | No | Default is false. If set to true, an overlay with the currently running Power FX command is placed on the screen |

### Browser configuration

| Property | Required | Description |
| -- | -- | -- |
| browser | Yes | The browser to be launched when testing. This should match the [browsers supported by Playwright(https://playwright.dev/dotnet/docs/browsers)]. |
| device | No | The device to emulate when launching the browser. This should match the [devices supported by Playwright](https://playwright.dev/dotnet/docs/api/class-playwright#playwright-devices)
| screenHeight | No | The height of the screen to use when launching the browser. If specified, screenWidth must also be specified. |
| screenWidth | No | The width of the screen to use when launching the browser. If specified, screenHeight must also be specified.|
