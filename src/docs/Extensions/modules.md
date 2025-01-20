# Test Engine Modules

Test Engine extensibility modules are an experimental features that is being considered to allow extension of Test Engine tests by making use of [Managed Extensibility Framework (MEF)](https://learn.microsoft.com/en-us/dotnet/framework/mef/) to allow .Net libraries to be created to extend test execution.

NOTE: This feature is subject to change and review and is very likely to change.

## Execution

Execution of any defined plugins is disabled by default and must be enabled in test settings. The extension sample [testPlan.fx.yaml](../../samples/extensions/testPlan.fx.yaml) demonstrates enable extensions to run the Sample PowerFx function.

By convention testengine.module.*.dll files in the same folder as the Power Apps Test engine folder will be loaded is extenions modules are enabled. Using the [Test Settings Extensions](..\..\src\Microsoft.PowerApps.TestEngine\Config\TestSettingExtensions.cs) you can enable and control allowed plugins and namespaces with the plugins.

## Getting Started

To run the sample extension

1. Ensure that you have compiled the solution. For example to build the debug version of the Test Engine

```powershell
cd src
dotnet build
```

2. Ensure you have correct version of playwright installed

```powershell
cd ..\bin\PowerAppTestEngine
.\playwright.ps1 install
```

3. Get the values for your environment and tenant id from the [Power Apps Portal](http://make.powerapps.com). See [Get the session ID for Power Apps](https://learn.microsoft.com/power-apps/maker/canvas-apps/get-sessionid#get-the-session-id-for-power-apps-makepowerappscom) for more information.

4. Ensure you have the [button clicker solution](..\..\samples\buttonclicker\ButtonClicker_1_0_0_4.zip) imported into your environment

5. Update the [config.dev.json](..\..\src\PowerAppsTestEngine\config.dev.json) environment id and tenant id

5. Run the sample

```powershell
cd samples\pause
dotnet ..\..\bin\Debug\PowerAppsTestEngine\PowerAppsTestEngine.dll -i testPlan.fx.yaml
```

## Exploring Samples

To enable the Pause PowerFx function the testengine.module.pause library uses the [PauseModule](..\..\src\testengine.module.pause\PauseModule.cs) class to implement a MEF module to register the Power Fx function.

## Configuring extensions

The configuration settings allow you to have finer grained control of what modules/actions you want to allow or deny.

### Deny Module Load

Extensions have a number of test settings that can be provided to control which extensions can be executed. The [testPlan-denyModule.fx.yaml](..\..\samples\extensions\testPlan-denyModule.fx.yaml) example demonstrates how to deny loading the sample module. When this sample is run it will fail as the Sample() Power Fx function will not be loaded.

### Deny .Net Namespace

In some cases you may want to deny specific commands. The [testPlan-denyCommand.fx.yaml](..\..\samples\extensions\testPlan-denyModule.fx.yaml) example demonstrates load of modules that do not match the defined rule. When this sample is run it will fail as the Sample() Power Fx function will not be loaded as it uses System.Console.

This example can also be extended to specific methods for example System.Conole::WriteLine as part of the namespace rules to deny (or allow) a specific method. The [testPlan-enableOnlyWriteLine.fx.yaml](..\..\samples\extensions\testPlan-enableOnlyWriteLine.fx.yaml) example demonstrates this configuration to to have a passing test.

## Further Extensions

Once you have explored Power Fx and Test Settings the following additional areas can be explored:

- Extending the Network Mocking with a module using [RouteAsync](https://playwright.dev/dotnet/docs/api/class-browsercontext#browser-context-route) to Abort, Fulfill from a local file or Continue

- Additional extensions could be considered by implementing the defined interfaces an making contributions and pull requests to increase use of Test Engine to cover additional test scenarios.
