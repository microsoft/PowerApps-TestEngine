# Debugging Samples

To debug any of the samples when using a local build from source strategy. You can to the following:

1. Open PowerApps-TestEngine folder in Visual Studio Code

2. Install Required Extensions. Ensure you have the C# extension installed in VS Code. You can find it in the Extensions view (Ctrl+Shift+X) by searching for "C#".

3. Add -w "True" to RunTests.ps1 in the sample you want to debug

4. Start Your test using PowerShell and wait for "Waiting, press enter to continue"

```pwsh
.\RunTests.ps1 
```

5. Attach the Debugger. In VS Code, go to the Debug view (Ctrl+Shift+D). 

6. Press F5 to start debugging.

7. VS Code will prompt you to select the process to attach to. Choose the process corresponding to your running .NET application. Select dotnet process related that has **PowerAppsTestEngine.dll** in the command line

8. Set Breakpoints. Open the code file from the src folder where you want to set breakpoints. Click in the left margin next to the line of code where you want to set a breakpoint, or press F9 to toggle a breakpoint on the current line.

9. Debugging Tools:

Step Over (F10): Execute the current line of code and move to the next line.
Step Into (F11): Step into the method call on the current line.
Step Out (Shift + F11): Step out of the current method and return to the calling method.
Continue (F5): Continue running the code until the next breakpoint or the end of the program.
Watch Variables:

10. Use the Watch window to monitor the values of variables. You can add variables to the Watch window by right-clicking on them in the code and selecting Add to Watch.

11. Debug Console:

Use the Debug Console to execute code or evaluate expressions during debugging.

12. Inspect Variables:

Hover over variables in your code to see their current values, or use the Variables pane in the Debug view to inspect variables.

13. Stop Debugging:

When you're done, you can stop the debugging session by pressing Shift + F5 or selecting the stop button in the Debug toolbar.

## Note

You can also debug the solution using Visual Studio by open the solution and then attach to process in step 5 using [Attach to running processes with the Visual Studio debugger](https://learn.microsoft.com/visualstudio/debugger/attach-to-running-processes-with-the-visual-studio-debugger). Once attached you can use Visual Studio Debugger to set breakpoints and inspect values.  
