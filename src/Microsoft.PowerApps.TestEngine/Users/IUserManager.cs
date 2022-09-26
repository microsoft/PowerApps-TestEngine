// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.PowerApps.TestEngine.Users
{
    /// <summary>
    /// Handles anything related to the user
    /// </summary>
    public interface IUserManager
    {
        /// <summary>
        /// Log in as user for currently running test
        /// </summary>
        /// <returns>Task</returns>
        public Task LoginAsUserAsync(string desiredUrl);
    }
}
