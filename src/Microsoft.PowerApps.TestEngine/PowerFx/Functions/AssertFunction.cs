// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.System;
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
            _logger.LogInformation("------------------------------\n\n" +
                "Executing Assert function.");

            if (!result.Value)
            {
                _logger.LogTrace($"{message.Value}");
                _logger.LogError("Assert failed. Property is not equal to the specified value.");
                throw new AssertionFailureException(message.Value);
            }

            _logger.LogTrace(message.Value);
            _logger.LogInformation("Successfully finished executing Assert function.");

            return FormulaValue.NewBlank();
        }
    }
}
