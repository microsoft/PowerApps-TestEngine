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
$environmentUrl = $config.environmentUrl
$user1Email = $config.user1Email
$useStaticContext = $config.useStaticContext
$getLatest = $config.getLatest
$userAuth = $config.userAuth
$env:ENTRA_CLIENT_ID = $config.applicationId
$authType = "default"

if ([string]::IsNullOrEmpty($userAuth)) {
    $userAuth = "storagestate"
}

if ($userAuth -eq "dataverse") {
    $authType = "storagestate"
}

# Define the folder path and time threshold
$folderPath = "$env:USERPROFILE\AppData\Local\Temp\Microsoft\TestEngine\TestOutput"

$staticContext = "FALSE"

if ($useStaticContext) {
    $staticContext = "TRUE"
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

if ([string]::IsNullOrEmpty($environmentId)) {
    Write-Error "Environment not configured. Please update config.json" -ForegroundColor Red
    return
}

if ([string]::IsNullOrEmpty($environmentUrl)) {
    Write-Error "Environment URL not configured. Please update config.json" -ForegroundColor Red
    return
}

# Build the latest debug version of Test Engine from source
Set-Location ..\..\src
Write-Host "Compiling the project..."
dotnet build

if ($config.installPlaywright) {
    Start-Process -FilePath "pwsh" -ArgumentList "-Command `"..\bin\Debug\PowerAppsTestEngine\playwright.ps1 install`"" -Wait
} else {
    Write-Host "Skipped playwright install"
}

Set-Location ..\bin\Debug\PowerAppsTestEngine
$env:user1Email = $user1Email

Write-Host "========================================" -ForegroundColor Green
Write-Host "RUNNING YAML TEST FILES" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

# Find all YAML files in the current directory
$yamlFiles = Get-ChildItem -Path "$currentDirectory" -Filter "*.yaml"

if ($yamlFiles.Count -eq 0) {
    Write-Host "No YAML files found in the directory." -ForegroundColor Yellow
    Set-Location $currentDirectory
    return
}

foreach ($yamlFile in $yamlFiles) {
    Write-Host "----------------------------------------" -ForegroundColor Yellow
    Write-Host "Running: $($yamlFile.Name)" -ForegroundColor Yellow
    Write-Host "----------------------------------------" -ForegroundColor Yellow

    $testStart = Get-Date

    # Run the test using copilot provider
    dotnet PowerAppsTestEngine.dll -c "$staticContext" -u "$userAuth" -a "$authType" --provider "copilot.portal" -i "$($yamlFile.FullName)" -t $tenantId -e $environmentId -d "$environmentUrl" -l "Debug" -w "TRUE"
}

# Reset the location back to the original directory.
Set-Location $currentDirectory

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

Write-Host "Opening report in browser..."
Write-Host $reportPath

Invoke-Item $reportPath
