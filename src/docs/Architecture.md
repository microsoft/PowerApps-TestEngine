## Overview

This document provides an architectural overview of the Power Apps Test Engine. The engine consists of multiple layers, each with specific responsibilities that contribute to delivering a comprehensive and automated testing framework for Power Apps. The layers include the Test Engine, Test Definition, Test Results, and Browser, with an extensibility model available for User Authentication, Providers, and Power Fx.

![Test Engine Overview Diagram](./media/overview.png)

The Power Apps Test Engine is a robust and modular framework that has been designed to facilitate comprehensive testing of various components within the Power Platform. At its core, the Test Engine features a Runner, which can be executed as part of the Power Platform Command Line Interface (PAC CLI) or through a build-from-source approach using open-source code. The PAC CLI option offers a supported and straightforward method to execute tests, while the build-from-source strategy, which requires the installation of the .Net SDK, lacks official support. 

Test suites and cases are formatted as YAML files, combining test settings and Power Fx Test Steps to define, manage and version the test cases. 

The test engine is made up of three extensible components for Authentication, Providers and Power Fx extensibility. Authentication is a foundational aspect, as test cases must authenticate with the Power Platform to run effectively. The Test Engine includes a set of modules, or Providers, that enable testing for Power Apps, including Canvas Applications and Model-Driven Applications, as well as experimental modules for Power Automate. The Power Fx Extensions allow for the extension of the Power Fx language, providing additional functions for Test Steps.

The results of tests can be added to standard CI/CD pipelines or uploaded to Dataverse to summarize and report on the outcome of a test.

## Layers

### Test Engine
The Test Engine is the core component of the testing framework.

- **pac test run:** Integrated into the product as a built-in feature.
- **Open source .Net application:** Available under open source licenses, allowing for customization and community contributions.

The Test Engine is responsible for orchestrating the entire testing process, invoking test cases, and managing the execution flow.

### Test Definition
Test Definition is where the specifics of what to test are outlined.

- **Yaml file:** Defines the test suite and individual test cases.
- **Power Fx:** Used to define the steps of a test case. Power Fx is a powerful, Excel-like formula language utilized in Power Apps.

The Yaml file format provides a flexible and human-readable way to specify test scenarios, while the Power Fx language allows for expressive and precise test steps.

### Test Results
Test Results summarize the outcomes of the tests.

- **Summarization:** Provides a concise overview of the tests executed, including pass/fail status, error messages, and other relevant metrics.

The results layer is essential for understanding the effectiveness and reliability of the tests and identifying areas for improvement.

### Browser

The Browser layer facilitates the automation of web browsers for providers that are testing web based Power Platform resources.

- **Wrapped Playwright:** Uses a wrapped version of Playwright for cross-browser automation.
- **JavaScript Wrappers:** Integration between the Provider and Test Engine is implemented through JavaScript wrappers. These wrappers are responsible for:
  - Collecting a list of items.
  - Controls and properties of items
  - Represention of controls as Power Fx records.
  - Represenation of lists as collections that can use functions like `CountRows()` or `Filter()`
  - Updating the underlying system based on Power Fx evaluations.

This layer ensures seamless interaction and manipulation of web elements during test executions.

## Extensibility Model

This sesction provides an overview of the extensibility model for Test Engine. For a deeper discussion on extensions with Test Engine refer to the [Extensions Documentation](./Extensions/README.md) that provides more detailed information.

### User Authentication

Please refer https://microsoft.github.io/PowerApps-TestEngine/context/security-testengine-authentication-changes/.

### Providers

![Providers overview](./media/providers.png)

Enables the testing of various aspects of Power Apps.

- **Canvas Apps:** Supports testing of canvas applications.
- **Model-driven Apps (MDA):** Supports testing of model-driven applications.

This extensibility ensures that the Test Engine is versatile and can cater to different types of Power Apps.

Notes:
1. Providers for Co Pilot Studio, Power Pages are being considered.
2.	The Canvas and Model Driven application providers have a dependency on installation of Playwright on the local environment or test agent to execute the web based tests.
3.	Power Automate unit testing is executed against the definition of the Cloud Flow xml in memory or from Dataverse.

#### Canvas Apps

The Canvas Apps provider is designed to facilitate interaction with canvas applications during test execution.

- **JavaScript Interface:** The provider includes a JavaScript script with the canvas application.
- **Interaction with JavaScript Object Model:** This script interacts with the JavaScript Object Model (JSOM) of the page.
- **Updating Power Fx State:** The script updates the Power Fx state with the controls and properties of the canvas app.
- **State Management:** When functions like `SetProperty` are included in a test step, the state of the page is updated via the JavaScript object model.

This mechanism ensures that the Test Engine can effectively read and manipulate the state of canvas applications, enabling comprehensive testing capabilities.

#### Model-driven Apps (MDA)
The Model-driven Apps provider is designed to enable interaction with model-driven applications during test execution.

- **Views, Details, Custom Pages:** Supports automated testing of different components within model-driven apps including views, detail pages, and custom pages.
- **Command Bars, Navigation:** Enables interaction with command bars and navigation elements of the application.
- **JavaScript Interface:** The provider includes a JavaScript script with the model-driven application.
- **Interaction with JavaScript Object Model:** This script interacts with the JavaScript Object Model (JSOM) of the page.
- **Updating Power Fx State:** The script updates the Power Fx state with the controls and properties of the model-driven app.
- **State Management:** When functions like `SetProperty` are included in a test step, the state of the page is updated via the JavaScript object model.

The Model-driven Apps provider follows the same pattern as the Canvas Apps provider, ensuring a consistent approach to state management and control interaction, thereby enabling comprehensive testing of model-driven applications.

### Power Fx

The test steps of a test file include in the ability to make use of Power Fx to define how you would like to test you solution. There are functions like [Assert()](./PowerFX/Assert.md) to vertify if the state is as expected. Other functions like [Select()](./PowerFX/Select.md) to select a item.

#### Extensibility

Facilitates the addition of custom Power Fx functions.

- **Custom Functions:** Are used to extend the functionality by adding new Power Fx functions tailored to specific testing needs or each provider.

This enhances the flexibility and power of the Test Engine, making it adaptable to varied testing requirements.

#### Power Fx Abstraction

The Power Fx Runtime creates Power Fx representation of controls on a page or elements to be tested. As properties and actions are executed then the Power Fx runtime calls the JavaScript integration layer for web based tests to update test state.

## JavaScript Integration

The Test Engine includes a set of client-side JavaScript classes that are used to abstract integration with Web Page components. These JavaScript classes provide the ability to interact with the JavaScript object model of the page. Common functionality the JavaScript classes provide:

1.	Query a list of controls
2.	Get and set properties of a control
3.	Trigger actions of controls. For example, a test case could start the selected event of a button.
After each operation is completed at the Power Fx runtime layer the JavaScript functions are called to get the state of the page and update the Power Fx representation of controls. 

## Test Types

The following test types could be considered as part of your testing strategy

| Test Type	| Description	| Considerations
|-----------|-------------|------------------|
| Power Apps - Unit Test	| Test of a deployed Power Apps and use of mocking to interact with Dataverse and Connectors |	1.	Will require mock test state to be provided as part of definition
| Power Automate – Unit Test	| Test of triggers, actions and logic of a Power Automate Cloud Flow |	1.	Mock state of triggers and actions to validate the expected outcome of cloud flows
| Power Apps Integration Test |	Execution of Power App with out mocking of data	 | 1.	Power Fx commands to control the state and order of tests
| | | 2.	Setup and Tear down functions to set known state of Data and Connected data
| Power Automate Integration Tests |	Power Autoate Cloud Flows triggered from a Power Apps	| 1.	Setup and Tear down functions to set known state of Data and Connected data


## Comparison with Playwright Testing

If you are familiar with Playwright based testing or other similar browser based testing methods the following table provides a comparison to using Test Engine

| Feature |	Playwright	| Test Engine |
|---------|-------------|-------------|
|Login | Process	Custom code to login and authenticate with login.microsoft.com. | Selectable authentication Provider for:<br/>1.	Browser using Persistent Cookies< <br/> 2.	Certificate Based Authentication
| Authoring Language	| As documented in [Supported languages \| Playwright](https://playwright.dev/docs/languages) | Yaml test files, Test Steps in Power Fx, Extensibility model with C#
| Record and Replay | [Test generator \| Playwright .NET](https://playwright.dev/dotnet/docs/codegen) | Test Studio Record and Export yaml. Record in Playwright and Execute C# Script | 
| Support Power Automate | No |	Yes using Power Automate provider
| Selector Model | Document Object Model and [Locators | Playwright](https://playwright.dev/docs/locators) JavaScript Object Model for Power Apps. Extend with Document Object Model and [Locators | Playwright](https://playwright.dev/docs/locators) for PCF and Components
| Record Video |	Yes, [Videos \| Playwright](https://playwright.dev/docs/videos#record-video) | Yes
| Navigation | GotoAsync in [Page \| Playwright .NET](https://playwright.dev/dotnet/docs/api/class-page#page-goto) | Power Fx Functions like Navigate
| Mock API	| [Mock APIs](https://playwright.dev/dotnet/docs/mock) |  Route API with Power FX actions to mock APIs
| Screenshots	| [Screenshots \| Playwright .NET](https://playwright.dev/dotnet/docs/screenshots) | Screenshot("name.jpg")
| Update controls |	Locators and [Actions \| Playwright .NET](https://playwright.dev/dotnet/docs/input) | Update PowerFx state SetProperty(USerName.Value,"Test")
