// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerApps.TestEngine.PowerFx.Functions
{
    /// <summary>
    /// The Assert function takes in a Power FX expression that should evaluate to a boolean value.
    /// If the value returned is false, rthen will return Power Fx Error that can be validated using IfError() or IsError()
    /// </summary>
    public class AssertNotErrorFunction : ReflectionFunction
    {
        private readonly ILogger _logger;

        public AssertNotErrorFunction(ILogger logger) : base("AssertNotError", FormulaType.Blank, FormulaType.Boolean, FormulaType.String)
        {
            _logger = logger;
        }

        public FormulaValue Execute(BooleanValue result, StringValue message)
        {
            _logger.LogInformation("------------------------------\n\n" +
                "Executing AssertNotError function.");

            if (!result.Value)
            {
                _logger.LogInformation($"{message.Value}");
                _logger.LogError("Assert failed. Property is not equal to the specified value.");

                return ErrorValue.NewError(new ExpressionError
                {
                    Kind = ErrorKind.InvalidArgument,
                    Severity = ErrorSeverity.Critical,
                    Message = message.Value
                });
            }

            _logger.LogTrace(message.Value);
            _logger.LogInformation("Successfully finished executing AssertNotError function.");

            return FormulaValue.NewBlank();
        }
    }
}
