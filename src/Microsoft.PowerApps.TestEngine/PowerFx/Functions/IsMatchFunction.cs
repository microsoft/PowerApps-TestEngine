// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Text.RegularExpressions;
using ICSharpCode.Decompiler.CSharp.Syntax.PatternMatching;
using Microsoft.Extensions.Logging;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerApps.TestEngine.PowerFx.Functions
{
    /// <summary>
    /// The IsMatchFunction class tests whether a text string matches a pattern.
    /// The pattern can comprise ordinary characters, predefined patterns, or a regular expression.
    /// </summary>
    public class IsMatchFunction : ReflectionFunction
    {
        private readonly ILogger _logger;

        public IsMatchFunction(ILogger logger) : base("IsMatch", FormulaType.Number, FormulaType.String, FormulaType.String)
        {
            _logger = logger;
        }

        public BooleanValue Execute(FormulaValue text, StringValue pattern)
        {
            _logger.LogDebug("------------------------------\n\n" +
                "Executing IsMatch function.");

            var textValue = String.Empty;

            if (text is StringValue stringValue)
            {
                textValue = stringValue.Value;
            }

            if (text is BlankValue)
            {
                return BooleanValue.New(false);
            }

            if (text is DateTimeValue dateTimeValue)
            {
                var utcValue = dateTimeValue.GetConvertedValue(TimeZoneInfo.Utc);
                textValue = (utcValue > new DateTime(utcValue.Year, utcValue.Month, utcValue.Day, 0, 0, 0, 0)) ? utcValue.ToString("o") : utcValue.ToString("yyyy-MM-dd");
            }
            else if (text.TryGetPrimitiveValue(out var value))
            {
                textValue = value.ToString();
            }

            if (string.IsNullOrEmpty(pattern.Value))
            {
                return BooleanValue.New(false);
            }

            bool isMatch = Regex.IsMatch(textValue, pattern.Value);
            return FormulaValue.New(isMatch);
        }
    }
}
