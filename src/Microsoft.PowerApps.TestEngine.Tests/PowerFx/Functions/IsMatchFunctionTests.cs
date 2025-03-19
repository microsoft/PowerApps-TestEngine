// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.PowerFx.Functions;
using Microsoft.PowerApps.TestEngine.Tests.Helpers;
using Microsoft.PowerFx.Types;
using Moq;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.PowerFx.Functions
{
    public class IsMatchFunctionTests
    {
        private Mock<ILogger> MockLogger;

        public IsMatchFunctionTests()
        {
            MockLogger = new Mock<ILogger>(MockBehavior.Strict);
        }

        public static IEnumerable<object[]> TestData()
        {
            yield return new object[] { "Hello world", "Hello", true }; // Happy path
            yield return new object[] { "Hello world", "hello", false }; // Case sensitivity
            yield return new object[] { "Hello world", "world$", true }; // Pattern at the end
            yield return new object[] { "Hello world", "^Hello", true }; // Pattern at the beginning
            yield return new object[] { "Hello world", "o w", true }; // Pattern in the middle
            yield return new object[] { "Hello world", " ", true }; // Space character
            yield return new object[] { "Hello world", "Hello world", true }; // Exact match
            yield return new object[] { "Hello world", "Goodbye", false }; // No match
            yield return new object[] { "", "Hello", false }; // Empty text
            yield return new object[] { "Hello world", "", false }; // Empty pattern
            yield return new object[] { "", "", false }; // Both empty
            yield return new object[] { "12345", "\\d+", true }; // Numeric pattern
            yield return new object[] { "abc123", "\\d+", true }; // Alphanumeric pattern
            yield return new object[] { "abc", "\\d+", false }; // No numeric match
            yield return new object[] { null, "Hello", false }; // Null text
            yield return new object[] { "Hello world", ".*", true }; // Match any character
            yield return new object[] { "Hello world", "^$", false }; // Match empty string
            yield return new object[] { 12345, "\\d+", true }; // Integer pattern
            yield return new object[] { (decimal)123.45, "\\d+\\.\\d+", true }; // Decimal pattern
            yield return new object[] { (double)123.451, "\\d+\\.\\d+", true }; // Double pattern
            yield return new object[] { "2024-11-09", "\\d{4}-\\d{2}-\\d{2}", true }; // Date pattern
            yield return new object[] { new DateTime(2024, 11, 09), "\\d{4}-\\d{2}-\\d{2}", true }; // Date pattern
            yield return new object[] { new DateTime(2024, 11, 09, 1, 0, 0), "\\d{4}-\\d{2}-\\d{2}T01", true }; // Date / Time pattern
            yield return new object[] { new DateTime(2024, 11, 09, 0, 1, 0), "\\d{4}-\\d{2}-\\d{2}T00:01", true }; // Date / Time pattern
            yield return new object[] { new DateTime(2024, 11, 09, 0, 0, 1), "\\d{4}-\\d{2}-\\d{2}T00:00:01", true }; // Date / Time pattern
            yield return new object[] { new DateTime(2024, 11, 09, 0, 0, 0, 1), "\\d{4}-\\d{2}-\\d{2}T00:00:00.001", true }; // Date / Time pattern
            yield return new object[] { new DateTime(2024, 11, 09, 0, 0, 0, 1), "2024-11-09T00:00:00\\.0010000Z", true }; // ISO 8601 format
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public void IsMatchFunctionPatternWithExpectedResult(object? text, string pattern, bool expectedResult)
        {
            LoggingTestHelper.SetupMock(MockLogger);
            var assertFunction = new IsMatchFunction(MockLogger.Object);

            FormulaValue textValue = FormulaValue.NewBlank();
            if (text is string textStringValue)
            {
                textValue = StringValue.New(textStringValue);
            }
            else if (text is int textIntValue)
            {
                textValue = NumberValue.New(textIntValue);
            }
            else if (text is decimal textDecimalValue)
            {
                textValue = NumberValue.New((double)textDecimalValue);
            }
            else if (text is double textDoubleValue)
            {
                textValue = NumberValue.New(textDoubleValue);
            }
            else if (text is DateTime textDateValue)
            {
                textValue = DateValue.New(textDateValue);
            }

            var result = assertFunction.Execute(
                textValue,
                StringValue.New(pattern)
            );
            Assert.IsType<BooleanValue>(result);

            Assert.Equal(expectedResult, (result as BooleanValue).Value);
        }
    }
}
