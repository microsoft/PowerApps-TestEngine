---
title: CoE Kit - Build from Source Example
---

The Power Platform Center of Excellence (CoE) starter kit is composed of various low-code solution elements within the Power Platform. Among these elements is a model-driven application designed to facilitate the setup and upgrade of the CoE Starter Kit. This sample includes Power Apps Test Engine tests, which can be utilized to automate and verify key aspects of the expected behavior of the Setup and Upgrade Wizard.

## Context

The sample [RunTests.ps1](https://github.com/microsoft/PowerApps-TestEngine/blob/main/samples/coe-kit-setup-wizard/RunTests.ps1) serves as an example of a "build from source" using the open-source licensed version of the Test Engine. The source code version may include features not yet released as part of the pac test run command in the Power Platform Command Line Interface action.

## Key Concepts

### Difference Between Build from Source and Power Platform CLI pac test run

The "build from source" approach refers to an open-source version of the Test Engine, licensed under the MIT license. This version is ideal for early adopters who want to try out the latest features and provide feedback. It allows users to access and test new functionalities before they are officially released, fostering a collaborative development environment where user feedback can directly influence the final product.

In contrast, the Power Platform CLI [pac test run](https://learn.microsoft.com/power-platform/developer/cli/reference/test) is a reviewed and compiled version distributed through the Microsoft signed components using the Power Platform Command Line Interface. This version includes features that have completed the review and build process, ensuring they are stable for a wider audience.

### Prerequisites for Source Code Version

To get started with the source code version, you will need to install several tools and have specific permissions. First, you need to install the .Net SDK 8.0, which is essential for building and running the tests. Additionally, PowerShell must be installed on your system, as it is a crucial task automation tool from Microsoft. The Power Platform Command Line Interface (CLI) is another necessary tool, allowing you to interact with Power Platform from your command line. You will also need to create a Power Platform environment, which can be done using the Power Platform Admin Center or the Power Platform Command Line. Furthermore, you must have System Administrator or System Customizer roles to make changes in your Power Platform environment. Lastly, a Git Client should be installed, and the CoE Starter Kit core module must be installed into the environment.

### Source Code Version Benefits

The source code version of the Test Engine includes the latest features, enabling users to test and use these features before they are officially released. This version allows for extensive testing capabilities, providing valuable feedback to ensure a more robust final release.

## Why Build from Source is useful for Power CAT Engineering

For the Power CAT Engineering team, contributing to the Power Apps Test Engine through a build from source approach offers significant advantages. This method allows our team to test new features early, ensuring that any issues are identified and addressed promptly. By accessing the latest functionalities before they are officially released, the team can provide valuable feedback that directly influences the development process with real world low code solutions that have been in usage for years.

The build from source approach enables the creation of new pull requests to enhance existing features. This collaborative effort not only improves the Test Engine but also simplifies and makes the testing of the CoE Starter Kit more comprehensive. The ability to test and refine features ensures that the final product is robust and reliable, meeting the high standards expected by users.

## Instructions to Build and Run Sample Tests

### Getting Started

As documented in the [CoE Setup Wizard Sample](https://github.com/microsoft/PowerApps-TestEngine/tree/main/samples/coe-kit-setup-wizard/) to begin, clone the repository using the git application and PowerShell command line. Ensure you are logged out of the pac CLI to clear any previous sessions. Then, log in to the Power Platform CLI and add the config.json file in the same folder as RunLibraryTests.ps1, replacing the values with your tenant and environment ID. Finally, run the sample tests from PowerShell.

### What to Expect

During the process, you will be prompted to log in to the Power Apps Portal. The Test Engine will execute the steps to test your install of the Setup and Install Wizard. If you choose to "Stay Signed In," future tests will use your saved credentials.
