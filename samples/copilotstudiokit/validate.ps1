# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# Script: validate.ps1
# This script validates that all required tools and dependencies are installed for the Copilot Studio Kit testing
#
# Parameters:
#   -Fix: Automatically attempt to fix missing dependencies (not yet implemented)
#   -Verbose: Enable verbose output
#   -ShowSensitiveValues: Show actual values for sensitive data like email, tenant ID, environment ID
#                        (by default these are masked with ***)

param(
    [switch]$Fix = $false,
    [switch]$Verbose = $false,
    [switch]$ShowSensitiveValues = $false
)

# If -Verbose is specified, enable verbose output system-wide
if ($Verbose) {
    $VerbosePreference = "Continue"
}

# Helper function to mask sensitive values for screen recording safety
function Hide-SensitiveValue {
    param(
        [string]$Value,
        [int]$VisibleChars = 4
    )
    
    if ([string]::IsNullOrEmpty($Value)) {
        return $Value
    }
    
    # If ShowSensitiveValues is enabled, return the value as-is
    if ($ShowSensitiveValues) {
        return $Value
    }    # For URLs, show protocol and domain structure but mask the subdomain
    if ($Value -match '^(https?://)([^\.]+)\.(.+)$') {
        $protocol = $matches[1]  # e.g., "https://"
        $subdomain = $matches[2]  # the subdomain part to mask
        $domain = $matches[3]     # e.g., "dynamics.crm.com"
        
        return "$protocol****.$domain"
    }
    
    # For file paths containing Users\username, mask the username
    if ($Value -match '([A-Za-z]:\\Users\\)([^\\]+)(\\.*)?') {
        $prefix = $matches[1]  # e.g., "C:\Users\"
        $username = $matches[2]  # the username part
        $suffix = $matches[3]   # the rest of the path (if any)
        
        if ([string]::IsNullOrEmpty($suffix)) {
            $suffix = ""
        }
        
        return "$prefix***$suffix"
    }
    
    # For emails, show first few chars and domain
    if ($Value -match '^[^@]+@[^@]+$') {
        $parts = $Value.Split('@')
        $username = $parts[0]
        $domain = $parts[1]
        
        if ($username.Length -le $VisibleChars) {
            return "***@$domain"
        } else {
            return "$($username.Substring(0, $VisibleChars))***@$domain"
        }
    }
      # Check if it's a GUID format (8-4-4-4-12 characters separated by hyphens)
    if ($Value -match '^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$') {
        # For GUIDs, show the first section and mask the rest
        $parts = $Value.Split('-')
        $firstSection = $parts[0]
        $maskedParts = @($firstSection)
        
        # Mask remaining sections by replacing each alphanumeric character with *
        for ($i = 1; $i -lt $parts.Length; $i++) {
            $maskedSection = $parts[$i] -replace '[0-9a-fA-F]', '*'
            $maskedParts += $maskedSection
        }
        
        return $maskedParts -join '-'
    }
    
    # For other long values, show first few chars
    if ($Value.Length -le $VisibleChars) {
        return "***"
    } else {
        return "$($Value.Substring(0, $VisibleChars))***"
    }
}

# Helper function to format text tables
function Format-TextTable {
    param (
        [Parameter(ValueFromPipeline = $true)]
        [string[]]$Data,
        [int]$ColumnCount = 3
    )
    
    begin {
        $rows = @()
        $index = 0
        $headerShown = $false
    }
    
    process {
        foreach ($item in $Data) {
            if ($index % $ColumnCount -eq 0) {
                # Start a new row
                $currentRow = @()
                $rows += $currentRow
            }
            
            $currentRow += $item
            $index++
        }
    }
    
    end {
        # Calculate column widths
        $columnWidths = @(0) * $ColumnCount
        foreach ($row in $rows) {
            for ($i = 0; $i -lt [Math]::Min($row.Count, $ColumnCount); $i++) {
                if ($row[$i].Length -gt $columnWidths[$i]) {
                    $columnWidths[$i] = $row[$i].Length
                }
            }
        }
        
        # Output header row
        Write-Host ""
        $headerRow = $rows[0]
        $line = "| "
        for ($i = 0; $i -lt [Math]::Min($headerRow.Count, $ColumnCount); $i++) {
            $line += $headerRow[$i].PadRight($columnWidths[$i]) + " | "
        }
        Write-Host $line -ForegroundColor Cyan
        
        # Output header separator
        $line = "|-"
        for ($i = 0; $i -lt $ColumnCount; $i++) {
            $line += "-" * $columnWidths[$i] + "-|-"
        }
        Write-Host $line -ForegroundColor Cyan
        
        # Output data rows
        for ($rowIndex = 1; $rowIndex -lt $rows.Count; $rowIndex++) {
            $dataRow = $rows[$rowIndex]
            $line = "| "
            for ($i = 0; $i -lt [Math]::Min($dataRow.Count, $ColumnCount); $i++) {
                $line += $dataRow[$i].PadRight($columnWidths[$i]) + " | "
            }
            Write-Host $line -ForegroundColor White
        }
        Write-Host ""
    }
}

$requiredTools = @(
    @{
        Name = ".NET SDK 8.0"
        Command = "dotnet --list-sdks"
        TestExpression = "8\."
        InstallCommand = "winget install Microsoft.DotNet.SDK.8"
        InstallNotes = "You can download .NET 8.0 SDK from https://dotnet.microsoft.com/download/dotnet/8.0"
    },
    @{
        Name = "PowerShell"
        Command = "pwsh --version"
        TestExpression = "PowerShell"
        InstallCommand = "winget install --id Microsoft.PowerShell --source winget"
        InstallNotes = "You can download PowerShell from https://learn.microsoft.com/powershell/scripting/install/installing-powershell"
    },    @{
        Name = "Power Platform CLI"
        Command = "pac help"
        TestExpression = "Microsoft PowerPlatform CLI"
        MinVersion = "1.43.6"
        InstallCommand = "dotnet tool install --global Microsoft.PowerApps.CLI.Tool"
        InstallNotes = "You can install Power Platform CLI using: dotnet tool install --global Microsoft.PowerApps.CLI.Tool. To upgrade an existing installation, use: dotnet tool update --global Microsoft.PowerApps.CLI.Tool"
    },
    @{
        Name = "Git"
        Command = "git --version"
        TestExpression = "git version"
        InstallCommand = "winget install --id Git.Git -e --source winget"
        InstallNotes = "You can download Git from https://git-scm.com/book/en/v2/Getting-Started-Installing-Git"
    },
    @{
        Name = "Azure CLI"
        Command = "az --version"
        TestExpression = "azure-cli"
        InstallCommand = "winget install -e --id Microsoft.AzureCLI"
        InstallNotes = "You can download Azure CLI from https://learn.microsoft.com/cli/azure/install-azure-cli"
    },
    @{
        Name = "Visual Studio Code (optional)"
        Command = "code --version"
        TestExpression = "[0-9]+\.[0-9]+"
        InstallCommand = "winget install -e --id Microsoft.VisualStudioCode"
        InstallNotes = "You can download VS Code from https://code.visualstudio.com/docs/setup/setup-overview"
        Optional = $true
    }
)

function Test-Command {
    param (
        [string]$Command
    )
    
    try {
        # Extract just the command name (before any arguments)
        $commandName = ($Command -split ' ')[0]
        
        # Check if the command exists
        if (-not (Get-Command $commandName -ErrorAction SilentlyContinue)) {
            if ($Verbose) {
                Write-Host "Command not found: $commandName" -ForegroundColor Yellow
            }
            return @()
        }
        
        # Execute the command and capture all output (both standard output and error output)
        $outputLines = @()
        
        # Use Start-Process for better control over execution and output
        $tempFile = [System.IO.Path]::GetTempFileName()
        try {
            # Construct the command with arguments
            $commandArgs = $Command.Substring($commandName.Length).Trim()
            
            if ($Verbose) {
                Write-Host "Executing: $commandName $commandArgs" -ForegroundColor DarkCyan
            }
            
            # Execute the command and redirect output to a temp file
            if ($commandArgs) {
                $process = Start-Process -FilePath $commandName -ArgumentList $commandArgs -NoNewWindow -Wait -RedirectStandardOutput $tempFile -RedirectStandardError "${tempFile}.err" -PassThru
            } else {
                $process = Start-Process -FilePath $commandName -NoNewWindow -Wait -RedirectStandardOutput $tempFile -RedirectStandardError "${tempFile}.err" -PassThru
            }
            
            # Read output from temp files
            if (Test-Path $tempFile) {
                $outputLines += Get-Content -Path $tempFile -ErrorAction SilentlyContinue
            }
            
            if (Test-Path "${tempFile}.err") {
                $errorContent = Get-Content -Path "${tempFile}.err" -ErrorAction SilentlyContinue
                $outputLines += $errorContent
                
                if ($Verbose -and $errorContent) {
                    Write-Host "Command returned error output:" -ForegroundColor Yellow
                    $errorContent | ForEach-Object { Write-Host "  $_" -ForegroundColor Yellow }
                }
            }
            
            # Check if command executed successfully
            if ($process.ExitCode -ne 0 -and $outputLines.Count -eq 0) {
                if ($Verbose) {
                    Write-Host "Command failed with exit code: $($process.ExitCode)" -ForegroundColor Red
                }
                return @()
            }
        }
        finally {
            # Clean up temp files
            if (Test-Path $tempFile) { Remove-Item -Path $tempFile -Force -ErrorAction SilentlyContinue }
            if (Test-Path "${tempFile}.err") { Remove-Item -Path "${tempFile}.err" -Force -ErrorAction SilentlyContinue }
        }
        
        if ($outputLines.Count -gt 0) {
            if ($Verbose) {
                Write-Host "Command output ($($outputLines.Count) lines):" -ForegroundColor DarkCyan
                $outputLines | Select-Object -First 5 | ForEach-Object { Write-Host "  $_" -ForegroundColor DarkGray }
                if ($outputLines.Count -gt 5) {
                    Write-Host "  ... and $($outputLines.Count - 5) more lines" -ForegroundColor DarkGray
                }
            }
            return $outputLines
        }
        else {
            if ($Verbose) {
                Write-Host "Command produced no output but executed successfully" -ForegroundColor DarkCyan
            }
            # Return an array with a single "Command exists" string if no output
            return @("Command exists")
        }
    }
    catch {
        if ($Verbose) {
            Write-Host "Error executing command: $Command" -ForegroundColor Red
            Write-Host $_.Exception.Message -ForegroundColor Red
        }
        return @()
    }
}

function Format-ValidationOutput {
    param (
        [string]$Name,
        [bool]$IsInstalled,
        [bool]$IsOptional = $false,
        [string]$Version = $null,
        [string]$MinVersion = $null
    )
      $status = if ($IsInstalled) { 
        "[OK] Installed" 
    } elseif ($IsOptional) { 
        "[!] Not installed (Optional)" 
    } elseif ($Version -and $MinVersion) { 
        "[X] Installed (v$Version) but requires v$MinVersion or higher" 
    } else { 
        "[X] Not installed" 
    }
    
    $color = if ($IsInstalled) { "Green" } else { if ($IsOptional) { "Yellow" } else { "Red" } }
    
    Write-Host "$Name : " -NoNewline
    Write-Host $status -ForegroundColor $color
}

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "Copilot Studio Kit - Environment Validation Tool" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

$allRequirementsMet = $true
$installCommands = @()

foreach ($tool in $requiredTools) {
    if ($Verbose) {
        Write-Host "Checking for tool: $($tool.Name)" -ForegroundColor Cyan
    }
    
    $resultLines = Test-Command -Command $tool.Command
    $isInstalled = $false
    $version = $null
    
    if ($resultLines.Count -gt 0) {
        # Get the pattern
        $pattern = $tool.TestExpression
        
        # Check each line of output for a match
        foreach ($line in $resultLines) {
            if ($line -match $pattern) {
                $isInstalled = $true
                
                # Handle version checking for Power Platform CLI
                if ($tool.Name -eq "Power Platform CLI" -and $tool.MinVersion) {
                    # Look for the version line in the output
                    foreach ($versionLine in $resultLines) {
                        if ($versionLine -match "Version:\s+([\d\.]+)") {
                            $version = $matches[1]
                            
                            # Compare versions
                            $minVersionRequired = [version]$tool.MinVersion
                            $currentVersion = [version]($version -replace '(\d+\.\d+\.\d+).*', '$1') # Extract just major.minor.patch
                            
                            if ($currentVersion -lt $minVersionRequired) {
                                $isInstalled = $false
                                if ($Verbose) {
                                    Write-Host "  Power Platform CLI version $currentVersion is less than required $minVersionRequired" -ForegroundColor Yellow
                                }
                            }
                            else {
                                if ($Verbose) {
                                    Write-Host "  Power Platform CLI version $currentVersion meets minimum required $minVersionRequired" -ForegroundColor Green
                                }
                            }
                            break
                        }
                    }
                }
                break
            }
        }
        
        # Debug output to help troubleshoot
        if ($Verbose) {
            Write-Host "  Tool: $($tool.Name)" -ForegroundColor Cyan
            Write-Host "  Pattern: $pattern" -ForegroundColor Cyan
            Write-Host "  Lines found: $($resultLines.Count)" -ForegroundColor Cyan
            Write-Host "  First few lines:" -ForegroundColor Cyan
            $resultLines | Select-Object -First 3 | ForEach-Object { Write-Host "    $_" -ForegroundColor DarkGray }
            if ($version) {
                Write-Host "  Version detected: $version" -ForegroundColor Cyan
            }
            Write-Host "  Installed: $isInstalled" -ForegroundColor $(if ($isInstalled) { "Green" } else { "Yellow" })
        }
    }
    
    Format-ValidationOutput -Name $tool.Name -IsInstalled $isInstalled -IsOptional ($tool.Optional -eq $true) -Version $version -MinVersion $tool.MinVersion
    
    if (-not $isInstalled -and (-not $tool.Optional)) {
        $allRequirementsMet = $false
        $installCommands += @{
            Name = $tool.Name
            Command = $tool.InstallCommand
            Notes = $tool.InstallNotes
        }
    }
}

Write-Host ""
if ($allRequirementsMet) {
    Write-Host "All required tools are installed. You're ready to run the tests!" -ForegroundColor Green
} else {
    Write-Host "Some required tools are missing. Please install them before running the tests." -ForegroundColor Red
    Write-Host ""
    Write-Host "Installation commands:" -ForegroundColor Yellow
    
    foreach ($cmd in $installCommands) {
        Write-Host ""
        Write-Host "$($cmd.Name):" -ForegroundColor Cyan
        Write-Host "  Command: $($cmd.Command)"
        Write-Host "  Notes: $($cmd.Notes)"
        
        if ($Fix) {
            Write-Host ""
            Write-Host "Attempting to install $($cmd.Name)..." -ForegroundColor Cyan
            try {
                Invoke-Expression $cmd.Command
                Write-Host "Installation completed. Please restart the PowerShell session." -ForegroundColor Green
            }
            catch {
                Write-Host "Installation failed. Please install manually." -ForegroundColor Red
                Write-Host $_.Exception.Message -ForegroundColor Red
            }
        }
    }
    
    if (-not $Fix) {
        Write-Host ""
        Write-Host "You can run this script with -Fix parameter to attempt automatic installation of missing components:" -ForegroundColor Yellow
        Write-Host "  .\validate.ps1 -Fix"
    }
}

# Function to check if current directory is part of PowerApps-TestEngine
function Test-IsInTestEngineRepo {
    try {
        # Get the git root directory
        $gitRootDir = git rev-parse --show-toplevel 2>$null
        if ($LASTEXITCODE -ne 0) {
            # Not in a git repo
            return $false
        }
        
        # Check if the directory name indicates it's the PowerApps-TestEngine repo
        $dirName = Split-Path -Path $gitRootDir -Leaf
        if ($dirName -eq "PowerApps-TestEngine") {
            return $true
        }
        
        # Check remote URLs for PowerApps-TestEngine
        $remotes = git remote -v 2>$null
        foreach ($remote in $remotes) {
            if ($remote -like "*PowerApps-TestEngine*") {
                return $true
            }
        }
        
        return $false
    }
    catch {
        return $false
    }
}

# Check for Power Platform Test Engine
Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "Power Platform Test Engine Status" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan

# Check if we're already in the PowerApps-TestEngine repository
$isInTestEngineRepo = Test-IsInTestEngineRepo

if ($isInTestEngineRepo) {
    Write-Host "Detected current directory is part of PowerApps-TestEngine repository" -ForegroundColor Green
    
    # Get the root directory of the repository
    $repoRootDir = git rev-parse --show-toplevel 2>$null
    
    # Check if it's properly built
    $binPath = Join-Path -Path $repoRootDir -ChildPath "bin\Debug\PowerAppsTestEngine\PowerAppsTestEngine.dll"
    if (Test-Path $binPath) {
        $maskedBinPath = Hide-SensitiveValue -Value $binPath
        Write-Host "PowerAppsTestEngine.dll is available at: $maskedBinPath" -ForegroundColor Green
    } else {
        Write-Host "PowerAppsTestEngine.dll is not built yet. Run 'dotnet build' in the src directory to build it." -ForegroundColor Yellow
    }
} else {
    # Check for local PowerApps-TestEngine subdirectory
    $testEnginePath = Join-Path -Path $PSScriptRoot -ChildPath "PowerApps-TestEngine"
      if (Test-Path $testEnginePath) {
        $maskedTestEnginePath = Hide-SensitiveValue -Value $testEnginePath
        Write-Host "Power Platform Test Engine is available at: $maskedTestEnginePath" -ForegroundColor Green
        
        # Check if it's properly built
        $binPath = Join-Path -Path $testEnginePath -ChildPath "bin\Debug\PowerAppsTestEngine\PowerAppsTestEngine.dll"
        if (Test-Path $binPath) {
            Write-Host "PowerAppsTestEngine.dll is available." -ForegroundColor Green
        } else {
            Write-Host "PowerAppsTestEngine.dll is not built yet. Run RunTests.ps1 to clone and build it." -ForegroundColor Yellow
        }
    } else {
        Write-Host "Power Platform Test Engine is not cloned yet. Run RunTests.ps1 to clone and build it." -ForegroundColor Yellow
    }
}

# Check for environment configuration
$configPath = Join-Path -Path $PSScriptRoot -ChildPath ".\config.json"
if (Test-Path $configPath) {
    Write-Host "==================================================" -ForegroundColor Cyan
    Write-Host "Environment Configuration Status" -ForegroundColor Cyan
    Write-Host "==================================================" -ForegroundColor Cyan
    $maskedConfigPath = Hide-SensitiveValue -Value $configPath
    Write-Host "Configuration file found at: $maskedConfigPath" -ForegroundColor Green
    
    try {
        $config = Get-Content -Path $configPath -Raw | ConvertFrom-Json
        $envId = $config.environmentId
        $envUrl = $config.environmentUrl
        $tenantId = $config.tenantId
          if ([string]::IsNullOrEmpty($envId) -or $envId -eq "00000000-0000-0000-0000-000000000000") {
            Write-Host "Environment ID is not properly configured." -ForegroundColor Red
        } else {
            $maskedEnvId = Hide-SensitiveValue -Value $envId
            Write-Host "Environment ID is configured: $maskedEnvId" -ForegroundColor Green
        }
          if ([string]::IsNullOrEmpty($tenantId) -or $tenantId -eq "00000000-0000-0000-0000-000000000000") {
            Write-Host "Tenant ID is not properly configured." -ForegroundColor Red
        } else {
            $maskedTenantId = Hide-SensitiveValue -Value $tenantId
            Write-Host "Tenant ID is configured: $maskedTenantId" -ForegroundColor Green
            
            # Check if Azure CLI is installed and check if tenant ID matches
            $azCliResult = Test-Command -Command "az --version"
            if ($azCliResult.Count -gt 0) {
                Write-Host "Checking if tenant ID matches Azure account..." -ForegroundColor Yellow
                try {
                    $azAccountOutput = Test-Command -Command "az account show --query tenantId -o tsv"
                    if ($azAccountOutput.Count -gt 0) {
                        # Join all output lines and trim whitespace
                        $azureTenantId = ($azAccountOutput -join "").Trim()
                        
                        if (-not [string]::IsNullOrEmpty($azureTenantId))
                        {
                            if ($tenantId -eq $azureTenantId) 
                            {
                                Write-Host "Tenant ID in config.json matches Azure account tenant ID." -ForegroundColor Green
                                
                                # Now validate the environment ID using Power Platform API
                                Write-Host "Validating Power Platform environment ID..." -ForegroundColor Yellow
                                
                                try {
                                    # Get access token for Power Platform API
                                    $environments = Test-Command -Command "pac env list --json"
                                    if ($environments.Count -gt 0) {
                                        $json = ($environments -join "").Trim()
                                        if (-not [string]::IsNullOrEmpty($json)) {
                                            $data = $json | ConvertFrom-Json
                                            
                                            # Trim the trailing slash from $envUrl for comparison
                                            $normalizedEnvUrl = $envUrl.TrimEnd('/')
                                            
                                            # Add verbose output for debugging
                                            Write-Verbose "Config Environment URL: $envUrl"
                                            Write-Verbose "Normalized Environment URL for comparison: $normalizedEnvUrl"
                                            
                                            $environmentMatch = $data | Where-Object { 
                                                # Also normalize the environment URLs from pac command by trimming any possible trailing slashes
                                                $pacEnvUrl = $_.EnvironmentUrl.TrimEnd('/')
                                                Write-Verbose "Comparing with PAC Environment URL: $($_.EnvironmentUrl) (Normalized: $pacEnvUrl)"
                                                $pacEnvUrl -eq $normalizedEnvUrl 
                                            }
                                            if ($environmentMatch -and $environmentMatch.Count -eq 1 ) 
                                            {
                                                $maskedEnvUrl = Hide-SensitiveValue -Value $envUrl
                                                Write-Host "Environment Match found: $maskedEnvUrl $($environmentMatch[0].FriendlyName)" -ForegroundColor Green
                                                if (-not ($environmentMatch[0].EnvironmentIdentifier.Id -eq $envId)) {
                                                    $maskedConfigEnvId = Hide-SensitiveValue -Value $envId
                                                    $maskedPacEnvId = Hide-SensitiveValue -Value $environmentMatch[0].EnvironmentIdentifier.Id
                                                    Write-Host "WARNING: Environment ID in config.json ($maskedConfigEnvId) does not match PAC environment ID ($maskedPacEnvId)!" -ForegroundColor Yellow
                                                }
                                                else {
                                                    $maskedEnvId = Hide-SensitiveValue -Value $envId
                                                    Write-Host "Environment ID matches: $maskedEnvId" -ForegroundColor Green
                                                }
                                            } 
                                            else
                                            {
                                                Write-Host "Error validating environment ID" -ForegroundColor Red
                                                foreach ($env in $data) {
                                                    $maskedEnvironmentUrl = Hide-SensitiveValue -Value $env.EnvironmentUrl
                                                    $maskedEnvironmentId = Hide-SensitiveValue -Value $env.EnvironmentIdentifier.Id
                                                    Write-Host "Environment: $maskedEnvironmentUrl - $($env.FriendlyName) (ID: $maskedEnvironmentId)"
                                                }
                                            }
                                        } 
                                        else
                                        {
                                            Write-Host "Could not parse environment data from PAC command." -ForegroundColor Yellow
                                        }
                                    }
                                    else
                                    {
                                        Write-Host "Failed to retrieve environments from PAC command" -ForegroundColor Yellow
                                    }
                                }
                                catch {
                                    Write-Host "Error validating environment ID: $_" -ForegroundColor Red
                                }
                            }    
                            else 
                            {
                                $maskedConfigTenantId = Hide-SensitiveValue -Value $tenantId
                                $maskedAzureTenantId = Hide-SensitiveValue -Value $azureTenantId
                                Write-Host "WARNING: Tenant ID in config.json ($maskedConfigTenantId) does not match Azure account tenant ID ($maskedAzureTenantId)!" -ForegroundColor Red
                                Write-Host "         You may need to run 'az login' with the correct account or update your config.json." -ForegroundColor Yellow
                            }
                        } else {
                            Write-Host "Could not retrieve Azure tenant ID. Are you logged in? Try running 'az login'." -ForegroundColor Yellow
                        }
                    } else {
                        Write-Host "Not logged in to Azure CLI. Run 'az login' to authenticate." -ForegroundColor Yellow
                    }
                }
                catch {
                    Write-Host "Error checking Azure tenant ID: $_" -ForegroundColor Red
                    Write-Host "Make sure you're logged in to Azure CLI with 'az login'" -ForegroundColor Yellow
                }
            }
        }
    }
    catch {
        Write-Host "Error parsing config.json. Please check the file format." -ForegroundColor Red
    }
} else {
    Write-Host ""
    Write-Host "==================================================" -ForegroundColor Cyan
    Write-Host "Environment Configuration Status" -ForegroundColor Cyan
    Write-Host "==================================================" -ForegroundColor Cyan
    Write-Host "Configuration file not found. Please create config.json with your environment settings." -ForegroundColor Red
}

Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "Storage State Credential Validation" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan

if (Test-Path $configPath) {
    try {
        # Check if user1Email is specified in config.json
        if ($config.user1Email) {
            $user1Email = $config.user1Email
            $maskedUser1Email = Hide-SensitiveValue -Value $user1Email
            Write-Host "User email found: $maskedUser1Email" -ForegroundColor Green
            
            # Extract username without domain for storage state file naming
            $userNameOnly = $user1Email.Split('@')[0]
            $maskedUserNameOnly = Hide-SensitiveValue -Value $userNameOnly
            Write-Host "Username for storage state: $maskedUserNameOnly" -ForegroundColor Green
            
            # Define the path to TestEngine temp directory
            $testEngineStorageDir = "$env:USERPROFILE\AppData\Local\Temp\Microsoft\TestEngine"
              # Check if TestEngine directory exists
            if (Test-Path -Path $testEngineStorageDir) {
                $maskedTestEngineStorageDir = Hide-SensitiveValue -Value $testEngineStorageDir
                Write-Host "TestEngine directory found: $maskedTestEngineStorageDir" -ForegroundColor Green
                
                # Look for storage state file matching the username
                $storageFiles = Get-ChildItem -Path $testEngineStorageDir -Filter ".storage-state-$userNameOnly*" -ErrorAction SilentlyContinue
                  if ($storageFiles.Count -gt 0) {
                    Write-Host "Found $($storageFiles.Count) storage state file(s) for $($maskedUserNameOnly):" -ForegroundColor Green
                    
                    foreach ($file in $storageFiles) {
                        $maskedFilePath = Hide-SensitiveValue -Value $file.FullName
                        Write-Host "  - $maskedFilePath" -ForegroundColor Green
                        
                        # Check state.json file
                        $stateJsonPath = Join-Path -Path $file.FullName -ChildPath "state.json"
                        if (Test-Path -Path $stateJsonPath) {
                            Write-Host "    - state.json exists" -ForegroundColor Green
                            
                            # Try to read the file content
                            try {
                                $content = Get-Content -Path $stateJsonPath -Raw -ErrorAction Stop
                                
                                # Check if the content looks like base64
                                $isBase64 = $content -match '^[a-zA-Z0-9+/]+={0,2}$'
                                
                                if ($isBase64) {
                                    Write-Host "    - state.json content appears to be Base64 encoded" -ForegroundColor Green
                                          # Try to decode Base64
                                    try {
                                        $decoded = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($content))
                                        Write-Host "    - Base64 content successfully decoded" -ForegroundColor Green
                                    }
                                    catch {
                                        Write-Host "    - Failed to decode Base64 content: $_" -ForegroundColor Red
                                    }
                                }
                                else {
                                    Write-Host "    - state.json content is not Base64 encoded" -ForegroundColor Red
                                }
                            }
                            catch {
                                Write-Host "    - Failed to read state.json: $_" -ForegroundColor Red
                            }
                        }
                        else {
                            Write-Host "    - state.json file not found" -ForegroundColor Red
                        }
                    }
                }                else {
                    Write-Host "No storage state files found for $maskedUserNameOnly" -ForegroundColor Yellow
                    Write-Host "Run RunTests.ps1 first to create credentials or check if user email is correct" -ForegroundColor Yellow
                }}
            else {
                $maskedTestEngineStorageDir = Hide-SensitiveValue -Value $testEngineStorageDir
                Write-Host "TestEngine directory not found: $maskedTestEngineStorageDir" -ForegroundColor Yellow
                Write-Host "Run RunTests.ps1 first to create the directory and credentials" -ForegroundColor Yellow
            }
        }
        else {
            Write-Host "user1Email not specified in config.json" -ForegroundColor Yellow
        }
    }
    catch {
        Write-Host "Error checking storage state: $_" -ForegroundColor Red
    }
}
else {
    Write-Host "Configuration file not found. Cannot check storage state." -ForegroundColor Red
}

Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
