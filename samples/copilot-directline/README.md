# Overview

This Microsoft Direct Line sample demonstrates how to interact with the Test actions of a published Safe Travels Agent using Direct Line

## Usage

1. Publish you Agent

2. Get the Secret and 

3. Create config.json file using secret from

```json
{
    "secret": "",
    "botFrameworkUrl": ""
}
```

4. Change the app id of your deployed Safe travel app in the testPlan.fx.yaml file

5. Execute the test

```pwsh
.\RunTests.ps1
```
