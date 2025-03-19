# Users

To ensure credentials are stored in secure manner, users are referenced by a persona name in the test definition. Storing credentials in test plan files is not supported.

References to the user credentials are located under the `environmentVariables` section as a list of `users`

Example:
```
environmentVariables:
    - users:
        - personaName: "User1"
          emailKey: "user1Email"
        - personaName: "User2"
          emailKey: "user2Email"
        
```

The `personaName` will be used as part of the test definition to indicate what user to run the test as.

### Environment variables

To store credentials as environment variables, you can set it as follows:
```powershell
# In PowerShell - replace variableName and variableValue with the correct values
$env:variableName = "variableValue"
```
Example powershell to set user credentials based on YAML:
```powershell
$env:user1Email = "someone@example.com"
```