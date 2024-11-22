# Overview

Pause the browser and open the Playwright Inspector inside the Power App

## Usage

2. Get the Environment Id and Tenant of the environment that the solution has been imported into

3. Create config.json file using tenant, environment and user1Email

```json
{
    "environmentId": "a0000000-1111-2222-3333-444455556666",
    "tenantId": "ccccdddd-1111-2222-3333-444455556666",
    "installPlaywright": false,
    "user1Email": "test@contoso.onmicosoft.com"
}
```

4. Execute the test

```pwsh
.\RunTests.ps1
```

## Import PCF Component

### Steps for Import PCF Component

1.Set up the config file [more detail](https://github.com/microsoft/PowerApps-TestEngine#import-a-sample-solution).

2.[Enable the Power Apps component framework feature](https://docs.microsoft.com/en-us/power-apps/developer/component-framework/component-framework-for-canvas-apps#enable-the-power-apps-component-framework-feature)

3.Import the solution

4.Play the app

5.Run test for Test Engine
## Important
Make sure you enable the PCF admin setting BEFORE importing the solution. [Enable the Power Apps component framework feature](https://docs.microsoft.com/en-us/power-apps/developer/component-framework/component-framework-for-canvas-apps#enable-the-power-apps-component-framework-feature)

If you didn't have it enabled before, you have to delete the solution, enable the PCF admin setting, re-import the solution.
