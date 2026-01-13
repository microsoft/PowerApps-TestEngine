# Recommendation 

Use the source code definition of web resource {{webresources\\filename.js}} and the sample in {{TestYamlSample}} and {{MockJsSample}} to create inside a tests folder inside workspace folder tests that creates a test yaml file for the JavaScript WebResource that makes use of Xrm SDK.

You MUST generate PowerShell that will validate all created **.te.yaml** and **testSettings.yaml** files

## Variables

If variables in the format {{name}} exist in the recommendation try read the values from the tests\variables.yaml or context from the workspace

If a tests\variables.yaml file does not exist query the Test Engine MCP Server to the "variables.yaml" template

## JavaScript WebResource Context

- The JavaScript web resource uses the Xrm SDK for Dynamics 365
- The test files should verify the correct functionality of the web resource
- Mock implementations should be used to simulate the Dynamics 365 environment

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
      - name: TestAction
        value: |
          {Script: Text, Setup: Text, Expected: Text}
    testFunctions:
      - description: Run a JavaScript test with proper setup and validation
        code: |
          RunJsTest(action: TestAction): TestResult =
            With({
              Response: Preview.AssertJavaScript({
                Location: action.Script, 
                Setup: action.Setup, 
                Run: action.Run,
                Expected: action.Expected
              })
            },
            If(
              IsError(Response),
              {PassFail: 1, Summary: "Failed: " & Text(Error)}, 
              {PassFail: 0, Summary: "Pass: " & action.Expected}
            ))
    ```

- Validate every generated testSettings.yaml file to ensure it is valid. 

- It MUST pass parameters to a user defined function with a record to encourage reuse and prevent copy/paste for different test cases. For example:

    ```yaml
    # yaml-embedded-languages: powerfx
    testSuite:
      testSuiteName: JavaScript WebResource Tests
      testSuiteDescription: Tests for JavaScript Web Resource functionality
      persona: User1
      appLogicalName: NotNeeded

      testCases:
        - testCaseName: Valid Function Call
          testCaseDescription: Tests that the function correctly handles valid input
          testSteps: |
            = RunJsTest({
                Script: "wfc_recommendNextActions.js", 
                Setup: "mockXrm.js", 
                Run: "isValid('testValue')",
                Expected: "true"
              })
    
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
        "useSource": true,
        "sourceBranch": "user/grant-archibald-ms/js-621",
        "compile": true,
        "environmentId": "<insert your environment id>",
        "environmentUrl": "<insert your environment url>",
        "tenantId": "<insert your tenantId>"
    }
    ```

- Example folder structure after test generation complete:

    ```
    tests
        - WebResources
            - wfc_recommendNextActions
                - HappyPath
                    test1.te.yaml
                - EdgeCases
                    test2.te.yaml
                - Exceptions
                    test3.te.yaml
                README.md
                testSettings.yaml
                mockXrm.js
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
- Test files should be named like testcasename.te.yaml
- All test related files should be in folder **tests** in the root of the workspace
- The README.md should be in the same folder as the test files. Minimum .Net SDK version is 8.0
- testSettings.yaml should be in the folder that the tests relate to
- The code must use PowerFx and the location should be to the relative path of the web resource being tested:

    ```powerfx
    Preview.AssertJavaScript({
      Location: "wfc_recommendNextActions.js", 
      Setup: "mockXrm.js", 
      Run: Join([
        "isCommandEnabled('new')"
      ]), 
      Expected: "true" 
    });
    ```

- The code will be running inside a sandbox so there is no need to save or restore any original functions
- Review all the files added or updated and make sure they are grouped into the best location assuming multiple types of tests could be applied to this solution

## Test Configuration

Attempt to use `az account show` and `pac env` to create a valid **config.json** to run the tests.

## Test Validation

Create a script to validate the YAML files against the schemas:

```powershell
# Validate-JsWebResourceTests.ps1
param(
    [Parameter(Mandatory = $true)]
    [string]$YamlFilePath,
    
    [Parameter(Mandatory = $false)]
    [ValidateSet("Simple", "Schema")]
    [string]$ValidationMode = "Simple"
)

# Validation logic here
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
dotnet PowerAppsTeatEngine.dll -p powerfx -i $testFileName -e $environmentId -t $tenantId -d $environmentUrl
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

- Dependencies (PowerApps CLI, .NET SDK, etc.)
- Configuration steps
- Test execution instructions
- Interpretation of test results
- Troubleshooting common issues
