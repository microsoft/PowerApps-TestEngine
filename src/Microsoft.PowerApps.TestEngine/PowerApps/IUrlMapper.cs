// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerApps.TestEngine.PowerApps
{
    /// <summary>
    /// Map urls based on the cloud
    /// </summary>
    public interface IUrlMapper
    {
        /// <summary>
        /// Generates url for login
        /// </summary>
        /// <returns>Login url</returns>
        public string GenerateLoginUrl();

        /// <summary>
        /// Generates the app url
        /// </summary>
        /// <returns>App url</returns>
        public string GenerateAppUrl();
    }
}
