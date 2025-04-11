# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# Get current directory so we can reset back to it after running the tests
$currentDirectory = Get-Location

$jsonContent = Get-Content -Path .\config.json -Raw
$config = $jsonContent | ConvertFrom-Json
$tenantId = $config.tenantId
$environmentId = $config.environmentId
$mdaName = $config.mdaName

if ([string]::IsNullOrEmpty($environmentId)) {
    Write-Error "Environment not configured. Please update config.json"
    return
}

$textResult = [string] (pac env list)

$foundEnvironment = $false
$textResult = [string] (pac env select --environment $environmentId)

try{
    $textResult -match "'(https://[^\s']+)'"
    $environmentMatch = $matches
    $foundEnvironment = $true
} catch {
    
}

# Extract the URL using a general regular expression
if ($foundEnvironment -and $environmentMatch.Count -ge 1) {
    $environmentUrl = $environmentMatch[1].TrimEnd("/")
} else {
    Write-Output "URL not found. Please create authentication and re-run script"
    pac auth create --environment $environmentId
    return
}

$customPage = $config.customPage

$mdaUrlList = "$environmentUrl/main.aspx?appname$mdaName&pagetype=entitylist&etn=account"
$mdaUrlCustom = "$environmentUrl/main.aspx?appname$mdaName&pagetype=custom&name=$customPage"

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

Write-Host "======================================================"
Write-Host "User 1 Persona Tests"
Write-Host "======================================================"

$env:user1Email=$config.userEmail1

dotnet PowerAppsTestEngine.dll -u "storagestate" --provider "powerapps.portal" -a "none" -i "$currentDirectory\user1-power-apps-portal.te.yaml" -t $tenantId -e $environmentId -l Debug

dotnet PowerAppsTestEngine.dll -u "storagestate" --provider "canvas" -a "none" -i "$currentDirectory\canvas-no-powerapps-licence.te.yaml" -t $tenantId -e $environmentId -l Debug

dotnet PowerAppsTestEngine.dll -u "storagestate" --provider "mda" -a "none" -i "$currentDirectory\entity-list-no-permissions.te.yaml" -t $tenantId -e $environmentId -d "$mdaUrlList" -l Debug
dotnet PowerAppsTestEngine.dll -u "storagestate" --provider "mda" -a "none" -i "$currentDirectory\custom-page-no-permissions.te.yaml" -t $tenantId -e $environmentId -d "$mdaUrlCustom" -l Debug

Write-Host "======================================================"
Write-Host "User 2 Persona Tests"
Write-Host "======================================================"

$env:user2Email=$config.userEmail2
dotnet PowerAppsTestEngine.dll -u "storagestate" --provider "powerapps.portal" -a "none" -i "$currentDirectory\user2-power-apps-portal.te.yaml" -t $tenantId -e $environmentId -l Debug

dotnet PowerAppsTestEngine.dll -u "storagestate" --provider "canvas" -a "none" -i "$currentDirectory\canvas-not-shared.te.yaml" -t $tenantId -e $environmentId -l Debug

# Reset the location back to the original directory.
Set-Location $currentDirectory