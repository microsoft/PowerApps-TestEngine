# test

This is used to define one test.

## YAML schema definition

| Property | Required | Description |
| -- | -- | -- |
| name | Yes | This is the name of the test, it will be used in reporting success and failure |
| description | No | Additional information describe what the test does |
| persona | Yes | This is the user that will be logged in to perform the test. This must match a persona listed in the [Users](./Users.md) section | 
| appLogicalName | Yes | This is the logical name of the app that is to be launched. It can be obtained from the solution. For canvas apps, you need to add it to a solution to obtain it |
| testSteps | Yes | A set of Power FX functions describing the steps needed to perform the test. 

### TestSteps

- This can use any existing [Power FX](https://docs.microsoft.com/en-us/power-platform/power-fx/overview) functions or [specific test functions](../PowerFX/README.md) defined by this framework.
- It should start with a | to allow for multiline YAML expressions followed by an = sign to indicate that it is a Power FX expression
- Functions should be separated by a ;
- Comments can be used and should start with //
