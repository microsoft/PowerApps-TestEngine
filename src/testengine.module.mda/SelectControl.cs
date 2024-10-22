// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Providers;
using Microsoft.PowerApps.TestEngine.Providers.PowerFxModel;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace testengine.module
{
    /// <summary>
    /// This will check the custom pages of a model driven app looking for a consent dialog
    /// </summary>
    public class SelectControlFunction : ReflectionFunction
    {
        private readonly ITestInfraFunctions _testInfraFunctions;
        private readonly ITestState _testState;
        private readonly ILogger _logger;

        private static TableType SearchType = TableType.Empty()
              .Add(new NamedFormulaType("Text", FormulaType.String, displayName: "Text"));

        public SelectControlFunction(ITestInfraFunctions testInfraFunctions, ITestState testState, ILogger logger)
            : base(DPath.Root.Append(new DName("Experimental")), "SelectControl", FormulaType.Blank, RecordType.Empty(), FormulaType.Number)
        {
            _testInfraFunctions = testInfraFunctions;
            _testState = testState;
            _logger = logger;
        }

        public BlankValue Execute(RecordValue control, NumberValue index)
        {
            _logger.LogInformation("------------------------------\n\n" +
                "Executing Experimental.SelectControl() function.");

            ExecuteAsync(control, index).Wait();

            return FormulaValue.NewBlank();
        }

        private async Task ExecuteAsync(RecordValue obj, NumberValue index)
        {
            _logger.LogInformation("------------------------------\n\n" +
                "Executing Select function.");

            if (obj == null)
            {
                _logger.LogError($"Object cannot be null.");
                throw new ArgumentException();
            }

            var powerAppControlModel = (ControlRecordValue)obj;

            var itemPath = powerAppControlModel.GetItemPath();
            itemPath.Index = (int)index.Value;

            // Experimental support allow selection control using data-control-name DOM element
            var match = _testInfraFunctions.Page.Locator($"[data-control-name='{powerAppControlModel.Name}']").Nth((int)index.Value - 1);

            await match.ClickAsync();

            _logger.LogInformation("Successfully finished executing SelectControl function.");
        }
    }
}

