# PowerShell script to run F&O Test Engine tests
param(
    [string]$TestPlan = "testPlan1.fx.yaml",
    [string]$Environment = "hxyaptghn4u2suvpvr.operations.int.dynamics.com",
    [string]$OutputDir = "TestOutput",
    [switch]$Headless = $false
)

$ErrorActionPreference = "Stop"

# Get the script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# Construct the full URL
$Url = "https://$Environment/?origin=discovery&cmp=USMF&mi=DefaultDashboard&source=testengine"

Write-Host "Running F&O Test Engine Test" -ForegroundColor Green
Write-Host "Test Plan: $TestPlan" -ForegroundColor Cyan
Write-Host "Environment: $Environment" -ForegroundColor Cyan
Write-Host "URL: $Url" -ForegroundColor Cyan
Write-Host "Output Directory: $OutputDir" -ForegroundColor Cyan
Write-Host ""

# Find PowerAppsTestEngine.exe
$TestEnginePath = Join-Path $ScriptDir "..\..\bin\Debug\PowerAppsTestEngineWrapper\PowerAppsTestEngine.exe"

if (-not (Test-Path $TestEnginePath)) {
    Write-Host "Error: PowerAppsTestEngine.exe not found at: $TestEnginePath" -ForegroundColor Red
    Write-Host "Please build the solution first." -ForegroundColor Yellow
    exit 1
}

# Build arguments
$Arguments = @(
    "-i", (Join-Path $ScriptDir $TestPlan),
    "-e", $Environment,
    "-o", $OutputDir,
    "-p", "fno.portal",
    "-u", "storagestate",
    "-d", $Url
)

if ($Headless) {
    # Note: headless mode is controlled in the test plan YAML file
    Write-Host "Note: To enable headless mode, set 'headless: true' in the test plan YAML" -ForegroundColor Yellow
}

Write-Host "Executing: $TestEnginePath $Arguments" -ForegroundColor Gray
Write-Host ""

# Run the test
& $TestEnginePath @Arguments

$exitCode = $LASTEXITCODE
Write-Host ""
Write-Host "Test execution completed with exit code: $exitCode" -ForegroundColor $(if ($exitCode -eq 0) { "Green" } else { "Red" })

exit $exitCode
