---
title: Test Engine Providers
---

The evolution of the Test Engine within the Power Platform ecosystem has been remarkable. Initially, the Test Engine was limited to supporting only canvas applications. However, with the introduction of the new provider model, the capabilities have expanded significantly.

## The First Version: Canvas Applications Only

When the Test Engine was first introduced, it was designed to support canvas applications exclusively. Canvas applications are a type of app within the Power Platform that allows users to design and build apps by dragging and dropping elements onto a canvas, much like creating a slide in PowerPoint. This approach is highly intuitive and enables rapid development of custom applications.

## The New Provider Model

The new provider model has broadened the scope of the Test Engine. It now supports not only canvas applications but also model-driven applications. Model-driven applications are data-centric apps that are built on top of the Dataverse, which is the underlying data platform for the Power Platform. These applications are typically used for  business processes or common data administration tasks and offer a more structured approach compared to canvas apps.

### Current Providers

1. **Canvas Applications**: As before, the Test Engine continues to support and extend range of test scenarios for canvas applications.
2. **Model-Driven Applications**: Now, the Test Engine can interact with model-driven applications, including:
   - **Entity Lists**: Lists of records from a specific entity in Dataverse.
   - **Entity Records**: Individual records within an entity.
   - **Custom Pages**: Custom-designed pages within model-driven apps.
3. **Power Apps Portal**: Allow automation of `https://make.powerapps.com` to perform and verify common operations. 

## Future Providers

The development team is also considering new providers for other parts of the Power Platform, including:

- **Power Automate**: Create tests for you automation workflows to control input values, interaction with connectors, verify control logic like if and loops and verify the returned results..
- **Microsoft Copilot Studio**: Aimed at testing custom copilots. This could include:
   - Response Exact Match: Check if the copilot's response exactly matches the expected result.
   - Attachments Match: Verify if the attachments provided by the copilot match the expected attachments.
   - Topic Match: Checks if the copilot correctly identifies and responds to the intended topic.
   - Generative Answers: For AI-generated responses, use AI Builder to compare the generated answer with a sample answer or validation instruction.

## Understanding Providers

Providers play a crucial role in abstracting the interaction with the item being tested. Unlike traditional web testing approaches like Playwright, which automate interactions via the Document Object Model (DOM), providers in the Test Engine understand the underlying state of the application. This tailored approach ensures more accurate and reliable testing of Power Platform components.

## Leveraging Power Fx Knowledge

One of the significant advantages of the new provider model is the ability to reuse Power Fx knowledge. Power Fx is the formula language used across the Power Platform, including Power Apps, Power Automate, Power Pages, Dataverse, and Copilot. This means that the skills and knowledge gained from building applications and automations can be directly applied to creating test steps across multiple providers, enhancing efficiency and consistency.

## Summary

In conclusion, the new provider model for the Test Engine represents a significant advancement in testing capabilities within the Power Platform. By supporting a broader range of applications and components, and by leveraging the power of Power Fx, it offers a more comprehensive and integrated testing solution.