# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# Get current directory so we can reset back to it after running the tests
$currentDirectory = Get-Location

$config = Get-Content -Path .\config.json -Raw | ConvertFrom-Json
$tenantId = $config.tenantId
$environmentId = $config.environmentId
$user1Email = $config.user1Email

# Build the latest debug version of Test Engine from source
Set-Location ..\..\src
dotnet build

if ($config.installPlaywright) {
    Start-Process -FilePath "pwsh" -ArgumentList "-Command `"..\bin\Debug\PowerAppsTestEngine\playwright.ps1 install`"" -Wait
} else {
    Write-Host "Skipped playwright install"
}

Set-Location ..\bin\Debug\PowerAppsTestEngine
# Run the tests for each user in the configuration file.
$env:user1Email = $user1Email
dotnet PowerAppsTestEngine.dll -u "storagestate" --provider "canvas" -a "none" -r True -i "$currentDirectory\recordCanvas.fx.yaml" -t $tenantId -e $environmentId -l Trace -w True

# Reset the location back to the original directory.
Set-Location $currentDirectory