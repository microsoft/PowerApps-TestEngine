# Get current directory so we can reset back to it after running the tests
$currentDirectory = Get-Location

$jsonContent = Get-Content -Path .\config.json -Raw
$config = $jsonContent | ConvertFrom-Json
$tenantId = $config.tenantId
$environmentId = $config.environmentId

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

$mdaUrl = "$environmentUrl/main.aspx?appid=$appId&pagetype=custom&name=admin_initialsetuppage_d45cf"

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
dotnet PowerAppsTestEngine.dll -u "browser" -p "mda" -a "none" -r True -i "$currentDirectory\record.fx.yaml" -t $tenantId -e $environmentId -d "$mdaUrl" -w True

# Reset the location back to the original directory.
Set-Location $currentDirectory