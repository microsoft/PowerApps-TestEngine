# Overview

The [IUserManager](../../../src/Microsoft.PowerApps.TestEngine/Users/IUserManager.cs) interface provides the ability for .Net assemblies to implement different authentication methods with test engine.

## Implementations

The following authentication MEF modules exist:

| Module | Name | Description |
|--------|------|-------------|
|test.user.environment.dll | environment | Read user name and password from environment variables
|test.user.browser.dll | browser | Login using the caches playwright browser context default profile. If the **BrowserContext** folder does not exist the user is prompted to interactively login |
|test.user.local.dll | local | Implements user manager that assumes that test resources are locally available to test engine and no web authentication is required |

## Import Process

Test Engine will search for .Net assemblies named **test.user.*.dll**. By default the user provider with the highest priority is selected. A user manager provider can be applied to a test engine execution.

> [!NOTE] In release mode only .Net Assemblies signed by trusted certificate providers in the Allow list will be imported.

## Roadmap

Other implementations that could be considered:

- [Certificate Based Authentication](https://learn.microsoft.com/entra/identity/authentication/concept-certificate-based-authentication) - Authenticate directly with X.509 certificates against their Microsoft Entra ID. Could be used to provide second factor identify provider depending on how the [Authentication binding policy](https://learn.microsoft.com/entra/identity/authentication/how-to-certificate-based-authentication) is configured.
- [Conditional Access Policy](https://learn.microsoft.com/entra/identity/conditional-access/concept-conditional-access-policies) Support various conditional access policies. For example browser agent.
