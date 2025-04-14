---
title: Build from Source
---

# Building the Open Source Version of Test Engine

This guide provides instructions for building the open source version of the Test Engine from source. The open source version includes the latest features, supports, and an open source pull request approach for new changes to be reviewed and merged if they align with the product direction. The main branch is synchronized with the internal build process to distribute release builds as part of the pac CLI. The open source version is a great way to understand and extend the Test Engine and contribute to the wider testing community.

## Hosting Options

Depending on your options available you have a have a range of different hosting options to carry out a build from source strategy

### Hyper-V

If you are Windows and have Hyper-V enabled as a service you could take advantage of building Virtual Machines to create isolated machines. You can use the [Create a virtual machine in Hyper-V](https://learn.microsoft.com/windows-server/virtualization/hyper-v/get-started/create-a-virtual-machine-in-hyper-v?tabs=hyper-v-manager).

> NOTE: You will need to expand the disk size required for Ubuntu Hyper-V machine, for example selecting 20GB should provide enough disk space. 

### Cloud Hosted Machines

You can select from a range of cloud hosted machines like [Quickstart: Create a Windows virtual machine in the Azure portal](https://learn.microsoft.com/azure/virtual-machines/windows/quick-create-portal)

### Power Automate Desktop

You can also take advantage of Power Platform [Hosted Machines](https://learn.microsoft.com/power-automate/desktop-flows/hosted-machines) for integrated solution with the Power Platform to have a Domain Joined Windows 11 Cloud PC.

## Windows Build

Using Windows 11 as an example you can use the following steps to build from source

1. Download PAC CLI https://learn.microsoft.com/power-platform/developer/howto/install-cli-msi

2. Get Git so we can clone the samples

```pwsh
winget install --id Git.Git -e --source winget
```

3. Get Visual Studio Code so that can edit config file

```pwsh
winget install -e --id Microsoft.VisualStudioCode
```

4. Get the Azure CLI So that we can get access token for Dataverse to store key

```pwsh
Winget install -e --id Microsoft.AzureCLI
```

5. Get New Version of PowerShell Core

```pwsh
winget install --id Microsoft.PowerShell --source winget
```

6. Get .Net 8.0 SDK Require for this build

```pwsh
winget install Microsoft.DotNet.SDK.8
```

## Ubuntu 22.04 Build

Using Ubuntu 22.0.4 LTS as an example you can use the following steps to build from source

1. Ensure you have latest updates and upgrades applied

```bash
sudo apt update
sudo apt upgrade
```

2. Install Git it is not installed

```bash
sudo apt install git 
```

3. Install Visual Studio code using snap

```bash
snap install code --classic
```

> NOTE: The classic flag is required as Visual Studio Code needs full access to the system similar to a traditionally installed application

4. Install Power Shell

```bash
snap install powershell --classic
```

5. Install DotNet 8.0 SDK

```bash
snap install dotnet-sdk --classic
```

6. Install Azure CLI

```bash
snap install curl
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
```

7. Install the Power Platform CLI

```bash
dotnet tool install --global Microsoft.PowerApps.CLI.Tool
```

8. Setup Environment Variables in your **~./.bashrc** file

```bash
export PATH="$PATH:~/.dotnet/tools"
export DOTNET_ROOT=/snap/dotnet-sdk/current
```

9. Setup defaults for Powershell. Create the following file **~/.config/powershell/profile.ps1** using Visual Studio Code

```pwsh
# Add .dotnet/tools to the PATH environment variable
$env:PATH += ":$HOME/.dotnet/tools"

# Set the DOTNET_ROOT environment variable
$env:DOTNET_ROOT = "/snap/dotnet-sdk/current"
```

10. Ensure that you have enough disk space allocated to build the solution.

## Verifying Your Environment

To verify your selected environment has all the prerequisite tools required

1. Verify that you can launch the Power Platform Command Line Tool

```pwsh
pac auth help
```

2. Verify you have a version of git client installed

```pwsh
git --version
```

3. Verify Visual Studio Code or alternative text editor is installed

```pwsh
code
```

4. Verify that the Azure CLI has been installed

```pwsh
az --version
```

5. Verify that PowerShell Core has been installed

```pwsh
pwsh --version
```

6. Verify that the .Net 8.0 SDK has been installed

```pwsh
dotnet --list-sdks
```

## Getting Sample Going

The following steps give an example of running sample from Microsoft Windows.

1. Clone the repository

```pwsh
git clone https://github.com/microsoft/PowerApps-TestEngine.git
```

2. Move to the downloaded repo

```pwsh
cd PowerApps-TestEngine
```

3. Open PowerShell (Windows + R)

```pwsh
pwsh
```

4. Allow local execution policy on Windows

```pwsh
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
```

5. Create a certificate to sign secrets. Using an Admin Version of PowerShell run the following on Windows

```pwsh
$Params = @{
DnsName = @("localhost", "localhost")
CertStoreLocation = "Cert:\CurrentUser\My"
NotAfter = (Get-Date).AddMonths(6)
KeyAlgorithm = "RSA"
KeyLength = 2048
}
New-SelfSignedCertificate @Params
```

> NOTE: 
> For Ubuntu the following could be used to create local certificate
>
> openssl req -x509 -nodes -days 180 -newkey rsa:2048 -keyout mycert.key -out mycert.crt -subj "/CN=localhost"

6. Sign into your Power Platform Environment(s) that contain login details and environment(s) to be tested

```pwsh
pac auth create --name Dev --environment https://contoso.crm.dynamics.com
pac auth create --name Hosting --environment https://contoso-host.crm.dynamics.com
```

7. Sign into Azure Command Line using account that has access to organization. This login could be interactive user or Service Principal.

az login --allow-no-subscriptions

8. Import the WeatherSample_*.zip from samples\weather into the environment you want to test

9. Import the TestEngine_*.zip (Will be used to store authentication state)

10. Add the config to the "samples/weather" folder using Visual Studio Code

```json
{
    "tenantId": "d1234567-1111-2222-3333-4444555666",
    "environmentId": "a000000-1824-e982-98d9-190558c8750f",
    "customPage": "te_snapshots_24d69",
    "appDescription": "Weather Sample",
    "user1Email": "user1@contoso.onmicrosoft.com",
    "runInstall": true,
    "installPlaywright": true,
    "DataProtectionUrl": "https://contoso-host.crm.dynamics.com/",
    "DataProtectionCertificateName": "CN=localhost",
    "pac": "optional/pac.exe",
    "auth": "certstore",
    "authstate": "dataverse"
}
```

> NOTE: 
> 1. The pac parameter is optional and only needed if needing to use a specific version of pac cli 
> 2. Can use auth value of certenv for based64 version of X.509 certificate to encrypt/decrypt
> 3. If using auth value of certenv the DataProtectionCertificateName is not required
> 4. You can use an authstate of **storagestate** on windows for local Data Protection API encrypted login details
> 5. The export.ps1 can be sued to export the local certificate for use on another machine

## Run the Sample

1. Open pwsh session

2. Change to samples/weather

```pwsh
cd samples/weather
```

3. Run the test using your configured **config.json** in samples\weather

```pwsh
pwsh -File RunTests.ps1
```
