# JavaScript Testing for Dynamics 365 Client-side Code

This sample demonstrates how to use the PowerApps Test Engine to test JavaScript code commonly used in Dynamics 365 applications, particularly focusing on:

1. Command bar button customizations
2. Form visibility/display logic
3. Field validation and business rules
4. Form event handlers

## Sample Structure

### Core JavaScript Files (D365 Webresource Pattern)

- **mockXrm.js**: Mock implementation of the Xrm client-side object model
- **commandBar.js**: Command bar button functions
- **formScripts.js**: Form event handlers
- **visibility.js**: Field/section/tab visibility control
- **validation.js**: Data validation functions

### Test Configuration & Scripts

- **commandBar.te.yaml**: Command bar-specific test file
- **formScripts.te.yaml**: Form script-specific test file
- **validation.te.yaml**: Validation-specific test file
- **visibility.te.yaml**: Visibility logic-specific test file
- **RunTests.ps1**: PowerShell script to execute all tests and generate detailed reports
- **config.json**: Configuration file for test execution

## How to Run

1. Ensure PowerApps Test Engine is installed
2. Configure your environment in `config.json` with your tenant ID, environment ID, and URL
3. Run all tests with detailed reporting: `.\RunTests.ps1`
4. Optional parameters:
   - Compile before running: `.\RunTests.ps1 -compile`
   - Record test interactions: `.\RunTests.ps1 -record`
   - Specify time threshold: `.\RunTests.ps1 -lastRunTime "2025-05-15 10:00"`

## Understanding the Tests

These tests use the `AssertJavaScript` PowerFx function to execute client-side D365 code against a mock Xrm object. The component test configuration files follow this pattern:

```yaml
testCaseName: CommandBar_NewButtonAlwaysEnabled
description: Tests that new button is always enabled
testSteps: |
  = Preview.AssertJavaScript({
      Location="commandBar.js", 
      Setup="mockXrm.js", 
      Run: "isCommandEnabled('new')", 
      Expected: "true" 
    });
```

Each test specifies:
- **Location**: JavaScript file containing the function to test
- **Setup**: JavaScript setup code or path to a file (like mockXrm.js)
- **Run**: The JavaScript code to execute, directly calling the functions
- **Expected**: The expected result of the test

The mock framework provides fake implementations of common D365 client-side APIs, allowing you to test form scripts without an actual Dynamics 365 instance.

## D365 JavaScript Structure

The project follows modern Dynamics 365 web resource patterns:

1. **Direct Function Declarations**: All JavaScript is organized as directly callable functions
   ```javascript
   function isCommandEnabled(commandName) { ... }
   ```

2. **Testable Functions**: Functions are designed to be independently testable
   ```javascript
   function validatePhoneNumber(phoneNumber) { ... }
   ```

3. **Separation of Concerns**: Each JavaScript file focuses on a specific area
   - Command bar customizations
   - Form event handlers
   - Field visibility logic
   - Data validation

## Detailed HTML Reports

The `RunTests.ps1` script generates detailed HTML reports with:

- Pass/fail statistics for each component
- Health score calculation (passing files / total files × 100)
- Charts and metrics for test performance
- Execution time analysis

To view the most recent report, check the `TestResults` folder after running the tests.
