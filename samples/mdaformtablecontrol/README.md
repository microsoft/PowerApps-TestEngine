# Overview

This Power Apps Test Engine sample demonstrates how to assert and interact with the values of Table, Form, and Classic Forms—EditForm and ViewForm controls—in a model-driven application form.

## Usage

1. **Build the Test Engine Solution**  
   Ensure the Power Apps Test Engine solution is built and ready to be executed.

2. **Get the URL of the Model-Driven Application Form**  
   Acquire the URL of the specific model-driven application form that you want to test.

3. **Modify the testPlan.fx.yaml**  
   Update the YAML file to assert expected values of the Table, Form, and Classic Forms—EditForm and ViewForm controls.

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
   Please replace the example URLs with your organization's URL. Validate the number of records in the Employee table before executing the test plan. If needed, update the number in the test plan.

   **Command for Form and Table Controls:**
   ```pwsh
   cd bin\Debug\PowerAppsEngine
   dotnet PowerAppsTestEngine.dll -i ..\..\..\samples\mdaformtablecontrol\formtablecontroltestplan.fx.yaml -e 00000000-0000-0000-0000-11112223333 -t 11112222-3333-4444-5555-666677778888 -u browser -p mda -d "https://orgfc708206.crm.dynamics.com/main.aspx?appid=706281e3-b1ed-ef11-be20-7c1e526718b6&pagetype=custom&name=cr693_employeedetails_7f620"
   ```

   **Command for Classic Form and Table Controls:**
   ```pwsh
   cd bin\Debug\PowerAppsEngine
   dotnet PowerAppsTestEngine.dll -i ..\..\..\samples\mdaformtablecontrol\classicformtablecontroltestplan.fx.yaml -e 00000000-0000-0000-0000-11112223333 -t 11112222-3333-4444-5555-666677778888 -u browser -p mda -d "https://orgfc708206.crm.dynamics.com/main.aspx?appid=706281e3-b1ed-ef11-be20-7c1e526718b6&pagetype=custom&name=cr7d6_classicforms_e08a2"
   ```

