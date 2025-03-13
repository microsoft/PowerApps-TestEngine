---
title: Common Topics
---

This page contains sets of related topics that enable you to quickly relate topics related to Automated Testing in the Power Platform. Whether you're just getting started or looking to deepen your expertise, you'll find valuable resources and links to help you succeed.

## Application Lifecycle Management

|Example|Description|
|-------|-----------|
| [Extending the Test Engine to Support Testing of the CoE Starter Kit Setup and Upgrade Wizard](../examples/coe-kit-extending-test-engine.md) | The CoE Starter Kit has extended the test engine to support testing of the Setup and Upgrade Wizard by breaking down tests into smaller steps, using variables and collections, and leveraging the Preview namespace to overcome limitations. This approach ensures thorough and reliable testing, leading to a more robust and user-friendly application.
| [CoE Starter Kit -  Infrastructure As Code](../examples/coe-kit-infrastructure-as-code.md) | The combination of Terraform and the CoE Starter Kit offers a robust solution for managing Power Platform environments by leveraging infrastructure as code to ensure consistency and reliability. This approach simplifies the setup and maintenance of environments, allowing us to create the foundations of an automated test matrix to test setup and upgrade process. 
| [Ring Deployment Model](./ring-deployment-model.md) | Discussion on how new features of Test Engine are deployed and made available as part of wider feature release Application Lifecycle Management model |

## Business Context

| Article | Notes |
|---------|-------|
| [The Strategic Importance of Automated Testing from a CXO Perspective](./strategic-importance-of-automated-testing-from-a-cxo-perspective.md) | Automated testing is not just a technical necessity but a strategic imperative for modern enterprises. From a CXO perspective, the implementation of automated testing frameworks can significantly enhance business outcomes, safeguard investments, and drive sustainable growth. Here’s a detailed look at why automated testing is crucial from a business standpoint, with a specific focus on the Power Apps Test Engine and how it addresses common challenges. | 
[Building a Sustainability Model](../discussion/building-a-sustainability-model.md) | In the fast-paced world of software development, the initial rush of quickly building and deploying a solution can be exhilarating. However, the true challenge lies in keeping your great idea alive and ensuring its sustainability over time. This discussion explores how to build a sustainability model that not only keeps your solution relevant but also engages the community and stakeholders in the process. 
[Embracing Testing Strategies for Low-Code Solutions: A Discussion for Enterprise Architects](../discussion/enterprise-architecture-discussion.md) | This discussion provides Enterprise Architects and related roles an overview of testing strategies for low-code solutions on the Power Platform, emphasizing the importance of automated testing, scalability, and integration with existing systems. It highlights key principles and practices to ensure that low-code applications are reliable, secure, and aligned with organizational goals.|
| [Growing to Enterprise Grade](./growing-to-enterprise-grade.md) | As solutions scale, the need for robust testing practices becomes more critical. This article explores how to transition from small-scale projects to enterprise-grade solutions, emphasizing the importance of automated testing in maintaining high standards of reliability, security, and performance. |
| [Impacts on People, Process, and Tooling](./impacts-on-people-process-and-tooling.md) | Implementing automated testing requires a shift in mindset and practices. This article discusses the cultural and organizational changes needed to adopt automated testing, the impact on development and operations processes, and the tools that can facilitate this transformation. | 
| [Low code Testing Principles](./low-code-test-design-principles.md) | These principles provide a structured framework for creating robust tests that validate the functionality and performance of low-code applications. | 
| [Why Automated Testing](./why-automated-testing.md) | Automated testing is essential for ensuring the reliability, security, and performance of applications. It provides a safety net that catches bugs early, reduces manual testing efforts, and ensures consistent quality. This is particularly important in the context of low-code Power Platform solutions, where rapid development cycles can lead to overlooked issues. |

## Generative AI

| Article | Notes |
|---------|-------|
| [Exploring Generative AI with Power Apps Test Engine](../discussion/generative-ai.md) | As part of our proposed session, we could dive into the transformative capabilities of Generative AI within the Power Apps Test Engine. This discussion could highlight key scenarios: using Generative AI to convert natural language into defined test steps and leveraging AI Builder prompts to create and measure the potential business value of low-code solutions against Objectives and Key Results 
| [Transformative Power of AI](./transformative-power-of-ai.md) | This article explores how AI can observe interactions and inform the agent to suggest happy paths, edge cases, and exception cases. This capability helps deliver a faster path to generate comprehensive test scenarios, enhancing the overall testing process. |

## Testing

| Article | Notes |
|---------|-------|
[Embracing Testing Strategies for Low-Code Solutions: A Discussion for Enterprise Architects](../discussion/enterprise-architecture-discussion.md) | This discussion provides Enterprise Architects and related roles an overview of testing strategies for low-code solutions on the Power Platform, emphasizing the importance of automated testing, scalability, and integration with existing systems. It highlights key principles and practices to ensure that low-code applications are reliable, secure, and aligned with organizational goals.|
[Implementing Effective Automated Testing Strategies in Power Platform Solutions](../discussion/implementing-effective-automated-testing-strategies-in-power-platform-solutions.md) | Automated testing is a crucial aspect of modern software development, ensuring the reliability and efficiency of solutions. This discussion explores how to implement effective automated testing strategies in Power Platform solutions, using the CoE Starter Kit Setup and Upgrade wizard as an example. We will discuss the layers of automated testing across Power Apps, Power Automate, and Dataverse. |
[Introduction to Testing Approaches](../discussion/introduction-to-testing-approaches.md) | Read discussion and give your feedback on the concepts of automated testing looking at concepts like black box and white box testing for Power Apps, including Canvas Apps, Custom Pages, and Model Driven Applications. We'll explore the importance of state management, connectors, workflows, and Dataverse state, and how to effectively test these components. |
| [Data Simulation](../discussion/data-simulation.md) | This discussion aims to explore the concepts of data simulation and mocking in the context of low code solutions, particularly focusing on Power Fx commands for Dataverse calls, connectors, and workflows. |
| [Test Authoring](../discussion/test-authoring.md) | In this discussion, we will explore the overview of authoring test cases using the Test Engine. We will delve into various aspects such as the CoE Kit Test Case Authoring, discoverability of visual elements, Test Studio, and the settings and configurations of the Test Engine. Additionally, we will discuss the role of Generative AI in enhancing the test authoring process. |

| Example | Description |
|---------|-------------|
| [Testing Variables and Collections in Power Apps with the Test Engine](../examples/custom-page-variables-and-collections.md) | The Test Engine in Power Apps offers robust capabilities for testing variables and collections, simplifying application state management. By leveraging the Set() function, developers can directly change the state of the application, making it easier to verify functionality and handle various scenarios.
| [CoE Starter Kit Power Automate Testing](../examples/coe-kit-powerautomate-testing.md) | The CoE Starter Kit Power Automate Testing feature is in the early stages of planning and aims to address the needs of users building and deploying Power Automate Cloud flows. Proper testing of these flows is crucial for maintaining accurate data collection and reporting, which supports better decision-making and governance within the organization

## Technical

### Context

| Context | Notes |
|---------|-------|
| [Test Engine Extensibility](./test-engine-extensibility.md) | Discussion on Managed Extensibility Framework (MEF) providers for Test Engine |
| [Test Engine Providers](./test-engine-providers.md) | Discussion on providers available for Test Engine to interact with different Power Platform resources |
| [Debugging Test Engine Tests](./debugging-test-engine-test.md) | Follow this guide on how to effectively debug your tests using a local build from source strategy |
| [Keeping up to date](./keeping-up-to-date.md) | Staying current with the latest features and updates in the Power Apps Test Engine allows you to leverage new capabilities and ensuring optimal performance. Here's how you can keep up to date based on the version of test engine you are using. |
| [Deep Dive: Test Engine Layers](./deep-dive-testengine-layers.md) | This article is designed to walk through the different layers of how test engine tests are implemented and understand the key interactions between the layers |
| [Testing Localized Power Apps](./testing-localized-power-app.md) | This article discusses the testing of localized Power Apps |

### Discussion Articles


| Article | Notes |
|---------|-------|----------|
[Low Code Power Platform Testing for the Code First Developer](../discussion/low-code-testing-for-code-first-developer.md) | This article is intended as a starter for discussion and contains content that is under development. It is based on experiences from teams like the Power CAT Engineering team as they apply low code testing principles to the low code Power Platform solutions they build and maintain. Ideally, this discussion serves as a great starting point to foster collaboration and gain input to help shape low code automation and engineering excellence in the wider low code Power Platform community. | 
[Playwright vs Power Apps Test Engine](../discussion/playwright-vs-test-engine.md) | When it comes to testing low-code Power Platform applications, a common question arises: why not just use Playwright to directly test a Power App rather than using the Power Apps Test Engine? This discussion aims to explore the strengths and limitations of both tools and provide insights into their best use cases. |
| [Authentication in Power Apps Test Engine](../discussion/authentication.md) | Authentication is a critical component of the test automation process. The sample script employs browser-based authentication, which offers a range of options to authenticate with Microsoft Entra. This method generates a persistent browser cookie, allowing for non-interactive execution of subsequent tests. The management of these browser cookies is governed by the guidelines provided in the Microsoft Entra documentation on session lifetime and conditional access policies. |


### Examples

| Example | Description |
|---------|-------------|
| [Extending TestEngine Power FX with C# Test Scripts](../examples/extending-testengine-powerfx-with-with-csharp-test-scripts.md) | The extensibility of TestEngine Power FX using C# test scripts allows developers to integrate web-based Playwright commands through code-first extensibility, enhancing browser automation capabilities. This approach enables the creation of custom test scripts that leverage Playwright's powerful features, improving productivity and maintainability by focusing on high-level test logic while handling common code efficiently
| [Understanding the "No Cliffs" Extensibility Model of Power Apps Test Engine](../examples/understanding-no-cliffs-extensibility-model.md) | The "no cliffs" extensibility model of Power Apps Test Engine ensures that users can extend its capabilities without encountering barriers, providing a seamless experience for both makers and developers. By leveraging Power FX and C# test scripts, this model simplifies handling complex scenarios like Power Apps consent dialogs to enhancing the efficiency and reliability of the testing process
| [Using Power Fx Namespaces in Testing](../examples/using-powerfx-namespaces-in-testing.md) | Power Fx namespaces allow developers to organize and separate different sets of functions within the language, helping to maintain clarity and avoid conflicts. By distinguishing between common features and specific actions, and separating stable features from experimental ones, namespaces ensure the stability and reliability of Power Apps.

## Security

| Context | Notes |
|---------|-------|
| [Secure First Initiative](./security-first-initiative.md) | By integrating these principles, we aim to create robust, resilient, and secure applications that can withstand evolving cyber threats. |

| Discussion | Description |
|------------|-------------|
| [Authentication in Power Apps Test Engine](../discussion/authentication.md) | Authentication is a critical component of the test automation process. The sample script employs browser-based authentication, which offers a range of options to authenticate with Microsoft Entra. This method generates a persistent browser cookie, allowing for non-interactive execution of subsequent tests. The management of these browser cookies is governed by the guidelines provided in the Microsoft Entra documentation on session lifetime and conditional access policies. |

| Example | Description |
|---------|-------------|
| [Testing Security](../examples/testing-security.md) | This article provides an example of how we can test browser-based authentication using multiple personas using Multi-Factor Authentication (MFA) with persistent cookie state and Power Apps security for Power Apps. 