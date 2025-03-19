---
title: Test Engine Security - Authentication Changes
---

When the experimental release of the Test Engine was initially made available, the only authentication method provided was through environment variables for the username and password. This approach presented several issues. Firstly, it relied on Basic authentication, which is inherently less secure as it transmits credentials in an easily decodable format. Additionally, this method did not align with Microsoft's strong recommendations for Multi-Factor Authentication (MFA), which is crucial for enhancing security by requiring multiple forms of verification.

The reliance on environment variables for authentication posed significant risks. Environment variables can be inadvertently exposed through logs, debugging sessions, or misconfigurations, leading to potential credential leaks. Moreover, Basic authentication does not provide the robust security measures needed to protect sensitive information, making it vulnerable to various attacks such as phishing and man-in-the-middle attacks.

In response to these security concerns, the newer updates to the Test Engine have introduced a new storage state provider that integrates with the browser's storage state. This update allows for Multi-Factor Authentication, significantly enhancing the security of the authentication process. By leveraging the browser's storage state, the Test Engine can now support more secure and user-friendly authentication methods, aligning with Microsoft's Security Framework Initiative (SFI).

The integration with the browser's storage state enables the use of modern authentication mechanisms, such as OAuth and OpenID Connect, which support MFA. These mechanisms provide a more secure and seamless user experience by allowing users to authenticate through familiar and trusted interfaces. Overall it allows the use organization controlled settings to manage the lifetime of the created persistent login state. 

In conclusion, the shift from environment variable-based authentication to a storage state provider that supports Multi-Factor Authentication marks a significant improvement in the security posture of the Test Engine. This change not only addresses the inherent vulnerabilities of Basic authentication but also aligns with Microsoft's broader security recommendations and initiatives, ensuring a more secure and reliable authentication process for users.

## Example: Previous vs. New Authentication Approach

### Previous Approach: Using pac test run
In the previous approach, the pac test run command required setting both the username and password as environment variables. Here is an example:

```pwsh
$env:user1Email="your_username"
$env:user1PAssword="your_password"
pac test run -test testPlan.yaml
```

This method relied on Basic authentication, which, as mentioned earlier, is less secure and does not support MFA.

### New Approach: Using Storage State Provider

In the new approach, only the email is set via an environment variable, and user credentials are collected during the first interactive session and saved to the storage state. Here is an example:

```pwsh
$env:user1Email="your_username"
pac test run  -test testPlan.yaml
```

During the first run, the user will be prompted to enter their credentials interactively. These credentials are then saved securely in the browser's storage state, allowing for MFA and eliminating the need to store sensitive information in environment variables.

### Deeper Dive into Storage State Authentication

For a more detailed understanding of the storage state authentication and the choices available, you can refer to the [Deep Dive - Test Engine Storage State Security](./security-testengine-storage-state-deep-dive.md).
