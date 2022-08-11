// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerApps.TestEngine.Config
{
    /// <summary>
    /// Defines the test plan to be run
    /// </summary>
    public class TestPlanDefinition
    {
        /// <summary>
        /// Gets or sets the definition of the test suite.
        /// </summary>
        public TestSuiteDefinition TestSuite { get; set; }

        /// <summary>
        /// Gets or sets the test settings.
        /// </summary>
        public TestSettings TestSettings { get; set; }

        /// <summary>
        /// Gets or sets the environment variables.
        /// </summary>
        public EnvironmentVariables EnvironmentVariables { get; set; }
    }
}
