---
title: Exploring Generative AI with Power Apps Test Engine
---

In our upcoming session, we will dive into the transformative capabilities of Generative AI within the Power Apps Test Engine.

This discussion will highlight two key scenarios: using Generative AI to convert natural language into defined test steps and leveraging AI Builder prompts to create and measure the potential business value of low-code solutions against Objectives and Key Results (OKRs).

Looking at the wider Generative AI landscape we also have the Co Pilot Studio Testing capabilities can further enhance test automation by integrating with the Power Apps Test Engine. This integration allows for more comprehensive and efficient testing processes, ensuring that all aspects of the application are thoroughly tested.

## The Power of Natural Language

First, we'll explore the ability to convert natural language into test cases and test steps. Imagine describing a test scenario in plain English and having the AI translate it into actionable test steps. This makes the testing process more accessible to those without a deep technical background. Second, we'll discuss the ability to use natural language to help define and refine the value of the solution to be developed. By leveraging AI Builder prompts, users can input their business objectives and key results, and AI Builder can generate insights and recommendations on how to achieve these goals.

## Converting Natural Language to Test Steps

How do you think Generative AI can simplify the process of creating test steps from natural language inputs? What benefits do you see in making the testing process more accessible to those without a deep technical background?

## Challenges in Validating Results

With great power comes great responsibility. What challenges do you foresee in ensuring the correctness of the generated syntax? How can we validate the results to ensure they meet the required standards and do not introduce errors? Let's discuss strategies to overcome these challenges and ensure the reliability of our AI-generated test steps.

## The Value of Power FX

Power FX can play a pivotal role in this ecosystem. As a known language with a comprehensive list of functions, controls, properties, and variables it can help in using a pos processing step to validate the generated test steps. By leveraging the ground truth of this known list, we can ensure that the generated test steps are accurate and adhere to the expected syntax and functionality. What are your thoughts on this approach?

## Combining Generative AI with Deterministic Code

Finally, let's explore the synergy between Generative AI and parameterized deterministic code. We can combining the creative capabilities of AI with the precision of deterministic code create a powerful testing framework. This approach allows us to generate flexible and dynamic test scenarios while maintaining control over the parameters and ensuring consistent results. What are your ideas on leveraging this combination?

## Discussion Questions

1. How do you see the role of Generative AI evolving in the context of software testing?
2. What are some real-world scenarios where you think Generative AI could make a significant impact?
3. How can we ensure that the adoption of Generative AI in testing does not compromise the quality and reliability of the software?
4. How can the testing community collaborate to create best practices for using Generative AI in testing?
5. What are your thoughts on the future of AI-driven testing and its impact on the software development lifecycle?
6. What additional features or capabilities would you like to see in the Power Apps Test Engine to enhance its integration with Generative AI?
7. How can do you feel Generative AI simplify the process of creating test steps from natural language inputs?
8. What benefits do you see in making the testing process more accessible to those without a deep technical background?
9. What challenges do you foresee in ensuring the correctness of the generated syntax?
10. How can combining the creative capabilities of AI with the precision of deterministic code create a powerful testing framework?
11. What are your ideas on leveraging the combination of Generative AI and parameterized deterministic code?
12. What examples would you like us to expand on?

## Simplifying Test Steps Creation

Generative AI can significantly simplify the process of creating test steps from natural language inputs by interpreting plain English descriptions and translating them into actionable test steps. This makes the testing process more accessible to those without a deep technical background, allowing a broader range of users to contribute to test creation.

### Ensuring Correctness of Generated Syntax

One of the main challenges in using Generative AI is ensuring the correctness of the generated syntax. To address this, we exploring validation mechanisms that compare the generated test steps against a known list of functions, controls, properties, and variables in Power FX that are used by the Power Platform component being tested. This helps ensure that the generated steps are accurate and adhere to the expected syntax and functionality.

### Leveraging Power FX

Power FX provides a solid foundation for validation due to its comprehensive list of functions, controls, properties, and variables. By leveraging this ground truth, we can ensure that the generated test steps are accurate and meet the required standards. This approach helps maintain the reliability and consistency of the AI-generated test steps.

### Combining AI with Deterministic Code

By combining the creative capabilities of Generative AI with the precision of parameterized deterministic code creates a powerful testing framework. This approach allows us to generate flexible and dynamic test scenarios while maintaining control over the parameters and ensuring consistent results. By leveraging both AI and deterministic code, we can achieve a balance between innovation and reliability in our testing processes.

## Business Value Assessment with AI Builder

Let's now explore the *An external link was removed to protect your privacy.* which can be used to create and measure the potential business value of low-code solutions against Objectives and Key Results (OKRs). This toolkit make use of AI Builder to assist with building the value story and expeceted value of low code solutions.

### Using AI Builder Prompts

The Business Value toolkit used AI Builder prompts internally that help users create and measure the potential business value of their low-code solutions. By leveraging these prompts built into the kit, users can define their business objectives and key results, and AI Builder can generate insights and recommendations on how to achieve these goals. For example, users can input their business objectives, such as increasing customer engagement or improving operational efficiency, and AI Builder can suggest specific actions and metrics to track progress.

### Updating Examples

As we proceed with automated testing of the Business value toolkit we will update the examples of how we structure the testing of the application to apply testing to the Power App and the Generative AI Builder prompts that make up the kit to allow each component to be tested in isolation.

## Co Pilot Studio Testing Capabilities

### Enhancing Test Automation

In addition to the scenarios mentioned above, the [Power CAT Copilot Studio Kit](https://github.com/microsoft/Power-CAT-Copilot-Studio-Kit) we are evaluating further enhance test automation by integrating with the kit as an option for the Power Apps Test Engine. This integration would allow allows for more comprehensive and efficient testing processes, ensuring that all aspects of the application are thoroughly tested.

### Co Pilot + Power Apps Test Engine better together

The Power Apps Test Engine, when integrated with Power FX, can significantly enhance the capabilities of the Power CAT Copilot Studio Kit in several ways:

- **Simplifying Test Creation**: The Power Apps Test Engine can convert natural language inputs into test steps, making it easier for makers to create and configure tests without needing deep technical knowledge. This aligns with the user-friendly nature of the Power CAT Copilot Studio Kit, which already supports bulk creation and updates through Excel export/import.

- **Ensuring Accuracy**: Power FX, with its comprehensive list of functions, controls, properties, and variables, can be used to validate the generated test steps. This ensures that the test steps are accurate and adhere to the expected syntax and functionality, which is crucial when evaluating copilot responses against expected results.

- **Enriching Test Results**: By integrating with the PAC CLI and test results it provides a path to include tests in your Continuous Integration and Continuous Deployment (CI/CD) process.

- **Handling Non-Deterministic AI Responses**: For AI-generated answers that are non-deterministic, AI Builder prompts can be used to compare the generated answer with a sample answer or validation instructions. Power FX can assist in defining these validation instructions and ensuring that the comparisons are accurate and reliable.

- **Real-Time Feedback and Adjustments**: The integration allows for real-time feedback and adjustments during the testing process. This means that any issues can be quickly identified and addressed, improving the overall quality and reliability of the copilot responses.

- **Collaborative Testing Environment**: The Power Apps Test Engine fosters a collaborative testing environment where team members can work together seamlessly. By leveraging Power FX, teams can share insights, track progress, and ensure that all testing objectives are met.

## Conclusion

By integrating Generative AI with the Power Apps Test Engine and leveraging Co Pilot Studio Testing capabilities, we can create a powerful and efficient testing framework. This approach not only simplifies the testing process but also ensures the accuracy and reliability of the generated test steps. As we continue to explore and refine these technologies, we can unlock new possibilities for innovation and improvement in software testing.