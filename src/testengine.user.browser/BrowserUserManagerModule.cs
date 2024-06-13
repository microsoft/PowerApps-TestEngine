// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Modules;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerApps.TestEngine.Users;
using Microsoft.PowerFx;

namespace testengine.user.environment
{
    [Export(typeof(IUserManager))]
    public class BrowserUserManagerModule : IUserManager
    {
        public string Name { get { return "browser"; } }

        public int Priority { get { return 100; } }

        public bool UseStaticContext { get { return true; } }

        public string Location { get; set; } = "BrowserContext";

        private IBrowserContext? Context { get; set; }

        public IPage? Page { get; set; }

        public Func<string, bool> DirectoryExists { get; set; } = (location) => Directory.Exists(location);

        public Action<string> CreateDirectory { get; set; } = (location) => Directory.CreateDirectory(location);

        public Func<string, string[]> GetFiles { get; set; } = (path) => Directory.GetFiles(path);

        public async Task LoginAsUserAsync(
            string desiredUrl,
            IBrowserContext context,
            ITestState testState,
            ISingleTestInstanceState singleTestInstanceState,
            IEnvironmentVariable environmentVariable,
            IUserCertificateProvider userCertificateProvider)
        {
            Context = context;

            if (!DirectoryExists(Location))
            {
                CreateDirectory(Location);
            }

            if (GetFiles(Location).Count() == 0)
            {
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
