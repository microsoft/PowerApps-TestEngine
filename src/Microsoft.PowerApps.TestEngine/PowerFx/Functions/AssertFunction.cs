// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Public.Values;

namespace Microsoft.PowerApps.TestEngine.PowerFx.Functions
{
    /// <summary>
    /// The Assert function takes in a Power FX expression that should evaluate to a boolean value.
    /// If the value returned is false, the test will fail.
    /// </summary>
    public class AssertFunction : ReflectionFunction
    {
        private readonly ILogger _logger;

        public AssertFunction(ILogger logger) : base("Assert", FormulaType.Boolean, FormulaType.Boolean, FormulaType.String)
        {
            _logger = logger;
        }

        public BooleanValue Execute(BooleanValue result, StringValue message)
        {
            if (!result.Value)
            {
                _logger.LogError($"Assert failed: {message.Value}");
            } else
            {
                _logger.LogInformation(message.Value);
            }
            return BooleanValue.New(result.Value);;
        }
    }
}
