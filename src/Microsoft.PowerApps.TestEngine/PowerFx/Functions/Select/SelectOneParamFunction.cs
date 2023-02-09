// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.PowerApps;
using Microsoft.PowerApps.TestEngine.PowerApps.PowerFxModel;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerApps.TestEngine.PowerFx.Functions
{
    /// <summary>
    /// This is the same functionality as the Power Apps Select function With a Param
    /// https://docs.microsoft.com/en-us/power-apps/maker/canvas-apps/functions/function-select
    /// </summary>
    public class SelectOneParamFunction : ReflectionFunction
    {
        private readonly IPowerAppFunctions _powerAppFunctions;
        private readonly Func<Task> _updateModelFunction;
        protected readonly ILogger _logger;

        public SelectOneParamFunction(IPowerAppFunctions powerAppFunctions, Func<Task> updateModelFunction, ILogger logger) : base("Select", FormulaType.Blank, RecordType.Empty())
        {
            _powerAppFunctions = powerAppFunctions;
            _updateModelFunction = updateModelFunction;
            _logger = logger;
        }

        public BlankValue Execute(RecordValue obj)
        {
            SelectAsync(obj).Wait();

            return FormulaValue.NewBlank();
        }

        private async Task SelectAsync(RecordValue obj)
        {
            _logger.LogInformation("------------------------------\n\n" +
                "Executing Select function.");

            if (obj == null)
            {
                _logger.LogTrace($"Object name: '{obj}'");
                _logger.LogError($"Object cannot be null.");
                throw new ArgumentException();
            }

            var powerAppControlModel = (ControlRecordValue)obj;
            var result = await _powerAppFunctions.SelectControlAsync(powerAppControlModel.GetItemPath());

            if (!result)
            {
                _logger.LogTrace($"Control name: {powerAppControlModel.Name}");
                _logger.LogError($"Unable to select control");
                throw new Exception();
            }

            await _updateModelFunction();

            _logger.LogInformation("Successfully finished executing Select function.");
        }
    }
}
