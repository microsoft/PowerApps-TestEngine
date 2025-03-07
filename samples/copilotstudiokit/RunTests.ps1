# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# Get current directory so we can reset back to it after running the tests
$currentDirectory = Get-Location

$jsonContent = Get-Content -Path .\config.json -Raw
$config = $jsonContent | ConvertFrom-Json
$tenantId = $config.tenantId
$environmentId = $config.environmentId
$user1Email = $config.user1Email
$user1Email = $config.user1Email
$record = $config.record
$compile = $config.compile

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

$appId = ""
try{
    $runResult = pac pfx run --file .\GetAppId.powerfx --echo
    $appId = $runResult[8].Split('"')[1] -replace '[^a-zA-Z0-9-]', ''
} catch {

}

if ([string]::IsNullOrEmpty($appId)) {
    Write-Error "App id not found. Check that the CoE Starter kit has been installed"
    return
}

$customPage = $config.customPage
$mdaUrl = "$environmentUrl/main.aspx?appid=$appId&pagetype=custom&name=$customPage"

# Build the latest debug version of Test Engine from source
Set-Location ..\..\src
if ($compile) {
    Write-Host "Compiling the project..."
    dotnet build
} else {
    Write-Host "Skipping compilation..."
}

if ($config.installPlaywright) {
    Start-Process -FilePath "pwsh" -ArgumentList "-Command `"..\bin\Debug\PowerAppsTestEngine\playwright.ps1 install`"" -Wait
} else {
    Write-Host "Skipped playwright install"
}

Set-Location ..\bin\Debug\PowerAppsTestEngine
$env:user1Email = $user1Email

if ($record) {
    Write-Host "========================================" -ForegroundColor Orange
    Write-Host "RECODE MODE" -ForegroundColor Orange
    Write-Host "========================================" -ForegroundColor Orange
}

Write-Host "========================================" -ForegroundColor Green
Write-Host "Entity List" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

Write-Host "----------------------------------------" -ForegroundColor Green
Write-Host "Agent List" -ForegroundColor Green
Write-Host "----------------------------------------" -ForegroundColor Green

$entityListUrl = "$environmentUrl/main.aspx?appid=$appId&pagetype=entitylist&etn=cat_copilotconfiguration&viewid=77dec5f3-551c-ef11-840b-6045bdd6c0ee&viewType=1039"
if ($record) {
    dotnet PowerAppsTestEngine.dll -u "storagestate" -p "mda" -a "none"  -r "True" -i "$currentDirectory\agents-list.te.yaml" -t $tenantId -e $environmentId -d "$entityListUrl" -l Debug
} else {
    dotnet PowerAppsTestEngine.dll -u "storagestate" -p "mda" -a "none" -i "$currentDirectory\agents-list.te.yaml" -t $tenantId -e $environmentId -d "$entityListUrl" -l Debug
}

Write-Host "----------------------------------------" -ForegroundColor Green
Write-Host "Agent Details" -ForegroundColor Green
Write-Host "----------------------------------------" -ForegroundColor Green

$entityDetails = "$environmentUrl/main.aspx?appid=$appId&pagetype=entityrecord&etn=cat_copilotconfiguration&id=af89dc3e-f1fa-ef11-bae2-7c1e5246ee31"
                                                      
if ($record) {
    dotnet PowerAppsTestEngine.dll -u "storagestate" -p "mda" -a "none"  -r "True" -i "$currentDirectory\agents-list.te.yaml" -t $tenantId -e $environmentId -d "$entityDetails" -l Debug
} else {
    dotnet PowerAppsTestEngine.dll -u "storagestate" -p "mda" -a "none" -i "$currentDirectory\agents-list.te.yaml" -t $tenantId -e $environmentId -d "$entityDetails" -l Debug
}

Write-Host "========================================" -ForegroundColor Green
Write-Host "Custom Pages" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

Write-Host "----------------------------------------" -ForegroundColor Green
Write-Host "Web Chat Playground" -ForegroundColor Green
Write-Host "----------------------------------------" -ForegroundColor Green

if ($record) {
    # Run the tests for each user in the configuration file.
    dotnet PowerAppsTestEngine.dll -u "storagestate" -p "mda" -a "none"  -r "True" -i "$currentDirectory\webChat-playground.te.yaml" -t $tenantId -e $environmentId -d "$mdaUrl" -l Debug
} else {
    Write-Host "Skipped recording"
    # Run the tests for each user in the configuration file.
    dotnet PowerAppsTestEngine.dll -u "storagestate" -p "mda" -a "none" -i "$currentDirectory\webChat-playground.te.yaml" -t $tenantId -e $environmentId -d "$mdaUrl" -l Debug
}

# Reset the location back to the original directory.
Set-Location $currentDirectory