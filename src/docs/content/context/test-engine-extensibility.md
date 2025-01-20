---
title: Test Engine Extensibility
---

Let's dive into the world of **Test Engine Extensibility**. Whether you're new to this concept or looking to deepen your understanding, this guide will walk you through the essentials and show you how to leverage the power of extensibility in your testing workflows.

![Overview diagram of test engine extensibility](/PowerApps-TestEngine/context/media/test-engine-extensibility.png)

## Common Features of the Test Engine

First things first, let's talk about what the test engine brings to the table:

- **Test Suite and Test Case Execution**: At its core, the test engine allows you to define and execute test suites and test cases. This means you can organize your tests into logical groups and run them systematically to ensure your application behaves as expected.

- **Saving Test Results and Log Files**: After running your tests, the engine can save the results in various formats, including the popular `.trx` files. These files store detailed information about each test run, making it easier to review outcomes and diagnose issues. Additionally, log files and video recordings capture the execution details, providing a comprehensive audit trail.

## Managed Extensibility Framework (MEF)

Now, let's introduce a powerful concept: the **[Managed Extensibility Framework (MEF)](https://learn.microsoft.com/en-us/dotnet/framework/mef/)**. Don't worry if you haven't heard of it before, MEF is a framework that allows you to extend your applications in a modular way. Think of it as a way to plug in new functionalities without altering the core system.

MEF enables three main classes of extensibility in the test engine:

1. **Authentication**: This class allows you to integrate various authentication mechanisms into your testing framework. Whether you're dealing with Multi Factor Authentication, or custom authentication protocols, MEF makes it easy to extend and adapt your test engine tests to handle different authentication scenarios.

2. **Providers**: Providers are modules that support specific platforms or technologies. For example, you can have providers for **Power Apps Canvas apps** and **Model Driven apps**. These providers enable the test engine to interact with and test applications built on these platforms seamlessly.

3. **Power Fx Functions**: Power Fx is a powerful formula language used in the Power Platform. With MEF, you can extend the test engine to support custom Power Fx functions, allowing you to create more sophisticated and tailored test scenarios. The CoE Kit has examples of [Extending Power Fx functions](../examples/extending-testengine-powerfx-with-with-csharp-test-scripts.md) using C#.

## Ring Deployment Model for Extensibility

Just like software features, the providers and modules in the test engine follow a **[Ring Deployment Model](./ring-deployment-model.md)**. This model ensures that new extensions are introduced in a controlled manner:

- **Experimental Namespace**: New features and extensions are first introduced in the Experimental namespace. This allows early adopters to test and provide feedback on these new capabilities before they are widely released.

- **TestEngine Namespace**: After initial testing and refinement, features are promoted to the TestEngine namespace. This stage is designed for wider usage, ensuring that the extensions are robust and ready for broader deployment.

By following this model, the test engine ensures that new functionalities are stable and reliable before they reach the general user base.

##s Wrapping Up

In summary, the Test Engine Extensibility framework empowers you to enhance your testing capabilities through modular extensions. Whether you're integrating new authentication methods, adding support for different application types, or extending the testing language with custom functions, MEF provides a flexible and powerful way to do so. And with the Ring Deployment Model, you can be confident that these extensions are thoroughly vetted and ready for prime time.