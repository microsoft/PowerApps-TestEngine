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

if ([string]::IsNullOrEmpty($pac)) {
    # Build the latest configuration version of Test Engine from source
    Set-Location ../../src
    dotnet restore -p:TargetFramework=net8.0
    dotnet build --configuration $configuration
    
    if ($config.installPlaywright) {
        if ($IsLinux) {
            Start-Process -FilePath "pwsh" -ArgumentList "-Command `"../bin/$configuration/PowerAppsTestEngine/playwright.ps1 install --with-deps`"" -Wait    
        } else {
            Start-Process -FilePath "pwsh" -ArgumentList "-Command `"../bin/$configuration/PowerAppsTestEngine/playwright.ps1 install`"" -Wait
        }
    } else {
        Write-Host "Skipped playwright install"
    }

    Set-Location "../bin/$configuration/PowerAppsTestEngine"
}

$env:user1Email = $user1Email

if ([string]::IsNullOrEmpty($pac)) {
    # Run the tests for each user in the configuration file.
    dotnet PowerAppsTestEngine.dll -p "dataverse" -a "none" -i "$currentDirectory/testPlan.fx.yaml" -t $tenantId -e $environmentId -d "$environmentUrl" -l Debug -w "True"
} else {
    & $pac test run -p "dataverse" -a "none" --test-run-file "$currentDirectory/testPlan.fx.yaml" -t $tenantId -e $environmentId -d "$environmentUrl"
}

# Reset the location back to the original directory.
Set-Location $currentDirectory