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

namespace testengine.user.environment
{
    [Export(typeof(IUserManager))]
    public class BrowserUserManagerModule : IUserManager
    {   
        public string Name { get { return "browser"; } }

        public int Priority { get { return 100; } }

        public bool UseStaticContext { get { return true; } }

        public string Location { get; set; } = "BrowserContext";

        private IBrowserContext? Context { get;set; }

        public IPage? Page { get;set; }

        public Func<string, bool> DirectoryExists { get;set; } = (location) => Directory.Exists(location);

        public Action<string> CreateDirectory { get;set; } = (location) => Directory.CreateDirectory(location);

        public Func<string, string[]> GetFiles { get;set; } = (path) => Directory.GetFiles(path);

        public async Task LoginAsUserAsync(
            string desiredUrl,
            IBrowserContext context,
            ITestState testState,
            ISingleTestInstanceState singleTestInstanceState,
            IEnvironmentVariable environmentVariable)
        {
            Context = context;

            if ( ! DirectoryExists(Location)) {
                CreateDirectory(Location);
            }

            if ( GetFiles(Location).Count() == 0 ) {
                ValidatePage();
                await Page.PauseAsync();
            }
        }

        private void ValidatePage()
        {
            if (Page == null)
            {
                Page = Context.Pages.First();
            }
        }
    }
}
