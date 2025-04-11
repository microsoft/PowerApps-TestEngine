
# Overview

This Power Apps Test Engine sample demonstrates how to assert and interact with the values of stream control in a model-driven application form.

## Usage

1. **Build the Test Engine Solution**  
   Ensure the Power Apps Test Engine solution is built and ready to be executed.

2. **Get the URL of the Model-Driven Application Form**  
   Acquire the URL of the specific Model-Driven Application form that you want to test.

3. **Modify the StreamControl_testPlan.fx.yaml**  
   Update the YAML file to assert expected values of the stream controls.

  > [!NOTE] The controls are referenced using the logical name.

4. **Update the Domain URL for Your Model-Driven Application**

   | URL Part | Description |
   |----------|-------------|
   | `appid=572ada03-15cc-ef11-a72e-000d3a12b0cb` | The unique identifier of your model-driven application. |
   | `etn=stream` | The name of the entity being validated. |
   | `id=26bafa27-ca7d-ee11-8179-0022482a91f4` | The unique identifier of the record being edited. |
   | `pagetype=custom` | The type of page to open. |

5. **Execute the Test for Custom Page**  
   Change the example below to the URL of your organization:

```pwsh
cd bin\Debug\PowerAppsEngine
dotnet PowerAppsTestEngine.dll -i ..\..\..\samples\mdastreamcontrols\StreamControls_testPlan.fx.yaml -e e5e36a60-11a5-e554-9d70-5f3daccad60b -t 72f988bf-86f1-41af-91ab-2d7cd011db47 -u storagestate --provider mda -d "https://orgdc37ebb8.crm.dynamics.com/main.aspx?appid=572ada03-15cc-ef11-a72e-000d3a12b0cb&pagetype=custom&name=cr693_mdastreamcontrolspage_90253"
```