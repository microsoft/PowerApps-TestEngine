# Overview

This guidanace provides an overview low code compared to high code testing and end to end context and example. 

## Low code vs High Code Testing

Low code testing involves creating tests using minimal code, often leveraging tools and frameworks that simplify the process. This approach leverages building blocks like Power Fx functions, which is designed to be user-friendly and accessible to those with limited coding experience. 

In contrast, high code testing requires extensive programming knowledge and the use of complex languages like C#, Java, JavaScript or Python. High code solutions also provide a wide array of choices on how the application can be created. These choices make the process of testing these solutions more specialized and require more technical expertise. By utilizing low code testing, developers and makers can quickly and efficiently create tests without the need for deep technical expertise, making it an ideal choice for many Power Platform solutions. The Test Engine approach provides a extension module to allow high code elements to be included as part of a test case.
Specific testing of low code Power Platform assets allows for a common language and approach that covers interactive applications, Dataverse, and Automation Cloud Flows. This unified testing strategy ensures consistency and reliability across different components of the Power Platform.

By using low code tools, testers can easily create and execute tests, ensuring that all aspects of the solution are thoroughly validated. This approach not only simplifies the testing process but also enhances collaboration between different teams, leading to more robust and reliable applications.

The planned Generative AI features of the Power Platform Test Engine enhance this process by being able to suggest and augment the tests created to accelerate and simplify the process of testing low code solutions as low code solutions have known structure and state to aid the process of test case creation.

## End-to-End Lifecycle Context

The following lifecycle outlines possible elements of the quality process. Depending on the criticality, expected lifetime, and risk profile of the application, different elements may need to be scaled back or emphasized. This spectrum of choices allows for flexibility based on the specific needs of the solution.

### Design and Planning

At the onset, the design phase should incorporate reliability testing strategies as outlined in the Power Platform reliability testing recommendations. This involves defining tests that will validate the resiliency, availability, and recovery capabilities of the solution. The spectrum of choices could range from manual testing to multiple levels of automated testing.

### Development and Local Execution

During the development phase, developers will execute tests locally to ensure code changes meet predefined criteria before integration. This local execution loop involves:
-	Writing and running unit tests to validate individual components.
-	Executing integration tests to ensure different components work together seamlessly.
-	Using static code analysis tools to detect and fix code quality issues early.

### Review and Approval

Prior to deployment, code and test execution results must undergo a thorough review process. This includes:
-	Peer reviews of code changes to catch potential issues.
-	A review board or automated system to approve code based on test results.
This process ensures that only thoroughly vetted and tested code proceeds to deployment.

### Approvals Options

Depending on the deployment process test results can be as part of Power Platform pipelines or traditional code first CI/CD pipelines. The results of executed tests can be used as quality gates to determine if the solution should be deployed to the target Power Platform environment.
