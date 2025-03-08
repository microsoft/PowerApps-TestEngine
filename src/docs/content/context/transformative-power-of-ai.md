---
title: Transformative Power of AI
---

The transformative power of AI in the realm of automated testing with the Power Platform lies in its ability to observe by example as you interact with the created low-code solution. By augmenting this with your knowledge and expectations of how the solution should work, Generative AI can suggest comprehensive test suites and cases. These suggestions cover expected "happy path" tests, edge cases, and exception cases, bridging the gap in domain knowledge of testing practices that may be new to many developers.

## Observing by Example

![Example test authoring process](/PowerApps-TestEngine/discussion/media/test-authoring.png)

[Generative AI](../discussion/generative-ai.md) when [authoring tests](../discussion/test-authoring.md) with tools like Test Engine can observe your interactions with the low-code solution, learning from the way you use and test the application. This observational capability allows AI to understand the intended functionality and user flows, enabling it to generate relevant test cases that reflect real-world usage scenarios.

## Augmenting with Knowledge and Expectations

By combining its observational insights with your knowledge and expectations, Generative AI can create test cases that are not only accurate but also comprehensive. This includes:

- **Happy Path Tests**: These tests ensure that the application works as expected under normal conditions, following the most common user flows.
- **Edge Cases**: AI can identify and test scenarios that occur at the boundaries of expected input ranges, ensuring the application handles these situations gracefully.
- **Exception Cases**: AI can also generate tests for unexpected or erroneous inputs, verifying that the application can handle errors without crashing or producing incorrect results.

### Generated Tests

Using the generated test results the following can be automatically generated as an example

#### Happy Path Scenarios

##### Initial Setup and Permissions

```yaml
testCase:
  name: Verify initial setup and permissions
  description: Verify that the app requests and sets up necessary permissions correctly.
  testSteps: |
    =
    // Set Expected Simulation values for Dataverse and Connectors
    // Start the application
    // Set the initial state
    // Click on action
    // Assert the results based on variable and collection changes
```

##### Navigation and Data Entry

```yaml
testCase:
  name: Verify navigation and data entry
  description: Verify that the user can navigate through the app and enter data correctly.
  testSteps: |
    =
    // Set Expected Simulation values for Dataverse and Connectors
    // Start the application
    // Set the initial state
    // Click on action
    // Assert the results based on variable and collection changes
```

#### Edge Cases

##### Boundary Values for Data Entry

```yaml
testCase:
  name: Verify boundary values for data entry
  description: Verify that the app handles boundary values for data entry fields.
  testSteps: |
    =
    // Set Expected Simulation values for Dataverse and Connectors
    // Start the application
    // Set the initial state
    // Click on action
    // Assert the results based on variable and collection changes
```

```yaml
testCase:
  name: Verify handling of missing permissions
  description: Verify that the app handles missing permissions correctly.
  testSteps: |
    =
    // Set Expected Simulation values for Dataverse and Connectors
    // Start the application
    // Set the initial state
    // Click on action
    // Assert the results based on variable and collection changes

```

```yaml
testCase:
  name: Verify handling of data save failures
  description: Verify that the app handles data save failures correctly.
  testSteps: |
    =
    // Set Expected Simulation values for Dataverse and Connectors
    // Start the application
    // Set the initial state
    // Click on action
    // Assert the results based on variable and collection changes
```

## Bridging the Knowledge Gap

For many developers, especially those new to testing practices, understanding how to create effective test cases can be challenging. Generative AI helps bridge this gap by providing intelligent suggestions based on observed interactions and best practices. This support is invaluable for ensuring that all critical aspects of the application are tested thoroughly.

## Common Patterns: Arrange, Act, Assert

Generative AI can also incorporate common testing patterns, such as "Arrange, Act, Assert," which are widely used in traditional software development. This pattern involves:

- **Arrange**: Setting up the initial conditions and inputs for the test.
- **Act**: Executing the action or function being tested.
- **Assert**: Verifying that the outcome matches the expected result.

By using these familiar patterns, AI-generated tests can be easily understood and maintained by developers, ensuring consistency and reliability in the testing process. Using approaches like [Data simulation](../discussion/data-simulation.md) bring common patterns to allow this approch to be applied.

## The Role of Power Fx

Power Fx plays a transformational role as an extensible and verifiable language that complements Generative AI. Power Fx allows for the creation of precise and verifiable test steps, ensuring that generated tests align with the available test steps and the application's logic. This combination of Generative AI and Power Fx provides a robust framework for automated testing, enhancing both the accuracy and reliability of the tests.

## Specific Domain Knowledge of Power Fx

Generative AI's specific domain knowledge of the Power Fx language and the object model behind applications, automation, and data structures is crucial. This knowledge allows AI to generate tests that are not just focused on the outer presentation layer but also on the underlying logic and data interactions. This creates more resilient tests that are abstracted from specific point-in-time changes as the Power Platform evolves.

By understanding the Power Fx language and object model, AI can:

- **Generate Accurate Test Steps**: Ensure that test steps are aligned with the application's logic and data structures.
- **Adapt to Changes**: Create tests that are resilient to changes in the application's presentation layer, focusing on the core functionality and data interactions.
- **Enhance Reliability**: Provide a deeper level of testing that covers both the user interface and the underlying logic, ensuring comprehensive test coverage.

## Conclusion

The transformative power of AI in automated testing lies in its ability to observe, learn, and generate comprehensive test cases that cover a wide range of scenarios. By leveraging Generative AI and Power Fx, developers can ensure that their low-code solutions are thoroughly tested and meet high standards of quality and reliability. This approach not only bridges the knowledge gap in testing practices but also enhances the overall efficiency and effectiveness of the testing process.