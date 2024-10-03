# Overview

This Power Apps Test Engine sample demonstrates how to clicking button of a canvas application

## Usage

1. Build the Test Engine solution

2. Get the Environment Id and Tenant of the environment that the samplication solution has been imported into

3. Execute the test for custom page changing the example below to the url of your organization using browser persistant cookies

```pwsh
cd bin\Debug\PowerAppsEngine
dotnet PowerAppsTestEngine.dll -i ..\..\..\samples\buttonclicker\testPlan.fx.yaml -e 00000000-0000-0000-0000-11112223333 -t 11112222-3333-4444-5555-666677778888 -u browser -p canvas
```

NOTES:
- If the BrowserCache folder does not exist with valid Persistent Session cookies an interactive login will be required
- After an interactive login has been completed a headless test session can be run