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
            IUserManagerLogin userManagerLogin)
        {
            Context = context;

            var logger = singleTestInstanceState.GetLogger();

            if (!DirectoryExists(Location))
            {
                logger.LogInformation($"Creating {Location}");
                CreateDirectory(Location);
            }

            await LoginComplete(desiredUrl, testState, environmentVariable, logger);
        }

        private void ValidatePage()
        {
            if (Page == null)
            {
                Page = Context.Pages.First();
            }
        }

        public async Task LoginComplete(string desiredUrl, ITestState testState, IEnvironmentVariable environmentVariable, ILogger logger)
        {
            var complete = false;

            var persona = testState.GetTestSuiteDefinition().Persona;
            var personaEmail = environmentVariable.GetVariable(persona);

            if ( string.IsNullOrEmpty(personaEmail) )
            {
                logger.LogInformation($"Missing user persona {persona} email. Prompting user");
                Console.Write("Persona Email? ");
                personaEmail = Console.ReadLine();
            }

            var started = DateTime.Now;

            while ( ! complete )
            {
                foreach (var page in Context.Pages)
                {
                    var host = new Uri(desiredUrl).Host;
                    if (page.Url.Contains($"https://{host}"))
                    {
                        logger.LogInformation($"Found destination url");
                        complete = true;
                        break;
                    }

                    if (page.Url.Contains("common/fido"))
                    {
                        logger.LogInformation($"Login required");
                        Console.WriteLine("Login required");
                        await page.PauseAsync();
                    }

                    if (page.Url.Contains("oauth2/authorize"))
                    {
                        if (await page.IsVisibleAsync($"[data-test-id=\"{personaEmail}\"]"))
                        {
                            logger.LogInformation($"Selecting {personaEmail}");
                            await page.ClickAsync($"[data-test-id=\"{personaEmail}\"]");
                        }
                    }
                    
                    if (page.Url.Contains("mcas.ms/aad_login"))
                    {
                        // TODO: Handle localized values
                        if (await page.GetByRole(AriaRole.Button, new() { Name = "Continue with current profile" }).IsVisibleAsync()) 
                        {
                            logger.LogInformation($"Continue with Microsoft Conditional Access");
                            await page.GetByRole(AriaRole.Button, new() { Name = "Continue with current profile" }).ClickAsync();
                        }
                    }
                }

                if (!complete)
                {
                    if (DateTime.Now.Subtract(started).TotalMinutes > 5)
                    {
                        throw new Exception("Unable to complete login");
                    }
                    Thread.Sleep(500);
                }
            }
        }
    }
}
