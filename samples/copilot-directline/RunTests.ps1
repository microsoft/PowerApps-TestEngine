# Get current directory so we can reset back to it after running the tests
$currentDirectory = Get-Location

$config = Get-Content -Path .\config.json -Raw  | ConvertFrom-Json
$tenantId = $config.TenantId
$clientId = $config.ClientId

if ([string]::IsNullOrEmpty($tenantId)) {
    Write-Error "TenantId not configured. Please update config.json"
    return
}

if ([string]::IsNullOrEmpty($clientId)) {
    Write-Error "ClientId not configured. Please update config.json"
    return
}

# Build the latest debug version of Test Engine from source
Set-Location ..\..\src
dotnet build

Set-Location ..\bin\Debug\PowerAppsTestEngine
$env:TenantId = $tenantId
$env:ClientId = $clientId
# Run the tests for each user in the configuration file.
                                                                                                                                           
dotnet PowerAppsTestEngine.dll  -l "Trace" -u "storagestate" -p "copilot.directline" -a "none" -i "$currentDirectory\testPlan.fx.yaml" -t "$tenantId" -e "00000000-0000-0000-0000-111122223333" -w "True"

# Reset the location back to the original directory.
Set-Location $currentDirectory