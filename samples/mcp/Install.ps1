# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

if (Test-Path -Path "$PSScriptRoot\config.json") {
    $config = (Get-Content -Path .\config.json -Raw) | ConvertFrom-Json
    $uninstall = $config.uninstall
    $compile = $config.compile
} else {
    Write-Host "Config file not found, assuming default values."
    $uninstall = $true
    $compile = $true
}

# Get current directory so we can reset back to it after running the tests
$currentDirectory = Get-Location

# Stop any running testengine.server.mcp processes
Write-Host "Stopping any running testengine.server.mcp processes..."
Get-Process -Name "testengine.server.mcp*" -ErrorAction SilentlyContinue | ForEach-Object {
    Write-Host "Stopping process ID $($_.Id)..."
    Stop-Process -Id $_.Id -Force
}

if ($uninstall) {
    Write-Host "Uninstalling the Test Engine MCP Server..."
    # Uninstall the testengine.server.mcp tool if it is already installed
    $installedTools = dotnet tool list -g
    if ($installedTools -match "testengine.server.mcp") {
            Write-Host "Uninstalling testengine.server.mcp..."
            dotnet tool uninstall -g testengine.server.mcp
    }    
}

Set-Location "$currentDirectory\..\..\src\testengine.server.mcp"
if ($compile) {
    Write-Host "Compiling the project..."
    dotnet build
    dotnet pack -c Debug --output ./nupkgs
} else {
    Write-Host "Skipping compilation..."
}

Write-Host "Installing the Test Engine MCP Server..."

# Find the greatest version of the .nupkg file in the nupkgs folder
Write-Host "Finding the greatest version of the .nupkg file..."
$nupkgFolder = "$currentDirectory\..\..\src\testengine.server.mcp\nupkgs"
$nupkgFiles = Get-ChildItem -Path $nupkgFolder -Filter "*.nupkg" | Sort-Object -Property Name -Descending

if ($nupkgFiles.Count -eq 0) {
    Write-Host "No .nupkg files found in the nupkgs folder."
    exit 1
}

$latestNupkg = $nupkgFiles[0]
Write-Host "Installing the Test Engine MCP Server from $($latestNupkg.BaseName)..."
dotnet tool install -g testengine.server.mcp --add-source $nupkgFolder --version $($latestNupkg.BaseName -replace 'testengine.server.mcp.', '')

# Get the absolute path to start.te.yaml using forward slashes
$startTeYamlPath = (Join-Path -Path $currentDirectory -ChildPath "start.te.yaml").Replace("\", "/")

Write-Host "Add the following to you setting.json in Visual Studio Code"
Write-Host "1. Test Setting Configuration"
Write-Host "2. The Organization URL of the environment you want to test"

Write-Host "----------------------------------------"

Write-Host @"
{
    "mcp": {
        "servers": {
            "TestEngine": {
                "command": "testengine.server.mcp",
                "args": [
                    "$startTeYamlPath"
                ]
            }
        }
    }
}
"@

Set-Location $currentDirectory
