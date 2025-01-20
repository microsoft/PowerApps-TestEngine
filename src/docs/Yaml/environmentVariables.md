# testSettings

This is used to define environment variables for the tests.

## YAML schema definition

| Property | Required | Description |
| -- | -- | -- |
| [users](./Users.md) | Yes | List of user credential references. Any users defined in the test definition must be listed here. At least one user must be present. |
| filePath | No |  The file path to a separate yaml file with all the environment variables. If provided, it will **override** all the [users](./Users.md) in the test plan. |
