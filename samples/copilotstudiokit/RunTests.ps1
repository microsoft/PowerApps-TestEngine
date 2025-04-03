# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# Get current directory so we can reset back to it after running the tests
$currentDirectory = Get-Location

$jsonContent = Get-Content -Path .\config.json -Raw
$config = $jsonContent | ConvertFrom-Json
$tenantId = $config.tenantId
$environmentId = $config.environmentId
$user1Email = $config.user1Email
$record = $config.record
$compile = $config.compile
# Extract pages and corresponding Test Scripts
$customPages = $config.pages.customPages
$entities = $config.pages.entities
$testScripts = $config.testScripts

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
    Write-Host "========================================" -ForegroundColor Blue
    Write-Host "RECODE MODE" -ForegroundColor Blue
    Write-Host "========================================" -ForegroundColor Blue
}

$token = (az account get-access-token --resource $environmentUrl | ConvertFrom-Json)

Write-Host "========================================" -ForegroundColor Green
Write-Host "ENTITIES" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

# Loop through Entity (List and details) and Execute Tests
foreach ($entity in $entities) {
    Write-Host "----------------------------------------" -ForegroundColor Yellow
    Write-Host $entity.name -ForegroundColor Yellow
    Write-Host "----------------------------------------" -ForegroundColor Yellow

    $formName = $entity.name

    if ($config.pages.list) {
        $matchingScript = "$formName-list.te.yaml"

        if (-not (Test-Path -Path "$currentDirectory\$matchingScript") ) {
            Write-Host "No matching test script found for: $matchingScript"
            continue
        }

        $entityName = $entity.entity
        $viewName = $entity.view

        # Query the saved query ID for the entity and view name
        $lookup = "$environmentUrl/api/data/v9.2/savedqueries?`$filter=returnedtypecode%20eq%20%27$entityName%27 and name eq %27$viewName%27&`$select=savedqueryid"
        $response = Invoke-RestMethod -Uri $lookup -Method Get -Headers @{Authorization = "Bearer $($token.accessToken)"}

        $viewId = $response.value.savedqueryid

        $mdaUrl = "$environmentUrl/main.aspx?appid=$appId&pagetype=entitylist&etn=$entityName&viewid=$viewId&viewType=1039"
        if ($record) {
            # Run the tests for each user in the configuration file.
            dotnet PowerAppsTestEngine.dll -u "storagestate" -p "mda" -a "none"  -r "True" -i "$currentDirectory\$matchingScript" -t $tenantId -e $environmentId -d "$mdaUrl" -l Debug
        } else {
            Write-Host "Skipped recording"
            # Run the tests for each user in the configuration file.
            dotnet PowerAppsTestEngine.dll -u "storagestate" -p "mda" -a "none" -i "$currentDirectory\$matchingScript" -t $tenantId -e $environmentId -d "$mdaUrl" -l Debug
        }
    }

    if ($config.pages.details) {
        $matchingScript = "$formName-details.te.yaml"

        if (-not (Test-Path -Path "$currentDirectory\$matchingScript") ) {
            Write-Host "No matching test script found for: $matchingScript"
            continue
        }

        # Query the record ID for the entity 
        $entityNamePlural = $entityName + "s"

        
        $idColumn = $entity.id
        $lookup = "$environmentUrl/api/data/v9.2/$entityNamePlural`?`$top=1&`$select=$idColumn"   

        $entityResponse = Invoke-RestMethod -Uri $lookup -Method Get -Headers @{Authorization = "Bearer $($token.accessToken)"}
        $recordId = $entityResponse.value | Select-Object -ExpandProperty $idColumn
    
        $mdaUrl = "$environmentUrl/main.aspx?appid=$appId&pagetype=entityrecord&etn=$entityName&id=$recordId"
        if ($record) {
            # Run the tests for each user in the configuration file.
            dotnet PowerAppsTestEngine.dll -u "storagestate" -p "mda" -a "none"  -r "True" -i "$currentDirectory\$matchingScript" -t $tenantId -e $environmentId -d "$mdaUrl" -l Debug
        } else {
            Write-Host "Skipped recording"
            # Run the tests for each user in the configuration file.
            dotnet PowerAppsTestEngine.dll -u "storagestate" -p "mda" -a "none" -i "$currentDirectory\$matchingScript" -t $tenantId -e $environmentId -d "$mdaUrl" -l Debug
        }
    }
}

if ($config.pages.customPage) {
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "CUSTOM PAGES" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green

    # Loop through Custom Pages and Execute Tests
    foreach ($customPage in $customPages) {

        # Ensure testScripts is an array
        $testScriptList = @($testScripts.customPageTestScripts)  # Extract values explicitly

        function Split-CustomPageName {
            param (
                [string]$inputString
            )
            # Step 1: Split by underscore or hyphen
            $words = $inputString -split "[-_]" 
            return $words
        }

        function Get-MatchingTestScript {
            param (
                [string[]]$wordList,      # List of words extracted from the custom page name
                [string[]]$testScriptList # List of available test script names
            )
            foreach ($script in $testScriptList) {
                foreach ($word in $wordList) {
                    if ($script -match [regex]::Escape($word)) {
                        return $script  # Return the first matching script
                    }
                }
            }
            return $null  # Return null if no match is found
        }
        $wordList= Split-CustomPageName -inputString $customPage

        $matchingScript = Get-MatchingTestScript -wordList $wordList -testScriptList $testScriptList

        if (-not $matchingScript) {
            Write-Host "No matching test script found for custom page: $customPage" -ForegroundColor Red
            continue  # Skip this iteration if no matching script is found
        }

        Write-Host "----------------------------------------" -ForegroundColor Yellow
        Write-Host $matchingScript -ForegroundColor Yellow
        Write-Host "----------------------------------------" -ForegroundColor Yellow

        $mdaUrl = "$environmentUrl/main.aspx?appid=$appId&pagetype=custom&name=$customPage"
        if ($record) {
            # Run the tests for each user in the configuration file.
            dotnet PowerAppsTestEngine.dll -u "storagestate" -p "mda" -a "none"  -r "True" -i "$currentDirectory\$matchingScript" -t $tenantId -e $environmentId -d "$mdaUrl" -l Debug
        } else {
            Write-Host "Skipped recording"
            # Run the tests for each user in the configuration file.
            dotnet PowerAppsTestEngine.dll -u "storagestate" -p "mda" -a "none" -i "$currentDirectory\$matchingScript" -t $tenantId -e $environmentId -d "$mdaUrl" -l Debug
        }
    }

    Write-Host "All custompages executed successfully!"
}
# Reset the location back to the original directory.
Set-Location $currentDirectory