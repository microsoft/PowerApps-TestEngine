---
title: 05 - Recording Your First Test
---

While the sample include `testPlan.fx.yaml` as an example, you can also create this file by simply interacting with the application. The Power Apps Test Engine provides a record mode that allows you to generate test plans based on your interactions with the app.

> NOTE: The recording feature is currently under development and improvement. This feature will continue to change and improve in future updates.

## Steps to Record Your First Test

Follow these steps to record your first test:

1. **Open the `RunTests.ps1` Script**:
    - Navigate to the `\samples\buttonclicker\` directory.
    - Open the `RunTests.ps1` script in a text editor like Visual Studio Code.

2. **Modify the Script to Enable Recording**:
    - Locate the line that runs the `PowerAppsTestEngine.dll` using the `dotnet` command.
    - Add `-r True` to the end of this line to enable recording mode. The modified line should look something like this:

    ```pwsh
    dotnet PowerAppsTestEngine.dll -u "storagestate" --provider "canvas" -a "none" -i "$currentDirectory\testPlan.fx.yaml" -t $tenantId -e $environmentId -r True
    ```

3. **Run the Script**:
    - Save the changes to `RunTests.ps1`.
    - Open a PowerShell terminal and navigate to the `\samples\buttonclicker\` directory.
    - Run the script by executing:

    ```pwsh
    pwsh -File RunTests.ps1
    ```

4. **Interact with the Application**:
    - The Power App should open, and the Playwright inspector window will appear.
    - Interact with the application by clicking buttons and performing the actions you want to record.

5. **Finish Recording**:
    - When you are ready to finish recording, select the **Continue** button in the Playwright inspector.
    - This will complete the recording session.

6. **Locate the Recorded Test File**:
    - Once the test is complete, navigate to the `TestOutput` folder.
    - You should find a `recorded.te.yaml` file in this folder. This file contains the recorded test steps based on your interactions with the application.

By following these steps, you can easily create a test plan by interacting with your Power App, making it simpler to automate your testing process.

## Summary

In this section, you learned how to record your first test using the Power Apps Test Engine. By enabling recording mode in the `RunTests.ps1` script and interacting with your Power App, you can generate a test plan based on your actions. This makes it easier to automate your testing process and ensure your application works as expected.
