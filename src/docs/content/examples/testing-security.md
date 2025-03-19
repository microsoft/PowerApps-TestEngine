---
title: Testing Security
---

This article provides an example of how we can test browser-based authentication using multiple personas using Multi-Factor Authentication (MFA) with persistent cookie state and Power Apps security for Power Apps. 

The authentication process allows for cookies that are used for authentication to be stored between test runs using `storageState` which allows the browser state to be stored between tests.

We use a Power Apps Test Engine [permissions sample](https://github.com/microsoft/PowerApps-TestEngine/tree/grant-archibald-ms/storage-state-389/samples/permissions) that demonstrates using two user personas, including a set of tests to log in to the Power Apps Portal, a canvas application, and a Model-Driven Application.

## Key Scenarios being tested

### 1. Login Process Using Interactive Multi-Factor Authentication

The login process involves using interactive MFA to authenticate the user. Once authenticated, the session cookies are stored persistently using `storageState` in the sample. This authentication method ensures that subsequent test runs can execute without the need for interactive login.

### 2. Handling Expired Entra Credentials

In scenarios where the user tries to access a Power Platform resource and the Entra credentials have expired, we demonstrate the use of a Temporary Access Pass (TAP). The steps include:
- Using TAP to gain temporary access.
- Deleting the TAP to simulate expired credentials.
- Ensuring the test fails and prompts the user to log in again.

### 3. Valid Login Permissions but Application Not Shared

This scenario tests the case where the user has valid login permissions, but the application is not shared with them. The test should verify that the user cannot access the application and receives an appropriate error message.

### 4. Valid Login Permissions but No Power Platform Security Roles
In this case, the user has valid login permissions but lacks the necessary Power Platform security roles to use the Model-Driven Application. The test should ensure that the user cannot perform actions within the application and receives an appropriate error message.

## Test Scenarios

### Happy Path

The "happy path" refers to the scenario where everything works as expected without any errors or issues. It is the ideal flow through the application, where the user successfully completes the intended tasks.

- **Login with MFA**: User successfully logs in using MFA, and the session cookies are stored persistently.
- **Access Application**: User accesses the Power Apps Portal, canvas application, and Model-Driven Application without issues.

### Edge Cases

An edge case refers to a scenario that occurs at the extreme ends or boundaries of the operating parameters of a system. These are situations that are uncommon or rare but can still happen and need to be accounted for in testing and development. Edge cases often reveal bugs or issues that might not be apparent during normal usage.

In the context of software testing, edge cases are important because they help ensure that the application can handle unexpected or unusual inputs and conditions gracefully. By testing edge cases, we can identify and fix potential problems before they affect users.

For example, in our scenario of browser-based authentication with Multi-Factor Authentication (MFA), an edge case might be:

- **No Permissions Applied**: The user attempts to access the application without any permissions applied. This is an unusual situation but needs to be tested to ensure the application handles it correctly by denying access and providing an appropriate error message.
- **No Security Role**: The user tries to use the Model-Driven Application without the necessary security roles. This is another edge case that tests whether the application correctly restricts access and informs the user of the missing roles.
By considering and testing edge cases, we can ensure that the application is robust and can handle a wide range of scenarios, including those that are less likely to occur.

### Exceptions

In the context of software testing and development, exceptions refer to unexpected or unusual conditions that disrupt the normal flow of a program. These are situations that the system does not typically encounter during regular operation but must be handled gracefully to prevent crashes or other undesirable behavior.

Exceptions can occur due to various reasons, such as invalid user input, network failures, or expired credentials. They are often used to signal errors or other exceptional conditions that require special handling.

For example, in our scenario of browser-based authentication with Multi-Factor Authentication (MFA), an exception might be:

- **Expired Credentials**: When a user's credentials have expired, the system cannot proceed with the authentication process. This is an exceptional condition that needs to be handled by prompting the user to log in again.

By testing for exceptions, we can ensure that the application can handle unexpected conditions without failing. This helps improve the robustness and reliability of the system, providing a better user experience even in the face of errors or unusual situations.

## Observing Test Outcomes

The Test Engine observes different positive and negative test cases and provides methods for validating expected errors. For example, the Test Engine will create a variable `ErrorDialogTitle` that contains details about edge cases and exception cases. This enables the execution of negative tests.

### Negative Testing

Negative testing is a testing approach where the system is tested with invalid, unexpected, or erroneous inputs to ensure that it can handle such situations gracefully. The goal of negative testing is to identify and handle expected error cases, ensuring that the system behaves as expected even when things go wrong.

Negative tests are useful because they help:

- Identify potential vulnerabilities and weaknesses in the system.
- Ensure that the system can handle invalid inputs without crashing or producing incorrect results.
- Validate that appropriate error messages are displayed to the user when something goes wrong.

#### Example of Negative Testing

In our scenario, the Test Engine uses the `ErrorDialogTitle` variable to capture error messages that occur during edge cases and exceptions. For instance, when testing for expired credentials, the Test Engine can check if the appropriate error message is displayed:

```powerfx
Assert(IsMatch(ErrorDialogTitle, "An error has occurred"))
```

This assertion verifies that the error dialog title matches the expected error message, confirming that the system correctly handles the expired credentials scenario.

### Edge Cases and Exceptions

By incorporating edge cases and exceptions into the testing process, we can ensure that the system is robust and reliable. Here are some examples:

#### Edge Case: No Permissions Applied

The user attempts to access the application without any permissions applied. The Test Engine checks if the appropriate error message is displayed, indicating that access is denied.
Edge Case: No Security Role

The user tries to use the Model-Driven Application without the necessary security roles. The Test Engine verifies that the user cannot perform actions within the application and receives an appropriate error message.

#### Exception: Expired Credentials

The user's credentials have expired. The Test Engine simulates this scenario by deleting the Temporary Access Pass (TAP) and checks if the system prompts the user to log in again.

By testing these scenarios, we can ensure that the system handles both positive and negative cases effectively, providing a smooth and secure user experience. Negative testing helps identify potential issues early, allowing developers to address them before they impact users.

The Test Engine observes different positive and negative test cases and provides methods for validating expected errors. For example:

```PowerFx
Assert(IsMatch(ErrorDialogTitle, "An error has occurred"))


#### Edge Case: Application Not Shared

The user has valid login permissions but the application is not shared with them. The Test Engine checks if the "Request access" dialog is displayed using the following assertion:

```PowerFx
Assert(ErrorDialogTitle="Request access")
```

## Invalid Security State

Let's have a look at a case where your test state can be invalid. This could occur when a Temporary Access Pass (TAP) has expired or has been deleted by an administrator.

When a user logs in, the authentication tokens are often stored in cookies. These tokens are required for subsequent requests to authenticate the user. If cookies are not enabled, expired, or related to sessions that are no longer valid, the browser context will not have access to these tokens or will have tokens that are invalid. This can result in errors like AADSTS50058.

When an error like this exists it can impact the saved cookies and it can generate Entra-based login errors. 

### Silent sign-in request error

In this example, we will later see the AADSTS50058 error occurring when a silent sign-in request is sent, but no user is signed in after the Temporary Access Pass (TAP) has expired or has been revoked.

Explanation:
Tests can receive the error "AADSTS50058: A silent sign-in request was sent but no user is signed in."

The error occurs because the silent sign-in request is sent to the login.microsoftonline.com endpoint. Entra validates the request and determines that no usable authentication methods exist. This prompts the interactive sign-in process again.

### Resolution

To resolve this type of issue, you can delete the saved storage state for the user and re-authenticate as the user with a new access token.

### Further Reading

For deeper discussion on authentication and methods you could review the following:
1. Start with [Microsoft Entra authentication documentation](https://learn.microsoft.com/entra/identity/authentication/)
2. Review Single Sign On and how it works  [Microsoft Entra seamless single sign-on: Technical deep dive](https://learn.microsoft.com/entra/identity/hybrid/connect/how-to-connect-sso-how-it-works)
3. Rview [What authentication and verification methods are available in Microsoft Entra ID?](https://learn.microsoft.com/en-us/entra/identity/authentication/concept-authentication-methods)

## Conclusion

By testing various scenarios, including the happy path, edge cases, and exceptions, we can ensure a robust authentication process. The use of persistent cookie state with storage State allows for seamless test execution without repeated interactive logins, enhancing the efficiency and reliability of the testing process.
