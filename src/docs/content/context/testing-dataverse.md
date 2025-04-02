---
title: Testing Dataverse
---

In this article, we will discuss the ability to create integration tests with Dataverse. This functionality leverages the Power Fx capability to connect with Dataverse Entities. We will cover the following points:

- **onTestCaseStart**: Resetting the current state.
- **ForAll** and **Remove** functions.
- Connecting to a Model Driven Application or specifying `$env:DATAVERSE_URL`.
- Using Azure CLI to login.
- Functions of interest: `ForAll()`, `Remove()`, `CountRows()`, `Collect()`, `First()`.

## Resetting the Current State

The `onTestCaseStart` confuration in the [sample](../../../../samples/dataverse/testPlan.fx.yaml) provides an example of how to reset the current state before each test case. This ensures that each test starts with a clean slate. For example:

```powerfx
= ForAll(Accounts, Remove(Accounts, ThisRecord))
```

This line of code removes all records from the Accounts table.

## ForAll and Remove Functions

The `ForAll()` function is used to iterate over a table and perform an action on each record. In the example above, it is used to remove all records from the Accounts table. The `Remove()` function is used to delete a specific record from a table.

## Connecting to Dataverse

When connecting to a Model Driven Application, you can specify the `$env:DATAVERSE_URL` environment variable to connect. Additionally, you can use the Azure CLI to login to the Power Platform:

```pwsh
az login --use-device-code --allow-no-subscriptions
```

## Functions of Interest

Here are some functions that are particularly useful when working with Dataverse:

- [Collect()](https://learn.microsoft.com/power-platform/power-fx/reference/function-clear-collect-clearcollect#collect): Adds records to a table.
- [CountRows()](https://learn.microsoft.com/power-platform/power-fx/reference/function-table-counts): Returns the number of records in a table.
- [First()](https://learn.microsoft.com/power-platform/power-fx/reference/function-first-last): Returns the first record in a table.
- [ForAll()](https://learn.microsoft.com/power-platform/power-fx/reference/function-forall): Iterates over a table and performs an action on each record.
- [Patch()](https://learn.microsoft.com/power-platform/power-fx/reference/function-patch): Update an existing record.
- [Remove()](https://learn.microsoft.com/power-platform/power-fx/reference/function-remove-removeif): Deletes a specific record from a table.


## Considerations

As you create you test steps with Power Fx dataverse take into account the following: 

1. Defaults() is not supported. As a result commands like `Patch(Accounts, Defaults(Accounts), {name:"test"})` will not work. 

2. As Use `Collect()` to an alternative to Patch() with the Defaults() function

## Sample Test

You can review thew [Account entity sample](../../../../samples/dataverse/testPlan.fx.yaml) which provides an example of integration tests that insert, update and  delete [account](https://learn.microsoft.com/power-apps/developer/data-platform/reference/entities/account) entities.
