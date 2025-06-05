# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# Script: RunTests.ps1
# This script runs tests for Copilot Studio Kit applications.
# It generates an Azure DevOps style HTML report by default, with optional fallback to classic report style.

# 
# Usage examples:
#   .\RunTests.ps1                                              # Run all tests with Azure DevOps style report
#   .\RunTests.ps1 -entityFilter "account"                      # Run only tests related to the account entity
#   .\RunTests.ps1 -pageTypeFilter "entitylist"                 # Run only list view tests
#   .\RunTests.ps1 -pageTypeFilter "entityrecord"               # Run only details view tests
#   .\RunTests.ps1 -pageTypeFilter "custom"                     # Run only custom page tests
#   .\RunTests.ps1 -pageTypeFilter "entitylist","entityrecord"  # Run both list and details tests (no custom pages)
#   .\RunTests.ps1 -pageTypeFilter @("entitylist","custom")     # Run list and custom page tests (no details)
#   .\RunTests.ps1 -customPageFilter "dashboard"                # Run only custom pages with "dashboard" in the name
#   .\RunTests.ps1 -startTime "2025-05-20 09:00"                # Run tests and show results since 2025-05-20 09:00
#   .\RunTests.ps1 -startTime "2025-05-20 09:00" -endTime "2025-05-24 09:00"  # Generate report from existing test runs between the specified dates without executing tests
#   .\RunTests.ps1 -testEngineBranch "feature/my-branch"        # Use a specific branch of PowerApps-TestEngine
#   .\RunTests.ps1 -generateReportOnly                          # Generate report from existing test data without running tests
#
# Multiple filters can be combined:
#   .\RunTests.ps1 -entityFilter "account" -pageTypeFilter "entityrecord"

# Check for optional command line arguments
param (
    [string]$startTime, # Start time for the test results to include in the report
    [string]$endTime, # End time for the test results to include in the report (when both startTime and endTime are provided, tests are not executed)
    [string]$entityFilter, # Filter tests by entity name
    [string[]]$pageTypeFilter, # Filter by page type(s) (list, details, custom) - can be multiple values
    [string]$customPageFilter, # Filter by custom page name
    [string]$testEngineBranch = "user/grant-archibald-ms/report-594", # Optional branch to use for PowerApps-TestEngine
    [switch]$forceRebuild, # Force rebuild of PowerApps-TestEngine even if it exists
    [switch]$generateReportOnly, # Only generate a report without running tests
    [switch]$useStaticContext = $false, # Use static context for test execution
    [switch]$usePacTest = $false # Use 'pac test run' instead of direct PowerAppsTestEngine.dll execution
)


# Function to execute a test, either using PowerAppsTestEngine.dll directly or pac test run
# Note: Report generation is always handled by PowerAppsTestEngine.dll regardless of execution mode
function Execute-Test {
    param(
        [string]$testScriptPath,       # Path to the test script file
        [string]$targetUrl,            # URL to test
        [string]$logLevel = "Debug",   # Log level
        [switch]$useStaticContextArg   # Whether to use static context
    )
    
    $staticContextArgValue = if ($useStaticContextArg) { "TRUE" } else { "FALSE" }
    $debugTestArgValue = if ($debugTests) { "TRUE" } else { "FALSE" }
    
    if ($usePacTest) {
        # Use pac test run command
        Write-Host "Running test using pac test run..." -ForegroundColor Green
        
        # Build the pac test run command
        $pacArgs = @(
            "test", "run",
            "--test-plan-file", "$testScriptPath",
            "--domain", "$targetUrl",
            "--tenant", $tenantId,
            "--environment-id", $environmentId,
            "--provider", "mda"
        )
        
        # Add optional arguments
        if ($useStaticContextArg) {
            $pacArgs += "--use-static-context"
        }
        
        if ($debugTests) {
            $pacArgs += "--debug"
        }
          # Note: We won't use --run-id with pac test, as we'll update the generated TRX files later
        # This gives us more control over the test result processing
        
        # Log the command being executed
        Write-Host "Executing: pac $($pacArgs -join ' ')" -ForegroundColor DarkGray
        
        # Execute pac command
        & pac $pacArgs
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Test execution failed with exit code: $LASTEXITCODE" -ForegroundColor Red
        }
    }
    else {
        # Use PowerAppsTestEngine.dll directly
        Write-Host "Running test using PowerAppsTestEngine.dll directly..." -ForegroundColor Green
        
        # Navigate to the test engine directory
        Push-Location $testEnginePath
        try {
            # Execute the test using dotnet command
            dotnet PowerAppsTestEngine.dll -c "$staticContextArgValue" -w "$debugTestArgValue" -u "$userAuth" -a "$authType" -p "mda" -a "none" -i "$testScriptPath" -t $tenantId -e $environmentId -d "$targetUrl" -l "$logLevel" --run-name $runName
            
            if ($LASTEXITCODE -ne 0) {
                Write-Host "Test execution failed with exit code: $LASTEXITCODE" -ForegroundColor Red
            }
        }
        finally {
            Pop-Location
        }
    }
}

$runName = [Guid]::NewGuid().Guid.ToString()
    
# Get current directory so we can reset back to it after running the tests
$currentDirectory = Get-Location

# Define the PowerApps Test Engine repository information
$testEngineRepoUrl = "https://github.com/microsoft/PowerApps-TestEngine"
$testEngineDirectory = Join-Path -Path $PSScriptRoot -ChildPath "..\PowerApps-TestEngine"
$testEngineBuildDir = Join-Path -Path $testEngineDirectory -ChildPath "src"
$testEngineBinDir = Join-Path -Path $testEngineDirectory -ChildPath "bin\Debug\PowerAppsTestEngine"

# Function to check if current directory is part of PowerApps-TestEngine
function Test-IsInTestEngineRepo {
    try {
        # Get the git root directory
        $gitRootDir = git rev-parse --show-toplevel 2>$null
        if ($LASTEXITCODE -ne 0) {
            # Not in a git repo
            return $false
        }
        
        # Check if the directory name indicates it's the PowerApps-TestEngine repo
        $dirName = Split-Path -Path $gitRootDir -Leaf
        if ($dirName -eq "PowerApps-TestEngine") {
            return $true
        }
        
        # Check remote URLs for PowerApps-TestEngine
        $remotes = git remote -v 2>$null
        foreach ($remote in $remotes) {
            if ($remote -like "*PowerApps-TestEngine*") {
                return $true
            }
        }
        
        return $false
    }
    catch {
        return $false
    }
}

# Function to setup the PowerApps Test Engine
# Function to build the PowerApps Test Engine
function Build-TestEngine {
    param(
        [string]$srcDir,      # Source directory containing the code to build
        [string]$message = "Building PowerApps Test Engine..."  # Custom message for build
    )
    
    Write-Host $message -ForegroundColor Green
    
    # Navigate to the src directory
    Push-Location $srcDir
    
    try {
        # Build the Test Engine
        dotnet build
        
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Failed to build PowerApps Test Engine. Please check the build logs."
            exit 1
        }
        
        Write-Host "PowerApps Test Engine built successfully!" -ForegroundColor Green
        return $true
    }
    finally {
        Pop-Location # Return from src directory
    }
}

# Function to verify the PowerApps Test Engine binary exists
function Test-TestEngineBinary {
    param(
        [string]$binDir  # Directory where the binary should exist
    )
    
    $dllPath = Join-Path -Path $binDir -ChildPath "PowerAppsTestEngine.dll"
    if (-not (Test-Path -Path $dllPath)) {
        Write-Error "PowerAppsTestEngine.dll not found at $dllPath. Please check the build process."
        return $false
    }
    
    Write-Host "Found PowerAppsTestEngine.dll at $dllPath" -ForegroundColor Green
    return $true
}

# Function to setup the PowerApps Test Engine
function Setup-TestEngine {    # Check if we're already in the PowerApps-TestEngine repository
    $isInTestEngineRepo = Test-IsInTestEngineRepo
      
    if ($isInTestEngineRepo) {
        Write-Host "Detected current directory is part of PowerApps-TestEngine repository" -ForegroundColor Green
        
        # Get the root directory of the repository
        $repoRootDir = git rev-parse --show-toplevel 2>$null
        
        # Use paths relative to the repository root
        $relativeSrcDir = Join-Path -Path $repoRootDir -ChildPath "src"
        $relativeBinDir = Join-Path -Path $repoRootDir -ChildPath "bin\Debug\PowerAppsTestEngine"
        $binDebugDir = Join-Path -Path $repoRootDir -ChildPath "bin\Debug"
        
        Write-Host "Using repository root directory: $repoRootDir" -ForegroundColor Green
        
        # Check if build is needed
        $needsBuild = $false
        
        # Check if the bin\Debug directory exists
        if (-not (Test-Path -Path $binDebugDir)) {
            Write-Host "bin\Debug directory doesn't exist. Building the project..." -ForegroundColor Yellow
            $needsBuild = $true
        }
        # Check if the PowerAppsTestEngine.dll exists
        elseif (-not (Test-Path -Path "$relativeBinDir\PowerAppsTestEngine.dll")) {
            Write-Host "PowerAppsTestEngine.dll not found. Building the project..." -ForegroundColor Yellow
            $needsBuild = $true
        }
        # Honor forceRebuild if specified
        elseif ($forceRebuild) {
            Write-Host "Force rebuild requested. Building the project..." -ForegroundColor Yellow
            $needsBuild = $true
        }
        else {
            Write-Host "Using existing build in $relativeBinDir" -ForegroundColor Green
        }
        
        # Build if needed
        if ($needsBuild) {
            Build-TestEngine -srcDir $relativeSrcDir -message "Building PowerApps Test Engine from local source..."
        }
          # Verify binary exists
        if (Test-TestEngineBinary -binDir $relativeBinDir) {
            Write-Host "Binary verified at $relativeBinDir" -ForegroundColor Green
            return $relativeBinDir
        } else {
            Write-Error "Failed to verify binary at $relativeBinDir"
            exit 1
        }
    }
    else {
        # Check if the PowerApps-TestEngine directory exists
        if (-not (Test-Path -Path $testEngineDirectory) -or $forceRebuild) {
            Write-Host "Setting up PowerApps Test Engine..." -ForegroundColor Cyan
            
            # Remove existing directory if it exists
            if (Test-Path -Path $testEngineDirectory) {
                Write-Host "Get latest changes..." -ForegroundColor Yellow
                Set-Location $testEngineDirectory
                git pull
            } else {
                # Clone the repository
                Write-Host "Cloning PowerApps Test Engine repository from $testEngineRepoUrl..." -ForegroundColor Green
                git clone "$testEngineRepoUrl" "$testEngineDirectory"
            }
            
            # Navigate to the repository directory
            Push-Location $testEngineDirectory
            
            try {
                # Check if a specific branch was specified
                if ($testEngineBranch -ne "main") {
                    Write-Host "Switching to branch: $testEngineBranch" -ForegroundColor Green
                    git checkout $testEngineBranch
                    
                    # Check if checkout was successful
                    if ($LASTEXITCODE -ne 0) {
                        Write-Host "Failed to switch to branch $testEngineBranch. Using main branch instead." -ForegroundColor Yellow
                        git checkout main
                    }
                }
                
                # Build the Test Engine using shared function
                Build-TestEngine -srcDir $testEngineBuildDir
            }
            finally {
                Pop-Location # Return from repository directory
            }
        }
        else {
            Write-Host "PowerApps Test Engine directory already exists at $testEngineDirectory" -ForegroundColor Green
        }
          # Verify binary exists
        if (Test-TestEngineBinary -binDir $testEngineBinDir) {
            Write-Host "Binary verified at $testEngineBinDir" -ForegroundColor Green
            return $testEngineBinDir
        } else {
            Write-Error "Failed to verify binary at $testEngineBinDir"
            exit 1
        }
    }
}

# This line was redundant, as we call Setup-TestEngine below

# Set up the Test Engine and get the path to the built binary
$testEnginePath = Setup-TestEngine
if ($testEnginePath -is [array]) {
    Write-Host "Converting array to string for testEnginePath" -ForegroundColor Yellow
    $testEnginePath = $testEnginePath[0]
}
Write-Host "Test Engine Path: $testEnginePath" -ForegroundColor Green

Set-Location $currentDirectory

$jsonContent = Get-Content -Path .\config.json -Raw
$config = $jsonContent | ConvertFrom-Json
$tenantId = $config.tenantId
$environmentId = $config.environmentId
$user1Email = $config.user1Email
$record = $config.record

# Extract pages and corresponding Test Scripts
$customPages = $config.pages.customPages
$entities = $config.pages.entities
$testScripts = $config.testScripts

# Initialize $runTests - default to true if not specified in config or overridden by testRunTime
$runTests = if ([bool]::TryParse($config.runTests, [ref]$null)) { [bool]$config.runTests } else { $true }

# Check if useStaticContext parameter was provided, otherwise get it from config
if (-not $PSBoundParameters.ContainsKey('useStaticContext')) {
    $useStaticContext = if ($null -eq $config.useStaticContext) { $false } else { $config.useStaticContext }
}
# Otherwise, the useStaticContext parameter value will be used (already set from param block)
$appName = $config.appName
$debugTests = $config.debugTests
$userAuth = $config.userAuth
$authType = "default"
$environmentUrl = $config.environmentUrl

if ([string]::IsNullOrEmpty($userAuth)) {
    $userAuth = "storagestate"
}

if ($userAuth -eq "dataverse") {
    $authType = "storagestate"
}

# Define the folder paths for test outputs
$testEngineBasePath = "$env:USERPROFILE\AppData\Local\Temp\Microsoft\TestEngine"
$folderPath = "$testEngineBasePath\TestOutput"

$extraArgs = ""

$debugTestValue = "FALSE"
$staticContext = "FALSE"

# Check if useStaticContext is true and set staticContext accordingly
if ($useStaticContext -eq $true) {
    Write-Host "Using static context: TRUE" -ForegroundColor Green
    $staticContext = "TRUE"
} else {
    Write-Host "Using static context: FALSE" -ForegroundColor Green
    $staticContext = "FALSE"
}

# Display execution mode
if ($usePacTest) {
    Write-Host "Test execution mode: pac test run (Power Platform CLI)" -ForegroundColor Cyan
    Write-Host "Report generation will still use PowerAppsTestEngine.dll" -ForegroundColor Cyan
} else {
    Write-Host "Test execution mode: PowerAppsTestEngine.dll (direct)" -ForegroundColor Cyan
}

if ($debugTests) {
    $debugTestValue = "TRUE"
}

if ($getLatest) {
   git pull
}

# Define start and end times for test reporting
$startTimeThreshold = $null
$endTimeThreshold = $null

# Process startTime parameter
if ($startTime) {
    try {        # Try parsing with multiple possible formats
        try {
            $startTimeThreshold = [DateTime]::ParseExact($startTime, "yyyy-MM-dd HH:mm", [System.Globalization.CultureInfo]::InvariantCulture)
            # Format successfully parsed
        } catch {
            # Try general parsing as fallback
            try {
                $startTimeThreshold = [DateTime]::Parse($startTime)
            } catch {
                throw "Could not parse the startTime format"
            }
        }
        Write-Host "Including test results from after $startTime" -ForegroundColor Yellow
    } catch {
        Write-Error "Invalid startTime format. Please use 'yyyy-MM-dd HH:mm'. Error: $($_.Exception.Message)"
        return
    }
} else {
    # Default: use current time as the start time
    $startTimeThreshold = Get-Date
    Write-Host "No start time provided. Using current time: $($startTimeThreshold.ToString('yyyy-MM-dd HH:mm'))" -ForegroundColor Yellow
}

# Process endTime parameter
if ($endTime) {
    try {        # Try parsing with multiple possible formats
        try {
            $endTimeThreshold = [DateTime]::ParseExact($endTime, "yyyy-MM-dd HH:mm", [System.Globalization.CultureInfo]::InvariantCulture)
            # Format successfully parsed
        } catch {
            # Try general parsing as fallback
            try {
                $endTimeThreshold = [DateTime]::Parse($endTime)
            } catch {
                throw "Could not parse the endTime format"
            }
        }
        Write-Host "Including test results until $endTime" -ForegroundColor Yellow
    } catch {
        Write-Error "Invalid endTime format. Please use 'yyyy-MM-dd HH:mm'. Error: $($_.Exception.Message)"
        return
    }
} else {
    # Default: use current time as the end time, will be updated after tests run
    $endTimeThreshold = Get-Date
    # Store the original parameter state to know if it was explicitly provided
    $endTimeProvided = $false
}

# Decide whether to run tests or just generate a report
if ($generateReportOnly -or ($startTime -and $endTime)) {
    # Don't run tests if generateReportOnly is specified or both start and end times are provided
    $runTests = $false
    
    if ($generateReportOnly) {
        Write-Host "Generating report only from existing test results" -ForegroundColor Yellow
        # If no specific time range was provided with -generateReportOnly, use a wide default range
        if (-not $startTime) {
            $startTimeThreshold = (Get-Date).AddDays(-7) # Default to last 7 days if no start time provided
            Write-Host "Using default time range: last 7 days" -ForegroundColor Yellow
        }
    } else {
        Write-Host "Generating report from existing test results between $startTime and $endTime" -ForegroundColor Yellow
    }
    
    Write-Host "Tests will not be executed" -ForegroundColor Yellow
} else {
    # Keep runTests as initialized earlier (from config or default to true)
    # Since we now default startTime to current time when not provided,
    # we want to make it clear we're running tests from now
    if (-not $startTime) {
        Write-Host "Will run tests and include results from current run only" -ForegroundColor Yellow
    }
}

if ([string]::IsNullOrEmpty($environmentId)) {
    Write-Error "Environment not configured. Please update config.json" -ForegroundColor Red
    return
}

$azTenantId = az account show --query tenantId --output tsv

if ($azTenantId -ne $tenantId) {
    Write-Error "Tenant ID mismatch. Please check your Azure CLI context." -ForegroundColor Red
    return
}

$token = (az account get-access-token --resource $environmentUrl | ConvertFrom-Json)

if ($token -eq $null) {
    Write-Error "Failed to obtain access token. Please check your Azure CLI context."
    return
}

$appId = ""
$lookup = "$environmentUrl/api/data/v9.2/appmodules?`$filter=name eq '$appName'`&`$select=appmoduleid"
$appResponse = Invoke-RestMethod -Uri $lookup -Method Get -Headers @{Authorization = "Bearer $($token.accessToken)"}
$appId = $appResponse.value.appmoduleid

$lookupApp = "$environmentUrl/api/data/v9.2/appmodules($appId)"
$appInfo = Invoke-RestMethod -Uri $lookupApp -Method Get -Headers @{Authorization = "Bearer $($token.accessToken)"}
$appDescriptor = $appInfo.descriptor | ConvertFrom-Json

$appEntities = $appDescriptor.appInfo.AppComponents.Entities | Measure-Object | Select-Object -ExpandProperty Count;

if ($runTests)
{
    $appTotal = ($appDescriptor.appInfo.AppElements.Count +  ($appEntities * 2)) 
        
    if ([string]::IsNullOrEmpty($appId)) {
        Write-Error "App id not found. Check that the Copilot Studio Kit has been installed"
        return
    }
    
    if ($config.installPlaywright) {
        # Get the absolute path to playwright.ps1
        $playwrightScriptPath = Join-Path -Path $testEnginePath -ChildPath "playwright.ps1"
        
        # Check if the file exists
        if (Test-Path -Path $playwrightScriptPath) {
            Write-Host "Running Playwright installer from: $playwrightScriptPath" -ForegroundColor Green
            Start-Process -FilePath "pwsh" -ArgumentList "-Command `"$playwrightScriptPath install`"" -Wait
        } else {
            Write-Error "Playwright script not found at: $playwrightScriptPath"
        }
    } else {
        Write-Host "Skipped playwright install"
    }

    $env:user1Email = $user1Email

    Write-Host "========================================" -ForegroundColor Green
    Write-Host "ENTITIES" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green    # Loop through Entity (List and details) and Execute Tests
    foreach ($entity in $entities) {
        $formName = $entity.name
        $entityName = $entity.entity
        
        # Skip if entity filter is specified and doesn't match current entity
        if (-not [string]::IsNullOrEmpty($entityFilter) -and $entityName -notlike "*$entityFilter*" -and $formName -notlike "*$entityFilter*") {
            Write-Host "Skipping $formName ($entityName) - doesn't match entity filter: $entityFilter" -ForegroundColor Gray
            continue
        }
        
        Write-Host "----------------------------------------" -ForegroundColor Yellow
        Write-Host $entity.name -ForegroundColor Yellow
        Write-Host "----------------------------------------" -ForegroundColor Yellow

        if ($config.pages.list -and ([string]::IsNullOrEmpty($pageTypeFilter) -or $pageTypeFilter -contains "entitylist")) {
            $matchingScript = "$formName-list.te.yaml"

            if (-not (Test-Path -Path "$currentDirectory\$matchingScript") ) {
                Write-Host "No matching test script found for: $matchingScript"
                continue
            }

            # Query the default (isdefault = true) public (querytype = 0) saved query ID for the entity
            $lookup = "$environmentUrl/api/data/v9.2/savedqueries?`$filter=returnedtypecode eq '$entityName' and isdefault eq true and querytype eq 0&`$select=savedqueryid"
            $response = Invoke-RestMethod -Uri $lookup -Method Get -Headers @{Authorization = "Bearer $($token.accessToken)"}

            $viewId = $response.value.savedqueryid
            $testStart = Get-Date
            $mdaUrl = "$environmentUrl/main.aspx?appid=$appId&pagetype=entitylist&etn=$entityName&viewid=$viewId&viewType=1039"

            # Execute the test using our helper function
            $testScriptPath = "$currentDirectory\$matchingScript"
            Execute-Test -testScriptPath $testScriptPath -targetUrl $mdaUrl -useStaticContextArg:$useStaticContext
        } else {
            Write-Host "Skipped list test script"
        }
        
        if ($config.pages.details -and ([string]::IsNullOrEmpty($pageTypeFilter) -or $pageTypeFilter -contains "entityrecord")) {
            $matchingScript = "$formName-details.te.yaml"

            if (-not (Test-Path -Path "$currentDirectory\$matchingScript") ) {
                Write-Host "No matching test script found for: $matchingScript"
                continue
            }

            # Query the record ID for the entity 
            if ($entityName[-1] -ne 's') {
                $entityNamePlural = $entityName + 's'
            } 
            else {
                $entityNamePlural = $entityName + 'es'
            }

            $idColumn = $entity.id
            $lookup = "$environmentUrl/api/data/v9.2/$entityNamePlural`?`$top=1&`$select=$idColumn"   

            $entityResponse = Invoke-RestMethod -Uri $lookup -Method Get -Headers @{Authorization = "Bearer $($token.accessToken)"}
            $recordId = $entityResponse.value | Select-Object -ExpandProperty $idColumn
            $testStart = Get-Date
            if ([string]::IsNullOrEmpty($recordId)) {
                $mdaUrl = "$environmentUrl/main.aspx?appid=$appId&pagetype=entityrecord&etn=$entityName"
            
            } else {
                $mdaUrl = "$environmentUrl/main.aspx?appid=$appId&pagetype=entityrecord&etn=$entityName&id=$recordId"
            }
            
          
            Write-Host "Skipped recording"
            # Run the tests for each user in the configuration file.
            $testScriptPath = "$currentDirectory\$matchingScript"
            Execute-Test -testScriptPath $testScriptPath -targetUrl $mdaUrl -useStaticContextArg:$useStaticContext
        } else {
            Write-Host "Skipped details test script"
        }
    }
    
    if ($config.pages.customPage -and ([string]::IsNullOrEmpty($pageTypeFilter) -or $pageTypeFilter -contains "custom")) {
        Write-Host "========================================" -ForegroundColor Green
        Write-Host "CUSTOM PAGES" -ForegroundColor Green
        Write-Host "========================================" -ForegroundColor Green

        # Loop through Custom Pages and Execute Tests
        foreach ($customPage in $customPages) {
            # Skip if custom page filter is specified and doesn't match current custom page
            if (-not [string]::IsNullOrEmpty($customPageFilter) -and $customPage -notlike "*$customPageFilter*") {
                Write-Host "Skipping custom page $customPage - doesn't match filter: $customPageFilter" -ForegroundColor Gray
                continue
            }

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
            Write-Host "----------------------------------------" -ForegroundColor Yellow            $testStart = Get-Date

            $mdaUrl = "$environmentUrl/main.aspx?appid=$appId&pagetype=custom&name=$customPage"
            
            Write-Host "Skipped recording"

            # Run the tests for each user in the configuration file.
            $testScriptPath = "$currentDirectory\$matchingScript"
            Execute-Test -testScriptPath $testScriptPath -targetUrl $mdaUrl -useStaticContextArg:$useStaticContext
        } 

        Write-Host "All custompages executed"
    }    # Reset the location back to the original directory.
    Set-Location $currentDirectory
    
    # Update the endTimeThreshold to current time after tests have run
    # Only do this if endTime wasn't explicitly provided
    if (-not $endTime) {
        $endTimeThreshold = Get-Date
    }
}

# Function to update the run name in TRX files to make them compatible with PowerAppsTestEngine.dll report generation
function Update-TrxFilesRunName {
    param(
        [string]$searchPath,        # Path to search for TRX files
        [DateTime]$startTime,       # Only process files created after this time
        [string]$newRunName         # The run name to insert into the TRX files
    )
    
    Write-Host "Searching for TRX files in $searchPath created after $($startTime.ToString('yyyy-MM-dd HH:mm:ss'))..." -ForegroundColor Cyan
    
    # Get all TRX files created after the start time
    $trxFiles = Get-ChildItem -Path $searchPath -Filter "*.trx" -Recurse | 
                Where-Object { $_.CreationTime -ge $startTime }
    
    if ($trxFiles.Count -eq 0) {
        Write-Host "No TRX files found matching the criteria." -ForegroundColor Yellow
        return
    }
    
    Write-Host "Found $($trxFiles.Count) TRX file(s) to process." -ForegroundColor Green
    
    foreach ($file in $trxFiles) {
        Write-Host "Processing $($file.FullName)..." -ForegroundColor Cyan
        
        try {
            # Load the TRX file as XML
            [xml]$trxXml = Get-Content -Path $file.FullName
            
            # Create a namespace manager to handle the XML namespaces
            $nsManager = New-Object System.Xml.XmlNamespaceManager($trxXml.NameTable)
            
            # Check if the document has a default namespace
            $defaultNs = $trxXml.DocumentElement.NamespaceURI
            if (-not [string]::IsNullOrEmpty($defaultNs)) {
                # Add the default namespace with a prefix to use in XPath queries
                $nsManager.AddNamespace("ns", $defaultNs)
                Write-Host "  Document has namespace: $defaultNs" -ForegroundColor DarkGray
                # Use the namespace prefix in our XPath
                $testRun = $trxXml.SelectSingleNode("//ns:TestRun", $nsManager)
            } else {
                # No namespace, use regular XPath
                $testRun = $trxXml.SelectSingleNode("//TestRun")
            }
            
            if ($testRun -ne $null) {
                $oldRunId = $testRun.id
                $oldRunName = $testRun.name
                
                Write-Host "  Original Run ID: $oldRunId" -ForegroundColor DarkGray
                Write-Host "  Original Run Name: $oldRunName" -ForegroundColor DarkGray
                
                # Update the run ID and name
                $testRun.id = $newRunName
                $testRun.name = "TestEngine Test Run $newRunName"
                
                Write-Host "  New Run ID: $newRunName" -ForegroundColor DarkGray
                Write-Host "  New Run Name: TestEngine Test Run $newRunName" -ForegroundColor DarkGray
                
                # Also update any TestRunConfiguration element that might contain the run ID
                if (-not [string]::IsNullOrEmpty($defaultNs)) {
                    $testRunConfig = $trxXml.SelectSingleNode("//ns:TestRunConfiguration", $nsManager)
                } else {
                    $testRunConfig = $trxXml.SelectSingleNode("//TestRunConfiguration")
                }
                
                if ($testRunConfig -ne $null -and $testRunConfig.HasAttribute("id")) {
                    $testRunConfig.SetAttribute("id", $newRunName)
                }
                
                # Save the modified TRX file
                $trxXml.Save($file.FullName)
                Write-Host "  Updated TRX file saved successfully." -ForegroundColor Green
            }
            else {                Write-Host "  Warning: TestRun element not found in TRX file." -ForegroundColor Yellow
            }
        }
        catch {
            Write-Host "  Error processing TRX file: $($_.Exception.Message)" -ForegroundColor Red
            Write-Host "  Exception details: $($_.Exception.GetType().FullName)" -ForegroundColor Red
            Write-Host "  Error processing TRX file: $_" -ForegroundColor Red
        }
    }
}

if ($usePacTest) {
    # When using pac test, we need to update the runName in all TRX files
    # to ensure they're identified by the PowerAppsTestEngine.dll report generator
    Write-Host "Processing test results from pac test run..." -ForegroundColor Cyan
    
    # Get the start time of this script execution as a reference point
    # Only process TRX files created after this script started running
    $scriptStartTime = $startTimeThreshold
    
    # Update the TRX files to use our runName - search in the entire TestEngine directory
    Update-TrxFilesRunName -searchPath $testEngineBasePath -startTime $scriptStartTime -newRunName $runName
}

$reportPath = [System.IO.Path]::Combine($folderPath, "test_summary_$runName.html")

# Generate report using PowerAppsTestEngine.dll directly, regardless of test execution mode
Write-Host "Generating report using PowerAppsTestEngine.dll..." -ForegroundColor Green

Push-Location $testEnginePath
try {
    dotnet PowerAppsTestEngine.dll --run-name $runName --output-file $reportPath --start-time $startTimeThreshold
}
finally {
    Pop-Location
}

# Report was successfully generated (either Azure DevOps style or classic)
Write-Host "HTML summary report available at $reportPath." -ForegroundColor Green

# Open the report in the default browser
Write-Host "Opening report in browser..." -ForegroundColor Green
Start-Process $reportPath
# Add information about how to regenerate this report using the startTime and endTime parameters
# Always use the most current timestamp as the end time
$currentTime = (Get-Date).ToString("yyyy-MM-dd HH:mm")

# Format the start time that was used - this is either the provided startTime or the current time when the script started
$formattedStartTime = $startTimeThreshold.ToString("yyyy-MM-dd HH:mm")

Write-Host "=============================================" -ForegroundColor Magenta
Write-Host "REPORT REGENERATION INFORMATION" -ForegroundColor Magenta
Write-Host "=============================================" -ForegroundColor Magenta
Write-Host "To regenerate this exact report without running tests again, use:" -ForegroundColor Magenta
Write-Host "./RunTests.ps1 -runName `"$runName`"" -ForegroundColor Yellow
Write-Host "=============================================" -ForegroundColor Magenta
