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
    public class AssertWithoutMessageFunction : ReflectionFunction
    {
        private readonly ILogger _logger;

        public AssertWithoutMessageFunction(ILogger logger) : base("Assert", FormulaType.Blank, FormulaType.Boolean)
        {
            _logger = logger;
        }

        public BlankValue Execute(BooleanValue result)
        {
            if (!result.Value)
            {
                _logger.LogError($"Assert failed: Assert Function failure");
                throw new InvalidOperationException($"Assert failed: Assert Function failure");
            } else
            {
                _logger.LogInformation("Assert Function success");
            }
            return FormulaValue.NewBlank();
        }
    }
}
