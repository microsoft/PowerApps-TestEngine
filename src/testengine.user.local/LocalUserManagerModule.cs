// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx;
using System.ComponentModel.Composition;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Modules;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.Playwright;
using System;
using System.Text.RegularExpressions;
using System.Linq;
using Microsoft.PowerApps.TestEngine.Users;

namespace testengine.user.local
{
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
