// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Helpers;
using Microsoft.PowerApps.TestEngine.Providers;
using Microsoft.PowerApps.TestEngine.Providers.PowerFxModel;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerApps.TestEngine.PowerFx.Functions
{
    /// <summary>
    /// This is the same functionality as the Power Apps Select File function With 2 Params
    /// https://docs.microsoft.com/en-us/power-apps/maker/canvas-apps/functions/function-select
    /// </summary>
    public class SelectFileTwoParamsFunction : ReflectionFunction
    {
        private readonly ITestWebProvider _TestWebProvider;
        private readonly Func<Task> _updateModelFunction;
        private readonly ILogger _logger;

        public SelectFileTwoParamsFunction(ITestWebProvider TestWebProvider, Func<Task> updateModelFunction, ILogger logger) : base("Select", FormulaType.Blank, RecordType.Empty(), FormulaType.String)
        {
            _TestWebProvider = TestWebProvider;
            _updateModelFunction = updateModelFunction;
            _logger = logger;
        }        

        public BlankValue Execute(RecordValue obj, StringValue filePath)
        {
            SelectAsync(obj, filePath).Wait();

            return FormulaValue.NewBlank();
        }      

        private async Task SelectAsync(RecordValue obj, StringValue filePath)
        {
            _logger.LogInformation("------------------------------\n\n" +
                "Executing Select function.");

            NullCheckHelper.NullCheck(obj, filePath, _logger);

            var powerAppControlModel = (ControlRecordValue)obj;
            var result = await _TestWebProvider.SelectControlAsync(powerAppControlModel.GetItemPath(), filePath?.Value);

            if (!result)
            {
                _logger.LogTrace($"Control name: {powerAppControlModel.Name}");
                _logger.LogError("Unable to select control");
                throw new Exception();
            }

            await _updateModelFunction();

            _logger.LogInformation("Successfully finished executing Select function.");
        }
    }
}
