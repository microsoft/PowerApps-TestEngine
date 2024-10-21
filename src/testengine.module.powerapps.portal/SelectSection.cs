// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
using testengine.module.powerapps.portal;

namespace testengine.module
{
    /// <summary>
    /// This provide the ability to select a section from the left bar. Compatible with powerApps.portal provider
    /// </summary>
    public class SelectSectionFunction : ReflectionFunction
    {
        private readonly ITestInfraFunctions _testInfraFunctions;
        private readonly ITestState _testState;
        private readonly ILogger _logger;

        public SelectSectionFunction(ITestInfraFunctions testInfraFunctions, ITestState testState, ILogger logger)
            : base(DPath.Root.Append(new DName("TestEngine")), "SelectSection", FormulaType.Blank, StringType.String)
        {
            _testInfraFunctions = testInfraFunctions;
            _testState = testState;
            _logger = logger;
        }

        public BlankValue Execute(StringValue section)
        {
            _logger.LogInformation("------------------------------\n\n" +
                "Executing TestEngine.SelectSection function.");

            ExecuteAsync(section).Wait();

            return BlankValue.NewBlank();
        }

        private async Task ExecuteAsync(StringValue section)
        {
            foreach (var page in _testInfraFunctions.GetContext().Pages) {
                var url = page.Url;
                var sectionName = section.Value.ToString();

                // TODO: Handle case section is not visible in the left navigation. If not consider adding steps to make visible from extra options in the portal
                if (url.Contains("powerapps.com") && url.Contains("/environments") && url.Contains("/home")) {
                    var selector = $"[data-test-id='{sectionName}']";
                    await page.WaitForSelectorAsync($"{selector}:visible");

                    await page.ClickAsync(selector);
                }
            }
        }
    }
}

