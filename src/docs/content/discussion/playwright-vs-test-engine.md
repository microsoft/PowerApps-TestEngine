---
title: Playwright vs Power Apps Test Engine
---

## Introduction

When it comes to testing low-code Power Platform applications, a common question arises: why not just use Playwright to directly test a Power App rather than using the Power Apps Test Engine? This discussion aims to explore the strengths and limitations of both tools and provide insights into their best use cases.

## Playwright: A Tool for Code-First Developers

### General Purpose Web-Based Tests

[Playwright](https://playwright.dev/) is a fantastic tool for code-first developers who are creating general-purpose web-based tests. It offers a robust framework for automating browser interactions and is highly versatile for various web applications. However, when it comes to testing Power Platform applications, there are several considerations to keep in mind.

### Correct Level of Integration

One of the key decisions when using Playwright is determining the correct level of integration for your tests. Should tests be written at the Document Object Model (DOM) level, or should they be more abstracted? Writing tests at the DOM level can provide fine-grained control over the elements and interactions on the page. However, this approach can also make tests more brittle and susceptible to changes in the way the Power Platform renders the page. As the Power Platform evolves, changes in the rendering engine or updates to the DOM structure can break tests that are tightly coupled to specific elements.

### Mocking Connectors, Workflows, and Dataverse

Another important aspect to consider is the need to mock connectors, calls to workflows, and Dataverse interactions. While Playwright's `page.routeAsync()` can be used to intercept calls to APIs, there is a range of ways that each request and response occurs that needs to be understood and abstracted if you want to mock network calls effectively. This can add complexity to your test setup and require a deep understanding of the underlying API interactions.

## Power Apps Test Engine: Tailored for Power Platform

### Abstraction Layer with Power Fx

The use of [Power Fx](https://learn.microsoft.com/power-platform/power-fx/overview) in the Power Apps Test Engine allows for an abstraction layer for common operations. This abstraction can create more resilient tests by leveraging knowledge of the JavaScript Object Model and Document Object Model used by different Power App implementations. This ensures a consistent experience across Canvas Apps, Model-Driven Apps, including Entity List, Entity Details, and custom pages. Additionally, Power Fx can abstract the details of simulating connection information, which simplifies test isolation and reduces the complexity of setting up and maintaining tests.

### Leveraging Existing Skills

Using Power Fx opens the option for team members to utilize their existing Power Fx skills to build tests. This makes it easier for non-developers to contribute to the testing process.

### Generative AI and Deterministic Code

Power Fx, as a constrained language with a defined list of functions, provides an easier abstraction layer for generative AI. This allows the combination of generative AI and deterministic code that can be parameterized, enhancing the flexibility and power of the testing framework.

### Observing the State of the Application

A critical question to address is how to observe the state of the application during testing. Power Apps often make use of variables and collections, and without the ability to interact with these values directly, it can increase the complexity of the test. Testers may need to infer the state of the application from the rendered state, which can be challenging and less reliable. The Power Apps Test Engine, with its integration of Power Fx, can help mitigate this issue by providing more direct access to the application's state and simplifying the process of state observation.

## Combining the Best of Both Worlds

### Built on Playwright

The Power Fx Test Engine is built on top of Playwright, allowing the best combination of abstraction and "no cliffs" extensibility if needed. This means that while the Test Engine provides a high level of abstraction, developers can still access the underlying Playwright programming model when required.

### No Cliffs Extensibility Model

The Power Fx model of the Test Engine provides a "[no cliffs extensibility](../examples/understanding-no-cliffs-extensibility-model.md)" model where C# scripts or Power Fx functions can be optionally created with access to the Playwright programming model. Additionally, the Managed Extensibility Framework (MEF) model allows code-first Playwright skills to be encapsulated and exposed as reusable Power Fx functions. This ensures that advanced customization and extension are possible without losing the benefits of the abstraction layer.

## Beyond Web-Based Testing

### Testing Power Automate

While Power Fx is useful for web-based Power Platform resources, there are components like Power Automate that do not need a web browser experience to run tests. The [Power Automate Testing](../examples/coe-kit-powerautomate-testing.md) model of the Test Engine allows a common Power Fx and YAML-based configuration that can also be consistently extended to testing those scenarios.

## Conclusion

In summary, while Playwright is a powerful tool for code-first developers, the Power Apps Test Engine offers significant advantages for testing Power Platform applications. Its use of Power Fx provides an accessible abstraction layer, leverages existing skills, and allows for generative AI integration. Additionally, the Test Engine's foundation on Playwright ensures that developers can still access advanced features when needed. This combination makes the Power Apps Test Engine a more suitable choice for many teams working with Power Platform applications.