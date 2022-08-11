// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerApps.TestEngine.System
{
    /// <summary>
    /// Wrapper over fetching environment variables
    /// </summary>
    public interface IEnvironmentVariable
    {
        /// <summary>
        /// Gets variable from environment
        /// </summary>
        /// <param name="name">Name of variable</param>
        /// <returns>Variable value</returns>
        public string GetVariable(string name);
    }
}
