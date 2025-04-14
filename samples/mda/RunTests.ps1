# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# Get current directory so we can reset back to it after running the tests
$currentDirectory = Get-Location

$config = (Get-Content -Path .\config.json -Raw) | ConvertFrom-Json
$tenantId = $config.tenantId
$environmentId = $config.environmentId
$user1Email = $config.user1Email

if ([string]::IsNullOrEmpty($environmentId)) {
    Write-Error "Environment not configured. Please update config.json"
    return
}


$foundEnvironment = $false
$textResult = (pac env select --environment $environmentId)
$textResult = (pac env list)

$environmentUrl = ""

Write-Host "Searching for $environmentId"

foreach ($line in $textResult) {
    if ($line -match $environmentId) {
        if ($line -match "(https://\S+/)") {
            $environmentUrl = $matches[0].Substring(0,$matches[0].Length - 1)
            $foundEnvironment = $true
            break
        }
    }
}

if ($foundEnvironment) {
    Write-Output "Found matching Environment URL: $environmentUrl"
} else {
    Write-Output "Environment ID not found."
    return
}

$mdaUrl = "$environmentUrl/main.aspx?appname=sample_AccountAdmin&pagetype=custom&name=sample_custom_cf8e6"

# Build the latest debug version of Test Engine from source
Set-Location ..\..\src
dotnet build

if ($config.installPlaywright) {
    Start-Process -FilePath "pwsh" -ArgumentList "-Command `"..\bin\Debug\PowerAppsTestEngine\playwright.ps1 install`"" -Wait
} else {
    Write-Host "Skipped playwright install"
}

Set-Location ..\bin\Debug\PowerAppsTestEngine
$env:user1Email = $user1Email
# Run the tests for each user in the configuration file.
dotnet PowerAppsTestEngine.dll -u "storagestate" --provider "mda" -a "none" -i "$currentDirectory\testPlan.fx.yaml" -t $tenantId -e $environmentId -d "$mdaUrl"

# Reset the location back to the original directory.
Set-Location $currentDirectory