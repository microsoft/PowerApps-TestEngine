---
title: 08 - Simulated Connector
---

## Introduction

In this module, we will explore how to use simulated connectors in your Power Apps tests. Simulated connectors allow you to mock network requests, providing predefined responses for testing purposes. This is particularly useful for testing scenarios where you want to control the data returned by external services.

## What is Mocking?

Mocking is a technique used in software testing to simulate the behavior of real objects. By using mocks, you can create controlled environments where you can test specific parts of your application in isolation. This helps ensure that your tests are reliable and repeatable, as they are not dependent on external factors.

### Benefits of Mocking

- **Test Isolation**: Mocking allows you to isolate the component you are testing from the rest of the system. This means you can focus on testing the functionality of that component without worrying about the behavior of other parts of the system.
- **Control Over Test Scenarios**: By using mocks, you can create specific scenarios, including both expected and edge cases. This helps you verify that your application behaves correctly under various conditions.
- **Consistency**: Mocking ensures that your tests produce consistent results, as the mocked responses are predefined and do not change.

## Example: Simulating Connection

Lets look at an example of simulating a connection. The first set fo checks validate the expected Weather from the WeatherService. After using the SimulateConnector function The alternative Location and Condition values are used.

> NOTES:
> 1. If the value does not match the test will return "One or more errors occurred. (Exception has been thrown by the target of an invocation.)"
> 2. Reload the page to reset the sample to the default state

{{% powerfx-interactive %}}
Assert(WeatherService.GetCurrentWeather("Seattle, WA").Location="Seattle, WA");
Assert(WeatherService.GetCurrentWeather("Seattle, WA").Condition="Sunny");

Preview.SimulateConnector({Name:"WeatherService", Then: {Location: "Other", Condition: "Cold"}});

Assert(WeatherService.GetCurrentWeather("Seattle, WA").Location="Other");
Assert(WeatherService.GetCurrentWeather("Seattle, WA").Condition="Cold");
{{% /powerfx-interactive %}}

Want to explore more concepts examples checkout the [Learning Playground](/PowerApps-TestEngine/learning/playground?title=assert-simulated-connector) to explore related testing concepts.

## Running the Test

To run the test, follow these steps:

1. **Navigate to the Connector Sample Directory**:
    - Open a terminal and navigate to the `\samples\connector\` directory in the Power Apps Test Engine repository.

2. **Run the Test Script**:
    - Execute the `RunTests.ps1` script to start the test:

    ```pwsh
    pwsh -File RunTests.ps1
    ```

## Understanding the Test Plan

The test plan for the connector sample includes a section that simulates the MSN Weather connector. This is defined in the `onTestSuiteStart` block of the `testPlan.fx.yaml` file. By using this value we can ensure that the expected simulation is applied before the test cases is loaded.

### Test Plan Configuration

Here's the relevant part of the `testPlan.fx.yaml` file:

```yaml
# yaml-embedded-languages: powerfx
testSuite:
  testSuiteName: Connector App
  testSuiteDescription: Verifies that you can mock network requests
  persona: User1
  appLogicalName: new_connectorapp_da583
  onTestSuiteStart: |
    = Preview.SimulateConnector({name: "msnweather", then: {responses: { daily: { day: { summary: "You are seeing the mock response" }}}}})
  testCases:
    - testCaseName: Fill in a city name and do the search
      testSteps: |
        = Screenshot("connectorapp_loaded.png");
          SetProperty(TextInput1.Text, "Atlanta");
          Select(Button1);
          Assert(Label4.Text = "You are seeing the mock response", "Validate the output is from the mock");
          Screenshot("connectorapp_end.png");

testSettings:
  locale: "en-US"
  recordVideo: true
  browserConfigurations:
    - browser: Chromium

environmentVariables:
  users:
    - personaName: User1
      emailKey: user1Email
      passwordKey: user1Password
```

### Explanation

- onTestSuiteStart: This block uses the Preview.SimulateConnector function to mock the MSN Weather connector. It specifies that any request to the msnweather connector should return a predefined response with the summary "You are seeing the mock response".
- testCases: This section defines the test steps. It includes filling in a city name, performing a search, and asserting that the label displays the mock response.
Steps in Detail
Simulate Connector:

The onTestSuiteStart block sets up the simulated connector response.

```powerfx
= Preview.SimulateConnector({name: "msnweather", then: {responses: { daily: { day: { summary: "You are seeing the mock response" }}}}})
```

Test Steps:

The test steps include taking a screenshot, setting the text input to "Atlanta", clicking the search button, and asserting the label text.

```powerfx
= Screenshot("connectorapp_loaded.png");
  SetProperty(TextInput1.Text, "Atlanta");
  Select(Button1);
  Assert(Label4.Text = "You are seeing the mock response", "Validate the output is from the mock");
  Screenshot("connectorapp_end.png");
```

## Summary

Using simulated connectors in your Power Apps tests allows you to control the responses from external services, making it easier to test various scenarios. By following the steps outlined in this section, you can set up and run tests that use simulated connectors, ensuring your application behaves as expected under different conditions.
