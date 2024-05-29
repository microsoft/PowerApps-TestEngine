// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.ComponentModel.Composition;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Users;

namespace testengine.user.local
{
    /// <summary>
    /// Implements IUserManager assuming that the components being tested are local to the test engine not authenticated via a web session
    /// </summary>
    [Export(typeof(IUserManager))]
    public class LocalUserManagerModule : IUserManager
    {
        public string Name { get { return "local"; } }

        public int Priority { get { return 50; } }

        public bool UseStaticContext { get { return false; } }

        public string Location { get; set; } = "";

        private IBrowserContext? Context { get; set; }

        public IPage? Page { get; set; }

        public async Task LoginAsUserAsync(
            string desiredUrl,
            IBrowserContext context,
            ITestState testState,
            ISingleTestInstanceState singleTestInstanceState,
            IEnvironmentVariable environmentVariable)
        {
            await Task.CompletedTask;
        }
    }
}
