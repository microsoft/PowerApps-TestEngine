# Users

To ensure credentials are stored in secure manner, users are referenced by a persona name in the test definition. Storing credentials in test plan files is not supported.

References to the user credentials are located under the `environmentVariables` section as a list of `users`

Example:
```
environmentVariables:
    - users:
        - personaName: "User1"
          emailKey: "user1Email"
          passwordKey: "user1Password"
        - personaName: "User2"
          emailKey: "user2Email"
          passwordKey: "user2Password"
        
```

The `personaName` will be used as part of the test definition to indicate what user to run the test as.

## Supported credentials storage mechanisms

> **Note:** Multi-factor authentication is not supported.

### Environment variables

To store credentials as environment variables, you can set it as follows:
```powershell
# In PowerShell - replace variableName and variableValue with the correct values
$env:variableName = "variableValue"
```

In the YAML, two properties need to be defined to indicate that this user's credentials are stored in environment variables:
- emailKey: The environment variable used to store the user's email.
- passwordKey: The environment variable used to store the user's password.

Example YAML:
```yaml
    - personaName: "User1"
      emailKey: "user1Email"
      passwordKey: "user1Password"
```

Example powershell to set user credentials based on YAML:
```powershell
$env:user1Email = "someone@example.com"
$env:user1Password = "fake password"
```