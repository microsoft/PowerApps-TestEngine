// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerApps.TestEngine.Config
{
    /// <summary>
    /// Defines settings for tests in the test plan.
    /// </summary>
    public class TestSettings
    {
        /// <summary>
        /// Gets or sets the list of browser configurations to be tested.
        /// At least one browser must be specified.
        /// </summary>
        public List<BrowserConfiguration>? BrowserConfigurations { get; set; }

        /// <summary>
        /// Gets or sets whether to record a video.
        /// Default is false.
        /// If set to true, a video recording of the test is captured.
        /// </summary>
        public bool RecordVideo { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to show the Power FX command overlay.
        /// Default is false.
        /// If set to true, an overlay with the currently running Power FX command is placed on the screen.
        /// </summary>
        public bool EnablePowerFxOverlay { get; set; } = false;

        /// <summary>
        /// Timeout in milliseconds. Default is 30000 (30s)
        /// </summary>
        public int Timeout { get; set; } = 30000;

        /// <summary>
        /// Number of workers. Default is 10.
        /// </summary>
        public int Workers { get; set; } = 10;
    }
}
