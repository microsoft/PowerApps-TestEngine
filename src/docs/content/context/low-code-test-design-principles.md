---
title: Low-Code Testing Principles for Power Platform
---

## Summary
As we move towards [enterprise-grade](./growing-to-enterprise-grade.md) solutions created using the Power Platform, it's essential to ensure the reliability and functionality of these applications. This article explores guiding principles for low-code testing, focusing on techniques such as record and replay, isolation, human-in-the-loop, [generative AI](./transformative-power-of-ai.md) for test suggestions, state changes observation, single responsibility tests, assertion for verification, abstract complexity, trust, and simplicity. Let's quickly summarize the principles and then define what each principle is with examples. 

> Note: These principles are under active development and refinement as we look at applying them to a range of different scenarios. Check back and also join the conversation on how you see these principles should evolve.

By following these principles, you can ensure that your low-code testing approach for Power Platform is effective, efficient, and reliable. These principles provide a structured framework for creating robust tests that validate the functionality and performance of low-code applications.

## Record and Replay
Record and replay techniques can be essential for observing the system's behavior and generating simulations. This involves capturing user interactions and system responses during the recording phase and then replaying these interactions to validate the system's behavior.

### Example
Imagine you have a Power Platform app that processes customer orders. By recording the process of placing an order, you can capture all interactions and responses. During replay, you can simulate the same order placement to ensure the app behaves consistently and correctly.

Looking at a Power Automate flow you could record the results of a execution run and then look at simulating the inputs to verify the flow logic and results.

## Isolation
Isolation ensures that tests are isolated by controlling inputs and dependent connectors, including Dataverse. This can involve combining the results of record and deploy so that external dependencies are simulated, allowing the tests to focus solely on the logic and behavior of the application.

### Example
Consider a scenario where your app interacts with an external payment gateway. By mocking the payment gateway responses, you can isolate the app's logic and test how it handles different payment outcomes without relying on the actual gateway.

## Human in the Loop
Human-in-the-loop involves using the knowledge of the maker or developer to express in natural language what the system is doing and what it is intended to do. This approach leverages the expertise of the app creator to provide context and clarity.

### Example
A maker can describe the intended behavior of a customer feedback form in natural language, explaining how the form should validate inputs and store feedback. This description can guide the creation of relevant test cases.

## Generative AI for Test Suggestions
Generative AI can be leveraged to suggest relevant tests by analyzing the known structure and behavior of low-code apps together with human-in-the-loop recordings or prompts. This approach helps in covering various edge and exception cases.

### Example
Using generative AI, you can analyze a Power Platform app's components and workflows to generate test cases that cover scenarios such as invalid input handling, boundary conditions.

## State Changes Observation
Monitoring the state changes of variables and collections during test execution is crucial. This involves tracking the values of different variables and collections before and after specific actions to ensure that the system's state transitions are correct.

### Example
In a task management app, you can observe the state changes of task status variables when a task is marked as complete. This ensures that the status updates correctly and triggers any associated workflows.

## Single Responsibility Tests
Designing each test to focus on a single item or functionality ensures that tests are simple, easy to understand, and maintain. By isolating tests to specific features, it becomes easier to identify the root cause of any issues that arise during testing.

### Example
Create individual tests for functionalities such as user login, data retrieval, and form submission. Each test should focus solely on verifying one aspect of the app's behavior.

## Assertion for Verification
Assertions are conditions that must be met for the test to pass. By including assertions, you can validate that the system behaves as expected and meets the defined requirements.

### Example
In a sales tracking app, you can assert that the total sales amount is correctly calculated and displayed after adding a new sale. This ensures that the calculation logic is accurate.

## Abstract Complexity
Utilizing the extensibility model of Power Fx to define test functions that encapsulate complex logic and interactions helps in creating reusable test functions. This simplifies the test creation process and ensures that tests remain readable and maintainable, even as the complexity of the scenarios increases.

### Example
Define a Power Fx function that simulates a multi-step approval process. This function can be reused across different tests to validate various approval scenarios, reducing redundancy and maintaining consistency.

## Trust
Trust is fundamental in testing. Without trust, a failing test might be ignored or dismissed. It's crucial to treat a failing test as a representation of a failure that needs to be fixed. Trust in tests ensures that they are taken seriously and that their results are acted upon.

### Example
If a test for a login function fails, it should be investigated and fixed immediately. Trusting the test results means acknowledging that the failure indicates a real issue that could affect users.

## Simplicity
Simplicity in tests means they don't require a large amount of setup, are easy to understand, and are more likely to keep running. Simple tests are more reliable and provide confidence that the solution continues to work as expected.

### Example
A simple test for a form submission should only involve filling out the form and checking the result. It should not require complex setup or dependencies, making it easy to run and understand.
