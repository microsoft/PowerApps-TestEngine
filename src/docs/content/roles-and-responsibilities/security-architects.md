---
title: Security Architects
---

Welcome to the section dedicated to Security Architects! Here, we'll explore the roles and responsibilities of security architects in the low-code testing landscape on the Power Platform. Let's dive into their interests, oversight, and the importance of automated testing.

## Interests and Oversight

As a Security Architect, your primary focus is on managing cybersecurity and data privacy, ensuring that low-code solutions do not expose the organization to risk. You play a crucial role in maintaining the security and compliance of these solutions. Your responsibilities include:

- **Cybersecurity Management**: Ensuring that low-code solutions are secure and do not introduce vulnerabilities into the organization's IT infrastructure.
- **Data Privacy**: Protecting sensitive data and ensuring that low-code solutions comply with data privacy regulations and organizational policies.
- **Compliance Assurance**: Verifying that low-code solutions adhere to security standards and regulatory requirements, providing a secure environment for development and deployment.

Your oversight extends to ensuring that the Power Platform solutions are integrated with existing security frameworks and processes, providing a cohesive approach to both low-code and code-first development.

## The Need for Automated Testing

Automated testing is a vital tool for Security Architects. It helps in:

- **Efficiency**: Automated tests can be run quickly and repeatedly, saving time and reducing the manual effort required for testing.
- **Consistency**: Automated tests provide consistent results, reducing the risk of human error and ensuring that tests are performed the same way every time.
- **Early Detection**: Automated tests can catch security issues early in the development process, allowing for quicker fixes and reducing the impact on the final product.
- **Scalability**: Automated testing can easily scale to accommodate larger projects and more complex testing scenarios, making it an essential tool for Security Architects.

By embracing automated testing, you can ensure that the Power Platform solutions are secure, compliant, and ready for deployment. This not only enhances the quality of the solutions but also contributes to the overall success of the organization.

## Persona-Based Testing
Persona-based testing involves creating different user personas to test how the application behaves under various conditions and user scenarios. This approach helps in identifying issues that might not be apparent when testing with a single user type. The [Permissions](https://github.com/microsoft/PowerApps-TestEngine/blob/main/samples/permissions/README.md) sample provides an example of how to create tests for the following test scenarios.

### Expected Test Cases

User with Permissions and License: Ensure that the application opens correctly for a user who has the necessary permissions and a valid license. Verify that all functionalities are accessible and perform as expected
.
User with Limited Permissions: Test the application for a user with restricted permissions to ensure that they can only access the features they are authorized to use.

### Edge Cases

Application Not Shared: Test scenarios where the application has not been shared with the user. The user should receive an appropriate error message or be denied access.

No Security Role: Verify that a user without the necessary security role cannot access the application or specific features within it.

### Exception Cases

No License: Ensure that users without a valid license cannot access the application. They should receive a clear message indicating the need for a license.

Expired License: Test the behavior of the application when a user's license has expired. The application should restrict access and prompt the user to renew their license.

## Context

| Context | Notes |
|---------|-------|
| [Secure First Initiative](../context/security-first-initiative.md) | By integrating these principles, we aim to create robust, resilient, and secure applications that can withstand evolving cyber threats. |
| [Test Engine Security - Authentication Changes](../context/security-testengine-authentication-changes.md) | Provides context on the evolution from basic authentication to more secure authentication providers with updated changes in the Power Apps Test Engine |
| [Deep Dive: Test Engine Storage State Security](../context/security-testengine-storage-state-deep-dive.md) | One of the key elements of automated discussion using the multiple profiles of automated testing of Power apps is the security model to allow login and the security around these credentials. Understanding this deep dive is critical to comprehend how the login credential process works, how login tokens are encrypted, and how this relates to the Multi-Factor Authentication (MFA) process. |

## Discussions

The following [discussions](../discussion) could be of interest

| Discussion | Description |
|------------|-------------|
| [Authentication in Power Apps Test Engine](../discussion/authentication.md) | Authentication is a critical component of the test automation process. The sample script employs browser-based authentication, which offers a range of options to authenticate with Microsoft Entra. This method generates a persistent browser cookie, allowing for non-interactive execution of subsequent tests. The management of these browser cookies is governed by the guidelines provided in the Microsoft Entra documentation on session lifetime and conditional access policies. |

## Example Concepts

| Example | Description |
|---------|-------------|
| [Testing Security](../examples/testing-security.md) | This article provides an example of how we can test browser-based authentication using multiple personas using Multi-Factor Authentication (MFA) with persistent cookie state and Power Apps security for Power Apps. 