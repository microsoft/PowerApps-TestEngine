---
title: Test Engine Test Authoring
---

To aid you in quickly getting started we have found a record and replay approach is an effective method to get started.

Given this experience lets dive in and see some of the options we have explored as we are building low code tests for our solutions.

![Example test authoring process](/PowerApps-TestEngine/discussion/media/test-authoring.png)

## Overview

In this discussion post, we will explore the and overview of authoring test cases using the Test Engine using features that are currently in the development of open source version of Test Engine. 

We will delve into various aspects using test cases we are generating for the CoE Kit Test we we looked at authoring, how to discover visual elements, a comparison against Test Studio, and the settings and configurations of the Test Engine. Additionally, we will discuss the role of Generative AI in enhancing the test authoring process.

## End to End Process

By following these steps, the Power Apps Test Engine provides a robust framework for testing and validating Power Apps, ensuring that they perform as expected in real-world scenarios.

### Recording and Observing

#### Initial Setup
The first stage of the process involves starting the deployed Power App with the Test Engine Recorder. This tool is essential for capturing the interactions and behaviors of the app during testing.

#### User Interaction
As a user, you interact with the page as you normally would. The recorder is designed to notice the click actions and calls that the browser makes to Dataverse or Power Platform Connectors. This means that every action you take is being tracked and recorded for later analysis.

#### Optional Voice Narrations
To provide more context on what the app is doing, you can optionally record voice narrations. These narrations can explain any edge cases or expected exceptions, offering a richer understanding of the app's functionality.

### Generating Test Assets

#### Automatic Video Recording
During the session, the Test Engine automatically generates a video recording. This visual documentation is crucial for reviewing the interactions and ensuring that nothing is missed.

#### YAML Test Files and Power Fx Steps
Based on the observed actions and network calls, the Test Engine automatically generates YAML test files and Power Fx steps. These files serve as the blueprint for the test cases, detailing every interaction and response.

#### Collecting Audio Files
In addition to video and YAML files, the Test Engine also collects audio files. These recordings document the testing process, providing an additional layer of context and detail.

### Generating Tests

#### Comprehensive Test Cases
Using the generated assets, the Test Engine creates comprehensive test cases that cover various scenarios and edge cases. This ensures that the app is thoroughly tested and any potential issues are identified.

#### Using Generative AI
The Test Engine leverages generative AI to combine video, YAML, and audio data. This advanced technology generates detailed test cases that accurately reflect the observed interactions and logic. The use of generative AI ensures that the test cases are both thorough and precise, capturing the nuances of the app's behavior.

## Join the Discussion

To kick off the conversation, here are some questions:

1. What challenges have you faced while authoring test cases?
2. How do you ensure the accuracy and reliability of your test cases?
3. What features would you like to see in a test authoring tool to make the process more efficient?

Join the discussion and share your thoughts on how these features or others could improve your test authoring experience!

## Test Persona

For this discussion we are assuming that this test recording process discussed below requires an interactive install of Test Engine within a desktop environment to record and author the tests. 

As we proceed we build on these early experiences and will look at ways to wrap this experience and make it also approachable to all makers so that they take advantage of tools like copilot to assist in the test management process

## Example: CoE Kit Test Case Authoring

As we considered the [CoE Kit Setup and Install Wizard](../examples/coe-kit-setup-and-install-wizard.md) one critical element was the test authoring process. Key elements of this example include the ability to mock dependencies like Dataverse state, Connectors, and Workflows. This level of isolation was importance was we considered options for a more controlled and predictable testing environment. 

The ability to access  [variables and collections](../examples/custom-page-variables-and-collections.md) to validate the state of the page was important as much of the user experience was dependent on the state of these variables to control the test for the application.

One of the standout features for us has been the point-and-click record experience, which enables our makers and users to interact with the application by selecting buttons, labels, and other controls. This interaction helps discover control names and suggested Power Fx that can be combined to build test cases. Below is an example of the auto-generated YAML created by a recording session.

### Generated YAML

Lets take a quick sample of a snippet of YAML that was generated by the Test Recorder. and have a look at what each element means and how it helps. By understanding each component of the YAML, you can see how it contributes to a comprehensive and maintainable test suite. This approach allows for testing various scenarios, user permissions, and interactions, ensuring a robust and reliable application.

```yaml
# yaml-embedded-languages: powerfx
testSuite:
  testSuiteName: Recorded test suite
  testSuiteDescription: Summary of what the test suite
  persona: User1
  appLogicalName: NotNeeded

  testCases:
    - testCaseName: Recorded test cases
      testCaseDescription: Set of test steps recorded from browser
      testSteps: |
        =
        Preview.SimulateDataverse({Action: "Query", Entity: "workflows", Then: Table({@odata.etag: "W/"2066801"", name: "SetupWizard>CreateGroup", statecode: "1", statecode@OData.Community.Display.V1.FormattedValue: "Activated", workflowid: "a1234567-1111-2222-3333-444455556666"})});
        Select(Button3);
        Select(btnBack);
        Select(btnBack);
        Select(Button2);
        Preview.SimulateConnector({Name: "logicflows", When: {Action: "triggers/manual/run"}, Then: {tenantid: "c1111111-0000-1111-2222-33334444555"}});
        Select(lblStepName);

testSettings:
  headless: false
  locale: "en-US"
  recordVideo: true
  extensionModules:
    enable: true
  browserConfigurations:
    - browser: Chromium
```

#### Visual Studio Code Syntax Highlighting

The first line when combined with the [YAML Embedded Languages](https://marketplace.visualstudio.com/items?itemName=harrydowning.yaml-embedded-languages) Visual Studio Code make it easy to add syntax hightlighting for the generated yaml and Power Fx functions. 

#### Test Suite
The testSuite section defines the overall test suite. It includes the name and description of the test suite, the persona used for the tests, and the logical name of the app being tested.

- **testSuiteName**: This is the name of the test suite. It helps in identifying the suite among others.
- **testSuiteDescription**: A brief summary of what the test suite covers.
persona: This specifies the login persona, allowing you to show the experience using different user permissions. This is crucial for testing scenarios where user roles and permissions vary.
- **appLogicalName**: The logical name of the app being tested. In this case, it is set to "NotNeeded", but can be configured with the name of Power App Canvas app being tested.

#### Test Cases

The testCases section contains individual test cases within the suite. Each test case has a name, description, and a series of test steps.

- **testCaseName**: The name of the test case.
- **testCaseDescription**: A description of what the test case is testing.
- **testSteps**: This is where the actual test recorded steps added.

####  SimulateDataverse

The `Preview.SimulateDataverse()` function allows you to simulate interactions with Dataverse. This includes querying entities and handling different states, edge cases, and exception cases. This is important for demonstrating how the application responds in various scenarios.

#### Select()

The Select() function is used to simulate user interactions with the application, such as clicking buttons or selecting controls. This is essential for triggering functionality within the app. However you can see that some of the controls are still using default control names like Button3 can make tests harder to understand. By having tests, you can update these names to more meaningful ones, ensuring the application continues to work and making it more maintainable for new makers.

#### SimulateConnector
The `Preview.SimulateConnector()` function allows you to simulate interactions with connectors. This is useful for isolating and making tests more portable, as it enables you to test the application without relying on live data or external systems.


#### Test Settings

The testSettings section provides control over various aspects of the test environment, such as the language used, recording of video, and the browser type.

## Discovering Visual Elements

The ability to easily discover visual elements is essential for building tests. There are different levels of elements that you may want to discover covering for controls and properties, variables, and collections that all combine to control the behavior of the application experience. Understanding these elements helps in creating more accurate and comprehensive test cases.

## Test Studio

When comparing this experience we found this experience extended beyond Test Studio currently offers which is limited to canvas applications. Using the Test Recorder functionality of Test Engine we where able to take advantage of new features to improve the range features to make more maintainable tests. In addition Test Engine has been expanded to other providers supported by Test Engine, making it a versatile tool for various testing needs across many different aspects of the Power Platform.

## Settings and Configuration of Test Engine

The settings and configuration of Test Engine allow for control over the recording process. For example, you can set up allow/deny lists of actions, connectors, and controls that you want to allow recording for. Additionally, we could define Power Fx code to format tests created, such as applying formatting or masking of recorded data.

The ability to use Power Fx to assign actions to keys is another powerful feature. For example, we could define a control-click action on a label and associate Power Fx code that helps generate the correct Power Fx command that will wait for the Text Property of a Label to have a certain value.

### Test Engine Recording Configuration

The Test Engine recording configuration  allows you to specify Power Fx templates to assign in response to recording actions. For example you can assign values based on keyboard modifiers like the Alt or Control key and use Power Fx expressions to modify the recorded values. Below are YAML samples that demonstrate these concepts:

#### Dataverse Simulation 

This sample demonstrates how to set the Then variable to the first item in the Then collection using the Set and First functions.

```yaml
testCases:
  - testCaseName: SimulateDataverse
    testCaseDescription: Actions to apply to SimulateDataverse generation
    testSteps: |
      = Set(Then, First(Then));

```

#### Power Platform Connector Simulation

In this example, the If function checks if the action is "office365users" and then sets the Then variable to the redacted data using the `Preview.RedactData() function that will take the sample data and apply defined steps to remove sentive data from the response

```yaml
testCases:
  - testCaseName: SimulateConnector
    testCaseDescription: Actions to apply to SimulateConnector generation
    testSteps: |
      = If(Action="office365users", Set(Then,Preview.RedactData(Then)));
```

#### Action Templates


This sample shows how to use the If function to check if the Alt key is pressed. If it is, it waits until the text of a control matches the selected text and then selects the control using the `Preview.WaitUntil()` otherwise it uses the `Select()` function.

```yaml
testCases:
  - testCaseName: SelectAction
    testCaseDescription: The default template to apply to select action
    testSteps: |
      = If(AltKey, "Preview.WaitUntil(\"{ControlName}\.Text = {SelectedText}\" "Select(\"{ControlName}\");");
```

## No Cliffs Extensibility

We also found the [no cliffs extensibility](../examples/understanding-no-cliffs-extensibility-model.md) model of Test Engine in our Test Authoring process where can encapsulate usable or complex components inside new Power Fx functions to improve the set of actions we can use to author our tests.

## Generative AI

Using the generated Power Fx command [Generative AI](./generative-ai.md) can be combined with the recorded Power Fx commands as example components to generate test cases using natural language. This capability can significantly enhance the test authoring process, making it more intuitive and efficient.

## How Can I Try This?

This feature is currently available in a development branch of the Power Apps Test Engine. You can read the sample [README](https://github.com/microsoft/PowerApps-TestEngine/blob/grant-archibald-ms/data-record-386/samples/coe-kit-setup-wizard) and follow the steps in the Record and Replay section

If you wanted to try this against another Power Platform Model Driven APplication custom page you could alter the [GetAppId.powerfx](https://github.com/microsoft/PowerApps-TestEngine/blob/grant-archibald-ms/data-record-386/samples/coe-kit-setup-wizard/GetAppId.powerfx) with the unique name of your model driven application.

### Notes

Some notes for the CoE Kit record and replay demo.

1. To use these features you need to use [Build from Source](../examples/coe-kit-build-from-source-run-tests.md) which requires you to have .Net SDK installed.
2. This demo uses the new Model Driven Application provider to test the canvas Application
3. You will need an installation of the [CoE Starter Kit Core](https://learn.microsoft.com/power-platform/guidance/coe/setup-core-components)

## Summary

In this discussion post, we have delved into various aspects of authoring test cases using the Test Engine. We explored test cases generated for the CoE Starter Kit visual tests. We discussed how to features like the test recorder enabled easier discovery visual elements. We briefly covered recording settings and configurations of the Test Engine to fine tune how tests are created. Additionally, we touched on the role of Generative AI in enhancing the test authoring process.

We invite you to join the discussion and share your thoughts on how these features or others could improve your test authoring experience. Your feedback is invaluable as we continue to refine and enhance our approach to low-code platform testing. Let's work together to make the test authoring process more intuitive and efficient for everyone!
