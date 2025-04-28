// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Globalization;
using System.Linq;
using Microsoft.PowerApps.TestEngine.PowerFx;
using Microsoft.PowerFx;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.PowerFx
{
    public class PowerFxHelperTests
    {

        [Theory]
        [InlineData("// Test", "")]
        [InlineData(@"// Test
A", @"A
")]
        [InlineData(@"// Test
A
// Other", @"A
")]
        public void RemoveComments(string text, string expectedText)
        {
            Assert.Equal(expectedText, PowerFxHelper.RemoveComments(text));
        }

        [Theory]
        [InlineData(new string[] { "A(): Text = true" }, new string[] { @"A(): Text = true" })]
        [InlineData(new string[] { "A(): Text = true", "false" }, new string[] { @"A(): Text = true;
false" })]
        public void JoinFunctions(string[] input, string[] expectedText)
        {
            Assert.Equal(expectedText, PowerFxHelper.JoinFunctions(input));
        }

        [Theory]
        [InlineData("A(): Text = true", true)]
        [InlineData("A(Name: Text): Text = true", true)]
        public void ContainsFunction(string input, bool expected)
        {
            Assert.Equal(expected, PowerFxHelper.ContainsFunction(input));
        }

        [Theory]
        [InlineData("en-US", "1#1;/*comment;;*/2+2", "1#1", "/*comment;;*/2+2")]
        [InlineData("en-US", "1+1;/*comment;;*/2+2", "1+1", "/*comment;;*/2+2")]
        [InlineData("fr", "1#1;2+2;;/*comment;;*/3+3;;Max(1;2;3)", "1#1;2+2", "/*comment;;*/3+3", "Max(1;2;3)")]
        [InlineData("fr", ";;", "")]
        [InlineData("fr", "1;;;;2", "1", "", "2")]
        [InlineData("fr", "Select(Button1);;Assert(Button1.Text=\"semicolons;;;;semicolons\");;1+2;;Max(1,2,3)", "Select(Button1)", "Assert(Button1.Text=\"semicolons;;;;semicolons\")", "1+2", "Max(1,2,3)")]
        [InlineData("en-US", "Select(Button1);Assert(Button1.Text=\"semicolons;;;;semicolons\");1+2;Max(1;2;3)", "Select(Button1)", "Assert(Button1.Text=\"semicolons;;;;semicolons\")", "1+2", "Max(1;2;3)")]
        [InlineData("en-US", "1 + 2", "1 + 2")]
        [InlineData("en-US", "1;;;2;;", "1", "", "", "2", "")]
        [InlineData("fr", "\"string \n;;String\";;1+2\n\n\n;;Max(\n1;2;\n3);;", "\"string \n;;String\"", "1+2\n\n\n", "Max(\n1;2;\n3)")]
        [InlineData("en-US", "$\";{1;2;3;Max(1;2;30,40)}\";Max(1;20;3)", "$\";{1;2;3;Max(1;2;30,40)}\"", "Max(1;20;3)")]
        [InlineData("fr", "$\";;;;{1;;2;;\n\n3;;Max(1;;2;;30,3;40)}\"\n\n\n;;Max(1;;20;;3);;1+1", "$\";;;;{1;;2;;\n\n3;;Max(1;;2;;30,3;40)}\"\n\n\n", "Max(1;;20;;3)", "1+1")]
        [InlineData("en-US", "$\";{1;2;3;Max(1;2;30#########<>><>>#3,40)}\";Max(1;20;3)", "$\";{1;2;3;Max(1;2;30#########<>><>>#3,40)}\"", "Max(1;20;3)")]
        [InlineData("en-US", "$\";{1;2;3;Max(1;2;30#########<>><>>#3,40)}\"", "$\";{1;2;3;Max(1;2;30#########<>><>>#3,40)}\"")]
        [InlineData("en-US", "Max(1;2;3)", "Max(1;2;3)")]
        [InlineData("it", "$\";;;;{1;;2;;3;;;;Max(1;;2;;;;30,3;40)}\";;;;", "$\";;;;{1;;2;;3;;;;Max(1;;2;;;;30,3;40)}\"", "")]
        [InlineData("en-US", "Max(1;2;Max(22;34;Sum(1;2,2)), 40);1+1;/*Comment;*/", "Max(1;2;Max(22;34;Sum(1;2,2)), 40)", "1+1", "/*Comment;*/")]
        [InlineData("en-US", "Max(1;2;Max(22;34;Sum(1;>?2,2)), 40);1+1;/*Comment;*/", "Max(1;2;Max(22;34;Sum(1;>?2,2)), 40)", "1+1", "/*Comment;*/")]
        [InlineData("en-US", "##Max(1;2;Max(22;34;Sum(1;>?2,2)), 40);1+1;/*Comment;*/", "##Max(1", "2", "Max(22", "34", "Sum(1", ">?2,2)), 40)", "1+1", "/*Comment;*/")]
        [InlineData("fr", "Max(1;;2;;3;30;;Max(230;;23;;33);;3);;Select(Button1;;Button2;;Button3);;", "Max(1;;2;;3;30;;Max(230;;23;;33);;3)", "Select(Button1;;Button2;;Button3)")]
        [InlineData("fr", "Max(1;;2;;3;30;;Max(230;;23;;33);;3);;Select(Button1;;Button2;;Button3;;;;;;);;", "Max(1;;2;;3;30;;Max(230;;23;;33);;3)", "Select(Button1;;Button2;;Button3;;;;", ")")]
        [InlineData("en-US", "Max(;;;1,2)", "Max(;;;1,2)")]
        [InlineData("en-US", ";;;;", "", "", "", "")]
        [InlineData("en-US", "")]
        [InlineData("en-US", "Max(;22)", "Max(;22)")]
        [InlineData("en-US", "Select(Button1/*Selecting button 1;;;*/);Assert(Button1.Text = Text(\"Ab\";\"Abcd\"))", "Select(Button1/*Selecting button 1;;;*/)", "Assert(Button1.Text = Text(\"Ab\";\"Abcd\"))")]
        [InlineData("en-US", "Select(Button1)", "Select(Button1)")]
        [InlineData("fr", "Max(1;;2;;3;30;;Max(230;;23;;33);;3)", "Max(1;;2;;3;30;;Max(230;;23;;33);;3)")]
        [InlineData("en-US", "'Test;2212", "'Test", "2212")]
        [InlineData("en-US", "'Test;;2333';Max(1;'Button1;Button2';23;/*\n\n;;Comment\n\n*/33,100;Max(100;200,20;30)))", "'Test;;2333'", "Max(1;'Button1;Button2';23;/*\n\n;;Comment\n\n*/33,100;Max(100;200,20;30)))")]
        [InlineData("en-US", "'Test;;2333';Max(1;'Button1;Button2';23;/*\n\n;;Comment\n\n33,100;Max(100;200,20;30)))", "'Test;;2333'", "Max(1;'Button1;Button2';23;/*\n\n;;Comment\n\n33,100;Max(100;200,20;30)))")]
        public void TestFormulasSeparatedByChainingOpAreExtractedCorrectly(string locale, string expression, params string[] expectedFormulas)
        {
            // Arrange
            // Setting this feature flag is no longer needed
            //FeatureFlags.StringInterpolation = true;
            var oldUICulture = CultureInfo.CurrentUICulture;
            var culture = new CultureInfo(locale);
            CultureInfo.CurrentUICulture = culture;
            var (result, engine) = GetCheckResultAndEngine(expression, locale);

            // Act
            var actualFormulas = PowerFxHelper.ExtractFormulasSeparatedByChainingOperator(engine, result, culture);

            // Assert
            try
            {
                CultureInfo.CurrentUICulture = oldUICulture;
            }
            catch
            {
                // no op
            }
            Assert.Equal(expectedFormulas, actualFormulas);
        }

        private static Engine GetEngine(string locale)
        {
            var recalcEngine = new RecalcEngine(new PowerFxConfig());
            return recalcEngine;
        }

        private static (CheckResult result, Engine engine) GetCheckResultAndEngine(string expression, string locale)
        {
            var engine = GetEngine(locale);
            return (engine.Check(expression, new ParserOptions { AllowsSideEffects = true }), engine);
        }
    }
}
