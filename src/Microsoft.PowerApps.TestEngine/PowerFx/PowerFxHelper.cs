// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Globalization;
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
        /// <param name="result">Check result instance created after processing the expression</param>
        /// <param name="culture">The locale to be used when excecuting tests</param>
        /// <returns>An enumerable of formulas extracted from the expression that are separated by chaining operator</returns>
        public static IEnumerable<string> ExtractFormulasSeparatedByChainingOperator(Engine engine, CheckResult result, CultureInfo culture)
        {
            if (string.IsNullOrEmpty(result?.Parse?.Text))
            {
                return new string[0];
            }

            var spansForFormulasSeparatedAcrossMultipleDepths = ExtractSpansOfFormulasSeparatedByChainingOperator(engine, result?.Parse.Text, culture);
            if (result?.Parse?.Root == null)
            {
                return spansForFormulasSeparatedAcrossMultipleDepths.Select(span => result.Parse.Text.Substring(span.Start, span.End - span.Start));
            }

            var spansForTopMostSeparatedFormulas = DetermineNearestFormulaSeparationForSpansVisitor.GetSpansForFormulasSeparatedAtTopMostLevel(spansForFormulasSeparatedAcrossMultipleDepths, result.Parse.Root);
            return spansForTopMostSeparatedFormulas.Select(span => result.Parse.Text.Substring(span.Start, span.End - span.Start));
        }

        /// <summary>
        /// Extracts the span that represent formulas separated by chaining operator at multiple levels and depths
        /// </summary>
        /// <param name="engine">Instance of an engine configured with the desired locale</param>
        /// <param name="expression">Expression from which formulas separated by chaining operator would be extracted</param>
        /// <param name="culture">The locale to be used when excecuting tests</param>
        /// <returns>Spans that represent formulas separated by chaining operator at multiple levels and depths</returns>
        private static IEnumerable<Span> ExtractSpansOfFormulasSeparatedByChainingOperator(Engine engine, string expression, CultureInfo culture)
        {
            var chainOperatorTokens = engine.Tokenize(expression, culture).Where(tok => tok.Kind == TokKind.Semicolon).OrderBy(tok => tok.Span.Min);
            var formulas = new List<Span>();
            var lowerBound = 0;

            foreach (var chainOpToken in chainOperatorTokens)
            {
                if (lowerBound < expression.Length)
                {
                    var upperBound = chainOpToken.Span.Min;
                    var formula = expression.Substring(lowerBound, upperBound - lowerBound);
                    formulas.Add(new Span(lowerBound, upperBound));
                }
                lowerBound = chainOpToken.Span.Lim;
            }

            if (lowerBound < expression.Length)
            {
                formulas.Add(new Span(lowerBound, expression.Length));
            }

            return formulas;
        }
    }
}
