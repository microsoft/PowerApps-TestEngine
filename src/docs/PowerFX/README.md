# Power Fx

There are several specifically defined functions for the test framework.

- [Assert](./Assert.md)
- [Screenshot](./Screenshot.md)
- [Select](./Select.md)
- [SetProperty](./SetProperty.md)
- [Wait](./Wait.md)

- [Preview.Pause](./Pause.md)
- [Preview.PlaywrightAction](./PlaywrightAction.md)
- [Preview.PlaywrightScript](./PlaywrightAction.md)

## Preview Functions

The following functions will be enabled in Debug build and when Preview is enabled as a Namespace

- [Preview.SimulateConnector](./SimulateConnector.md)
- [Preview.SimulateDataverse](./SimulateDataverse.md)

## Naming

When creating additional functions using [modules](../modules.md) for Power Fx in the Test Engine, it's important to follow naming standards to ensure consistency and readability.

Here are some guidelines for naming your functions in Power Fx:

1. Use descriptive names that accurately describe the function's purpose.
2. Use PascalCase, where the first letter of each word is capitalized, for function names.
3. Avoid using abbreviations or acronyms unless they are widely understood and commonly used.
4. Use verbs at the beginning of function names to indicate what the function does.
5. Use nouns at the end of function names to indicate what the function operates on.

By following these naming standards, your Power Fx code will be easier to read and maintain, and other developers will be able to understand your code more easily.

### Use Namespaces

Namespaces should be used for Power Fx functions in the Power Apps Test Engine for several reasons. First, using namespaces ensures that there is no clash with built-in functions, which can cause confusion and errors. By using namespaces, Power Fx functions can be organized and grouped together in a clear and concise manner.

Additionally, namespaces make it clear that these Power Fx functions belong to the Test Engine, and are not part of the larger Power Apps ecosystem. This helps to avoid confusion and ensures that the functions are used appropriately within the context of the Test Engine.

Overall, using namespaces for Power Fx functions in the Power Apps Test Engine is a best practice that helps to ensure clarity, organization, and consistency in the testing process.

### Using Descriptive Names

Using descriptive names is important because it makes it easier for others (and yourself) to understand what the function or service does. A good name should be concise but also convey the function's or service's purpose. For example, instead of naming a function "Calculate," you could name it "CalculateTotalCost" to make it clear what the function is doing.

Anti-pattern: Using vague or ambiguous names that don't clearly convey the function's or service's purpose. For example, naming a function "ProcessData" doesn't give any indication of what the function actually does.

### Use Pascal Case

Using PascalCase is a convention that is widely used in many programming languages, and it helps make your code more readable and consistent. By capitalizing the first letter of each word, you can more easily distinguish between different words in the name. For example, instead of naming a function "calculateTotalcost," you could name it "CalculateTotalCost" to make it easier to read.

Anti-pattern: Using inconsistent capitalization or other naming conventions. For example, naming a function "Calculate_total_cost" or "calculateTotalCost" would be inconsistent and harder to read.

### Avoid Abbreviations

Using abbreviations or acronyms can make it harder for others to understand what your code does, especially if they are not familiar with the specific terminology you are using. If you do use abbreviations or acronyms, make sure they are widely understood and commonly used in your field. For example, using "GUI" for "Graphical User Interface" is a widely understood and commonly used abbreviation.

Anti-pattern: Using obscure or uncommon abbreviations or acronyms that are not widely understood. For example, using "NLP" for "Natural Language Processing" might be confusing for someone who is not familiar with that term.

### Use Verbs at start

Using verbs at the beginning of function names helps to make it clear what the function does. This is especially important when you have many functions that operate on similar data or perform similar tasks. For example, instead of naming a function simply "TotalCost," you could name it "CalculateTotalCost" to indicate that the function calculates the total cost.

Good practice: Using clear and concise verbs that accurately describe what the function does. For example, using verbs like "calculate," "validate," "filter," or "sort" can help make the function's purpose clear.

Anti-pattern: Using vague or misleading verbs that don't accurately describe what the function does. For example, using a verb like "execute" or "perform" doesn't give any indication of what the function actually does.

### Use Nouns at end

Using nouns at the end of function or service names helps to make it clear what the function or service operates on. For example, instead of naming a function "CalculateTotal" you could name it "CalculateTotalCost" to indicate that the function operates on cost data.

Good practice: Using clear and concise nouns that accurately describe what the function or service operates on. For example, using nouns like "cost," "customer," "order," or "invoice" can help make the function or service's purpose clear.

Anti-pattern: Using vague or misleading nouns that don't accurately describe what the function or service operates on. For example, using a noun like "data" or "information" doesn't give any indication of what the function or service actually operates on.
