# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# Check for optional command line argument for last run time
param (
    [string]$lastRunTime,
    [switch]$compile,
    [switch]$record
)

# Get current directory so we can reset back to it after running the tests
$currentDirectory = Get-Location

$timeStart = Get-Date

# Check for config.json file
if (Test-Path -Path .\config.json) {
    $jsonContent = Get-Content -Path .\config.json -Raw
    $config = $jsonContent | ConvertFrom-Json
    $tenantId = $config.tenantId
    $environmentId = $config.environmentId
    $environmentUrl = $config.environmentUrl
    $staticContext = $config.useStaticContext
    $debugTests = $config.debugTests
} else {
    # Default values if no config file is present
    $tenantId = $null
    $environmentId = $null
    $environmentUrl = $null
    $staticContext = $false
    $debugTests = $false
}

# Prompt for required values if they are missing
if ([string]::IsNullOrEmpty($tenantId)) {
    $tenantId = Read-Host -Prompt "Enter your tenant ID"
}

if ([string]::IsNullOrEmpty($environmentId)) {
    $environmentId = Read-Host -Prompt "Enter your environment ID"
}

if ([string]::IsNullOrEmpty($environmentUrl)) {
    $environmentUrl = Read-Host -Prompt "Enter your environment URL (e.g., https://yourorg.crm.dynamics.com)"
}

# Define the folder path for TestEngine output
$folderPath = "$env:USERPROFILE\AppData\Local\Temp\Microsoft\TestEngine\TestOutput"

# Set values for flags
$debugTestValue = $debugTests ? "TRUE" : "FALSE"
$staticContextValue = $staticContext ? "TRUE" : "FALSE"

# Initialize the dictionary (hash table) for test results
$dictionary = @{}

# Define the TestData class
class TestData {
    [string]$TestFile
    [int]$PassCount
    [int]$FailCount
    [bool]$IsSuccess

    TestData([string]$testFile, [int]$passCount, [int]$failCount, [bool]$isSuccess) {
        $this.TestFile = $testFile
        $this.PassCount = $passCount
        $this.FailCount = $failCount
        $this.IsSuccess = $isSuccess
    }

    # Override ToString method for better display
    [string]ToString() {
        return "TestFile: $($this.TestFile), PassCount: $($this.PassCount), FailCount: $($this.FailCount), IsSuccess: $($this.IsSuccess)"
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
        if (-not $value.IsSuccess) {
            $dictionary[$key].IsSuccess = $false
        }
        Write-Host "Updated key '$key' with value '$($dictionary[$key])'."
    } else {
        # Add the key-value pair if the key does not exist
        $dictionary[$key] = $value
        Write-Host "Added key '$key' with value '$value'."
    }
}

# Function to update test data based on .trx files
function Update-TestData {
    param (
        [string]$folderPath,
        [datetime]$timeThreshold,
        [string]$testFile
    )

    AddOrUpdate -key $testFile -value (New-Object TestData($testFile, 0, 0, $true))
    
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
            $isSuccess = $failCount -eq 0 -and $passCount -gt 0

            AddOrUpdate -key $testFile -value (New-Object TestData($testFile, $passCount, $failCount, $isSuccess))
        }
    }
}

# Verify Azure CLI context
if ($tenantId) {
    $azTenantId = az account show --query tenantId --output tsv 2>$null
    
    if ($azTenantId -ne $tenantId) {
        Write-Warning "Azure CLI tenant ID ($azTenantId) does not match specified tenant ID ($tenantId)."
        $useCliAuth = Read-Host -Prompt "Would you like to log in with the correct tenant ID? (Y/N)"
        
        if ($useCliAuth -eq "Y" -or $useCliAuth -eq "y") {
            az login --tenant $tenantId
        }
    }
}

# Build the latest debug version of Test Engine from source if requested
if ($compile) {
    Set-Location "$currentDirectory\..\..\src"
    Write-Host "Compiling the project..."
    dotnet build
    
    # Install Playwright if needed
    Start-Process -FilePath "pwsh" -ArgumentList "-Command `"..\bin\Debug\PowerAppsTestEngine\playwright.ps1 install`"" -Wait
    
    Set-Location ..\bin\Debug\PowerAppsTestEngine
} else {
    # Just navigate to the compiled binary
    Write-Host "Using pre-compiled binaries..."
    Set-Location ..\..\bin\Debug\PowerAppsTestEngine
}

# Find all the *.te.yaml files in the JavaScript tests directory
$testFiles = Get-ChildItem -Path "$currentDirectory\*.te.yaml" -File

Write-Host "Found $($testFiles.Count) test files to execute."

$totalTests = $testFiles.Count
$passedTests = 0

# Execute each test file
foreach ($testFile in $testFiles) {
    $testFilePath = $testFile.FullName
    $testFileName = $testFile.Name
    
    Write-Host "----------------------------------------" -ForegroundColor Yellow
    Write-Host "Executing test: $testFileName" -ForegroundColor Yellow
    Write-Host "----------------------------------------" -ForegroundColor Yellow
    
    $testStart = Get-Date
   
    dotnet PowerAppsTestEngine.dll -p "powerfx" -i "$testFilePath" -t $tenantId -e $environmentId -d "$environmentUrl" -l "Debug"
    
    # Update test results data
    Update-TestData -folderPath $folderPath -timeThreshold $testStart -testFile $testFileName
}

# Reset the location back to the original directory
Set-Location $currentDirectory

# Set the timeThreshold if provided as parameter
if ($lastRunTime) {
    try {
        $timeThreshold = [datetime]::ParseExact($lastRunTime, "yyyy-MM-dd HH:mm", $null)
    } catch {
        Write-Error "Invalid date format. Please use 'yyyy-MM-DD HH:mm'."
        $timeThreshold = $timeStart
    }
} else {
    $timeThreshold = $timeStart
}

# Generate HTML summary report with timestamp
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$reportPath = "$folderPath\summary_report_$timestamp.html"

# Function to generate HTML table representation of the TestData dictionary
function Generate-HTMLTable {
    param (
        [hashtable]$dictionary,
        [int]$total
    )

    # Initialize HTML table
    $htmlTable = @"
    <h2>Test Execution Results</h2>
    <table id="coverageTable">
        <tr>
            <th>Test File</th>
            <th>Pass Count</th>
            <th>Fail Count</th>
            <th>Status</th>
        </tr>
"@

    $passedFiles = 0
    # Iterate through the dictionary and add rows to the table
    foreach ($key in $dictionary.Keys | Sort-Object) {
        $value = $dictionary[$key]
        if (-not [string]::IsNullOrEmpty($value)) {
            $status = $value.IsSuccess ? "Pass" : "Fail"
            $statusClass = $value.IsSuccess ? "pass" : "fail"
            
            if ($value.IsSuccess) {
                $passedFiles++
            }
            
            $htmlTable += "<tr><td>$($value.TestFile)</td><td>$($value.PassCount)</td><td>$($value.FailCount)</td><td class='status-$statusClass'>$status</td></tr>"
        }
    }

    # Close the table
    $htmlTable += "</table>"
    
    $healthPercentage = $total -eq 0 ? "0" : ($passedFiles / $total * 100).ToString("0")
    
    # Add calculation and formula
    $mathFormula = @"
    <h3>Test Health Calculation:</h3>
    <div class="math-formula">
        <p>Health Percentage = (Number of Passing Test Files / Total Test Files) × 100</p>
        <p>Health Percentage = ($passedFiles / $total) × 100 = $healthPercentage%</p>
    </div>
"@

    $htmlTable += $mathFormula
    
    return @{
        Table = $htmlTable
        HealthPercentage = $healthPercentage
        PassedFiles = $passedFiles
    }
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

# Calculate totals
$passCount = ($testResults | Measure-Object -Property Passed -Sum).Sum
$failCount = ($testResults | Measure-Object -Property Failed -Sum).Sum
$totalTestCases = $passCount + $failCount

# Generate table and get health stats
$tableResult = Generate-HTMLTable -dictionary $dictionary -total $testFiles.Count
$healthPercentage = $tableResult.HealthPercentage
$healthPercentageValue = [int]$healthPercentage
$passedFiles = $tableResult.PassedFiles

# Create HTML report
$htmlReport = @"
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>D365 JavaScript Test Results</title>
    <script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
    <script src="https://cdn.jsdelivr.net/npm/tabulator-tables@5.2.7/dist/js/tabulator.min.js"></script>
    <link href="https://cdn.jsdelivr.net/npm/tabulator-tables@5.2.7/dist/css/tabulator.min.css" rel="stylesheet">
    <style>
        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            margin: 20px;
            background-color: #f8f9fa;
            color: #333;
        }
        h1, h2, h3 {
            color: #0078d4;
        }
        .dashboard {
            display: flex;
            flex-wrap: wrap;
            gap: 20px;
            margin-bottom: 30px;
        }
        .metric-card {
            background-color: white;
            border-radius: 8px;
            padding: 15px;
            box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
            flex: 1;
            min-width: 200px;
        }
        .metric-title {
            font-size: 16px;
            color: #666;
            margin-bottom: 10px;
        }
        .metric-value {
            font-size: 24px;
            font-weight: bold;
        }
        .health-good {
            color: #107c10;
        }
        .health-warning {
            color: #ff8c00;
        }
        .health-poor {
            color: #d13438;
        }
        .chart-container {
            width: 100%;
            max-width: 800px;
            margin: 0 auto 30px auto;
            background-color: white;
            border-radius: 8px;
            padding: 20px;
            box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
        }
        .status-pass {
            color: #107c10;
            font-weight: bold;
        }
        .status-fail {
            color: #d13438;
            font-weight: bold;
        }
        .math-formula {
            background-color: #f0f0f0;
            padding: 15px;
            border-radius: 5px;
            margin-top: 20px;
            font-family: 'Consolas', monospace;
        }
        #coverageTable {
            width: 100%;
            border-collapse: collapse;
            margin-bottom: 20px;
        }
        #coverageTable th, #coverageTable td {
            border: 1px solid #ddd;
            padding: 8px;
            text-align: left;
        }
        #coverageTable th {
            background-color: #0078d4;
            color: white;
        }
        #coverageTable tr:nth-child(even) {
            background-color: #f2f2f2;
        }
    </style>
</head>
<body>
    <h1>D365 JavaScript Test Results</h1>
    <p>Run Date: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")</p>
    
    <div class="dashboard">
        <div class="metric-card">
            <div class="metric-title">Test Files</div>
            <div class="metric-value">$($testFiles.Count)</div>
        </div>
        <div class="metric-card">
            <div class="metric-title">Passing Files</div>
            <div class="metric-value">$passedFiles / $($testFiles.Count)</div>
        </div>
        <div class="metric-card">
            <div class="metric-title">Test Cases</div>
            <div class="metric-value">$totalTestCases</div>
        </div>
        <div class="metric-card">
            <div class="metric-title">Health Score</div>
            <div class="metric-value $($healthPercentageValue -ge 80 ? 'health-good' : ($healthPercentageValue -ge 60 ? 'health-warning' : 'health-poor'))">$healthPercentage%</div>
        </div>
    </div>
    
    <div class="chart-container">
        <h2>Test Results Summary</h2>
        <canvas id="testChartSummary"></canvas>
    </div>
    
    <div class="chart-container">
        <h2>Test Files Status</h2>
        <canvas id="fileStatusChart"></canvas>
    </div>
    <div class="chart-container">
        $($tableResult.Table)
    </div>
    
    <div class="chart-container">
        <h2>Interactive Test Results Table</h2>
        <div id="results-table"></div>
    </div>
    
    <script>
        // Test Cases Chart (Pass/Fail)
        const testCasesCtx = document.getElementById('testChartSummary').getContext('2d');
        const testCasesChart = new Chart(testCasesCtx, {
            type: 'pie',
            data: {
                labels: ['Passed', 'Failed'],
                datasets: [{
                    data: [$passCount, $failCount],
                    backgroundColor: ['#107c10', '#d13438'],
                    hoverOffset: 4
                }]
            },
            options: {
                responsive: true,
                plugins: {
                    title: {
                        display: true,
                        text: 'Test Case Results'
                    },
                    legend: {
                        position: 'bottom'
                    }
                }
            }
        });
        
        // Test Files Chart (Pass/Fail)
        const filesCtx = document.getElementById('fileStatusChart').getContext('2d');
        const filesChart = new Chart(filesCtx, {
            type: 'pie',
            data: {
                labels: ['Passing Files', 'Failing Files'],
                datasets: [{
                    data: [$passedFiles, $($testFiles.Count - $passedFiles)],
                    backgroundColor: ['#107c10', '#d13438'],
                    hoverOffset: 4
                }]
            },
            options: {
                responsive: true,
                plugins: {
                    title: {
                        display: true,
                        text: 'Test File Status'
                    },
                    legend: {
                        position: 'bottom'
                    }
                }
            }
        });
        
        // Prepare table data from HTML table
        const tableData = [];
        const tableRows = document.querySelectorAll("#coverageTable tr:not(:first-child)");
        
        tableRows.forEach(row => {
            const cells = row.querySelectorAll("td");
            if (cells.length === 4) {
                tableData.push({
                    "testFile": cells[0].innerText,
                    "passCount": parseInt(cells[1].innerText) || 0,
                    "failCount": parseInt(cells[2].innerText) || 0, 
                    "status": cells[3].innerText
                });
            }
        });
        
        // Initialize Tabulator with the extracted data
        if (tableData.length > 0) {
            var table = new Tabulator("#results-table", {
                data: tableData,
                layout: "fitColumns",
                responsiveLayout: true,
                columns: [
                    {title: "Test File", field: "testFile", sorter: "string", width: 250},
                    {title: "Pass Count", field: "passCount", sorter: "number", hozAlign: "right"},
                    {title: "Fail Count", field: "failCount", sorter: "number", hozAlign: "right"},
                    {title: "Status", field: "status", sorter: "string", formatter: function(cell) {
                        return cell.getValue() === "Pass" ? 
                            "<span style='color:#107c10; font-weight:bold;'>✓ Pass</span>" : 
                            "<span style='color:#d13438; font-weight:bold;'>✗ Fail</span>";
                    }}
                ]
            });
        }
    </script>
</body>
</html>
"@

# Save HTML report
$htmlReport | Out-File -FilePath $reportPath -Encoding UTF8

Write-Host "HTML summary report generated at $reportPath"
Write-Host "Opening report in browser..."

# Open the report in the default browser
Invoke-Item $reportPath
