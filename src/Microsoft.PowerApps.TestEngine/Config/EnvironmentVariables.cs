// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerApps.TestEngine.Config
{
    /// <summary>
    /// Defines the environment variables to be used for the tests
    /// </summary>
    public class EnvironmentVariables
    {
        /// <summary>
        /// Gets or sets the file path to a separate file with environment variables.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Gets or sets the list of user credential references.
        /// Any users defined in the test definition must be listed here. 
        /// At least one user must be present.
        /// </summary>
        public List<UserConfiguration> Users { get; set; } = new List<UserConfiguration>();
    }
}
