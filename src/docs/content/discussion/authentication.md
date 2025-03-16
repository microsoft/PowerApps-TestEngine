---
title: Authentication in Power Apps Test Engine
---

## Overview

Authentication is a critical component of the test automation process. This discussion focuses ons browser-based authentication options inside Test Engine. When authenticating as part of tests Test Engine offers a range of options to authenticate with Microsoft Entra. 

![Test Engine Authentication method overview diagram from browser, certificate and conditional access policies](/PowerApps-TestEngine/examples/media/authentication-options.png)

These method can range from using persistent browser cookie, allowing for non-interactive execution of subsequent tests. The management of these browser cookies is governed by the guidelines provided in the Microsoft Entra documentation on session lifetime and conditional access policies.

As the Power CAT Engineering team looked at the different authentication options for the CoE Starter Kit, we started with browser-based authentication for local testing. This approach works with the conditional access policies of our environments and allows a quick and less intrusive process to authenticate as different test personas.

As we look at our continuous integration and deployment process, we will evaluate certificate-based authentication to determine the correct mix of authentication and management trade-offs to select authentication options and what secret store is used to securely host the login credentials.

By understanding and implementing these concepts, you can ensure that their test automation processes are secure, compliant, and capable of supporting multi-factor authentication in alignment with their Entra settings and policies. The choice between browser-based and certificate-based authentication will depend on the specific requirements and constraints of your organization, including the need for interactive sessions, the security of certificate storage, and compliance with conditional access policies.

## Learn from examples

The [Testing Security](../examples/testing-security.md) example provides valuable insights into how browser-based authentication using Multi-Factor Authentication (MFA) can be effectively tested in Power Apps. It highlights the use of persistent cookie state, which allows the browser state to be stored between tests. This method ensures that subsequent test runs can execute without the need for repeated interactive logins, enhancing the efficiency and reliability of the testing process.

Key scenarios tested in the example include handling expired Entra credentials using an example of Temporary Access Pass (TAP), verifying login permissions when the application is not shared with the user, and ensuring that users without the necessary Power Platform security roles cannot perform actions within the application. These scenarios demonstrate the importance of testing both positive and negative cases to ensure a robust and secure authentication process.

The example also emphasizes the significance of edge cases and exceptions in testing. By considering scenarios such as no permissions applied or expired credentials, the testing process can identify potential vulnerabilities and ensure that the system handles unexpected conditions gracefully. This approach helps improve the robustness and reliability of the system, providing a better user experience even in the face of errors or unusual situations.

## Supporting Multi-Factor Authentication

The use of browser-based authentication in the test automation process is particularly advantageous for supporting multi-factor authentication (MFA). MFA is a security measure that requires users to provide multiple forms of verification before gaining access to a system. By leveraging browser-based authentication, the test automation script can seamlessly integrate with the organization's MFA settings and policies.

## Persistent Session State and Entra-Based Authentication Tokens

Persistent session state is a key feature of browser-based authentication. It allows users to remain signed in after closing and reopening their browser window. This is achieved through the use of persistent browser cookies, which store the authentication tokens issued by Microsoft Entra. These tokens enable non-interactive execution of subsequent tests, reducing the need for repeated authentication.

## Conditional Access Policies

Conditional access policies play a crucial role in controlling browser-based authentication. These policies can be used to enforce specific requirements, such as the allowed browser agents, operating systems, and management policies. For instance, the organization may require that the machine running the tests is managed by Microsoft Intune. This ensures that the device complies with the organization's security policies and can be trusted to execute the tests.

## Certificate-Based Authentication

Certificate-based authentication is another method that can be used in the Power Apps Test Engine. This method requires certificates to be configured as an authentication method and optionally configured as a method of second factor authentication. Certificates can be stored in the user's personal certificate store or in a secure location accessible by the pipeline. This method is useful for scenarios where no user interaction is required.

### Comparison of Authentication Methods

To aid in the authentication discussion providing a brief summary of the different authentication methods 

#### Browser-Based Authentication

- **Advantages**: Supports many MFA methods, generates persistent browser cookies, allows non-interactive execution of subsequent tests.
- **Disadvantages**: Requires an initial interactive session to create the browser context.

#### Certificate-Based Authentication

- **Advantages**: Can be optionally configured as a 2FA claim, useful for scenarios where no user interaction is required.
- **Disadvantages**: Requires certificates to be configured and stored securely.

#### Conditional Access Policies

- **Advantages**: Enforces security requirements, ensures compliance with organizational policies, controls access based on device and location.
- **Disadvantages**: Can be complex to configure and span multiple teams, may restrict access in certain scenarios, requires ongoing management and updates.

## Logging for the Login Process

As part of the login process, there could be issues with the selected authentication method. This process could be run interactively or as part of a headless test execution, such as tests run inside an Azure DevOps Build agent or GitHub runner. Without detailed logs, it can be difficult to diagnose and resolve issues in the login process.

### Enhanced Logging Solution

When you encounter issues the log level can be set to Debug or greater which will generated enhanced logs to summarize the login process. This information includes:

- Selected Authentication Method
- Summary of the request sent as part of the login process
- Network page requests and HTTP status codes

Using this information will enable users to diagnose and resolve authentication issues that occur.

## Discussion Questions

As you consider authentication as part of the Tst Engine, consider the following questions:

1. What are the key challenges you have faced with browser-based authentication in your test automation processes?
2. How will the impacts of your organizations security standards affect your automated testing strategy? 
3. Will your organizations conditional access policies limit the choices of where and how you can run your automated tests?
4. Have you implemented certificate-based authentication in your test automation? If so, what were the trade-offs and benefits?
5. How do you ensure compliance with your organization's security policies when running tests from CI/CD build agents?
