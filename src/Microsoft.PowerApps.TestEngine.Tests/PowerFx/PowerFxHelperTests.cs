// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Globalization;
using Microsoft.PowerApps.TestEngine.PowerFx;
using Microsoft.PowerFx;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.PowerFx
{
    public class PowerFxHelperTests
    {
        public PowerFxHelperTests() 
        {
        }

        [Theory]
        [InlineData("en-US", "1#1;/*comment;;*/2+2", "1#1", "/*comment;;*/2+2")]
        [InlineData("fr", "1#1;2+2;;/*comment;;*/3+3;;Max(1;2;3)", "1#1;2+2", "/*comment;;*/3+3", "Max(1;2;3)")]
        [InlineData("fr", ";;", "")]
        [InlineData("fr", "1;;;;2", "1", "", "2")]
        [InlineData("fr", "Select(Button1);;Assert(Button1.Text=\"semicolons;;;;semicolons\");;1+2;;Max(1,2,3)", "Select(Button1)", "Assert(Button1.Text=\"semicolons;;;;semicolons\")", "1+2", "Max(1,2,3)")]
        [InlineData("en-US", "Select(Button1);Assert(Button1.Text=\"semicolons;;;;semicolons\");1+2;Max(1;2;3)", "Select(Button1)", "Assert(Button1.Text=\"semicolons;;;;semicolons\")", "1+2", "Max(1", "2", "3)")]
        [InlineData("en-US", "1 + 2", "1 + 2")]
        [InlineData("en-US", "1;;;2;;", "1", "", "", "2", "")]
        [InlineData("fr","\"string \n;;String\";;1+2\n\n\n;;Max(\n1;2;\n3);;", "\"string \n;;String\"", "1+2\n\n\n", "Max(\n1;2;\n3)")]
        public void TestFormulasSeparatedByChainingOpAreExtractedCorrectly(string locale, string expression, params string[] expectedFormulas)
        {
            // Arrange
            var engine = GetEngine(locale);
      
            // Act
            var actualFormulas = PowerFxHelper.ExtractFormulasSeparatedByChainingOperator(engine, expression);

            // Assert
            Assert.Equal(expectedFormulas, actualFormulas);
        }

        private static Engine GetEngine(string locale)
        {
            var recalcEngine = new RecalcEngine(new PowerFxConfig(new CultureInfo(locale)));
            return recalcEngine;
        }
    }
}
