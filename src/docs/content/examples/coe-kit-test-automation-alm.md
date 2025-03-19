---
title: CoE Starter Kit Test Automation ALM
---

The goal of implementing the following stages is to maintain the quality and reduce the manual effort for new releases of the CoE Starter Kit. This article will discuss the key topics in a narrative conversation style.

## CoE Kit - Power Platform Low Code ALM Release and Continuous Deployment Process

When it comes to automating the release and continuous deployment process for the CoE Starter Kit, there are several options to consider. 

### Automation Options

You can use a physical machine, such as a laptop or PC, to run your automation scripts. Alternatively, you can trigger these scripts from cloud flows or pipelines, providing more flexibility and scalability.

![Overview diagram that shows overview of local editing and hosted options to execute tests as part of ALM process](/PowerApps-TestEngine/examples/media/coe-kit-alm-release-continuous-deployment-process.png)

#### Code First Approach

For those who prefer a code-first approach, an Azure DevOps license and a Windows Custom Build Agent can be used to manage the automation process.

#### Low Code Approach

One alternative for execution of tests is combining Power Platform Pipelines and Power Automate. Using this approach to run the automated tests Power Automate Desktop could be used using a hosted configuration using only Power Platform resources this examples needs a Power Automate license. 

For organizations with Conditional access policies for authentication, you can use a Microsoft Intune Joined Windows 11 Cloud hosted PC to execute Desktop flows. This setup requires a Power Automate Hosted Process license and an Intune license, such as Intune Plan 1 or Microsoft 365 Business Premium.

## CoE Kit – Target ALM Architecture

Let's dive into the target Application lifecycle for the CoE Kit that has been selected for the CoE Kit

![Target ALM lifecycle for CoE Kit from Environments, Azure DevOps Repository, Power Platform Pipelines, Approvals and GitHub Release](/PowerApps-TestEngine/examples/media/coe-kit-target-alm.png)

By following this structured approach, we ensure that our development, testing, and deployment processes are efficient, reliable, and scalable.

### Authoring
In the authoring phase, we deploy Power Apps, Power Automate Cloud Flows, and Dataverse components to a development environment. This is where the initial creation and testing of these components take place. We use Visual Studio Code, a powerful code editor, along with the Power Apps Test Engine to record, edit, and commit test cases. These test cases are then stored in an Azure DevOps repository, which acts as a central hub for our code and test management.

### Environment Strategy
Our environment strategy is crucial for maintaining a structured and efficient development process. We use multiple development environments, each linked to Azure DevOps Git Repositories using the feature to [Natively connect your environments to source control](https://learn.microsoft.com/power-platform/release-plan/2024wave2/power-apps/connect-environment-source-control). This linkage allows us to manage our code versions and collaborate effectively. By connecting environments to source control, we ensure that changes are tracked, and we can easily revert to previous versions if needed.

### Deployment
Deployment is the process of moving our developed components from one environment to another, such as from development to testing or production. We utilize [pipelines in Power Platform](https://learn.microsoft.com/power-platform/alm/pipelines) to automate this process. Pipelines help streamline deployments, reduce manual errors, and ensure consistency across environments. They also allow us to define specific steps and conditions for each deployment stage, making the process more reliable and efficient.

### Gated Build
A gated build is a quality control mechanism that ensures only approved changes are deployed. We use Power Automate Cloud flows to send an approval email containing links to the changed files, test results, and a summary of the changes. The approver reviews this information and decides whether to accept or reject the deployment. This step helps catch potential issues early and maintains the integrity of our production environment.

### Build Process
The build process involves compiling and testing our solution to ensure it works as expected. We use an Azure DevOps Build agent to execute tests against the deployed solution in each environment. The test results are then uploaded to the Azure DevOps build results, providing a clear overview of the solution's performance and any issues that need to be addressed.

### Release
Once a production release is ready, we have an extension that packages and publishes both managed and unmanaged solutions. Managed solutions are locked and cannot be modified, while unmanaged solutions are open for customization. The source code for the release is then merged into our external GitHub repository, making it available for the community to use and contribute to. This practice promotes transparency, collaboration, and continuous improvement.
