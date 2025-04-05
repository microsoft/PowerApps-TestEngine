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

# Define the folder path and time threshold
$folderPath = "$env:USERPROFILE\AppData\Local\Temp\Microsoft\TestEngine\TestOutput"

$extraArgs = ""

$staticContext = "FALSE"

if ($useStaticContext) {
    $staticContext = "TRUE"
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




if ($runTests)
{
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

    $token = (az account get-access-token --resource $environmentUrl | ConvertFrom-Json)

    $appId = ""
    $lookup = "$environmentUrl/api/data/v9.2/appmodules?`$filter=name eq '$appName'`&`$select=appmoduleid"
    $appResponse = Invoke-RestMethod -Uri $lookup -Method Get -Headers @{Authorization = "Bearer $($token.accessToken)"}
    $appId = $appResponse.value.appmoduleid

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

            $mdaUrl = "$environmentUrl/main.aspx?appid=$appId&pagetype=entitylist&etn=$entityName&viewid=$viewId&viewType=1039"
            if ($record) {
                # Run the tests for each user in the configuration file.
                dotnet PowerAppsTestEngine.dll -c "$staticContext"-u "storagestate" -p "mda" -a "none"  -r "True" -i "$currentDirectory\$matchingScript" -t $tenantId -e $environmentId -d "$mdaUrl" -l "Debug"
            } else {
                Write-Host "Skipped recording"
                # Run the tests for each user in the configuration file.
                dotnet PowerAppsTestEngine.dll -c "$staticContext" -u "storagestate" -p "mda" -a "none" -i "$currentDirectory\$matchingScript" -t $tenantId -e $environmentId -d "$mdaUrl" -l "Debug"
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

            if ([string]::IsNullOrEmpty($recordId)) {
                Write-Host "No record found for entity: $entityName" -ForegroundColor Red
                continue
            }
        
            $mdaUrl = "$environmentUrl/main.aspx?appid=$appId&pagetype=entityrecord&etn=$entityName&id=$recordId"
            if ($record) {
                # Run the tests for each user in the configuration file.
                dotnet PowerAppsTestEngine.dll -c "$staticContext" -u "storagestate" -p "mda" -a "none"  -r "True" -i "$currentDirectory\$matchingScript" -t $tenantId -e $environmentId -d "$mdaUrl" -l "Debug"
            } else {
                Write-Host "Skipped recording"
                # Run the tests for each user in the configuration file.
                dotnet PowerAppsTestEngine.dll -c "$staticContext" -u "storagestate" -p "mda" -a "none" -i "$currentDirectory\$matchingScript" -t $tenantId -e $environmentId -d "$mdaUrl" -l "Debug"
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
                dotnet PowerAppsTestEngine.dll -c "$staticContext" -u "storagestate" -p "mda" -a "none"  -r "True" -i "$currentDirectory\$matchingScript" -t $tenantId -e $environmentId -d "$mdaUrl" -l "Debug" 
            } else {
                Write-Host "Skipped recording"
                # Run the tests for each user in the configuration file.
                dotnet PowerAppsTestEngine.dll -c "$staticContext" -u "storagestate" -p "mda" -a "none" -i "$currentDirectory\$matchingScript" -t $tenantId -e $environmentId -d "$mdaUrl" -l "Debug" 
            }
        }

        Write-Host "All custompages executed successfully!"
    }
    # Reset the location back to the original directory.
    Set-Location $currentDirectory
}

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
    <title>Test Summary Report</title>
    <script src="https://cdnjs.cloudflare.com/ajax/libs/Chart.js/4.4.1/chart.umd.min.js" integrity="sha512-CQBWl4fJHWbryGE+Pc7UAxWMUMNMWzWxF4SQo9CgkJIN1kx6djDQZjh3Y8SZ1d+6I+1zze6Z7kHXO7q3UyZAWw==" crossorigin="anonymous" referrerpolicy="no-referrer"></script>
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
            var badgeContainer = document.createElement('div');
            badgeContainer.innerHTML = ``
                <span class="badge badge-pass">Passed: `${passCount}</span>
                <span class="badge badge-fail">Failed: `${failCount}</span>
            ``;
            document.body.insertBefore(badgeContainer, document.getElementById('chart-container'));
        });
    </script>
</body>
</html>
"@

# Replace placeholders with actual values
$passCount = ($testResults | Measure-Object -Property Passed -Sum).Sum
$failCount = ($testResults | Measure-Object -Property Failed -Sum).Sum
$preContent = $preContent -replace "#PassCount#", $passCount
$preContent = $preContent -replace "#FailCount#", $failCount

# Generate HTML report
$reportHtml = $testResults | ConvertTo-Html -Property File, Total, Executed, Passed, Failed, Error, Timeout, Aborted, Inconclusive, PassedButRunAborted, NotRunnable, NotExecuted, Disconnected, Warning, Completed, InProgress, Pending -Title "Test Summary Report" -PreContent $preContent
$reportHtml | Out-File -FilePath $reportPath

Write-Host "HTML summary report generated successfully at $folderPath."

$job = Start-Job -ScriptBlock {
    param($path)
    python -m http.server 8080 --directory $path
} -ArgumentList $folderPath

$jobId = $job.Id

python -m webbrowser -t "http://localhost:8080/summary_report_$timestamp.html"

Read-Host "Please press enter when complete"

Stop-Job -Id $jobId
Remove-Job -Id $jobId