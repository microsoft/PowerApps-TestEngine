// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerApps.TestEngine.Config
{
    /// <summary>
    /// Configuration for the user.
    /// </summary>
    public class UserConfiguration
    {
        /// <summary>
        /// Gets or sets the name of the persona.
        /// </summary>
        public string PersonaName { get; set; } = "";

        /// <summary>
        /// Gets or sets the environment variable key for fetching the user email.
        /// </summary>
        public string EmailKey { get; set; } = "";

        /// <summary>
        /// Gets or sets the environment variable key for fetching the user password.
        /// </summary>
        public string PasswordKey { get; set; } = "";
    }
}
