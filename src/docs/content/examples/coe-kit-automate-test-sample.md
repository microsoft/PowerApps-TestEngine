---
title: Executing CoE Starter Kit Test Automation
---

## Wider Context

We are starting our testing with the Setup and Upgrade Wizard however the CoE Starter Kit has a much wider set of components that need to be tested. We are continually reviewing our automated test approach to consider the wider scope of the CoE Starter Kit.

![Diagram showing the different layers of CoE Starter Kit Test across - Authentication, Power Apps, Power Automate, AI Builder, Dataverse, Power Platform and Microsoft Fabric](/PowerApps-TestEngine/examples/media/coe-kit-testing-layers.png)

Overall the automated testing of the CoE Starter Kit will need to address the various layers and components of the CoE Starter Kit that require testing including:

- **Browser Login**: Ensuring compatibility with multi-factor authentication setups.
- **Model-Driven and Canvas Apps**: Validating the performance and functionality of both types of applications.
- **Power Automate Features**: Testing the integration and functionality of Power Automate features called from Power Apps
- **Cloud Flows**: How we will apply unit and integration testing to the over 100 cloud flows need to be tested for end-to-end integration.
- **Desktop Flows**: Ensuring desktop flows operate correctly within the kit.
- **AI Builder Prompts**: Testing AI builder prompts within the [Business Value Assessment Toolkit](https://learn.microsoft.com/power-platform/guidance/coe/business-value-toolkit).
- **Database Setup**: Verifying the overall environment setup and database configurations.
- **Power Platform**: Deployment of the CoE Starter kit across multiple environments globally from [Early release](https://learn.microsoft.com/en-us/power-platform/admin/early-release) and other globally available [regions](https://learn.microsoft.com/power-platform/admin/regions-overview)
- **Microsoft Fabric and Power BI**: Ensuring the new data export process using Microsoft Fabric integrates seamlessly with Power BI.

## Setup and Upgrade Wizard

The CoE Starter Kit Test Automation for the Setup and Upgrade Wizard is a comprehensive process designed to ensure seamless configuration and functionality of the CoE Starter Kit Core. 

These tests have been created to demonstrates how a complex solution that includes Power Apps, Power Automate and Dataverse components can be combined to demonstrate how to perform unit and integration tests with the deployed solution.

The sample can be [CoE Kit setup wizard](https://github.com/microsoft/PowerApps-TestEngine/tree/grant-archibald-md/integration-merge/samples/coe-kit-setup-wizard) in the Power Apps Test Engine repository. The starting guidance includes the steps for the necessary prerequisites that need to be in place to execute the test

### Configuration

The journey begins with the configuration file, `config.json`. This file is pivotal as it supplies the tenant and environment details where the CoE Starter Kit Core has been imported. By accurately configuring this file, we ensure that the automation script interacts with the correct environment, laying the foundation for a successful test execution.

### Power Platform Command Line Interface

Next, we leverage the Power Platform Command Line Interface (CLI) to authenticate the script with our environment. Before running the script, we can execute the [pac auth clear](https://learn.microsoft.com/power-platform/developer/cli/reference/auth#pac-auth-clear) and [pac auth create --environment](https://learn.microsoft.com/power-platform/developer/cli/reference/auth#pac-auth-clear) commands. 

These commands set up and authenticate our connection with the Power Platform, ensuring that the script has the necessary permissions to perform its tasks. Additionally, the sample script utilizes the [pac fx run](https://learn.microsoft.com/power-platform/developer/cli/reference/power-fx#pac-power-fx-run) command to query Dataverse and retrieve the Model Driven Application application ID in our environment.

### Build from Source

To ensure we are using the latest version of the Power Apps Test Engine, we build it from the source. This involves cloning the repository and using the .Net SDK to compile the latest version. By doing so, we guarantee that our test automation is equipped with the most recent features and improvements, enhancing the reliability and accuracy of our tests.

### Authentication

Authentication is a critical component of the test automation process. The sample script employs browser-based authentication, which offers a range of options to authenticate with Microsoft Entra. This method generates a persistent browser cookie, allowing for non-interactive execution of subsequent tests. The management of these browser cookies is governed by the guidelines provided in the Microsoft Entra documentation on session lifetime and conditional access policies.

![Authentication Options](/PowerApps-TestEngine/examples/media/authentication-options.png)

As the Power CAT Engineering team looked at the different authentication options for the CoE Starter Kit we have started with browser based authentication for local testing. This approach works with conditional access policies of our environments and allows a quick and less intrusive process to authenticate as different test persona.

As we look at our continuous integration and deployment process we will evaluate certificate based authentication to determine the correct mix of authentication and management trade-offs to select authentication options and what secret store is used to securely host the login credentials.

#### Supporting Multi-Factor Authentication
The use of browser-based authentication in the test automation process is particularly advantageous for supporting multi-factor authentication (MFA). MFA is a security measure that requires users to provide multiple forms of verification before gaining access to a system. By leveraging browser-based authentication, the test automation script can seamlessly integrate with the organization's MFA settings and policies.

#### Implications for Browser Agents, Operating Systems, and Management Policies

Several factors come into play when implementing this approach, including the allowed browser agents, operating systems, and management policies. For instance, the organization may require that the machine running the tests is managed by Microsoft Intune. This ensures that the device complies with the organization's security policies and can be trusted to execute the tests.

#### Interactive Users vs. CI/CD Build Agents
The implications of this approach extend beyond interactive users to the execution of tests from a continuous integration and deployment (CI/CD) build agent. In such scenarios, it may be necessary to use a custom build agent that adheres to the organization's security policies. This includes ensuring that the build agent is managed by Microsoft Intune and that it can handle browser-based authentication and MFA.

#### Managing Browser-Based Cookies
The management of browser-based cookies is crucial for maintaining the security and integrity of the test automation process. Persistent browser cookies allow for non-interactive execution of subsequent tests, reducing the need for repeated authentication. However, it is essential to manage these cookies in accordance with the organization's conditional access policies. The following resources provide valuable insights into managing browser-based cookies:

[Persistence of browsing sessions](https://learn.microsoft.com//entra/identity/conditional-access/concept-session-lifetime#persistence-of-browsing-sessions): This document discusses how persistent browser sessions allow users to remain signed in after closing and reopening their browser window.

[Conditional access policies for browser persistence](https://learn.microsoft.com/entra/identity/conditional-access/policy-all-users-persistent-browser) : This example demonstrates how to optionally protect user access on devices by preventing browser sessions from remaining signed in after the browser is closed and setting a sign-in policy.
By understanding and implementing these concepts, organizations can ensure that their test automation processes are secure, compliant, and capable of supporting multi-factor authentication in alignment with their Entra settings and policies.

### Model Driven Application Provider

The automation process also makes use of the new Model Driven Application provider. This provider is instrumental in automating the custom page of the Setup and Upgrade Wizard, ensuring that all configurations and settings are correctly applied and tested.

### Power FX based Test steps

The Power Apps Test Engine is designed to simplify the testing process by abstracting away much of the complexity involved in interacting with Power Apps. One of the key areas where this abstraction is evident is in the login process. Instead of requiring teams to write extensive code to handle authentication, the Test Engine manages this process seamlessly. This allows teams to focus on the actual test actions within the Power App, rather than the surrounding processes.

In summary, the Power Apps Test Engine abstracts the complexity of tasks like the login process, enabling teams to focus on testing the core functionality of their Power Apps. This approach not only simplifies the testing process but also enhances productivity and reduces the learning curve for teams new to automated testing. By leveraging the built-in capabilities of the Test Engine and its extensibility options, teams can create robust and maintainable tests that align with their organizational requirements.

#### Test Steps Walkthrough

The test cases take advantage of a set of Power FX steps to abstract interacting with the Power App. The [CoE Setup and Install Wizard](./coe-kit-setup-and-install-wizard.md) steps through these low code functions to describe how the test cases and steps interact to validate the behavior of the application.

#### Code First vs Low Code

Many teams often start with code-first tools like [Playwright](https://playwright.dev/), which require detailed scripting for every interaction, including login and authentication. While these tools are powerful, they can be time-consuming and complex to set up, especially for teams that are new to automated testing. By using the Power Apps Test Engine, teams can leverage pre-built steps that handle common tasks, such as logging in, without needing to write custom code for each scenario.

The Power FX steps used in the test cases provide a high-level abstraction for interacting with the Power App. This means that testers can write tests using a declarative approach, specifying what actions need to be performed rather than how to perform them. This not only speeds up the test creation process but also makes the tests easier to read and maintain.

#### "No cliffs" Extensibility

Moreover, the Power Apps Test Engine offers "no cliffs" extensibility, allowing teams to extend its capabilities when needed. For example, if a specific test scenario requires custom functionality, teams can use [C# scripts](./extending-testengine-powerfx-with-with-csharp-test-scripts.md) or wrap common functionality as new [Power Fx functions](./understanding-no-cliffs-extensibility-model.md). This flexibility ensures that the Test Engine can accommodate a wide range of testing needs, from simple interactions to complex workflows.

### Test Output - Traces, Logs, and Videos

Finally, the test output managed by the configurable log level provides a summary of the test. This is contained in the `TestOutput` folder which contains detailed logs and video recordings of the test sessions, providing valuable insights for later analysis. 

Additionally, the Test Engine generates a `.trx` file, which can be parsed or uploaded to a CI/CD process to summarize the outcomes of the tests. This comprehensive documentation ensures that any issues can be quickly identified and addressed, maintaining the integrity and performance of the CoE Starter Kit Core.
