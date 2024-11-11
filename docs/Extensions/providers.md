# Test Engine Providers

## Overview

Test Engine providers are essential components that extend the functionality of the Test Engine by enabling it to interact with various systems and services. Providers can be web-based or non-web-based, and they can include custom Power Fx functions to enhance their capabilities.

## What is a Provider?

A provider in the context of the Test Engine is a modular component that implements specific functionality to interact with external systems or services. Providers are designed to be easily extensible and reusable, allowing developers to create custom solutions tailored to their needs.

### Key Features of Providers

- **Modularity**: Providers are designed to be modular, allowing for easy integration and extension. This modularity is achieved using the Managed Extensibility Framework (MEF), which is a library for creating lightweight and extensible applications. MEF allows application developers to discover and use extensions with no configuration required, and it lets extension developers easily encapsulate code and avoid fragile hard dependencies. For more information on MEF, you can refer to the [Managed Extensibility Framework (MEF) documentation on Microsoft Learn](https://learn.microsoft.com/dotnet/framework/mef/).
- **Reusability**: Providers can be reused across different projects and scenarios.
- **Extensibility**: Providers can be extended with custom functionality, including Power Fx functions.

## Benefits of Other Modular Components

### Common Microsoft Entra Authentication Process

Using a common Microsoft Entra authentication process ensures secure and consistent authentication across different providers. This simplifies the integration process and enhances security.

### Extensible Power Fx Language

The extensible Power Fx language allows test commands to be defined as a set of Power Fx commands, abstracting complexity and making it easier to create and manage tests. This modular approach enhances reusability and maintainability. 

## Implementing a New Test Engine MEF-Based Provider

The initial focus for new providers will be on areas of the Power Platform that do not yet include implementation of a provider. For example, Microsoft Copilot Studio, Power Automate, and Power Pages. Other providers for related services could also be considered.

Before implementing a new provider review the existing providers and start a discussion on if those providers could be extended to include new scope that covers additional features and functionality.

### Extending Beyond Power Platform

While the initial focus is on Power Platform resources, the concepts of a provider can be applied to other web-based and non-web-based tests that would benefit from the value of having a low-code abstraction and testing approach. For example, it could be extended to test your custom website so that you have a common set of tools that include both your low-code and code-first resources.

### Example Project

The repository includes sample C# projects that implement [sample provider](../../src/testengine.provider.example/) and [tests](../../src/testengine.provider.example.tests/). Let look at how you could use these example projects to build a new provider.

1. Copy the folders `testengine.provider.example` and `testengine.provider.example.tests` to new names. For example for a custom website `testengine.provider.contoso.website` and `testengine.provider.contoso.website.tests`

2. Rename the csproj files in each folder to names that match the new folder

3. Edit the created csproj to copy the compiled MEF module to the PowerAppsTestEngine folder.

```xml
    <ItemGroup>
        <MySourceFiles Include="..\..\bin\$(configuration)\testengine.provider.contoso.website\testengine.provider.contoso.website.dll" />
    </ItemGroup>
```

4. Rename `ExampleProvider.cs` to a name that matches your implementation. For example `ContosoWebsiteProvider.cs`

5. Update the name of your provider.

```csharp
    /// <summary>
    /// Unique name of this provider
    /// </summary>
    public string Name { get { return "contoso.website"; } }
```

5. Update the unit test project to reference your new provider.

6. Rename `ExampleProviderTests.cs` to a name that matches your implementation. For example `ContosoWebsiteProviderTests.cs`

7. Update unit test to verify the new provider name you have created.
