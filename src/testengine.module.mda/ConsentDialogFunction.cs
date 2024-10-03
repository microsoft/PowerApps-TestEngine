// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace testengine.module
{
    /// <summary>
    /// This will check the custom pages of a model driven app looking for a consent dialog
    /// </summary>
    public class ConsentDialogFunction : ReflectionFunction
    {
        private readonly ITestInfraFunctions _testInfraFunctions;
        private readonly ITestState _testState;
        private readonly ILogger _logger;

        private static TableType SearchType = TableType.Empty()
              .Add(new NamedFormulaType("Text", FormulaType.String, displayName: "Text"));

        public ConsentDialogFunction(ITestInfraFunctions testInfraFunctions, ITestState testState, ILogger logger)
            : base(DPath.Root.Append(new DName("TestEngine")), "ConsentDialog", FormulaType.Blank, SearchType)
        {
            _testInfraFunctions = testInfraFunctions;
            _testState = testState;
            _logger = logger;
        }

        public BlankValue Execute(TableValue searchFor)
        {
            _logger.LogInformation("------------------------------\n\n" +
                "Executing ConsentDialog function.");

            ExecuteAsync(searchFor).Wait();

            return FormulaValue.NewBlank();
        }

        private async Task ExecuteAsync(TableValue searchFor)
        {
            var page = _testInfraFunctions.GetContext().Pages.Where(p => p.Url.Contains("main.aspx")).First();

            var timeout = _testState.GetTimeout();

            IFrame contentFrame = null;
            DateTime started = DateTime.Now;

            // Loop until either search item found, consent dialog found or timeout occurs
            while (contentFrame == null)
            {
                // Search for the consent dialog IFrame
                foreach (var frame in page.Frames)
                {
                    if (frame.Url.Contains("consent"))
                    {
                        contentFrame = frame;
                        break;
                    }
                }

                if (contentFrame == null)
                {
                    if (await FindSearchItem(searchFor, page))
                    {
                        // Exit the function a Consent Dialog check as search item has been found
                        return;
                    }

                    Thread.Sleep(1000);
                    if (DateTime.Now.Subtract(started).TotalSeconds > timeout)
                    {
                        _logger.LogInformation("Did not find consent dialog");
                        throw new Exception("Did not find consent dialog or text");
                    }
                }
            }

            // TODO: Handle localization of label
            var allowButton = contentFrame.GetByRole(AriaRole.Button, new FrameGetByRoleOptions { Name = "Allow", Exact = true });

            if (allowButton != null)
            {
                if (await allowButton.IsEnabledAsync())
                {
                    _logger.LogInformation("Successfully finished executing ConsentDialog function.");
                    await allowButton.ClickAsync();
                }
            }
        }

        /// <summary>
        /// Attempt to search for items on the page that indicate that should exit search for consent dialog
        /// </summary>
        /// <param name="searchFor">The record to search for</param>
        /// <param name="page">The page to be searched</param>
        /// <returns><c>True</c> if a match was found, <c>False</c> if not</returns>
        private async Task<bool> FindSearchItem(TableValue searchFor, IPage page)
        {
            foreach (var row in searchFor.Rows)
            {
                if (!row.IsBlank)
                {
                    var record = row.Value;

                    if (record.Fields.Any(f => f.Name == "Text"))
                    {
                        if (record.GetField("Text").TryGetPrimitiveValue(out var value))
                        {
                            var textItem = page.GetByText(value.ToString(), null);

                            if (await textItem.CountAsync() > 0)
                            {
                                _logger.LogInformation($"Found {value.ToString()}, exiting consent search");
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }
    }
}

