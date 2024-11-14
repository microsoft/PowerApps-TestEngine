# Get current directory so we can reset back to it after running the tests
$currentDirectory = Get-Location

$config = (Get-Content -Path .\config.json -Raw) | ConvertFrom-Json
$tenantId = $config.tenantId
$environmentId = $config.environmentId
$user1Email = $config.user1Email

if ([string]::IsNullOrEmpty($environmentId)) {
    Write-Error "Environment not configured. Please update config.json"
    return
}

$textResult = [string] (pac env list)

$foundEnvironment = $false
$textResult = [string] (pac env select --environment $environmentId)

try{
    $textResult -match "'(https://[^\s']+)'"
    $environmentMatch = $matches
    $foundEnvironment = $true
} catch {
    
}

# Extract the URL using a general regular expression
if ($foundEnvironment -and $environmentMatch.Count -ge 1) {
    $environmentUrl = $environmentMatch[1].TrimEnd("/")
} else {
    Write-Output "URL not found. Please create authentication and re-run script"
    pac auth create --environment $environmentId
    return
}

$mdaUrl = "$environmentUrl/main.aspx?appname=sample_AccountAdmin&pagetype=custom&name=sample_custom_cf8e6"

# Build the latest debug version of Test Engine from source
Set-Location ..\..\src
dotnet build

if ($config.installPlaywright) {
    Start-Process -FilePath "pwsh" -ArgumentList "-Command `"..\bin\Debug\PowerAppsTestEngine\playwright.ps1 install`"" -Wait
} else {
    Write-Host "Skipped playwright install"
}

Set-Location ..\bin\Debug\PowerAppsTestEngine
$env:user1Email = $user1Email
# Run the tests for each user in the configuration file.
dotnet PowerAppsTestEngine.dll -u "storagestate" -p "mda" -a "none" -i "$currentDirectory\testPlan.fx.yaml" -t $tenantId -e $environmentId -d "$mdaUrl"

# Reset the location back to the original directory.
Set-Location $currentDirectory