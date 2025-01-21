---
title: CoE Kit Learning Path Example
---

## Introduction

The Center of Excellence (CoE) Kit has become a vital tool for a large number of users globally. As the user base continues to grow, the CoE Kit faces significant architectural changes. Ensuring the quality of releases amidst these changes is crucial. The existing application has been developed over four years, incorporating Power Apps, Power Automate, Dataverse, and Power BI as seen in our [Executing CoE Starter Kit Test Automation](../examples/coe-kit-automate-test-sample.md) example

![Overview diagram CoE Kit Learning Path (Business, Architect and Technical) - Test Approach with Simulation, Power Apps, Power Automate and AI Builder](/PowerApps-TestEngine/examples/media/coe-kit-learning-path.png)
## Challenges

### End-to-End Testing

The initial approach to testing wanted to rush to creating end-to-end (e2e) tests. However, testing a large system end-to-end, especially one composed of multiple technologies, can be overwhelming. This complexity necessitates a strategy to simplify the process and make it more manageable.

![Diagram showing the different layers of CoE Starter Kit Test across - Authentication, Power Apps, Power Automate, AI Builder, Dataverse, Power Platform and Microsoft Fabric" caption="Components to test in CoE Starter Kit](/PowerApps-TestEngine/examples/media/coe-kit-testing-layers.png)

### Learning Curve

To address the learning curve and complexity, we are looking for strategies to ramp up our team to learning mastery to take advantage of low-code tests. Building a learning path and providing samples are key components of this strategy. The samples need to be simpler but should also offer transferable skills.

## Learning Path and Samples

Based on user feedback, we created three tracks of learning:

1. **Business Users**
2. **Architects**
3. **Makers / Engineers**

### Business and Architecture Tracks

For the [business path](/PowerApps-TestEngine/learning/business-path) and [architecture learning](/PowerApps-TestEngine/learning/architecture) tracks, we have developed assessment questions. These assessments will help us grow and evolve our learning guidance and architecture guidance.

#### Business Users

The learning path for business users includes interactive assessment questions designed to guide them through the process of applying low-code automated testing. These questions help users understand the value of automated testing, identify key areas where it can be applied, and develop a basic understanding of the tools and techniques involved. By answering these questions, business users can gain insights into how automated testing can improve efficiency, reduce errors, and support innovation within their organizations.

#### Architects

Architects follow a learning path that includes interactive assessment questions focused on the architectural aspects of automated testing. These questions cover topics such as integrating automated tests with security protocols, aligning testing strategies with the Power Well Architected framework, and ensuring seamless integration with CI/CD pipelines. By engaging with these assessments, architects can develop a comprehensive understanding of how to design and implement robust, secure, and scalable automated testing solutions.

### Technical Track

For the [technical track](../learning/), we are building on samples that introduce key concepts and help grow confidence among developers. These samples are designed to be straightforward yet effective in teaching transferable skills. Additionally, we emphasize the power of simulation to allow components to be tested in isolation. This approach enables verification of the application's behavior without needing to manage all the state of Dataverse to run tests.

#### Importance of Simulation

Simulation allows components to be tested in isolation, eliminating the need to test the entire system end-to-end. For example, the Weather Sample provides a simple scenario where the MSN Weather and Dataverse connectors are simulated using test data. This allows the functionality of the Power App to be independently tested. These skills can then be applied to more complex scenarios, such as the CoE Kit Setup and Upgrade Wizard.

#### Skills Transfer

The concepts learned from Power Apps testing can be transferred to testing Power Automate cloud flows. By using simulated triggers and actions, developers can validate the control logic of the flow without needing to manage the entire state of Dataverse. This approach is particularly useful for verifying changes in conditional logic as cloud flows are refactored.

Additionally, these skills can be extended to non-deterministic testing, such as AI Builder tests within the Business Value Toolkit. This ensures that AI models and their integrations are functioning correctly under various conditions.

#### Record and Replay

The Record and Replay feature simplifies the generation of initial test cases. By interacting with the application and observing clicks and network calls, this feature generates sample test data and Power Fx test steps. This process does not require deep technical knowledge of Power Fx or testing, making it accessible for developers at all skill levels.

## Addressing the Learning Curve

To effectively address the learning curve, we have leveraged several resources and strategies:

- **Automated Test Samples**: We are contributing a set of comprehensive examples of automated tests for the Power Apps Test Engine, demonstrating how to execute tests across various components such as Power Apps, Power Automate, and Dataverse. These examples help users understand the broader context and the specific steps needed to perform unit and integration tests. Learn more

- **Learning Modules**: Our learning modules cover essential topics such as setting up automated tests, recording tests, and understanding Power Fx commands. These modules are designed to help users quickly get up to speed with automated testing in the Power Platform. Explore the learning modules

- **Architecture Guidance**: We provide guidance on the architecture of automated tests within the Power Well Architected (PoWA) framework. This includes best practices for designing reliable, secure, and efficient tests that integrate seamlessly into CI/CD pipelines. Additional guidance for [Enterprise Architects](../roles-and-responsibilities/enterprise-architects.md), [Solution Architects](../roles-and-responsibilities/solution-architects.md) and [Security Architected](../roles-and-responsibilities/security-architects.md) also helped

- **Business Path**: For business stakeholders, we offer a dedicated learning path that emphasizes the importance of collaboration, innovation, and efficiency in automated testing. This path helps business users understand the value of automated testing and how it can drive enterprise-grade solutions. Learn about the [business path](../learning/business-path) learning module.

## Transferring Knowledge

The foundational concepts and testing strategies learned through the CoE Kit are highly transferable to building automated tests for Power Automate. Given that the CoE Kit includes over 100 cloud flows, it is essential to verify changes in conditional logic as these flows are refactored. 

By applying the principles of automated testing, developers can ensure that each component of a cloud flow functions correctly in isolation before integrating it into the larger system. This approach minimizes the complexity of managing the entire state of Dataverse during tests and allows for more focused and efficient verification of specific behaviors.

Key steps in transferring knowledge to Power Automate testing include:

- **Understanding Conditional Logic**: Learn how to test various conditional branches within cloud flows to ensure they execute as expected under different scenarios.
- **Isolated Component Testing**: Utilize simulation techniques to test individual components of a flow in isolation, verifying their behavior without the need for a fully populated Dataverse environment.
- **Refactoring and Regression Testing**: Implement automated regression tests to quickly identify any issues introduced during the refactoring of cloud flows, ensuring that existing functionality remains intact.

By leveraging these strategies, developers can build robust automated tests for Power Automate, enhancing the reliability and maintainability of cloud flows within the CoE Kit.

## Conclusion

By focusing on these three tracks and providing targeted learning paths and samples, we aim to simplify the testing process and empower our different user roles to effectively apply testing to the the CoE Kit. This approach will ensure that we maintain the quality of our releases while adapting to architectural changes.

