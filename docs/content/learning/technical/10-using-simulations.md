---
title: 10 - Simulations Example
---

## Learning Objectives

In this section, you will learn how to load a sample Weather application. This application is designed to call the MSN Weather and then have the user save the current weather at the location and categorize how this weather relates to them. This example highlights a number of items:
- Calling the MSN Weather connector and testing how the results of this connector are used by the application. 
- Interacting with Dataverse to look at how data queried from Dataverse is used as a collection of data for controls.
- Using Assert statements with a known state setup by the Simulation functions to verify that the application works as expected.

This section builds on concepts introduced in [Simulating Connector](./08-simulating-connector.md) and [Simulating Dataverse](./09-simulating-dataverse.md).

## Let's Get Started

### Getting Setup

To follow the steps in this module carry out the following actions.

1. Import the `WeatherSample_*.zip` solution file from the cloned repository using [Import solutions](https://learn.microsoft.com/power-apps/maker/data-platform/import-update-export-solutions).
2. Publish app customizations of the imported solution
3. Start the **Weather Snapshots** model driven application
4. Select **Allow** to consent to MSN Weather connection

## Run Tests

1. Verify that the config file in the samples\weather has been configured for your environment, tenant and user1Email

    ```json
    {
        "tenantId": "a222222-1111-2222-3333-444455556666",
        "environmentId": "12345678-1111-2222-3333-444455556666",
        "customPage": "te_snapshots_24d69",
        "appDescription": "Weather Sample",
        "user1Email": "test@contoso.onmicrosoft.com",
        "runInstall": true,
        "installPlaywright": true,
        "languages": [
            {"id":1031, "name": "de-de", "file":"testPlan.eu.fx.yaml"},
            {"id":1033, "name": "en-us", "file":"testPlan.fx.yaml"},
            {"id":1036, "name": "fr-fr", "file":"testPlan.eu.fx.yaml"}
        ]
    }
    ```

2. You have authenticated with the Power Platform CLI

    ```pwsh
    pac auth create -name Dev --environment 12345678-1111-2222-3333-444455556666
    ```

3. You have logged into the Azure CLI with account that has access to the environment that you have deployed

    ```pwsh
    az login --allow-no-subscriptions
    ```

4. Run the test

    ```pwsh
    cd samples\weather
    pwsh -File RunTests.ps1
    ```

### Exploring the sample

Lets have a closer look at the weather [testPlan.fx.yaml](https://github.com/microsoft/PowerApps-TestEngine/blob/grant-archibald-ms/enhanced-sample-495/samples/weather/testPlan.fx.yaml) for key concepts it demonstrates

1. Power Fx commands before test case starts. The yaml file includes `onTestSuiteStart` for Power Fx statements to run before the test case starts. In this case it executes Power Fx statements to simulate calls to Dataverse and MSN Weather connector.

2. Dateverse simulation. Power Fx commands that will watch for queries to dateverse and return test data so that application is an a known state. This approach also allows for edge case and exception cases to be managed without needing to manage and maintain data in dataverse.

    ```powerfx
    Experimental.SimulateDataverse({
            Action: "Query",
            Entity: "te_weathercategories",
            Then: Table(
            {
                'te_categoryname': "Test Category",
                'createdon': "2024-12-02T17:52:45Z",
                'te_weathercategoryid': "f58de6c-905d-457d-846b-3e0b2aa4c5fd"
            }
            )
        });
    ```

3. Simulating connectors. Setup watch for calls to Power Platform connectors and return test results. For this test the following Power Fx allows the application to be tested for how it uses the results.

    ```powerfx
    Experimental.SimulateConnector(
        {
        name: "msnweather", 
        then: {
            responses: { 
                weather: { 
                    current: {
                    temp: 30,
                    feels: 20,
                    cap: "Sunny"
                    }
                },
                source: { location: "Test Location" }
            },
            units: { temperature: "^F" }
            }
        }
    )
    ```

4. Verify test data. You can use assert function calls to verify that the simulated data has been applied as expected

    ```powerfx
    Assert(CountRows(WeatherCategory.Items)=1);
    Assert(CountRows(Gallery1.Items)=1);
    ```

5. Check application features. You can interact with Power Fx controls and validate how the results are used

    ```powerfx
    Select(SearchNow);
    Assert(Summary.Text="Match: Test Location, Temp: 30^F, Feels: 20^F, Sunny");
    ```

## Summary

In this section, you learned how to load and test a sample Weather application. You explored how to call the MSN Weather connector, interact with Dataverse, and use assert statements to verify the application's functionality. By following the key steps and exploring the sample, you gained insights into simulating connectors and Dataverse interactions to ensure your application works as expected.

<a href="/powerfuldev-testing/learning/11-localization" class="btn btn--primary">Localization</a>
