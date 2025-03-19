---
title: Power Customer Advisory Team (Power CAT)
---

Welcome to the section dedicated to the [Power Customer Advisory Team](https://aka.ms/whoispowercat) (Power CAT)! We are part of the Microsoft Power Platform engineering team, and our mission is to ensure the success of our key enterprise customers with the Power Platform. Let's dive into who we are, what we do, and how we interact with various roles to help people be successful, especially in the context of automated testing.

## Who We Are

We are a diverse group of technical architects, community managers, program managers, developers, and content creators, located all over the world. Our shared passion for the possibilities of low-code drives us to work closely with a specific group of key enterprise customers, doing whatever it takes to ensure their success with the Power Platform.

## CoE Kit and Test Automation

The Center of Excellence (CoE) Starter Kit has evolved over 4 years. It includes a wide range of components of the Power Platform, which is a suite of applications, connectors, and a data platform (Dataverse) that provides a rapid development environment to build custom apps for your business needs.

### Components of the CoE Starter Kit

As shown in [CoE Kit Automated Test](../examples/coe-kit-automate-test-sample.md) we have many components of our end to end solution to test including:

- **Power Apps**: These are applications built using the Power Platform. There are two types of Power Apps included in the CoE Starter Kit:
  - **Canvas Apps**: These apps allow you to design and build a user interface by dragging and dropping elements onto a canvas, similar to designing a slide in PowerPoint. You can connect to various data sources and use Excel-like expressions to add logic and functionality.
  - **Model-Driven Apps**: These apps are built on top of Dataverse and are more data-centric. They automatically generate a user interface based on the data model you define, making it easier to create complex applications without extensive coding.

- **Power Automate Cloud Flows**: Power Automate is a service that helps you create automated workflows between your favorite apps and services to synchronize files, get notifications, collect data, and more. Cloud Flows are workflows that run in the cloud and can be triggered by various events, such as new environments being added to Dataverse after calling Administration connectors.

- **Dataverse**: Dataverse the scalable data service and app platform that we used to securely store and manage inventory and usage data used by the kit. It provides a common data model that allows you to integrate data from multiple sources and create a unified view of your business.

- **Creator Kit**: The Creator Kit is a set of tools and components that help you build custom user interfaces in Power Apps. It makes use of PCF (PowerApps Component Framework), which allows developers to create reusable components using standard web technologies like HTML, CSS, and JavaScript. These components can be used to enhance the functionality and appearance of your Power Apps.

### Importance of Automated Testing

Given the complexity and wide range of components included in the CoE Starter Kit, automated testing is essential to ensure the quality and reliability of the solution. The CoE Starter Kit is used by thousands of customers globally and needs to be updated frequently to incorporate new product features. Additionally, it needs to be tested across many geographies around the world to ensure consistent performance and reliability.

Automated testing helps in:
- **Ensuring Consistency**: Automated tests can be run frequently to ensure that new changes do not introduce regressions and that the solution remains stable over time.
- **Saving Time and Resources**: Automated tests can execute repetitive tasks quickly and accurately, freeing up human testers to focus on more complex and exploratory testing.
- **Improving Reliability**: Automated tests can catch bugs early in the development cycle, reducing the cost and complexity of fixing issues later in the process.

By leveraging automated testing, the CoE Starter Kit can maintain its high standards of quality and continue to meet the needs of its global user base.

### Examples

The following [examples](../examples) could be of interest based on how the CoE Starter Kit is applying test Automation.

| Example | Description |
|---------|-------------|
| [CoE Starter Kit Test Automation ALM](../examples/coe-kit-test-automation-alm.md) | The CoE Starter Kit Test Automation ALM aims to maintain quality and reduce manual effort for new releases by automating the release and continuous deployment process. This involves using tools like Power Automate Desktop, Terraform, and the Test Engine to provision environments, install dependencies, and validate setups, ensuring consistent and reliable operations.
| [CoE Starter Kit -  Infrastructure As Code](../examples/coe-kit-infrastructure-as-code.md) | The combination of Terraform and the CoE Starter Kit offers a robust solution for managing Power Platform environments by leveraging infrastructure as code to ensure consistency and reliability. This approach simplifies the setup and maintenance of environments, allowing us to create the foundations of an automated test matrix to test setup and upgrade process. 
| [CoE Starter Kit Power Automate Testing](../examples/coe-kit-powerautomate-testing.md) | The CoE Starter Kit Power Automate Testing feature is in the early stages of planning and aims to address the needs of users building and deploying Power Automate Cloud flows. Proper testing of these flows is crucial for maintaining accurate data collection and reporting, which supports better decision-making and governance within the organization

## Interaction with Roles

As we engage with various roles across the spectrum to ensure their success and provide feedback to Microsoft Engineering teams on the importance of automated testing for each role:

- **Business Stakeholders**: We help them understand the importance of automated testing in ensuring business continuity, solution performance, and mitigating security risks. Their feedback helps us improve the testing processes to better meet business needs.
- **Business Unit Leads**: We guide them in demonstrating the impact and quality of solutions created through automated testing. Their insights help us refine our tools and resources to better support their goals.
- **Managers**: We provide training and resources to ensure their teams have the required knowledge to build and test solutions effectively. Their feedback helps us enhance our training programs and materials.
- **Makers**: We support the low-code maker community by providing testing processes to create sustainable solutions. Their experiences and feedback help us improve the usability and effectiveness of our testing tools.
- **Trainers**: We collaborate with trainers to develop enterprise training programs focused on low-code testing. Their feedback helps us ensure that our training programs are comprehensive and effective.
- **Enterprise Architects**: We work with them to implement quality control and integration with other IT investments. Their feedback helps us ensure that our testing tools and processes align with enterprise standards.
- **Solution Architects**: We assist in ensuring a robust ALM process that includes automated testing. Their insights help us refine our tools and processes to better support solution architecture.
- **DevOps Engineers / Architects**: We collaborate on integrating Power Platform into the CI/CD process and managing test assets. Their feedback helps us improve the integration and management of automated testing within the CI/CD pipeline.
- **Software Engineers**: We help them augment their code-first skills with automated testing for low-code solutions. Their experiences and feedback help us enhance the capabilities of our testing tools.
- **Support Engineers**: We ensure they have the tools and knowledge to support and fix low-code solutions effectively. Their feedback helps us improve the support and maintenance aspects of our testing tools.
- **Audit**: We provide tools and guidance to maintain an end-to-end audit trail and verify compliance. Their feedback helps us ensure that our testing tools meet compliance and audit requirements.
- **Security Architects**: We assist in managing cybersecurity and data privacy through automated testing. Their feedback helps us enhance the security features of our testing tools.

## Guidance

Power CAT produces a wealth of resources to guide our customers through their digital transformation with Microsoft Power Platform. These resources are based on our experiences and expertise from working with customers and are designed to help you succeed.

### Power Well-Architected

The [Power Platform Well-Architected](https://aka.ms/powa) framework is a set of best practices, architecture guidance, and review tools. It helps you make informed decisions about the design, planning, and implementation of modern application workloads with Microsoft Power Platform.

### Power Platform Guidance

Microsoft Power Platform Guidance provides valuable information to help you create and implement the business and technology strategies necessary to succeed with the Power Platform.

### Power Platform Adoption

For customers working with Power CAT we help them jumpstart your Microsoft Power Platform adoption journey with our workbook, maturity model, and best practices. These resources can help you shape technology, business, and people strategies to drive desired business outcomes for your adoption effort.

### Power Platform Blogs

We contribute blog posts that cover a wide range of topics and areas of interest on the official Power Platform, Power Apps, Power Automate, Power Virtual Agents, and Power Pages blogs.

### Power Up Program

The [Microsoft Power Up Program](https://powerup.microsoft.com/) is designed for career switchers. It enables non-tech professionals to successfully transition into a new career path in low-code application development using Microsoft Power Platform. As we expand into Enterprise delivery of this program the role of Automated Testing is a an area that could be considered.

## Tools

Power CAT produces numerous tools to guide our customers to success with Microsoft Power Platform in their digital transformation. These tools are created based on our experiences and expertise from working with customers.

The Power CAT Engineering tools team works in close collaboration with the Power Apps Test Engine team as an early adopter and contributor to low-code automated testing.

## Videos

Explore our collection of Microsoft Power CAT tools videos, tutorials, demos, and more. These resources are designed to help you get the most out of the Power Platform and achieve your goals.



