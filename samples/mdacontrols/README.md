# Overview

This sample demonstrates how to use the Power Apps Test Engine to validate the values of controls within a Model-Driven Application (MDA) form.

## Usage

1. **Build the Test Engine Solution**  
   Ensure the Power Apps Test Engine solution is built and ready to be executed.

2. **Obtain the URL of the MDA Form**  
   Acquire the URL of the specific Model-Driven Application form that you want to test.

3. **Select the Appropriate YAML File**  
   Choose the control's YAML file (e.g., `TextControl_testPlan.fx.yaml`) that corresponds to your application and custom page.

4. **Update the Domain URL for Your Model-Driven Application**

   | URL Part | Description |
   |----------|-------------|
   | `appid=a1234567-cccc-44444-9999-a123456789123` | Unique identifier of your Model-Driven Application. |
   | `etn=test` | Logical name of the entity being validated. |
   | `id=26bafa27-ca7d-ee11-8179-0022482a91f4` | Unique identifier of the record being edited. |
   | `pagetype=entityrecord` | Type of page to open. |

5. **Execute the test for custom page changing the example below to the url of your organization**

```pwsh
cd bin\Debug\PowerAppsEngine
dotnet PowerAppsTestEngine.dll -i ..\..\..\samples\mda\testPlan.fx.yaml -e 00000000-0000-0000-0000-11112223333 -t 11112222-3333-4444-5555-666677778888 -u browser -p mda -d "https://contoso.crm4.dynamics.com/main.aspx?appid=9e9c25f3-1851-ef11-bfe2-6045bd8f802c&pagetype=custom&name=sample_custom_cf8e6"