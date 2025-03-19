---
title: Understanding the "No Cliffs" Extensibility Model of Power Apps Test Engine
---

## Introduction

The "no cliffs" extensibility model of Power Apps Test Engine is designed to provide a seamless experience for both makers and developers. This model ensures that users can extend the capabilities of the Power Apps Test Engine without hitting any barriers or "cliffs." In this example, we will explore this model using the consent dialog of a Model Driven Application custom page as an example.

## What is a Consent Dialog?

A [consent dialog](https://learn.microsoft.com/power-apps/maker/canvas-apps/connections-list#connection-consent-dialog) is a prompt that appears to users, asking for their permission to access certain resources or perform specific actions. This dialog is crucial for maintaining security and ensuring that users are aware of and agree to the actions being taken on their behalf.

![Example of the connection consent dialog for an app connecting to a SharePoint site.](https://learn.microsoft.com/power-apps/maker/canvas-apps/media/connections-list/power_apps_consent_dialog.png)

## Importance of the Consent Dialog

The consent dialog is important because it helps prevent unauthorized access and actions. It ensures that users are informed and have given their explicit consent before any sensitive operations are performed. This is particularly important in scenarios where the application needs to access user data or perform actions that could impact the user's experience

## Challenges with Consent Dialogs in Testing

One of the challenges with consent dialogs is that they can make tests non-deterministic. The prompt can conditionally appear based on various factors, such as user permissions or previous interactions. This conditional appearance can complicate the testing process, as the test engine needs to handle these dialogs appropriately.

## Abstracting Complexity with Power Fx

Power FX, a low-code language used in Power Apps, helps abstract the complexity of conditionally waiting for the consent dialog and creating connections if needed. By using Power FX, makers can define the logic for handling consent dialogs in a more straightforward and intuitive manner.

### Example: Handling Consent Dialog with Power Fx

Here is an example of how Power FX can be used to handle a consent dialog in a custom page:

```powerfx
Preview.ConsentDialog(Table({Text: "Center of Excellence Setup Wizard"}))
```

In this example, the ConsentDialog function checks if the consent dialog is visible. If it is, the function responds to the dialog confirming consent for the test account. Once the dialog is handled the remaining test steps will be executed.

The Table argument allows the consent dialog wait process to exit is a label with the provided text is visible.

## Extending Power FX Test functions using C#

The following code provides a high level example of of a Power Fx [ReflectionFunction](https://learn.microsoft.com/dotnet/api/microsoft.powerfx.reflectionfunction) is defined that allows the `Preview.ConsentDialog()` to be defined and make use of C# to define the conditional logic to react to searching for the conditional dialog within a timeout interval. 

```csharp
﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace testengine.module
{
    /// <summary>
    /// This will check the custom pages of a model driven app looking for a consent dialog
    /// </summary>
    public class ConsentDialogFunction : ReflectionFunction
    {
        private readonly ITestInfraFunctions _testInfraFunctions;
        private readonly ITestState _testState;
        private readonly ILogger _logger;
    
        public ConsentDialogFunction(ITestInfraFunctions testInfraFunctions, ITestState testState, ILogger logger)
            : base(DPath.Root.Append(new DName("TestEngine")), "ConsentDialog", FormulaType.Blank, SearchType)
        {
            _testInfraFunctions = testInfraFunctions;
            _testState = testState;
            _logger = logger;
        }

        public BlankValue Execute(TableValue searchFor)
        {
            _logger.LogInformation("------------------------------\n\n" +
                "Executing ConsentDialog function.");

            ExecuteAsync(searchFor).Wait();

            return FormulaValue.NewBlank();
        }

        private async Task ExecuteAsync(TableValue searchFor)
        {
            var page = _testInfraFunctions.GetContext().Pages.Where(p => p.Url.Contains("main.aspx")).First();

            // ... IPage to handle consent dialog with timeout
        }
    }
}
```

## Conclusion

The "no cliffs" extensibility model of Power Apps Test Engine, combined with the power of Power FX, provides a robust and flexible solution for handling complex scenarios like consent dialogs. By abstracting the complexity and making it easier for makers to define their logic, this model ensures a seamless and efficient testing process.
