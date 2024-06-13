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

5. Execute the test

```pwsh
cd bin\Debug\PowerAppsEngine
dotnet PowerAppsTestEngine.dll -i ..\..\..\samples\mda\testPlan.fx.yaml -e 00000000-0000-0000-0000-000000000000 -t 00000000-0000-0000-0000-000000000000 -u browser -p mda -l Debug -d https://orgc08dc7f0.crm.dynamics.com/main.aspx?appid=a1234567-cccc-44444-9999-a1234567891234&pagetype=entityrecord&etn=test&id=26bafa27-ca7d-ee11-8179-0022482a91f4
```
