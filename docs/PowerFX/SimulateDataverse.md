# Simulate Dataverse

The Preview.SimulateDataverse function allows you to simulate responses from the Dataverse without actually querying the live data. This is particularly useful for testing and development purposes, as it enables you to create predictable and controlled responses for various scenarios.

```powerfx
Preview.SimulateDatarse({ Action: "query", Entity: "TableName", When: { Field: "value" }, Then: Table({Name: "Test"}) })
```

| Name | Description |
|------|-------------|
| Action | The dataverse action to simulate from Query, Create, Update, Delete
| Entity | The name pluralized entity name from [metadata](https://learn.microsoft.com/power-apps/developer/data-platform/webapi/web-api-service-documents)
| When | The optional query string to apply
| Filter | A Power Fx expression that needs to be matched. This will automatically be mapped to odata $filter command |
| When | The Power Fx table to return in the odata value response that will be returned to the Power App

## Recording Sample Values

To obtain values for the `Preview.SimulateDataverse()` function you can use the network trace of the Browser Developer Tools when using [Preview.Pause()](./Pause.md) where you can filter traffic by searching for **/api/data/v**

## Example

1. Simulate a Query Response with Sample Data

When the Power App queries all accounts, respond with sample data:

```powerfx
Preview.SimulateDataverse({Action:"query",Entity: "accounts", Then: Table({accountid: "a1234567-1111-2222-3333-44445555666", name: "Test"}) });
```

2. Simulate a Query with Specific Conditions

When make request with account with query name of Other return no results

```powerfx
Preview.SimulateDataverse({Action:"query",Entity: "accounts", When: {Name: "Other"}, Then: Table()});
```

## Why This Function is Useful
The `Preview.SimulateDataverse()` function is useful because it allows developers and makers to:

1. **Test and Debug**: Simulate different scenarios and responses without affecting live data, making it easier to test and debug applications.
1. **Predictable Results**: Create controlled and predictable responses, which is essential for automated testing and ensuring consistent behavior.
1. **Development Efficiency**: Speed up the development process by allowing developers to work with simulated data instead of waiting for actual data to be available.

By using this function, you can ensure that your Power Apps behave as expected in various scenarios, leading to more robust and reliable applications.