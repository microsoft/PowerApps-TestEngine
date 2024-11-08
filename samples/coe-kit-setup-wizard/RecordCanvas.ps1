# Get current directory so we can reset back to it after running the tests
$currentDirectory = Get-Location

$jsonContent = Get-Content -Path .\config.json -Raw
$config = $jsonContent | ConvertFrom-Json
$tenantId = $config.tenantId
$environmentId = $config.environmentId

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
dotnet PowerAppsTestEngine.dll -u "storagestate" -p "canvas" -a "none" -r True -i "$currentDirectory\record2.fx.yaml" -t $tenantId -e $environmentId -l Debug -w True

# Reset the location back to the original directory.
Set-Location $currentDirectory