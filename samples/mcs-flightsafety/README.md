# MCS Flight Safety - Copilot Testing Sample

This sample demonstrates how to test Microsoft Copilot Studio agents using the PowerApps Test Engine. The Test Engine provides two different providers for testing Copilot Studio agents, each with distinct capabilities and requirements.

## Provider Comparison: `copilot` vs `copilot.portal`

### `copilot` Provider

The `copilot` provider integrates directly with the Copilot Client SDK and provides programmatic access to Copilot Studio agents.

**Key Characteristics:**
- Uses the official Copilot Client SDK
- Requires OAuth token authentication
- Programmatic API-based testing approach
- More suitable for automated CI/CD pipelines

**Requirements:**
1. **Entra App Registration**: Must be configured with appropriate permissions
2. **Power Platform API Permissions**: Requires `CopilotStudio.Copilot.Invoke` scope
3. **Admin Consent**: Depending on your organization's setup, this permission may require administrator consent to be granted to the App Registration
4. **OAuth Token**: Valid authentication token must be provided

**Use Cases:**
- Automated testing in CI/CD pipelines
- Integration testing scenarios
- Programmatic validation of copilot responses
- Testing without UI dependencies

### `copilot.portal` Provider

The `copilot.portal` provider tests agents through the Copilot Studio Portal web interface, simulating real user interactions.

**Key Characteristics:**
- Tests through the web-based Copilot Studio Portal interface
- Simulates actual user interactions in the browser
- Uses Playwright for web automation
- More closely mirrors end-user experience

**Requirements:**
1. **Interactive Web Session**: Must establish a valid web session via Test Engine
2. **Browser Automation**: Relies on Playwright for web interface interaction
3. **Portal Access**: User must have access to the Copilot Studio Portal
4. **Web Authentication**: Standard web-based authentication to the portal

**Use Cases:**
- End-to-end user experience testing
- Visual validation of copilot interactions
- Testing portal-specific features
- Scenarios requiring browser-based context

## Test Execution

## Security Considerations

### `copilot` Provider Security
- OAuth tokens are obtained using MSAL
- App Registration permissions should follow principle of least privilege
- Consider using service principals for automated scenarios
- Monitor token usage and access patterns

### `copilot.portal` Provider Security
- Web sessions are temporary and browser-based
- No entra App registration is required
- Relies on standard web authentication mechanisms
- Session management handled by the portal

## Choosing the Right Provider

**Choose `copilot` when:**
- Building automated test suites for CI/CD
- You have proper OAuth infrastructure in place
- Admin consent for API permissions is available
- You need programmatic, headless testing

**Choose `copilot.portal` when:**
- Testing the complete user experience
- OAuth setup is not feasible
- You need to validate portal-specific functionality
- Visual validation is important
- Admin consent for API permissions is not available

## Troubleshooting

### Common `copilot` Provider Issues
- **Permission Denied**: Verify Entra App Registration has `CopilotStudio.Copilot.Invoke` scope
- **Admin Consent Required**: Contact your IT administrator to grant consent for the App Registration
- **Token Expired**: Refresh your OAuth token
- **Scope Issues**: Ensure the token includes the required Power Platform scopes

### Common `copilot.portal` Provider Issues
- **Portal Access**: Verify user has access to Copilot Studio Portal
- **Browser Issues**: Ensure Playwright dependencies are installed
- **Session Timeout**: Re-authenticate if the web session expires
- **Network Connectivity**: Verify connection to Copilot Studio Portal

## Additional Resources

- [Copilot Studio Documentation](https://docs.microsoft.com/en-us/microsoft-copilot-studio/)
- [Entra App Registration Guide](https://docs.microsoft.com/en-us/azure/active-directory/develop/quickstart-register-app)
- [Power Platform API Permissions](https://docs.microsoft.com/en-us/power-platform/admin/api-permissions)
