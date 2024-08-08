## Overview

This document provides an architectural overview of the Power Apps Test Engine. The engine consists of multiple layers, each with specific responsibilities that contribute to delivering a comprehensive and automated testing framework for Power Apps. The layers include the Test Engine, Test Definition, Test Results, and Browser, with an extensibility model available for User Authentication, Providers, and Power Fx.

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

### User Authentication
Provides the capability to authenticate the test session.

- **Extensible Authentication Mechanisms:** Allows for different methods of authentication to be plugged in, ensuring secure test sessions.

### Providers
Enables the testing of various aspects of Power Apps.

- **Canvas Apps:** Supports testing of canvas applications.
- **Model-driven Apps (MDA):** Supports testing of model-driven applications.

This extensibility ensures that the Test Engine is versatile and can cater to different types of Power Apps.

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
