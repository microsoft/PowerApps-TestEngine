# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# Check for optional command line argument for last run time
param (
    [string]$lastRunTime
)

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
$runTests = $config.runTests
$useStaticContext = $config.useStaticContext
$appName = $config.appName
$debugTests = $config.debugTests
$getLatest = $config.getLatest

# Define the folder path and time threshold
$folderPath = "$env:USERPROFILE\AppData\Local\Temp\Microsoft\TestEngine\TestOutput"

$extraArgs = ""

$debugTestValue = "FALSE"
$staticContext = "FALSE"

if ($useStaticContext) {
    $staticContext = "TRUE"
}

if ($debugTests) {
    $debugTestValue = "TRUE"
}

if ($getLatest) {
   git pull
}

if ($lastRunTime) {
    try {
        $timeThreshold = [datetime]::ParseExact($lastRunTime, "yyyy-MM-dd HH:mm", $null)
    } catch {
        Write-Error "Invalid date format. Please use 'yyyy-MM-DD HH:mm'."
        return
    }
} else {
    $timeThreshold = Get-Date
}

function Update-TestData {
    param (
        [string]$folderPath,
        [datetime]$timeThreshold,
        [string]$entityName,
        [string]$entityType
    )

    AddOrUpdate -key ($entityName + "-" + $entityType) -value (New-Object TestData($entityName, $entityType, 0, 0))   

    # Find all folders newer than the specified time
    $folders = Get-ChildItem -Path $folderPath -Directory | Where-Object { $_.LastWriteTime -gt $timeThreshold }

    # Initialize array to store .trx files
    $trxFiles = @()

    # Iterate through each folder and find .trx files
    foreach ($folder in $folders) {
        $trxFiles += Get-ChildItem -Path $folder.FullName -Filter "*.trx"
    }

    # Parse each .trx file and update pass/fail counts in TestData
    foreach ($trxFile in $trxFiles) {
        $xmlContent = Get-Content -Path $trxFile.FullName -Raw
        $xml = [xml]::new()
        $xml.LoadXml($xmlContent)

        # Create a namespace manager
        $namespaceManager = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
        $namespaceManager.AddNamespace("ns", "http://microsoft.com/schemas/VisualStudio/TeamTest/2010")

        # Find the Counters element
        $counters = $xml.SelectSingleNode("//ns:Counters", $namespaceManager)

        # Extract the counter properties and update TestData
        if ($counters) {
            $passCount = [int]$counters.passed
            $failCount = [int]$counters.failed

            AddOrUpdate -key ($entityName + "-" + $entityType) -value (New-Object TestData($entityName, $entityType, $passCount, $failCount))
        }
    }
}

# Initialize the dictionary (hash table)
$dictionary = @{}

# Define the TestData class
class TestData {
    [string]$EntityName
    [string]$EntityType  # list, record, custom
    [int]$PassCount
    [int]$FailCount

    TestData([string]$entityName, [string]$entityType, [int]$passCount, [int]$failCount) {
        $this.EntityName = $entityName
        $this.EntityType = $entityType
        $this.PassCount = $passCount
        $this.FailCount = $failCount
    }

    # Override ToString method for better display
    [string]ToString() {
        return "EntityName: $($this.EntityName), EntityType: $($this.EntityType), PassCount: $($this.PassCount), FailCount: $($this.FailCount)"
    }
}

# Function to add or update a key-value pair
function AddOrUpdate {
    param (
        [string]$key,
        [object]$value
    )

    if ($dictionary.ContainsKey($key)) {
        # Update the pass/fail properties if the key exists
        $dictionary[$key].PassCount += $value.PassCount
        $dictionary[$key].FailCount += $value.FailCount
        Write-Host "Updated key '$key' with value '$($dictionary[$key])'."
    } else {
        # Add the key-value pair if the key does not exist
        $dictionary[$key] = $value
        Write-Host "Added key '$key' with value '$value'."
    }
}

if ($runTests)
{
    if ([string]::IsNullOrEmpty($environmentId)) {
        Write-Error "Environment not configured. Please update config.json" -ForegroundColor Red
        return
    }

    $azTenantId = az account show --query tenantId --output tsv

    if ($azTenantId -ne $tenantId) {
        Write-Error "Tenant ID mismatch. Please check your Azure CLI context." -ForegroundColor Red
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
        Write-Output "Environment ID not found." -ForegroundColor Red
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

    Write-Host $appEntities

    $appTotal = ($appDescriptor.appInfo.AppElements.Count +  ($appEntities * 2)) 
        
    if ([string]::IsNullOrEmpty($appId)) {
        Write-Error "App id not found. Check that the Copilot Studio Kit has been installed"
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

    Write-Host "========================================" -ForegroundColor Green
    Write-Host "ENTITIES" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green

    # Loop through Entity (List and details) and Execute Tests
    foreach ($entity in $entities) {
        Write-Host "----------------------------------------" -ForegroundColor Yellow
        Write-Host $entity.name -ForegroundColor Yellow
        Write-Host "----------------------------------------" -ForegroundColor Yellow

        $formName = $entity.name
        $entityName = $entity.entity

        if ($config.pages.list) {
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
            if ($record) {
                # Run the tests for each user in the configuration file.
                dotnet PowerAppsTestEngine.dll -c "$staticContext" -w "$debugTestValue" -u "storagestate" -p "mda" -a "none"  -r "True" -i "$currentDirectory\$matchingScript" -t $tenantId -e $environmentId -d "$mdaUrl" -l "Debug"
            } else {
                Write-Host "Skipped recording"
                # Run the tests for each user in the configuration file.
                dotnet PowerAppsTestEngine.dll -c "$staticContext" -w "$debugTestValue" -u "storagestate" -p "mda" -a "none" -i "$currentDirectory\$matchingScript" -t $tenantId -e $environmentId -d "$mdaUrl" -l "Debug"
            }

            Update-TestData -folderPath $folderPath -timeThreshold $testStart -entityName $entityName -entityType "list"
        } else {
            Write-Host "Skipped list test script"
        }

        if ($config.pages.details) {
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

            if ([string]::IsNullOrEmpty($recordId)) {
                Write-Host "No record found for entity: $entityName" -ForegroundColor Red
                continue
            }

            $testStart = Get-Date
        
            $mdaUrl = "$environmentUrl/main.aspx?appid=$appId&pagetype=entityrecord&etn=$entityName&id=$recordId"
            if ($record) {
                # Run the tests for each user in the configuration file.
                dotnet PowerAppsTestEngine.dll -c "$staticContext" -w "$debugTestValue" -u "storagestate" -p "mda" -a "none"  -r "True" -i "$currentDirectory\$matchingScript" -t $tenantId -e $environmentId -d "$mdaUrl" -l "Debug"
            } else {
                Write-Host "Skipped recording"
                # Run the tests for each user in the configuration file.
                dotnet PowerAppsTestEngine.dll -c "$staticContext" -w "$debugTestValue" -u "storagestate" -p "mda" -a "none" -i "$currentDirectory\$matchingScript" -t $tenantId -e $environmentId -d "$mdaUrl" -l "Debug"
            }

            Update-TestData -folderPath $folderPath -timeThreshold $testStart -entityName $entityName -entityType "details"
        } else {
            Write-Host "Skipped details test script"
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

            $testStart = Get-Date

            $mdaUrl = "$environmentUrl/main.aspx?appid=$appId&pagetype=custom&name=$customPage"
            if ($record) {
                # Run the tests for each user in the configuration file.
                dotnet PowerAppsTestEngine.dll -c "$staticContext" -w "$debugTestValue" -u "storagestate" -p "mda" -a "none"  -r "True" -i "$currentDirectory\$matchingScript" -t $tenantId -e $environmentId -d "$mdaUrl" -l "Debug" 
            } else {
                Write-Host "Skipped recording"
                # Run the tests for each user in the configuration file.
                dotnet PowerAppsTestEngine.dll -c "$staticContext" -w "$debugTestValue" -u "storagestate" -p "mda" -a "none" -i "$currentDirectory\$matchingScript" -t $tenantId -e $environmentId -d "$mdaUrl" -l "Debug" 
            }

            Update-TestData -folderPath $folderPath -timeThreshold $testStart -entityName $customPage -entityType "custom"
        } 

        Write-Host "All custompages executed"
    }
    # Reset the location back to the original directory.
    Set-Location $currentDirectory
}

$global:healthPercentage = ""

# Find all folders newer than the specified time
$folders = Get-ChildItem -Path $folderPath -Directory | Where-Object { $_.LastWriteTime -gt $timeThreshold }

# Initialize arrays to store .trx files and test results
$trxFiles = @()
$testResults = @()

# Iterate through each folder and find .trx files
foreach ($folder in $folders) {
    $trxFiles += Get-ChildItem -Path $folder.FullName -Filter "*.trx"
}

# Parse each .trx file and count pass and fail tests
foreach ($trxFile in $trxFiles) {
    $xmlContent = Get-Content -Path $trxFile.FullName -Raw
    $xml = [xml]::new()
    $xml.LoadXml($xmlContent)

    # Create a namespace manager
    $namespaceManager = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
    $namespaceManager.AddNamespace("ns", "http://microsoft.com/schemas/VisualStudio/TeamTest/2010")

    # Find the Counters element
    $counters = $xml.SelectSingleNode("//ns:Counters", $namespaceManager)

    # Extract the counter properties
    $total = [int]$counters.total
    $executed = [int]$counters.executed
    $passed = [int]$counters.passed
    $failed = [int]$counters.failed
    $error = [int]$counters.error
    $timeout = [int]$counters.timeout
    $aborted = [int]$counters.aborted
    $inconclusive = [int]$counters.inconclusive
    $passedButRunAborted = [int]$counters.passedButRunAborted
    $notRunnable = [int]$counters.notRunnable
    $notExecuted = [int]$counters.notExecuted
    $disconnected = [int]$counters.disconnected
    $warning = [int]$counters.warning
    $completed = [int]$counters.completed
    $inProgress = [int]$counters.inProgress
    $pending = [int]$counters.pending

    $testResults += [PSCustomObject]@{
        File = $trxFile.FullName
        Total = $total
        Executed = $executed
        Passed = $passed
        Failed = $failed
        Error = $error
        Timeout = $timeout
        Aborted = $aborted
        Inconclusive = $inconclusive
        PassedButRunAborted = $passedButRunAborted
        NotRunnable = $notRunnable
        NotExecuted = $notExecuted
        Disconnected = $disconnected
        Warning = $warning
        Completed = $completed
        InProgress = $inProgress
        Pending = $pending
    }
}

# Generate HTML summary report with timestamp
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$reportPath = "$folderPath\summary_report_$timestamp.html"

# Define PreContent with injected JavaScript for badges
$preContent = @"
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <title>Test Engine Summary Report</title>
    <script src="https://cdnjs.cloudflare.com/ajax/libs/Chart.js/4.4.1/chart.umd.min.js" integrity="sha512-CQBWl4fJHWbryGE+Pc7UAxWMUMNMWzWxF4SQo9CgkJIN1kx6djDQZjh3Y8SZ1d+6I+1zze6Z7kHXO7q3UyZAWw==" crossorigin="anonymous" referrerpolicy="no-referrer"></script>
    <script src="https://cdnjs.cloudflare.com/ajax/libs/mathjax/3.2.2/es5/tex-svg.min.js" integrity="sha512-EtUjpk/hY3NXp8vfrPUJWhepp1ZbgSI10DKPzfd+3J/p2Wo89JRBvQIdk3Q83qAEhKOiFOsYfhqFnOEv23L+dA==" crossorigin="anonymous" referrerpolicy="no-referrer"></script>
    <style>
        .badge {
            display: inline-block;
            padding: 0.5em 1em;
            font-size: 1em;
            font-weight: bold;
            color: #fff;
            border-radius: 0.25em;
        }
        .badge-pass {
            background-color: #28a745;
        }
        .badge-fail {
            background-color: #dc3545;
        }
    </style>
</head>
<body>
    <h1>Test Summary Report</h1>
    <div id="chart-container" style="width: 50%; margin: auto;">
        <canvas id="test-summary-chart"></canvas>
    </div>
    #Coverage#
    <style>
        .badge-pass {
            background-color: #28a745;
            color: white;
        }
        .badge-fail {
            background-color: #dc3545;
            color: white;
        }
        .badge-health {
            background-color: grey;
            color: white;
        }
    </style>
    <script>
        document.addEventListener('DOMContentLoaded', function () {
            var ctx = document.getElementById('test-summary-chart').getContext('2d');
            var testSummaryChart = new Chart(ctx, {
                type: 'bar',
                data: {
                    labels: ['Passed', 'Failed'],
                    datasets: [{
                        label: 'Test Results',
                        data: [#PassCount#, #FailCount# ],
                        backgroundColor: ['#28a745', '#dc3545'],
                        borderColor: ['#28a745', '#dc3545'],
                        borderWidth: 1
                    }]
                },
                options: {
                    scales: {
                        y: {
                            beginAtZero: true
                        }
                    }
                }
            });

            // Generate badges
            var passCount = #PassCount#;
            var failCount = #FailCount#;
            var healthPercentage = #HealthPercent#;
            var badgeContainer = document.createElement('div');
            badgeContainer.innerHTML = ``
                <span class="badge badge-pass">Passed: `${passCount}</span>
                <span class="badge badge-fail">Failed: `${failCount}</span>
                <span class="badge badge-health">Health: `${healthPercentage}%</span>
            ``;
            document.body.insertBefore(badgeContainer, document.getElementById('chart-container'));
        });
    </script>
</body>
</html>
"@

# Function to generate HTML table representation of the TestData dictionary
function Generate-HTMLTable {
    param (
        [hashtable]$dictionary,
        [int] $total
    )

    # Initialize HTML table
    $htmlTable = @"
    <h2>Health Check Coverage</h2>
<table>
    <tr>
        <th>Name</th>
        <th>Type</th>
        <th>Pass Count</th>
        <th>Fail Count</th>
    </tr>
"@

    $numerator = 0;
    # Iterate through the dictionary and add rows to the table
    foreach ($key in $dictionary.Keys) {
        $value = $dictionary[$key]
        $numerator += ($value.PassCount -gt 0) -and ($value.FailCount -eq 0) ? 1 : 0
        $htmlTable += "<tr><td>$($value.EntityName)</td><td>$($value.EntityType)</td><td>$($value.PassCount)</td><td>$($value.FailCount)</td></tr>"
    }

    # Close the table
    $htmlTable += "</table>"
    
    $global:healthPercentage = $total -eq 0 ? "0" : ($numerator / $total * 100).ToString("0") 
    $percentage = $global:healthPercentage + "%"
    
    # Add KaTeX code to show the calculation
    $katexCode = @"
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/KaTeX/0.16.9/katex.min.css">
    <script src="https://cdnjs.cloudflare.com/ajax/libs/KaTeX/0.16.9/katex.min.js" integrity="sha512-LQNxIMR5rXv7o+b1l8+N1EZMfhG7iFZ9HhnbJkTp4zjNr5Wvst75AqUeFDxeRUa7l5vEDyUiAip//r+EFLLCyA==" crossorigin="anonymous" referrerpolicy="no-referrer"></script>
    <script src="https://cdnjs.cloudflare.com/ajax/libs/KaTeX/0.16.9/contrib/auto-render.min.js" integrity="sha512-iWiuBS5nt6r60fCz26Nd0Zqe0nbk1ZTIQbl3Kv7kYsX+yKMUFHzjaH2+AnM6vp2Xs+gNmaBAVWJjSmuPw76Efg==" crossorigin="anonymous" referrerpolicy="no-referrer"></script>
    <script>
    document.addEventListener("DOMContentLoaded", function() {
        renderMathInElement(document.body, {
            delimiters: [
                {left: "`$`$", right: "`$`$"}
            ]
            });
        });
    </script>
    <h3>Calculation:</h3>
    <p>`$`$ = \left( \frac{\text{Number of Passed Entities}}{\text{Total Entities}} \right) \times 100 `$`$</p>
    <p>`$`$ = \left( \frac{$numerator}{$total} \right) \times 100 `$`$</p>
    <p>`$`$ = $percentage `$`$%</p>
"@

    $htmlTable += $katexCode

    return $htmlTable
}

# Replace placeholders with actual values
$passCount = ($testResults | Measure-Object -Property Passed -Sum).Sum
$failCount = ($testResults | Measure-Object -Property Failed -Sum).Sum
$preContent = $preContent -replace "#PassCount#", $passCount
$preContent = $preContent -replace "#FailCount#", $failCount
$preContent = $preContent -replace "#Coverage#", (Generate-HTMLTable -dictionary $dictionary -total $appTotal)
$preContent = $preContent -replace "#HealthPercent#", $global:healthPercentage

# Generate HTML report
$reportHtml = $testResults | ConvertTo-Html -Property File, Total, Executed, Passed, Failed, Error, Timeout, Aborted, Inconclusive, PassedButRunAborted, NotRunnable, NotExecuted, Disconnected, Warning, Completed, InProgress, Pending -Title "Test Summary Report" -PreContent $preContent
$reportHtml | Out-File -FilePath $reportPath

Write-Host "HTML summary report generated successfully at $folderPath."

Write-Host "Opening report in browser..."
Write-Host $reportPath

Invoke-Item $reportPath