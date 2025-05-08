# Overview

This Power Apps Test Engine sample illustrates how to assert and interact with both modern and classic gallery controls in a model-driven application form. It covers Horizontal, Vertical, and Flexible height Gallery controls.

## Usage

1. **Build the Test Engine Solution**  
   Ensure the Power Apps Test Engine solution is built and ready to be executed.

2. **Get the URL of the Model-Driven Application Form**  
   Acquire the URL of the specific model-driven application form that you want to test.

3. **Modify the verticalgallery_testPlan.fx**  
   Update the YAML file to assert expected values of the Horizontal, Vertical and Flexible height gallery controls.

   > **Note:** The controls are referenced using the [logical name](https://learn.microsoft.com/power-apps/developer/data-platform/entity-metadata#table-names).
4. **Update the Domain URL for Your Model-Driven Application**

   | URL Part                                       | Description                                             |
   | ---------------------------------------------- | ------------------------------------------------------- |
   | `appid=a1234567-cccc-44444-9999-a123456789123` | The unique identifier of your model-driven application. |
   | `etn=`                                         | The name of the entity being validated.                 |
   | `id=26bafa27-ca7d-ee11-8179-0022482a91f4`      | The unique identifier of the record being edited.       |
   | `pagetype=custom`                              | The type of page to open.                               |
   | `UserAuth=storagestate`                        | The type of user authentication to use.                 |
   | `UseStaticContext=True`                        | A flag indicating the use of a static context.          |

5. **Execute the Test for Custom Page**  
   Please replace the example URLs with your organization's URL. 

     **Command for Modern Vertical Gallery Control:**
   ```pwsh
   cd bin\Debug\PowerAppsEngine
   dotnet PowerAppsTestEngine.dll -i ..\..\..\samples\mdagallerycontrols\formtablecontroltestplan.fx.yaml -e 00000000-0000-0000-0000-11112223333 -t 11112222-3333-4444-5555-666677778888 -u browser -p mda -d "https://orgfc708206.crm.dynamics.com/main.aspx?appid=65cdcc8e-54bc-ef11-a72f-000d3a12b0cb&pagetype=custom&name=cr693_mdagallerycontrol_846d9"
   ```

   **Command for Modern Horizontal Gallery Control:**
   ```pwsh
   cd bin\Debug\PowerAppsEngine
   dotnet PowerAppsTestEngine.dll -i ..\..\..\samples\mdagallerycontrols\horizontalgallery_testPlan.fx.yaml -e 00000000-0000-0000-0000-11112223333 -t 11112222-3333-4444-5555-666677778888 -u browser -p mda -d "https://orgfc708206.crm.dynamics.com/main.aspx?appid=65cdcc8e-54bc-ef11-a72f-000d3a12b0cb&pagetype=custom&name=cr7d6_productdetails_6f49a"
   ```

   **Command for Modern Flexible Height Gallery Control:**
   ```pwsh
   cd bin\Debug\PowerAppsEngine
   dotnet PowerAppsTestEngine.dll -i ..\..\..\samples\mdagallerycontrols\flexiblegallery_testPlan.fx.yaml -e 00000000-0000-0000-0000-11112223333 -t 11112222-3333-4444-5555-666677778888 -u browser -p mda -d "https://orgfc708206.crm.dynamics.com/main.aspx?appid=65cdcc8e-54bc-ef11-a72f-000d3a12b0cb&pagetype=custom&name=cr693_flexiblegallery_f5374"
   ```

   **Command for Classic - Blank Horizontal and Flexible Height Gallery Control:**
   ```pwsh
   cd bin\Debug\PowerAppsEngine
   dotnet PowerAppsTestEngine.dll -i ..\..\..\samples\mdagallerycontrols\blankgallery_testPlan.fx.yaml -e 00000000-0000-0000-0000-11112223333 -t 11112222-3333-4444-5555-666677778888 -u browser -p mda -d "https://orgfc708206.crm.dynamics.com/main.aspx?appid=65cdcc8e-54bc-ef11-a72f-000d3a12b0cb&pagetype=custom&name=cr7d6_blankhorizontalgallery_311c9"
   ```

   **Command for Classic - Blank Vertical Gallery Control:**
   ```pwsh
   cd bin\Debug\PowerAppsEngine
   dotnet PowerAppsTestEngine.dll -i ..\..\..\samples\mdagallerycontrols\blankverticalgallery_testPlan.fx.yaml -e 00000000-0000-0000-0000-11112223333 -t 11112222-3333-4444-5555-666677778888 -u browser -p mda -d "https://orgfc708206.crm.dynamics.com/main.aspx?appid=65cdcc8e-54bc-ef11-a72f-000d3a12b0cb&pagetype=custom&name=cr7d6_blankverticalgallery_a4937"