// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerApps.TestEngine.PowerFx
{
    /// <summary>
    /// Static entity with methods to help with any powerfx related operations
    /// </summary>
    public static class PowerFxHelper
    {
        /// <summary>
        /// Extracts the formulas separated by chaining/variadic operator.
        /// Operator is ";" when decimal separator for the locale is "." and is ";;" when decimal separator is ","
        /// </summary>
        /// <param name="engine">Instance of an engine configured with the desired locale</param>
        /// <param name="expression">Expression from which formulas separated by chaining operator would be extracted</param>
        /// <returns>An enumerable of formulas extracted from the expression that are separated by chaining operator</returns>
        public static IEnumerable<string> ExtractFormulasSeparatedByChainingOperator(Engine engine, string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                return new string[0];
            }

            var chainOperatorTokens = engine.Tokenize(expression).Where(tok => tok.Kind == TokKind.Semicolon).OrderBy(tok => tok.Span.Min);
            var formulas = new List<string>();
            var lowerBound = 0;
            foreach (var chainOpToken in chainOperatorTokens)
            {
                if (lowerBound < expression.Length)
                {
                    var upperBound = chainOpToken.Span.Min;
                    var formula = expression.Substring(lowerBound, upperBound - lowerBound);
                    formulas.Add(formula);
                }
                lowerBound = chainOpToken.Span.Lim;
            }

            if (lowerBound < expression.Length)
            {
                formulas.Add(expression.Substring(lowerBound));
            }

            return formulas;
        }

    }
}
