# Overview

This Microsoft Copilot Studio sample demonstrates how to interact with the Test actions of a Safe Travels Agent

## Usage

1. Login to the Environment and ensure you have completed the getting started dialog

2. Get the Environment Id and Tenant of the environment that the agent has been created in

3. Create config.json file using tenant, environment and user1Email

```json
{
    "environmentId": "a0000000-1111-2222-3333-444455556666",
    "tenantId": "ccccdddd-1111-2222-3333-444455556666",
    "installPlaywright": false,
    "user1Email": "test@contoso.onmicosoft.com"
}
```

4. Change the app id of your deployed Safe travel app in the testPlan.fx.yaml file

5. Execute the test

```pwsh
.\RunTests.ps1
```
