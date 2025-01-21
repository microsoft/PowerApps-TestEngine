---
title: Layers of the Power Apps Test Engine
---

This article is designed to walk through the different layers of how test engine tests are implemented and understand the key interactions between the layers.

![Overview diagram of layers of test engine across recording, test case definition, test steps](/PowerApps-TestEngine/context/media/test-engine-layers.png)

Let have a look at the key layers that make up the Test Engine testing process. 

> Note: This deep dive does not include the common login process which is covered in more depth in [Deep Dive - Test Engine Storage State Security](./security-testengine-storage-state-deep-dive.md)

| Layer    | Description | Example |
|----------|-------------|---------|
| [Recorder](/PowerApps-TestEngine/discussion/test-authoring) | Provides the ability to author tests by starting a test session and observe use of Application, Connectors, Dataverse to generate recorded test steps using User Clicks/ Entry, Network Calls, Voice Annotations. | Select(Button1); Experimental.SimulateDataverse({Action:"", Entity: "", Then: Table({}) }) |
| Test Case Definition | A YAML file with a suite of tests, test steps as Power Fx, Test Settings and the user persona that the test case is related to. | Test Case Definition: `user1@contoso.onmicrosoft.com`, Browser Type (Chromium, Microsoft Edge...), onTestSuiteBegin
| Test Steps | Power Fx statements that are used to define and abstract interaction with Power Platform components. They provide common Power Fx languages features and an extensibility model to allow interaction with different providers and to define Power Fx functions encapsulate test features. | SetProperty(Dropdown1.SelectedItems, Table(First(GalleryItems.Items)));<br/> Assert(CountRows(GalleryItems.Items)=1);
| [Test Providers](/PowerApps-TestEngine/context/test-engine-providers) | Responsible for providing data to and executing the actions expressed as Power Fx steps. Examples include Power Apps Canvas as Model Driven Applications and Power Automate. | Validate Test Ready, Execute JavaScript, Map Power Fx Data Request to Results from Test Interface
| Test Interfaces | Responsible for executing low level test integration with Web Pages for Power Apps and in memory unit test execution. Common examples include use of Browsers and Playwright to use JavaScript Object Model and the Document Object Model as a fallback. |  IBrowserContext.GotoAsync(); <br/> IBrowserContext.RouteAsync();  <br/> IPage.EvalAsync();


## Examples

[Overview diagram of examples at each layers of test engine across recording, test case definition, test steps](./media/test-engine-layer-examples.png)

Lets have a look at example of each of these layers

| Layers | Description | Output |
|--------|-------------|--------|
| Recorder | The recorder will take three forms of input Click and Data entry actions by the interactive user, observing calls to Dataverse and Power Platform Connectors and Voice Annotations. | This stage will generate a Yaml test file, Power Fx test steps that represent the actions taken and audio files |
| Test Case Definition | Includes a set of related test cases with environment variables for the user persona to run the test as. Other settings will include selecting the browser type and timeouts. Power Fx statements will be used define steps to execute before a test starts and steps to for each test step | The results of test case definition are used to generate test logs and test results |
| Test Steps | The Power Fx statements to interact with the Power App. It can be used to wait for expected input to appear, update the state of the application and verify the state of controls, variables and collections | Test pass / fail outcome |
| Test Providers | Provider the interface between the Test Engine and Power Fx Test steps and the Power Platform Application being tested. | Test Logs summarizing the actions taken and results
| Test Interfaces | Provide the lowest level integration. Key focus where possible is using JavaScript Object Model integration to provide abstraction from rendering changes in the HTML. There are options to take advantage of CSS selectors to interact with the Document Object model id required | Data from Controls, Variables, Collections. Ability to interact with controls and set new values | 

## Deep Dive

[Deep dive diagram of layers of test engine across Providers, Playwright and Power Fx](./media/test-engine-layers-deep-dive.png)

### Providers, Playwright and Power Fx

Diving a little deeper and example of the end to end process starts with Provider, for example, the Model Driven Application provider. It provides implementation of validating if the page has been loaded and is ready to execute the test. It starts the process of interacting with Playwright to query the available controls and properties. 

These results are inserted into Power Fx as **ControlRecordValue** objects. These records contain the type data of the control and properties to interact with. They also have a reference to the test provider so that they can use the provider to connect with the active Playwright page and retrieve the current value.

An important implementation detail to remember with **ControlRecordValue** is that the actual data is not stored in C#. The provider makes calls to runtime components of the test interface in Playwright to evaluate the current value.

### Test Steps and Providers

#### ControlRecordValue

As individual Power Fx statements are evaluated, the **ControlRecordValue** class makes calls to the Provider which then uses Playwright to make use of JavaScript calls to the active page to obtain the current value of control properties and update the state of the page. The key element in this process generally is the JavaScript Object Model as the primary source of truth.

Given the page knows the controls and state, the Provider can use the JavaScript integration layer to request state and update state.

#### ItemPath

A key concept in this layer is **ItemPath** objects. Item Paths allow a generic selector model where it specifies the name, property that can be requested or updated for the controls. The index property is used to interact with collections to interact with data and controls inside grids and collections.

An **ItemPath** can have a parentControl. Parent controls are important concepts when referencing properties of collections where the Record that the property belongs to.

### Playwright Integration

The lowest level is the JavaScript interface layer. The purpose of this layer is to provide the provider the list of controls, properties, variables, and collections that are present in the implementation.

Key functions allow the properties and number of items to be queried to evaluate Power Fx test steps. Functions like SetProperty make use of JavaScript interface functions like setValue for Controls.
