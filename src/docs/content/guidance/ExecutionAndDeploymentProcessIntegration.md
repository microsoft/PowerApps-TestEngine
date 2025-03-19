# Test Execution and Deployment Process Integration

## Execution Agents
Tests can be executed as part of the deployment process using agents configured in Low code Power Automated Hosted Process or Azure DevOps CI/CD pipelines. These agents will:
- Run automated tests during the build phase to catch issues early.
- Execute end-to-end tests post-deployment to validate the entire system.

## Test Agent Selection
The following table outlines some factors that can be considered when selecting a test execution environment for the tests.

| Criteria |	Power Automate Hosted Process |	Azure DevOps Custom Windows Agent
|----------|----------------------------------|----------------------------|
| Executive Summary	| Investing in Power Automate Hosted Process offers significant advantages in integration capabilities, ease of use, and fostering low-code innovation. This leads to increased efficiency, reduced operational costs, and enhanced agility. | Azure DevOps with a custom Windows Build Agent to comply with Conditional Access policy and Microsoft Intune provides pro code solution for CI/CD pipelines, offering flexibility and integration with various development tools. It supports traditional development workflows and needs to be managed by customers.
| Easier Integration Tests	| Seamless Integration Over 1,000 connectors for Microsoft and third-party services. |	CI/CD Integration: Easily integrates with CI/CD pipelines for automated testing.
| |	Unified Platform: Simplifies complex workflows and data processing tasks. |	Custom Scripts: Supports custom scripts and tools for comprehensive testing. 
|	| Example: Automate data extraction from ERP, process in Excel, update CRM records.	| Example: Automate build, test, and deployment processes using Azure DevOps pipelines.
| Security Integration	| User interface tests will be Intune Managed Windows 11 machines. This makes the process of integrating with organization defined Security policies like Conditional Access policies easier to integrate with |	It is likely to require Microsoft Intune registration and management. If standard security policies are not able to be run on custom agent, then alternative security reviews and exceptions may need to be sought.
| Support for CI/CD Frameworks	| Complements existing CI/CD frameworks with pre-deployment checks, post-deployment validations, etc. |Built-in support for Azure DevOps, GitHub Actions, Jenkins, and other CI/CD tools.
| |	Example: Automate validation of deployment environments, run smoke tests, notify stakeholders.	| Example: Use custom agents to run automated tests and deployments across multiple environments.
| Fostering Low-Code Innovation |	Empowering Business Users: Enables business users to create/manage workflows without heavy IT reliance | Developer Focused: Primarily supports traditional development teams and workflows.
| |	Rapid Prototyping: Facilitates quick testing and refinement of new processes |	Custom Development: Supports custom development and complex CI/CD pipelines
| |	Example: Marketing team automates lead generation and follow-up processes. | Example: Development team automates build and deployment processes for faster release cycles.
| Cost Profile |	Reduced Development and Management Costs: Less need for custom development to build and manage the infrastructure to execute tests.	| Infrastructure Costs: Require physical or cloud hosted infrastructure costs to provision, upgrade machines. 
| Use Case |	Automating repetitive tasks, integrating with services, running desktop flows	| CI/CD pipelines, building, testing, and deploying code
| Management and Maintenance	| Fully managed by Microsoft |	Requires cloud hosted or own agent infrastructure and maintenance 
| Scalability |	Scales easily with additional machines in Machine Groups | Scales with additional agents, requires infrastructure scaling
| Integration and Compatibility	| Seamless integration with Power Automate and other Microsoft services	| Integrates well with Azure DevOps, GitHub, and other CI/CD tools
| Security and Compliance	| Built-in security features and compliance with Microsoft standards and Intune policies	| Security depends on self-management 
| Team Alignment	| Preferred by low-code teams	| Preferred by pro dev teams
| Innovation vs. Control	| Empowers low-code innovation	| Traditional development tools and processes
