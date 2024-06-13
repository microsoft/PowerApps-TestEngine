# Overview of the Model Driven Application Web Test Provider for the Power Apps Test Engine

## Purpose

The Model Driven Application Provider implements a [Managed Extensibility Framework](https://learn.microsoft.comdotnet/framework/mef/) (MEF) extension enhances the Power Apps Test Engine by enabling comprehensive unit and integration testing for web-based Model Driven Applications (MDA).

The provider encapsulates the XRM SDK using Playwright by allowing tests to be created with low code Power Fx commands for test for automation, ensuring a seamless and efficient testing process.

This extension build on the Test Engine YAML definitions to define and execute test suites and cases, generating test results as TRX files, which can be integrated into CI/CD processes.

## Key Features

1. **Unit and Integration Testing**:
   - The extension supports comprehensive unit and integration testing to ensure that both individual components and the interactions between different modules within Model Driven Applications operate correctly.
   - Unit testing verifies the proper functioning of individual application elements.
   - The ability to mock data connections to isolate the behavior of the application
   - Integration testing ensures that the overall application perform as expected when connected to dependent components

2. **Low Code Extensibility with Power Fx**:
   - The provider encapsulates the XRM SDK within low code Power Fx commands, enabling users to efficiently interact with the Model Driven Application’s data and logic.
   - This approach simplifies test scripting, making it accessible to users with limited coding experience.
   - Power Fx commands allow testers and developers to write and execute test cases without requiring deep expertise in JavaScript.
   - By working with the JavaScript object model tests are abstracted from changes in the Document Object Model (DOM) used to represent the application

3. **Playwright for Web Automation**:
   - The extension leverages Playwright to automate testing for web-based Model Driven Applications.
   - Playwright enables reliable and consistent automation of the application’s user interface, verifying that UI components behave as expected under various conditions.
   - Provide for multi browser testing
   - ALlow for recorded videos and headless testing

4. **YAML-Based Test Definition and Execution**:
   - The extension uses YAML definitions to structure and manage test suites and test cases.
   - This supports clear and concise test configurations, facilitating easier maintenance and updates.
   - Tests can be executed automatically, streamlining the testing process within development pipelines.

5. **Generation of TRX Files for CI/CD Integration**:
   - After test execution, the results are generated as TRX files.
   - These TRX files can be integrated into CI/CD pipelines, providing feedback on the quality and reliability of the application.
   - This integration helps maintain high standards and continuous quality assurance throughout the development lifecycle.

6. **Seamless Integration with Power Apps**:
   - The MEF extension integrates smoothly with the existing Power Apps Test Engine, ensuring a cohesive testing environment.
   - It supports a consistent testing framework across all stages of application development, from initial development to pre-deployment validation.

7. **Enhanced Developer and Tester Productivity**:
   - The low code approach with Power Fx commands and the use of Playwright for automation, combined with YAML-based definitions, enhances productivity.
   - Developers and testers can rapidly create and execute test cases, locate issues, and iterate on solutions efficiently.

## Benefits

- **High-Quality Applications**: Ensures comprehensive testing of web-based Model Driven Applications, leading to reliable and robust deployments.
- **Efficient Development and Testing**: Reduces the time and effort needed for testing, allowing teams to focus on innovation and development.
- **Accessibility**: Low code Power Fx commands make testing accessible to a broader range of users, including non-developers.
- **Consistency**: Provides a standardized testing framework, ensuring predictable and manageable processes.
- **CI/CD Integration**: Enhances continuous integration and continuous deployment processes by incorporating test results seamlessly, ensuring ongoing quality assurance.

## Roadmap

1. **Generative AI for Test Case Creation**:
   - Future enhancements could include the ability to use generative AI to automate the process of converting natural language descriptions into test cases and Power Fx test steps.
   - This will significantly accelerate the creation and maintenance of tests by enabling users to describe testing scenarios in plain language.
   - The AI will interpret these descriptions and automatically generate the corresponding test cases and steps, reducing the manual effort involved and increasing the accuracy and consistency of test scripts.

2. **Enhanced Natural Language Processing (NLP)**:
   - Improved NLP capabilities will allow for more sophisticated understanding and interpretation of natural language inputs.
   - This will enable more complex and nuanced test scenarios to be accurately translated into executable test steps.
   - This could include synthetic test data generation.

## Examples

[Basic MDA - Assert Form Properties](../../samples/mda/testPlan.fx.yaml)

## Capabilities

The following table outlines the scope of testing Model Driven Applications and current support for features in the provider

| Capability               | Description                                                                                     | Supported |
|--------------------------|-------------------------------------------------------------------------------------------------|-----------|
| Forms (Get)              | Ability to read all [controls](./controls.md) as Power Fx variables.                                           | Y
| Forms (Set)              | Ability to set Power Fx variables which change form data.       |
| Navigation               | Provides methods for navigating forms and items in Model Driven Apps.                           |
| Panels                   | Provides a method to display a web page in the side pane of Model Driven Apps forms.            |
| Views (Get)              | Ability to query grids
| Views (Actions)          | Ability to select and take actions on view items.                |
| Web API                  | Supports operations with both Online and Offline Web API.                                       |
| Data Operations          | Allows CRUD (Create, Read, Update, Delete) operations on records within Model Driven Apps.      |
| Workflow Execution       | Enables triggering workflows and monitoring their progress.                                     |
| Business Logic           | Ability to trigger and test custom business logic and validations.                            |
| Security and Roles       | Provides methods to manage and test role-based security within the application.                 |
| Notifications            | Capability to display and manage system and user notifications within the application.          |
| Entities and Attributes  | Powers querying and manipulation of entity attributes and relationships.                        |
| User Context             | Methods to retrieve and utilize user context information within tests.                          |
| Global Variables         | Supports the use of global variables for maintaining state and shared data across tests.        |
| Audit and Logs           | Ability to access and interact with audit logs and system logs for compliance and debugging.    |
| Solutions                | Allows for importing, exporting, and managing solutions within the application.                 |
| Command Bars and Actions | - Ability to interact with command bars to automate and validate command bar actions and custom buttons.|
| Custom Pages             | The ability to test custom pages |
| Localization             | Ability to specify locale for localization of navigation, commands and labels |
