// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerApps.TestEngine.Config
{
    /// <summary>
    /// Browser configuration to be tested
    /// </summary>
    public class BrowserConfiguration
    {
        /// <summary>
        /// Gets or sets the browser to be launched when testing.
        /// This should match the browsers supported by Playwright.
        /// https://playwright.dev/dotnet/docs/browsers
        /// </summary>
        public string Browser { get; set; } = "";

        /// <summary>
        /// Gets or sets the device to emulate when launching the browser. 
        /// This should match the devices supported by Playwright.
        /// https://playwright.dev/dotnet/docs/api/class-playwright#playwright-devices
        /// </summary>
        public string Device { get; set; }

        /// <summary>
        /// Gets or sets the width of the screen to use when launching the browser.
        /// If specified, screenHeight must also be specified.
        /// </summary>
        public int? ScreenWidth { get; set; }

        /// <summary>
        /// Gets or sets the height of the screen to use when launching the browser.
        /// If specified, screenWidth must also be specified.
        /// </summary>
        public int? ScreenHeight { get; set; }

        /// <summary>
        ///  Gets or sets the name of this config. This will be used to display in the test results.
        ///  If not specified, the browser will be used
        /// </summary>
        public string ConfigName { get; set; }
    }
}
