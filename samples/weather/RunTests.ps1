# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# Get current directory so we can reset back to it after running the tests
$currentDirectory = Get-Location

$jsonContent = Get-Content -Path ./config.json -Raw
$config = $jsonContent | ConvertFrom-Json
$tenantId = $config.tenantId
$environmentId = $config.environmentId
$environmentUrl = $config.environmentUrl
$user1Email = $config.user1Email
$appDescription = $config.appDescription
$languages = $config.languages
$env:DataProtectionUrl = $config.DataProtectionUrl
$env:DataProtectionCertificateName = $config.DataProtectionCertificateName
$configuration = $config.configuration
$auth = $config.auth
$authstate = $config.authstate

if ([string]::IsNullOrEmpty($configuration)) {
    $configuration = "Debug"
}

if ([string]::IsNullOrEmpty($authstate)) {
    $authstate = "dataverse"
}

if (-not [string]::IsNullOrEmpty($authstate)) {
    switch ( $authstate ) {
        "dataverse" {
            break;
        }
        "storagestate" {
            break;
        }
        default {
            Write-Error "Invalid auth state $authstate"
            return
        }
    }
}

if ([string]::IsNullOrEmpty($auth)) {
    $auth = "certstore"
}

if (("certenv" -eq $auth) -and ([string]::IsNullOrEmpty($env:DataProtectionCertificateName))) {
    Write-Error "Environment variable DataProtectionCertificateName does not exist"
    return
}

if (-not [string]::IsNullOrEmpty($environmentUrl)) {
    $foundEnvironment = $true
}

if ($foundEnvironment) {
    Write-Output "Found matching Environment URL: $environmentUrl"
} else {
    Write-Output "Environment ID not found."
    return
}

# Select the environment
pac env select --environment $environmentUrl

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

$appId = ""
try{
    $file = (Join-Path -Path $currentDirectory -ChildPath "GetAppId.powerfx")
    $runResult = pac pfx run --environment $environmentUrl --file $file --echo
    $appId = $runResult[8].Split('"')[1] -replace '[^a-zA-Z0-9-]', ''
} catch {

}

if ([string]::IsNullOrEmpty($appId)) {
    Write-Error "App id not found. Check that the $appDescription has been installed"
    return
}

$customPage = $config.customPage
$mdaUrl = "$environmentUrl/main.aspx?appid=$appId&pagetype=custom&name=$customPage"

if ([string]::IsNullOrEmpty($pac)) {
    # Build the latest configuration version of Test Engine from source
    Set-Location ../../src
    dotnet build --configuration $configuration

    if ($config.installPlaywright) {
        Start-Process -FilePath "pwsh" -ArgumentList "-Command `"../bin/$configuration/PowerAppsTestEngine/playwright.ps1 install`"" -Wait
    } else {
        Write-Host "Skipped playwright install"
    }

    Set-Location "../bin/$configuration/PowerAppsTestEngine"
}

$env:user1Email = $user1Email

if ($null -eq $languages) {
    if ([string]::IsNullOrEmpty($pac)) {
        # Run the tests for each user in the configuration file.
        dotnet PowerAppsTestEngine.dll -u "dataverse" -p "mda" -a $auth -i "$currentDirectory/testPlan.fx.yaml" -t $tenantId -e $environmentId -d "$mdaUrl" -l Debug
    } else {

    }
} else {
    foreach ($language in $languages) {
        $uri = "$environmentUrl/api/data/v9.1/usersettingscollection($userId)"
        $body = @{
            uilanguageid = $language.id
        } | ConvertTo-Json
        Invoke-RestMethod -Uri $uri -Method Patch -Headers @{Authorization = "Bearer $($token.accessToken)"; "Content-Type" = "application/json"} -Body $body

        $languageId = $language.id
        $languageName = $language.name
        $languageFile = $language.file

        $languageTest = "$currentDirectory/testPlan-${languageId}.fx.yaml"
        Copy-Item "$currentDirectory/$languageFile" $languageTest
        $text = Get-Content  $languageTest 
        $text = $text.Replace("locale: ""en-US""", "locale: ""${languageName}""")
        Set-Content -Path  $languageTest -Value $text 

        dotnet PowerAppsTestEngine.dll -u "dataverse" -p "mda" -a $auth -i "$languageTest" -t $tenantId -e $environmentId -d "$mdaUrl" -l Debug -w True
    }
}

# Reset the location back to the original directory.
Set-Location $currentDirectory