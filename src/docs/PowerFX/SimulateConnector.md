# Simulate Connection

The Preview.SimluateConnection function allows you to simulate requests to Power Platform connector and provide responses without actually making live requests. This is particularly useful for testing and development purposes, as it enables you to create predictable and controlled responses for various scenarios.

```powerfx
Preview.SimulateConnection({Name: "connectorname", Action: "actionname", Parameters: {}, Filter: "optionalfilter", Then: {Value: Table()}})
```

## Parameters

| Name | Description |
|------|-------------|
| Name | The name of the connector from thr url of the [connector list](https://learn.microsoft.com/connectors/connector-reference/connector-reference-powerapps-connectors). For example the name of the [Office 365 Users](https://learn.microsoft.com/en-us/connectors/office365users/) is **office365users**
| Action | The part of the url request that will match against the action
| Parameters | A Power Fx Record that will be mapped to Query parameters required to me matched
| Filter | A Power Fx expression that needs to be matched |

## Recording Sample Values

To obtain values for the `Preview.SimulateConnection()` function you can use the network trace of the Browser Developer Tools when using [Preview.Pause()](./Pause.md) where you can filter traffic by searching for **/invoke**

## Examples

1. Query user using Power 365 Users connector

```powerfx
Preview.SimulateConnection({Name: "office365users", Action: "/v1.0/me", Then: {
    displayName: "Sample User",
    "id": "c12345678-1111-2222-3333-44445555666",
    "jobTitle": null,
    "mail": "sample@contoso.onmicrosoft.com",
    "userPrincipalName": "sample@contoso.onmicrosoft.com",
    "userType": "Member"
}})
```

2. Query groups using Power 365 groups connector

```powerfx
Preview.SimulateConnection({Name: "office365groups", Filter: "name = 'allcompany@contoso.onmicrosoft.com'", Then: Table()})
```
