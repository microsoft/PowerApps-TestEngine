# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# Get current directory so we can reset back to it after running the tests
$currentDirectory = Get-Location

$jsonContent = Get-Content -Path .\config.json -Raw
$config = $jsonContent | ConvertFrom-Json
$tenantId = $config.tenantId
$environmentId = $config.environmentId
$environmentUrl = $config.environmentUrl
$user1Email = $config.user1Email
$compile = $config.compile
$repository = $config.repository

$azTenantId = az account show --query tenantId --output tsv

if ($azTenantId -ne $tenantId) {
    Write-Error "Tenant ID mismatch. Please check your Azure CLI context."
    return
}

$token = (az account get-access-token --resource $environmentUrl | ConvertFrom-Json)

if ($token -eq $null) {
    Write-Error "Failed to obtain access token. Please check your Azure CLI context."
    return
}

Set-Location "$currentDirectory\..\..\src"
if ($compile) {
    Write-Host "Compiling the project..."
    dotnet build
} else {
    Write-Host "Skipping compilation..."
}

Set-Location "$currentDirectory\..\..\bin\Debug\PowerAppsTestEngine"

$env:TEST_ENGINE_SOLUTION_PATH = $repository

# Run the tests for each user in the configuration file.
dotnet PowerAppsTestEngine.dll -p "mcp" -i "$currentDirectory\start.te.yaml" -t $tenantId -e $environmentId -d "$environmentUrl"

Set-Location "$currentDirectory"