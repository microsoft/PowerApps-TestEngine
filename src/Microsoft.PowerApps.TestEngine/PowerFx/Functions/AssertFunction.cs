// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerApps.TestEngine.PowerFx.Functions
{
    /// <summary>
    /// The Assert function takes in a Power FX expression that should evaluate to a boolean value.
    /// If the value returned is false, the test will fail.
    /// </summary>
    public class AssertFunction : ReflectionFunction
    {
        private readonly ILogger _logger;

        public AssertFunction(ILogger logger) : base("Assert", FormulaType.Blank, FormulaType.Boolean, FormulaType.String)
        {
            _logger = logger;
        }

        public BlankValue Execute(BooleanValue result, StringValue message)
        {
            if (!result.Value)
            {
                _logger.LogError($"Assert failed: {message.Value}");
                throw new InvalidOperationException($"Assert failed: {message.Value}");
            } else
            {
                _logger.LogInformation(message.Value);
            }
            return FormulaValue.NewBlank();
        }
    }
}
