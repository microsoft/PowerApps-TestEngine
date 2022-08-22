// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Helpers;
using Microsoft.PowerApps.TestEngine.PowerApps;
using Microsoft.PowerApps.TestEngine.PowerApps.PowerFxModel;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerApps.TestEngine.PowerFx.Functions
{
    /// <summary>
    /// This will allow you to set properties for controls
    /// </summary>
    public class SetPropertyFunction : ReflectionFunction
    {
        protected readonly IPowerAppFunctions _powerAppFunctions;
        protected readonly ILogger _logger;

        public SetPropertyFunction(IPowerAppFunctions powerAppFunctions, ILogger logger) : base("SetProperty", FormulaType.Blank, RecordType.Empty(), FormulaType.String, FormulaType.Boolean)
        {
            _powerAppFunctions = powerAppFunctions;
            _logger = logger;
        }

        public BooleanValue Execute(RecordValue obj, StringValue propName, FormulaValue value)
        {
            SetProperty(obj, propName, value).Wait();
            return FormulaValue.New(true);
        }

        protected async Task SetProperty(RecordValue obj, StringValue propName, FormulaValue value)
        {
            _logger.LogInformation("------------------------------\n\n" +
                "Executing SetProperty function.");

            NullCheckHelper.NullCheck(obj, propName, value, _logger);

            var controlModel = (ControlRecordValue)obj;
            var result = await _powerAppFunctions.SetPropertyAsync(controlModel.GetItemPath(propName.Value), value);

            if (!result)
            {
                _logger.LogDebug("Error occurred on DataType of type " + value.GetType());
                _logger.LogTrace("Property name: " + propName.Value);
                _logger.LogError("Unable to set property with SetProperty function.");

                throw new Exception();
            }

            _logger.LogInformation("Successfully finished executing SetProperty function.");
        }

    }
}
