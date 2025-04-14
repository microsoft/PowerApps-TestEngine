# Overview

This Power Apps Test Engine sample demonstrates how to assert the values of controls in a model driven application form.

## Usage

1. Build the Test Engine solution

2. Get the URL of model driven application form

3. Modify the [testPlan.fx.yaml](./testPlan.fx.yaml) to assert expected values of the form controls.

  > [!NOTE] The controls are referenced using the [logical name](https://learn.microsoft.com/power-apps/developer/data-platform/entity-metadata#table-names).

4. Update the domain url for your model driven application

| Url Part | Description |
|----------|-------------|
| appid=a1234567-cccc-44444-9999-a123456789123 | The unique identifier of your model driven application |
| etn=test | The name of the entity being validated |
| id=26bafa27-ca7d-ee11-8179-0022482a91f4 | The unique identifier the record being edited |
| pagetype=entityrecord | The type of page to open.

5. Execute the test for custom page changing the example below to the url of your organization

```pwsh
cd bin\Debug\PowerAppsEngine
dotnet PowerAppsTestEngine.dll -i ..\..\..\samples\mda\testPlan.fx.yaml -e 00000000-0000-0000-0000-11112223333 -t 11112222-3333-4444-5555-666677778888 -u "storagestate" --provider mda -d "https://contoso.crm4.dynamics.com/main.aspx?appid=9e9c25f3-1851-ef11-bfe2-6045bd8f802c&pagetype=custom&name=sample_custom_cf8e6"
```
