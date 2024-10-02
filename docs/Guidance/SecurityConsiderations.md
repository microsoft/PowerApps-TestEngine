# Security Considerations

Executing tests securely is paramount especially where interactive user accounts are used. The following table outlines some factors that should be considered and how this can map to Power Platform quality testing.

## Authentication

The following authentication options could be considered

| Type	| Description	| Considerations
|-------|---------------|----------------|
| Browser Persistent Cookies	| One time login and using persistent cookies for headless login	| 1.	Storage of Browser Context that contains Persistent Cookies
| | | 2.	Configuration of Persistent Cookie settings in Entra
| | | 3.	Conditional Access Policies
| Certificate Based Authentication |	Configuration of Entra to allow Certificate Based Authentication| 	1.	Issue and Revocation of Certificates
| | | 2.	Certificate renewal process
| Conditional Access Policies |	Compliance with applied conditional Access polices	| 1.	Supported Browser type selection. For example, Edge, Chrome
| | | 2.	Test Agent network locations
| | | 3.	Risk profile of users


## Multi Factor Authentication – Authenticator Enabled
When using Two-factor authentication making use of authentication applications like Microsoft Authenticator consider the following:
1.	Using Browser authentication type where Certificate Based Authentication is not an available choice.
2.	Browser authentication requires an initial interactive login to provide Persistent cookies later non interactive sessions
3.	Security and storage of Browser Context used by Test Engine
4.	Review the [Lifetime and revocation of browser-based cookies managed by Entra](https://learn.microsoft.com/entra/identity/authentication/concepts-azure-multi-factor-authentication-prompts-session-lifetime) for recommended Persistent cookie settings
5. The role of [Configure authentication session management with Conditional Access](https://learn.microsoft.com/entra/identity/conditional-access/howto-conditional-access-session-lifetime).

## Multi Factor Authentication – Certificate Based Authentication Enabled
Entra has been configured to enable Certificate Based Authentication for multi factor authentication
1.	Consider using Certificate authentication type
2.	Process to generate and issue certificates to test agents
3.	Process to review and revoke generated certificates

## Conditional Access Policies

Conditional Access policies are used to define and enforce access controls based on specific conditions, such as user location, device compliance, and application sensitivity. In the context of browser-based testing, these policies can restrict access to the Power Platform based on the browser agent being used, the location from which the user is signing in, and other factors. By implementing these policies, organizations can ensure that only trusted users and devices can perform browser-based testing, reducing the risk of unauthorized access and potential security breaches.
1.	Allowed Browser Agents: Specify which browser agents are permitted to access the Power Platform. This can include popular browsers like Chrome, Edge, and Firefox, while blocking unsupported or less secure browsers.
2.	Sign Locations: Define the geographic locations from which users are allowed to sign in. This can help prevent access from unauthorized or high-risk locations.
3.	Device Compliance: Ensure that only compliant devices, which meet the organization's security standards, are allowed to access the Power Platform. This can include checking for up-to-date antivirus software, encryption, and other security measures.
4.	Multi-Factor Authentication (MFA): Require users to authenticate using multiple factors, such as a password and a mobile app, to enhance security and reduce the risk of unauthorized access.
5.	Session Controls: Implement session controls to manage the duration and scope of user sessions. This can include setting time limits for sessions and requiring re-authentication after a certain period of inactivity.
6.	Network Location: Restrict access based on the network location, such as allowing access only from the corporate network or trusted VPNs.
7.	User Risk: Assess the risk level of users based on their behavior and history. High-risk users may be required to undergo additional verification steps or have their access restricted.

## Test Environment

The test environment is a crucial aspect of the quality lifecycle, ensuring that tests are executed in a controlled and representative setting. One area to consider using a shallow copy of the production environment with test data and connection to test system applied. This approach allows for accurate simulation of real-world scenarios while maintaining the integrity and security of the production environment.

Executing tests using a shallow copy of the production environment involves creating a replica of the production environment with minimal data. This ensures that the tests are conducted in an environment that closely mirrors the actual production setup, providing reliable and accurate results. The test data used in this environment should be carefully curated to represent typical use cases and edge cases, allowing for comprehensive testing of the solution.

By using a shallow copy of the production environment, teams can identify and resolve issues before they impact the live system. This approach also helps in maintaining the consistency and reliability of the testing process, ensuring that the solution meets the required quality standards.

## Power Platform Security Model for Security Groups and Sharing of the Deployed Test Solution

The Power Platform security model is designed to ensure that only authorized users have access to the deployed test solution. This involves the use of security groups and sharing mechanisms to control access and maintain the integrity of the solution.

### Security Groups

Security groups are used to managing user access to the Power Platform. By assigning users to specific security groups, administrators can control who has access to the deployed test solution and what actions they can perform. This helps ensure that only authorized users can interact with the solution, reducing the risk of unauthorized access and potential security breaches.
1.	Creating Security Groups: Administrators can create security groups in the Power Platform admin center or through Azure Active Directory. These groups can be based on roles, departments, or specific access requirements.
2.	Assigning Users to Security Groups: Users can be added to security groups based on their roles and responsibilities. This ensures that only users with the necessary permissions can access the deployed test solution.
3.	Managing Security Groups: Administrators can manage security groups by adding or removing users, updating group permissions, and monitoring group activities. This helps maintain the security and integrity of the deployed test solution.

## Sharing the Deployed Test Solution

Sharing the deployed test solution involves granting access to specific users or security groups. This ensures that only authorized users can access and interact with the solution.
1.	Sharing with Security Groups: Administrators can share the deployed test solution with specific security groups. This allows for easy management of user access and ensures that only authorized users can interact with the solution.
2.	Sharing with Individual Users: In some cases, it may be necessary to share the deployed test solution with individual users. This can be done by granting access to specific users based on their roles and responsibilities.
3.	Managing Shared Access: Administrators can manage shared access by updating permissions, revoking access, and monitoring user activities. This helps maintain the security and integrity of the deployed test solution.
By using security groups and sharing mechanisms, administrators can ensure that only authorized users have access to the deployed test solution. This helps maintain the security and integrity of the solution, reducing the risk of unauthorized access and potential security breaches.
