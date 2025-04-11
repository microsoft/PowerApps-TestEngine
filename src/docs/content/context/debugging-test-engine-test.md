---
title: Debugging Test Engine Tests
---

For code-first developers eager to dive deep into the mechanics of the Test Engine, understanding how to debug authentication modules, providers, or Power Fx modules is crucial. Follow this guide on how to effectively debug your tests using a local build from source strategy.

## Setting Up Your Environment

1. Open the Project: Begin by opening the PowerApps-TestEngine folder in Visual Studio Code. This is your main workspace where you have cloned the repository to.

2. Install Required Extensions: Ensure you have the C# extension installed in VS Code. This extension is essential for debugging .NET applications. You can find it in the Extensions view (Ctrl+Shift+X) by searching for "C#".

3. Verify you are on the correct branch and have pulled the latest changes.

4. Preparing for Debugging. Modify the samples Run Script: To enable debugging, add -w "True" to dotenet PowerAppsTestEngine.dll in the RunTests.ps1 script of the sample you want to debug. This modification allows the script to wait for the debugger to attach. For example for the button clicker example it will look like

```PowerShell
dotnet PowerAppsTestEngine.dll -u "storagestate" --provider "canvas" -a "none" -i "$currentDirectory\testPlan.fx.yaml" -t $tenantId -e $environmentId -w "True"
```

5. Start the Test: Run your test using PowerShell and wait for the prompt "Waiting, press enter to continue".

```pwsh
.\RunTests.ps1
```

6. Attach the Debugger: In VS Code, navigate to the Debug view (Ctrl+Shift+D).

7. Start Debugging: Press F5 to start the debugging process. VS Code will prompt you to select the process to attach to. Choose the process corresponding to your running .NET application, specifically the dotnet process related to **PowerAppsTestEngine.dll**

8. Set Breakpoints: Open the code file from the src folder where you want to set breakpoints. Click in the left margin next to the line of code where you want to set a breakpoint, or press F9 to toggle a breakpoint on the current line.

## Utilizing Debugging Tools

1. Step Through Code:

Step Over (F10): Execute the current line of code and move to the next line.
Step Into (F11): Step into the method call on the current line.
Step Out (Shift + F11): Step out of the current method and return to the calling method.
Continue (F5): Continue running the code until the next breakpoint or the end of the program.

2. Watch Variables: Use the Watch window to monitor the values of variables. You can add variables to the Watch window by right-clicking on them in the code and selecting "Add to Watch".

3. Debug Console: Use the Debug Console to execute code or evaluate expressions during debugging. This tool is invaluable for on-the-fly checks and quick evaluations.

4. Inspect Variables: Hover over variables in your code to see their current values, or use the Variables pane in the Debug view to inspect variables.

5. Stop Debugging: When you're done, you can stop the debugging session by pressing Shift + F5 or selecting the stop button in the Debug toolbar.

By following these steps, you can gain a deeper understanding of how the Test Engine works and effectively debug your authentication modules, providers, or Power Fx modules. Happy debugging!