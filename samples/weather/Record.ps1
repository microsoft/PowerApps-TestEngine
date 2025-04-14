# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# Get current directory so we can reset back to it after running the tests
$currentDirectory = Get-Location

$jsonContent = Get-Content -Path .\config.json -Raw
$config = $jsonContent | ConvertFrom-Json
$tenantId = $config.tenantId
$environmentId = $config.environmentId
$user1Email = $config.user1Email
$appDescription = $config.appDescription

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

$token = (az account get-access-token --resource $environmentUrl | ConvertFrom-Json)

$uri = "$environmentUrl/api/data/v9.1/systemusers?`$filter=internalemailaddress eq '$user1Email'"
$response = Invoke-RestMethod -Uri $uri -Method Get -Headers @{Authorization = "Bearer $($token.accessToken)"}
$userId = $response.value.systemuserid

Write-Host $userId

$uri = "$environmentUrl/api/data/v9.1/usersettingscollection($userId)"
$body = @{
    uilanguageid = 1033  # English
} | ConvertTo-Json
Invoke-RestMethod -Uri $uri -Method Patch -Headers @{Authorization = "Bearer $($token.accessToken)"; "Content-Type" = "application/json"} -Body $body

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
    Write-Error "App id not found. Check that the $appDescription has been installed"
    return
}

$customPage = $config.customPage

$mdaUrl = "$environmentUrl/main.aspx?appid=$appId&pagetype=custom&name=$customPage"

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

dotnet PowerAppsTestEngine.dll -u "storagestate" --provider "mda" -a "none" -r True -i "$currentDirectory\record.fx.yaml" -t $tenantId -e $environmentId -d "$mdaUrl" 

$uri = "$environmentUrl/api/data/v9.1/usersettingscollection($userId)"
$body = @{
    uilanguageid = 1036  # French
} | ConvertTo-Json
Invoke-RestMethod -Uri $uri -Method Patch -Headers @{Authorization = "Bearer $($token.accessToken)"; "Content-Type" = "application/json"} -Body $body

dotnet PowerAppsTestEngine.dll -u "storagestate" --provider "mda" -a "none" -r True -i "$currentDirectory\record.fx.yaml" -t $tenantId -e $environmentId -d "$mdaUrl" 

# Reset the location back to the original directory.
Set-Location $currentDirectory