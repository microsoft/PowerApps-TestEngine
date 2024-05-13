// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.System;

namespace Microsoft.PowerApps.TestEngine.Users
{
    /// <summary>
    /// Handles anything related to the user
    /// </summary>
    public interface IUserManager
    {
        /// <summary>
        /// The name of the user manager as multiple Manager instances may exist
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The relative priority order of the user manager of multiple macthes are found. Higher values will be prioritized first
        /// </summary>
        public int Priority { get; }

        /// <summary>
        /// Determines if the user manager should use static context to provide cached user state as part of a test session
        /// </summary>
        /// <value><c>True</c> if static context, <c>False</c> if not</value>
        public bool UseStaticContext { get; }

        /// <summary>
        /// The location to use for this user session
        /// </summary>
        /// <value>Path or resource where the user session should be located</value>
        public string Location { get; set; }

        /// <summary>
        /// Log in as user for currently running test
        /// </summary>
        /// <param name="desiredUrl">The location to open after a successful login</param>
        /// <param name="context">The current open browser context state</param>
        /// <param name="testState">The current overall tset state being executed</param>
        /// <param name="singleTestInstanceState">The instance of the running test</param>
        /// <param name="environmentVariable">Provides access to environment to enable successfull login</param>
        /// <returns>Task</returns>
        public Task LoginAsUserAsync(
            string desiredUrl,
            IBrowserContext context,
            ITestState testState,
            ISingleTestInstanceState singleTestInstanceState,
            IEnvironmentVariable environmentVariable);
    }
}
