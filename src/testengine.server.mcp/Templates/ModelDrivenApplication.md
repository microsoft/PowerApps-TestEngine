# Recommendation 

Use the source code definition of {{entities\foldername}} and the sample in {{MDAListSample}} and  {{MDADetailSample}} to create inside a tests folder inside workspace folder tests that creates a test yaml file for the Model Driven Application tests for the forms.

You MUST generate PowerShell that will validate all created **.te.yaml** and **testSettings.yaml** files

## Variables

If variables in the format {{name}} exist in the recommendation try read the values from the tests\variables.yaml or context from the workspace

If a tests\variables.yaml file does not exist query the Test Engine MCP Server to the "variables.yaml" template

## Model Driven Application Context

- The tests should verify the functionality of the cref8_powerofattorney model-driven application
- Focus on form navigation, data entry, and validation scenarios
- Use the Dataverse functions to interact with the application

## Test Case Generation

The generated **test** folder must:

- Each should be a valid Test Engine yaml file that implements tests using Power Fx 
- Example Power Fx of the User Defined function that should Be in **testSettings.yaml**
- Ensure testSettings.yaml file meet this [schema](https://raw.githubusercontent.com/microsoft/PowerApps-TestEngine/refs/heads/user/grant-archibald-ms/mcp-606/samples/mcp/settings-schema.json)

    ```yaml
    locale: "en-US"
    headless: false
    recordVideo: true
    extensionModules:
      enable: true
      parameters:
        enableDataverseFunctions: true
    timeout: 3000
    browserConfigurations:
      - browser: Chromium
        channel: msedge
    powerFxTestTypes:
      - name: TestResult
        value: |
          {PassFail: Number, Summary: Text}
      - name: ControlName
        value: |
          {ControlName: Text}
      - name: FormAction
        value: |
          {FormName: Text, ControlName: Text, Action: Text, ExpectedValue: Text}
    testFunctions:
      - description: Verify a form control value
        code: |
          VerifyFormControl(action: FormAction): TestResult =
            With({
              CurrentValue: Preview.GetValue(action.ControlName).Text
            },
            If(
              IsError(AssertNotError(CurrentValue = action.ExpectedValue, "Control value doesn't match expected")),
              {PassFail: 1, Summary: "Failed: " & action.ControlName & " expected " & action.ExpectedValue & " but got " & CurrentValue}, 
              {PassFail: 0, Summary: "Pass: " & action.ControlName & " = " & action.ExpectedValue}
            ))
      - description: Set a form control value
        code: |
          SetFormControl(action: FormAction): TestResult =
            With({
              Result: Preview.SetValueJson(action.ControlName, JSON(action.ExpectedValue))
            },
            If(
              IsError(Result),
              {PassFail: 1, Summary: "Failed to set " & action.ControlName & " to " & action.ExpectedValue}, 
              {PassFail: 0, Summary: "Pass: Set " & action.ControlName & " to " & action.ExpectedValue}
            ))
      - description: Wait until control is visible
        code: |
          WaitUntilVisible(control: Text): Void =
            Preview.PlaywrightAction(Concatenate("//div[@data-id='", control, "']"), "wait");
    ```

- Validate every generated testSettings.yaml file to ensure it is valid. 

- It MUST pass parameters to a user defined function with a record to encourage reuse and prevent copy/paste for different test cases. For example:

    ```yaml
    # yaml-embedded-languages: powerfx
    testSuite:
      testSuiteName: Power of Attorney Form Tests
      testSuiteDescription: Tests for Power of Attorney model-driven form functionality
      persona: User1
      appLogicalName: cref8_powerofattorney

      testCases:
        - testCaseName: Create New POA
          testCaseDescription: Tests that a new Power of Attorney can be created
          testSteps: |
            = 
            // Wait for form to load
            WaitUntilVisible("cref8_name");
            // Set form fields
            SetFormControl({FormName: "POA Form", ControlName: "cref8_name", Action: "Set", ExpectedValue: "Test POA"});
            // Verify form field
            VerifyFormControl({FormName: "POA Form", ControlName: "cref8_name", Action: "Get", ExpectedValue: "Test POA"});
    
    testSettings:
      filePath: ./testSettings.yaml

    environmentVariables:
      users:
        - personaName: User1
          emailKey: user1Email
          passwordKey: NotNeeded
    ```

- Validate every generated *.te.yaml with the following [schema](https://raw.githubusercontent.com/microsoft/PowerApps-TestEngine/refs/heads/user/grant-archibald-ms/mcp-606/samples/mcp/test-schema.json)

- Ensure YAML attributes appear in the samples. Remove any nodes or properties that do not appear in the samples

## Test Structure Generation

- The tests MUST be created in a folder named **tests**
- Have a RunTest.ps1 that follows the rules below
- Any data changes that could affect the state of Dataverse should be included with Test_ prefix to ensure that other test data is not affected by the test
- Include a .gitignore for PowerApps-TestEngine folder and the config.json file
- The RunTest.ps1 reads from **config.json**  

    ```json
    {
        "useSource": false,
        "sourceBranch": "",
        "compile": false,
        "environmentId": "<insert your environment id>",
        "environmentUrl": "<insert your environment url>",
        "tenantId": "<insert your tenantId>"
    }
    ```

- Example folder structure after test generation complete:

    ```
    tests
        - ModelDrivenApps
            - cref8_powerofattorney
                - HappyPath
                    create-poa.te.yaml
                    update-poa.te.yaml
                - EdgeCases
                    invalid-data.te.yaml
                - Exceptions
                    error-handling.te.yaml
                README.md
                testSettings.yaml
    ```

- If the config.json does not exist:
   
   Check if user session is logged in using Azure CLI
   Use az account show to populate the tenantId
   Check if user is logged into pac cli
   Prompt the user to select the environmentId and environmentUrl for the values in the config file

- `$environmentId`, `$tenantId` and `$environmentUrl` variables must come from the config.json. 
- If more than one *.te.yaml file exists in the test folder, samples like https://github.com/microsoft/PowerApps-TestEngine/blob/user/grant-archibald-ms/js-621/samples/javascript-d365-tests/RunTests.ps1 to generate a test summary report should be included 
- Generate Happy Path, Edge Cases and Exception cases as separate yaml test files
- Ensure common testSettings.yaml file is used to share common User Defined Types and Functions like in https://github.com/microsoft/PowerApps-TestEngine/blob/main/samples/copilotstudiokit/testSettings.yaml
- Test files should be named like testcasename.te.yaml (e.g., create-poa.te.yaml)
- All test related files should be in folder **tests** in the root of the workspace
- The README.md should be in the same folder as the test files. Minimum .Net SDK version is 8.0
- testSettings.yaml should be in the folder that the tests relate to
- Review all the files added or updated and make sure they are grouped into the best location assuming multiple types of tests could be applied to this solution

## Test Configuration

Attempt to use `az account show` and `pac env` to create a valid **config.json** to run the tests.

## Test Validation

Create a script to validate the YAML files against the schemas:

```powershell
# Validate-ModelDrivenAppTests.ps1
param(
    [Parameter(Mandatory = $true)]
    [string]$YamlFilePath,
    
    [Parameter(Mandatory = $false)]
    [ValidateSet("Simple", "Schema")]
    [string]$ValidationMode = "Simple"
)

# Basic validation for Model Driven App test files
function Test-MDAppYamlBasic {
    param([string]$FilePath)
    
    $yamlContent = Get-Content -Path $FilePath -Raw
    $errors = @()
    
    # Check for required elements
    if (-not ($yamlContent -match "testSuite:")) {
        $errors += "Missing required 'testSuite' section"
    }
    
    if (-not ($yamlContent -match "testCases:")) {
        $errors += "Missing required 'testCases' section"
    }
    
    if (-not ($yamlContent -match "testSettings:")) {
        $errors += "Missing required 'testSettings' section"
    }
    
    # Add more validation as needed
    
    return @{
        IsValid = ($errors.Count -eq 0)
        Errors = $errors
    }
}

# Main validation logic
if ($ValidationMode -eq "Simple") {
    $result = Test-MDAppYamlBasic -FilePath $YamlFilePath
    
    if ($result.IsValid) {
        Write-Host "Validation successful: $YamlFilePath appears to be valid." -ForegroundColor Green
    } else {
        Write-Host "Validation failed for $YamlFilePath" -ForegroundColor Red
        foreach ($error in $result.Errors) {
            Write-Host "- $error" -ForegroundColor Red
        }
    }
} else {
    # Schema validation would go here
    Write-Host "Schema validation not implemented yet" -ForegroundColor Yellow
}
```

## Source Code Version

If using source code when $useSource is true it should:
1. Clone PowerApps-TestEngine from https://github.com/microsoft/PowerApps-TestEngine
2. If the folder PowerApps-TestEngine exists it should pull new changes
3. It should take an optional git branch to work from and checkout that branch if a non-empty value exists in the config file
4. It should change to src folder and run dotnet build if config.json compile: true
5. It should change to folder bin/Debug/PowerAppsTestEngine
6. It should run the generated test with the following command line:

```PowerShell
dotnet PowerAppsTestEngine.dll -p powerfx -i $testFileName -e $environmentId -t $tenantId -d $environmentUrl
```

## PAC CLI version

If using pac cli when $useSource = $false:

1. Check pac cli exists using pac --version
2. Check pac cli is greater than 1.43.6
3. Use the following command:

```PowerShell
pac test run --test $testFile --provider powerFx --environment-id $environmentId --tenant $tenantId --domain $environmentUrl
```

## Documentation

The README.md must provide information on how to complete execution of the test and any required configuration by the user. It should state any required tool dependencies and how to login. Include:

- Prerequisites
  - PowerApps CLI (version 1.43.6 or higher)
  - .NET SDK (version 8.0 or higher)
  - Azure CLI
  - Power Platform access with appropriate permissions
  
- Configuration
  - How to set up config.json with environment details
  - How to authenticate with Azure and Power Platform
  
- Running Tests
  - Step-by-step instructions for different test scenarios
  - How to interpret test results
  
- Test Structure
  - Explanation of test organization (HappyPath, EdgeCases, Exceptions)
  - Description of test settings and custom functions
  
- Troubleshooting
  - Common errors and their solutions
  - Where to find logs and how to interpret them
