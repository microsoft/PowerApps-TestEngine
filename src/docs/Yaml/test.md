# test

This is used to define one test.

## YAML schema definition

| Property | Required | Description |
| -- | -- | -- |
| testSuiteName | Yes | This is the name of the test suite |
| testSuiteDescription | No | Additional information describe what the test suite does |
| persona | Yes | This is the user that will be logged in to perform the test. This must match a persona listed in the [Users](./Users.md) section | 
| appLogicalName | Yes | This is the logical name of the app that is to be launched. It can be obtained from the solution. For canvas apps, you need to add it to a solution to obtain it |
| appId | No | This is the id of the app that is to be launched. This is required and used only when app logical name is not present. App id should be used only for canvas apps that are not in the solution
| networkRequestMocks | No | Defines network request mocks needed for the test |
| testCases | Yes | Defines test cases in the test suite. Test cases contained in test suites are run sequentially. The app state is persisted across all test cases in a suite |
| onTestCaseStart | No | Defines the steps that need to be triggered for every test case in a suite before the case begins executing |
| onTestCaseComplete | No | Defines the steps that need to be triggered for every test case in a suite after the case finishes executing |
| onTestSuiteComplete | No | Defines the steps that need to be triggered after the suite finishes executing |

### NetworkRequestMocks

| Property | Required | Description |
| -- | -- | -- |
| requestURL | Yes | This is the request URL that will get mock response. Glob patterns are accepted |
| responseDataFile | Yes | This is a text file with the mock response content. All text in this file will be read as the response |
| Method | No | This is the request's method (GET, POST, etc.) |
| Headers | No | This is a list of header fields in the request in the format of [fieldName : fieldValue] |
| requestBodyFile | No | This is a text file with the request body. All text in this file will be read as the request body |

For optional properties, if no value is specified, the routing applies to all. For example, if Method is null, we send back the mock response whatever the method is as long as the other properties all match.

For Sharepoint/Dataverse/Connector apps, requestURL and Method can be the same for all requests. `x-ms-request-method` and `x-ms-request-url` in  headers may need to be configured in that case to identify different requests.

### TestCases

| Property | Required | Description |
| -- | -- | -- |
| testCaseName | Yes | This is the name of the test case, it will be used in reporting success and failure |
| testCaseDescription | No | Additional information describe what the test case does |
| testSteps | Yes | A set of Power FX functions describing the steps needed to perform the test case |

### TestSteps

- This can use any existing [Power Fx](https://docs.microsoft.com/en-us/power-platform/power-fx/overview) functions or [specific test functions](../PowerFX/README.md) defined by this framework.
- It should start with a | to allow for multiline YAML expressions followed by an = sign to indicate that it is a Power Fx expression
- Functions should be separated by a ;
- Comments can be used and should start with //
