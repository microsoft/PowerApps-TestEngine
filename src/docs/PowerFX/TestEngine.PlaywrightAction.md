# Experimental.PlaywrightAction

` Experimental.PlaywrightAction(Locator, Action)`

` Experimental.PlaywrightAction(Url, Action)`

This use the locators or Url to apply an action to the current web page.

## Locators

When selecting actions that require a locator you can make use of [CSS Selectors](https://developer.mozilla.org/en-US/docs/Web/CSS/CSS_Selectors) or XPath queries.

Locators for web pages are based on Playwright locators. More information on locators is available from [Playwright documentation](https://playwright.dev/docs/other-locators).

Playwright also supports experimental React and vue base selectors that can be useful for selecting elements on code first extensions like PCF controls within a Power App.

## Actions

The following actions are supported

| Action   | Description                            |
|----------|----------------------------------------|
| click    | Select matching locator items          |
| exists   | Returns True or False is locator exist |
| navigate | Navigate to the url                    |
| wait     | Wait for locator items to exist        |

## Examples

` Experimental.PlaywrightAction("//button", "click")`

` Assert(Experimental.PlaywrightAction("//button", "exists") = true)`

` Experimental.PlaywrightAction("https://www.microsoft.com", "navigate")`

` Experimental.PlaywrightAction("//button", "wait")`
