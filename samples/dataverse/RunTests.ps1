# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# Get current directory so we can reset back to it after running the tests
$currentDirectory = Get-Location

$config = Get-Content -Path .\config.json -Raw  | ConvertFrom-Json
$tenantId = $config.tenantId
$environmentId = $config.environmentId
$environmentUrl = $config.environmentUrl
$user1Email = $config.user1Email
$buildFromSource = $config.buildFromSource

if ([string]::IsNullOrEmpty($environmentId)) {
    Write-Error "Environment not configured. Please update config.json"
    return
}

if ([string]::IsNullOrEmpty($environmentUrl)) {
    Write-Error "Environment Url not configured. Please update config.json"
    return
}

# Build the latest debug version of Test Engine from source
Set-Location ..\..\src
if ( $buildFromSource ) {
    Write-Host "Building from source"
    dotnet build
} else {
    Write-Host "Skipped building from source, will use the latest debug version"
}

if ($config.installPlaywright) {
    Start-Process -FilePath "pwsh" -ArgumentList "-Command `"..\bin\Debug\PowerAppsTestEngine\playwright.ps1 install`"" -Wait
} else {
    Write-Host "Skipped playwright install"
}

Set-Location ..\bin\Debug\PowerAppsTestEngine
$env:user1Email = $user1Email

# Run the tests for each user in the configuration file.
Write-Host "------------------------------------------------------------" -ForegroundColor Green
Write-Host "Dateverse Tests (Account Entity)" -ForegroundColor Green
Write-Host "------------------------------------------------------------" -ForegroundColor Green

dotnet PowerAppsTestEngine.dll -u "storagestate" -p "powerfx" -a "none" -i "$currentDirectory\testPlan.fx.yaml" -t $tenantId -e $environmentId -d $environmentUrl

Write-Host "------------------------------------------------------------" -ForegroundColor Green
Write-Host "Dateverse AI Tests (AI Builder)" -ForegroundColor Green
Write-Host "------------------------------------------------------------" -ForegroundColor Green

dotnet PowerAppsTestEngine.dll -u "storagestate" -p "powerfx" -a "none" -i "$currentDirectory\ai-prompt.fx.yaml" -t $tenantId -e $environmentId -d $environmentUrl 

# Reset the location back to the original directory.
Set-Location $currentDirectory