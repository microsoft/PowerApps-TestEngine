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
        var page = context.Pages.First();

        if ( page.Url == "about:blank" ) {
            var nextPage = context.Pages.Skip(1).First();
            await page.CloseAsync();
            page = nextPage;
        }

        foreach ( var frame in page.Frames ) {
            if ( await frame.Locator("button:has-text('Button')").CountAsync() > 0 ) {
                await frame.ClickAsync("button:has-text('Button')");
            }
        }
    }
}