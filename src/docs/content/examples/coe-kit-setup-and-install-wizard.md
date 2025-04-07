---
title: CoE Kit Setup and Install Wizard
---

## Use Case Example

Once example we have been working on recently is Automated testing of the [Setup and Wizard](https://learn.microsoft.com/power-platform/guidance/coe/setup-core-components) as we considered Automated Test for this application we had to look the following.

1. How do we collaborate with the Test Engine team to improve the Test Engine?
2. How do we setup and install the solution?
3. Could we automate the creation of the environment, install of dependencies, setup of connections?
4. How we extend testing a Model Driven Application with custom pages?
5. How do we handle the user consent dialog?
6. How build tests to interact with a complicated multi stage setup process?
7. How can we create integration tests calling Power Automate Cloud Flows?
8. How can we validating the successful setup and state with Dataverse?
9. How can we scale what we are learning to improve guidance?

## Early Adopter and Build from Source

We collaborated closely with the Test Engine team by contributing code to the repository to use a [build from source](./coe-kit-build-from-source-run-tests.md) strategy. This included adding new code for authentication, providers for the Model Driven app, and expanding Power Fx functions to make testing easier. By building the open source from code, we applied a build process to integrate tests as part of our deployment process. This ensured that our tests were consistently run and validated during each deployment.

## Automating Setup

We leveraged the Power Platform Terraform provider to automate the setup process. This included creating environments, importing the Creator Kit, establishing connections, and installing the CoE Kit release files. By automating these steps, we ensured a consistent and repeatable setup process, which is crucial for automated verification. This approach not only saved time but also reduced the potential for human error, making our testing process more reliable and gives us the ability to deploy and test globally in multiple regions.

## Custom Pages Dealing with Global Variables

Handling global variables was crucial for managing the steps of the install wizard. By effectively controlling the state of the application, we were able to test different parts of the process more easily. This approach ensured that our tests were robust and could handle various scenarios, ultimately improving the reliability of our automated testing framework.

### Setup and Upgrade Wizard Example

The Setup and Upgrade Wizard of the CoE Starter Kit provides a good example of working with global variables. 

The state of the page which the current state of the Subway Navigation control and the wizard steps is controlled by a common variable. This application is made up of multiple screens that allows the user to verify that the different elements of the CoE Starter Kit have been setup and is in a working state.

![Center of Excellence Setup and Upgrade Wizard screenshot](https://learn.microsoft.com/en-us/power-platform/guidance/coe/media/coesetupwizard.png)

### Power FX Test Scenario

Lets look at how test engine helps with testing this scenario. This example demonstrates that by being able to interact with the Power FX variables it greatly simplifies the testing of this application as a key global variable controls the state of the application. 

By being able to get and set the variable rather than having to infer where in the process the app is the variable can easily be asserted to verify the state of the app.

![Center of Excellence integration test example diagram that shows the Power FX and interaction with the Power App and Playwright](/PowerApps-TestEngine/examples/media/coe-kit-global-variable-example.png)

Key parts of this example are:

1. The ability for the test to conditionally waits until the optional Consent Dialog is completed.

2. The Power FX provider for Model Driven Application custom page has been updated the Power Fx state with the initial state.

3. The ```Set(configStep, 1)``` function call updates the step of the upgrade process to the Confirm prerequisites step. By updating this variable the Power Apps Provider updates the Model Driven Application custom page state.

4. Using ```Assert()``` and ```CountRows()``` functions to check that the FluentDetailsList with requirements shown in the right panel has items. This could be extended to filter functions to ensure specific status of teh required components.

5. Selection of the Next button using ```Select(btnNext)``` to move to the second step.

6. Validating that the global variable has now been updated to the second step of the Setup and Upgrade wizard.

Our [CoE Kit - Extending Test Engine](./coe-kit-extending-test-engine.md) discusses specific Power Fx steps and configuration that was applied to test the Model Driven App custom page of the Setup and Upgrade Wizard.

## Handling Conditional Dialogs

The custom page of the application introduced testing complexities such as the consent dialog that appears the first time the application runs. To handle this, we created a Power Fx function that conditionally checked for the consent dialog and approved it if it appeared. This approach simplified the process and ensured that our tests could run smoothly without manual intervention.

We does this look like? In one of our test steps we take advantage of the Power Fx extensions for test engine to add a command similar to the following.

```powerfx
Preview.ConsentDialog(Table({Text: "Center of Excellence Setup Wizard"}));
```

This function waits to see if the Consent Dialog Appears, if it does it accepts the connections. If the text "Center of Excellence Setup Wizard" appears then it continues with the remaining test steps.

## PowerFX functions to extend testing

Power Fx and the extensibility model made it easy to hide complex operations like the conditional consent dialog behind simple Power Fx functions. This abstraction allowed us to focus on writing tests without worrying about the underlying complexities, making our testing process more efficient and maintainable.

The [ConsentDialogFunction](https://github.com/microsoft/PowerApps-TestEngine/blob/main/src/testengine.module.mda/ConsentDialogFunction.cs) provides an example of the C# extension to Test Engine that allows the complexity of the conditional consent dialog to be handled. This is a good example of combining the extensibility model of code first C# extensions with low code PowerFX to simplify the test case.

## Scaling Guidance

In addition to focusing on the technical code elements of core functionality and tests, we are contributing to the wider low-code [testing guidance documentation](https://github.com/microsoft/PowerApps-TestEngine/tree/main/docs) based on our experiences so far. 

By sharing our learnings, we aim to help the broader community implement effective automated testing strategies, ensuring that others can benefit from our insights and improve their own testing processes.

## Further Reading

- [CoE Starter Kit - Infrastructure As Code](./coe-kit-infrastructure-as-code.md) discussing how we are using Terraform to setup and install the kit using automated install.
- [Extending the Test Engine to Support Testing of the CoE Starter Kit Setup and Upgrade Wizard](./coe-kit-extending-test-engine.md) discussing the Power Fx based test steps we are using to automate and verify an install.
- [CoE Starter Kit Power Automate Testing](./coe-kit-powerautomate-testing.md) discussing how we are applying different levels of tests to automate the Power Automate Cloud flows that are part of the CoE Kit.
- [CoE Kit - Power Platform Low Code ALM Release and Continuous Deployment Process](./coe-kit-test-automation-alm.md) discussing investments we are making in the Application Lifecycle Management process we are using to build, deploy and support the CoE Starter Kit.
- [Executing CoE Starter Kit Test Automation](./coe-kit-automate-test-sample.md) discussing how to execute the CoE Kit automated tests using a build from source strategy.