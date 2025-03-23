# Overview

This Power Apps Test Engine sample demonstrates how to create integration tests with Dataverse using only Power Fx

## Getting Started

To run the following sample

1. Install the [Azure Command Line Interface](https://learn.microsoft.com/cli/azure/install-azure-cli)

2. Ensure that you are logged in to Azure CLI 

```pwsh
az login --use-device-code --allow-no-subscriptions
```

3. Install the [.Net 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) 

4. Install [Power Shell](https://learn.microsoft.com/powershell/scripting/install/installing-powershell?view=powershell-7.5)

## Usage

1. Get the Environment Id and Tenant of the environment that the solution has been imported into

2. Create config.json file using tenant, environment and user1Email

```json
{
    "environmentId": "a0000000-1111-2222-3333-444455556666",
    "environmentUrl": "https://contoso.crm.dynamics.com/",
    "tenantId": "ccccdddd-1111-2222-3333-444455556666",
    "installPlaywright": false,
    "user1Email": "test@contoso.onmicosoft.com"
}
```

3. Execute the test

```pwsh
.\RunTests.ps1
```
