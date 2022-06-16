// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerApps.TestEngine.Config
{
    /// <summary>
    /// Defines one test
    /// </summary>
    public class TestDefinition
    {
        /// <summary>
        /// Gets or sets the name of the test.
        /// It will be used in reporting success and failure.
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// Gets or sets the additional information that describes what the test does.
        /// Optional.
        /// </summary>
        public string Description { get; set; } = "";

        /// <summary>
        /// Gets or sets the user that will be logged in to perform the test.
        /// This must match a personal listed in the user configuration.
        /// </summary>
        public string Persona { get; set; } = "";

        /// <summary>
        /// Gets or sets the logical name of the app to be launched.
        /// It can be obtained from the solution.
        /// For canvas apps, you need to add it to a solution to obtain it.
        /// </summary>
        public string AppLogicalName { get; set; } = "";

        /// <summary>
        /// Gets or sets the network requests to be mocked.
        /// </summary>
        public List<NetworkRequestMock>? NetworkRequestMocks { get; set; }
        /// <summary>
        /// Gets or sets the the Power FX functions describing the steps needed to perform the test.
        /// </summary>
        public string TestSteps { get; set; } = "";
    }
}
