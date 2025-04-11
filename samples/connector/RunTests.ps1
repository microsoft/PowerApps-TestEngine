# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# Get current directory so we can reset back to it after running the tests
$currentDirectory = Get-Location

$config = Get-Content -Path .\config.json -Raw  | ConvertFrom-Json
$tenantId = $config.tenantId
$environmentId = $config.environmentId
$user1Email = $config.user1Email

if ([string]::IsNullOrEmpty($environmentId)) {
    Write-Error "Environment not configured. Please update config.json"
    return
}

# Build the latest debug version of Test Engine from source
Set-Location ..\..\src
dotnet build

if ($config.installPlaywright) {
    Start-Process -FilePath "pwsh" -ArgumentList "-Command `"..\bin\Debug\PowerAppsTestEngine\playwright.ps1 install`"" -Wait
} else {
    Write-Host "Skipped playwright install"
}

Set-Location ..\bin\Debug\PowerAppsTestEngine

Copy-Item -Path "$currentDirectory\response.json" -Destination "." -Force

$env:user1Email = $user1Email
# Run the tests for each user in the configuration file.
dotnet PowerAppsTestEngine.dll -u "storagestate" --provider "canvas" -a "none" -i "$currentDirectory\testPlan.fx.yaml" -t $tenantId -e $environmentId
dotnet PowerAppsTestEngine.dll -u "storagestate" --provider "canvas" -a "none" -i "$currentDirectory\testPlan-simulated.fx.yaml" -t $tenantId -e $environmentId

# Reset the location back to the original directory.
Set-Location $currentDirectory