#r "Microsoft.Playwright.dll"
#r "Microsoft.Extensions.Logging.dll"
using Microsoft.Playwright;
using Microsoft.Extensions.Logging;
using System.Linq;

public class PlaywrightScript {
    public static void Run(IBrowserContext context, ILogger logger) {
        var page = context.Pages.First();
        foreach ( var frame in page.Frames ) {
            if ( frame.Locator("button:has-text('Button')").CountAsync().Result > 0 ) {
                frame.ClickAsync("button:has-text('Button')").Wait();
            }
        }
    }
}