# TestEngine.PlaywrightScript

`TestEngine.PlaywrightScript(csxFileName)`

The PlaywrightScript function provides a "no cliffs" extensibility for Test Engine providing the ability to execute CSharp Scripts (*.csx) files inside a Test Engine web provider based test that uses Playwright as web page test framework.

You can use the playwright inspector to record C# commands to build the C# script

## C# Script

This action takes advantage of [dotnet-script](https://github.com/dotnet-script/dotnet-script) and the underlying [Rosyln](https://github.com/dotnet/roslyn) compiler to allow projectless scripting of Playwright code. The Action assumes the following:

1. Any required .Net Assemblies are globally available or in the current folder and can be loaded using #r compiler directive
2. A public class named **PlaywrightScript** MUST exist
3. A method with **public static void Run(IBrowserContext context, ILogger logger)** MUST exist

## Sample Test

A sample [testPlan.fx.yaml](../../samples/playwrightscript/testPlan.fx.yaml) and [sample.csx](../../samples/playwrightscript/sample.csx) provide a demonstration of how this action can be integrated into a Test Engine test.

## Example

` TestEngine.PlaywrightScript("sample.csx")

Where sample could use template to include Playwright

```csharp
#r "Microsoft.Playwright.dll"
#r "Microsoft.Extensions.Logging.dll"
using Microsoft.Playwright;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;

public class PlaywrightScript {
    public static void Run(IBrowserContext context, ILogger logger) {
        Execute(context, logger).Wait();
    }

    public static async Task Execute(IBrowserContext context, ILogger logger) {
        // Insert your code here
    }
}
```
