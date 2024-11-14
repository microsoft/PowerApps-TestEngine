# Overview

This Power Apps Test Engine sample demonstrates how interact with containers of canvas application

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

## Date_Case and DateTime_Case

Please note that the DatePicker control shows date according to your system timezone.

The dates used in the test plan here is written for EST timezone. 

If you are in a different timezone, make sure to play/open the app to verify the date shown and make any corrections to the test plan if needed.