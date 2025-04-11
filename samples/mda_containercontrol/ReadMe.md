# Overview

This Power Apps Test Engine sample demonstrates how to assert and interact with the various controls within the container, Vertical and Horizontal container controls in a model-driven application form.

## Usage

1. **Build the Test Engine Solution**  
   Ensure the Power Apps Test Engine solution is built and ready to be executed.

2. **Get the URL of the Model-Driven Application Form**  
   Acquire the URL of the specific Model-Driven Application form that you want to test.

3. **Modify the MDAContainerControls_testPlan.fx.yaml**  
   Update the YAML file to assert expected values of the shape controls.

  > [!NOTE] The controls are referenced using the [logical name](https://learn.microsoft.com/power-apps/developer/data-platform/entity-metadata#table-names).
4. **Update the Domain URL for Your Model-Driven Application**

   | URL Part | Description |
   |----------|-------------|
   | `appid=a1234567-cccc-44444-9999-a123456789123` | The unique identifier of your model-driven application. |
   | `etn=` | The name of the entity being validated. |
   | `id=26bafa27-ca7d-ee11-8179-0022482a91f4` | The unique identifier of the record being edited. |
   | `pagetype=custom` | The type of page to open. |

5. **Execute the Test for Custom Page**  
   Change the example below to the URL of your organization:

```pwsh
cd bin\Debug\PowerAppsEngine
dotnet PowerAppsTestEngine.dll -i ..\..\..\samples\mda_containercontrol\MDAContainerControls_testPlan.fx.yaml -e 00000000-0000-0000-0000-11112223333 -t 11112222-3333-4444-5555-666677778888 -u "storagestate" --provider mda -d "https://contoso.crm4.dynamics.com/main.aspx?appid=9e9c25f3-1851-ef11-bfe2-6045bd8f802c&pagetype=custom&&name=cr693_mdacontainercontrol_bca1f"