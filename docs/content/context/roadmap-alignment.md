---
title: Roadmap Alignment
---

## Managed Operations

As outlined at Ignite by Nirav Shah the Corporate Vice President (CVP) for Dataverse in [Introducing managed operations for Microsoft Dynamics 365 and Power Platform](https://www.microsoft.com/en-us/power-platform/blog/it-pro/introducing-managed-operations-for-microsoft-dynamics-365-and-power-platform/) we introduced the concept of Managed Operations which aligns the work for this session with wider Managed features in the Power Platform.

![Overview diagram of Power Platform Managed Operations](https://www.microsoft.com/en-us/power-platform/blog/wp-content/uploads/2024/11/IntroManagedOps-2048x1164.jpg)

### Managed Operations Definition

Power Platform managed operations, a suite of capabilities to empower organizations of all sizes to build, deploy, and operate their most critical workloads. Built with both existing and emerging AI-driven solutions in mind, these capabilities ensure stability and minimize disruption while maximizing the productivity of operations teams.

### Managed Operations and Test Engine

Nirav's post includes references to the role of Power Apps Test Engine, which is currently in preview, it executes test automations on standalone canvas apps and it simplifies testing with features like Power Fx-based YAML test authoring, DOM abstraction for control references, and connector mocking to avoid API side effects. These change management capabilities maximize the reliability of your applications, flows, and agents in production.

### Impact on the CoE Kit

The Observability and Insights is the key driver behind updates to the CoE to align with these new Microsoft Fabric enhancements. This set of changes is one of the factors driving the need for more automated testing as we include and integrate these changes.

## CoE Kit Early Adopter Feedback

As the Program Manager of the Power Platform Center of Excellence, I am excited with the product features we can take advantage of. 

Responding to these changes we are undergoing an architecture change to include a more scalable and supportable method of integrating with new product features for Inventory and Usage as part of Managed Operations.

This transformation is also supported by our Power CAT Engineering team's broader strategy to enhance our [Application Lifecycle Management](../examples/coe-kit-test-automation-alm.md) (ALM) fundamentals, improving not only our deployment process but also incorporating automated tests as part of our release process.

### Architecture Change and ALM Investments

Our new CoE Starter Kit architecture aims to provide a more scalable and supportable framework for integrating with new product features. 

This change will enable us to better manage inventory and usage, ensuring that our solutions remain robust and efficient as they grow. As part of this transformation, we are making significant investments in our ALM fundamentals.

These investments will enhance our deployment process, making it more streamlined and efficient, and will also include automated tests as an integral part of our release process.

### Collaboration with Power CAT Engineering and Power Apps Test Engine Teams

The Power CAT Engineering team is a key contributor and collaborator in this process. We are working closely with the Power Apps Test Engine team to explore how our end-to-end solution can incorporate automated tests. This collaboration aims to ensure that our solutions are thoroughly tested and meet the highest standards of quality and reliability.

## Alignment with Release Wave

Our efforts are aligned with the upcoming release wave announcements, which include several enhancements to the Power Apps Test Engine. As part of [Enhanced testing for Power Apps](https://learn.microsoft.com/power-platform/release-plan/2024wave2/power-apps/execute-tests-power-apps-securely), these improvements will support the handling of larger-scale Power Apps without regressions, resulting in more robust applications with fewer bugs.

### Feature Details

- **Expanded Support for Test Engine Tests**: The Test Engine will have enhanced capabilities for testing canvas apps, introducing model-driven app testing, and providing more robust authentication options.
- **Use of YAML and Power Fx**: The expanded support includes the use of YAML and Power Fx for defining and executing tests, allowing for more flexible and powerful testing scenarios.

## CoE Starter Kit Context

It's important to note that the [CoE Starter Kit](https://learn.microsoft.com/power-platform/guidance/coe/starter-kit) which is an open source solution is built on Power Apps, Power Automate, and Dataverse. This foundation allows us to leverage the full capabilities of the Power Platform, ensuring that our solutions are scalable, efficient, and aligned with best practices. Given this starting point it puts us in an excellent position to both use and contribute back to the wider Power Platform Automated testing story and provide you examples of how we do this.

## Ongoing Updates and Discussions

As we update and apply testing to the CoE Starter Kit, we will continue to add deeper discussions and examples to the site. This will provide valuable insights and practical guidance on how to implement automated testing effectively. Our goal is to create a comprehensive resource that supports our community in building robust, high-quality solutions on the Power Platform.

## Conclusion

Our roadmap alignment reflects our commitment to building scalable, supportable, and high-quality solutions on the Power Platform. By investing in our ALM fundamentals and collaborating with the Power CAT Engineering and Power Apps Test Engine teams, we are ensuring that our solutions are well-tested and reliable. These efforts, combined with the enhancements in the upcoming release wave, will enable us to deliver more robust applications that meet the evolving needs of our users. We look forward to sharing our progress and insights as we continue to enhance the CoE Starter Kit and our overall testing practices.
