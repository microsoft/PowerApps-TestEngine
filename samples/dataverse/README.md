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

## Alternate Build

If you do not have the .Net 8.0 SDK installed you can also try using podman to build the solution

1. You can install Podman using [Podman Installation Instructions](https://podman.io/docs/installation)

2. Create a podman machine if you have not already

```pwsh
podman machine init
```

3. Run the image

```pwsh
$grandParentLocation = Resolve-Path "$(Get-Location)\..\.."
podman run -v ${grandParentLocation}:/app mcr.microsoft.com/dotnet/sdk:8.0 sh -c "cd /app/src && dotnet build"
```

## Usage

1. Get the Environment Id and Tenant of the environment that the solution has been imported into

2. Create config.json file using tenant, environment and user1Email

```json
{
    "environmentId": "a0000000-1111-2222-3333-444455556666",
    "environmentUrl": "https://contoso.crm.dynamics.com/",
    "tenantId": "ccccdddd-1111-2222-3333-444455556666",
    "installPlaywright": false,
    "buildFromSource": true,
    "user1Email": "test@contoso.onmicosoft.com"
}
```

3. Execute the test

```pwsh
.\RunTests.ps1
```
