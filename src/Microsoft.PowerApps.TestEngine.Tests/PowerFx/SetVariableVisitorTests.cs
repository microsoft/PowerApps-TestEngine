// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.PowerFx
{
    public class SetVariableVisitorTests
    {
        private RecalcEngine _recalcEngine;
        private SetVariableVisitor _visitor;

        public SetVariableVisitorTests()
        {
            var powerFxConfig = new PowerFxConfig();
            var vals = new SymbolValues();
            var symbols = (SymbolTable)vals.SymbolTable;
            symbols.EnableMutationFunctions();
            powerFxConfig.SymbolTable = symbols;

            powerFxConfig.EnableSetFunction();

            _recalcEngine = new RecalcEngine(powerFxConfig);
            _visitor = new SetVariableVisitor(_recalcEngine);
        }

        [Theory]
        [MemberData(nameof(GetPrimativeData))]
        public void PrimativeData(string code, object expected)
        {
            // Arrange
            var parseResult = Engine.Parse(code);
            parseResult.Root.Accept(_visitor);

            // Act
            _recalcEngine.Eval(code, null, new ParserOptions { AllowsSideEffects = true });

            // Assert
            _recalcEngine.TryGetValue("myVar", out FormulaValue variable);
            variable.TryGetPrimitiveValue(out object value);

            Assert.Equal(expected, value);
        }

        public static IEnumerable<object[]> GetPrimativeData()
        {
            yield return new object[] { "Set(myVar, 123)", (decimal)123 };
            yield return new object[] { "Set(myVar, \"abc\")", "abc" };
            yield return new object[] { "Set(myVar, true)", true };
            yield return new object[] { "Set(myVar, false)", false };
            yield return new object[] { "Set(other, \"abc\");Set(myVar, Len(other))", (double)3 };
            yield return new object[] { "Set(myVar, GUID(\"d61bbeca-0186-48fa-90e1-ff7aa5d33e2d\"))", Guid.Parse("d61bbeca-0186-48fa-90e1-ff7aa5d33e2d") };

            // TODO Date, DateTime, Record
        }

        [Theory]
        [MemberData(nameof(GetFunctionData))]
        public void Function(string code, object expected)
        {
            // Arrange
            var parseResult = Engine.Parse(code);
            parseResult.Root.Accept(_visitor);

            // Act
            _recalcEngine.Eval(code, null, new ParserOptions { AllowsSideEffects = true });

            // Assert
            _recalcEngine.TryGetValue("myVar", out FormulaValue variable);
            variable.TryGetPrimitiveValue(out object value);

            Assert.Equal(expected, value);
        }

        public static IEnumerable<object[]> GetFunctionData()
        {
            yield return new object[] { "Set(myVar, CountRows(Table()))", (double)0 };
            yield return new object[] { "Set(myVar, CountRows(Table({Name:\"Name\"})))", (double)1 };
            yield return new object[] { "Set(data, Table({Name:\"Name\"}));Set(myVar,CountRows(data))", (double)1 };
            // TODO Date, DateTime, Record
        }

        [Fact]
        public void TestRecord()
        {
            // Arrange
            var code = "Set(myVar,{Name:\"Test\"})";
            var parseResult = Engine.Parse(code);
            parseResult.Root.Accept(_visitor);

            // Act
            _recalcEngine.Eval(code, null, new ParserOptions { AllowsSideEffects = true });

            // Assert
            _recalcEngine.TryGetValue("myVar", out FormulaValue variable);

            Assert.IsAssignableFrom<RecordValue>(variable);

            var recordValue = variable as RecordValue;

            var value = recordValue.GetField("Name");

            Assert.IsAssignableFrom<StringValue>(value);

            var stringValue = value as StringValue;

            Assert.Equal("Test", stringValue.Value);
        }

        [Fact]
        public void TestEmptyRecord()
        {
            // Arrange
            var code = "Set(myVar,{})";
            var parseResult = Engine.Parse(code);
            parseResult.Root.Accept(_visitor);

            // Act
            _recalcEngine.Eval(code, null, new ParserOptions { AllowsSideEffects = true });

            // Assert
            _recalcEngine.TryGetValue("myVar", out FormulaValue variable);

            Assert.IsAssignableFrom<RecordValue>(variable);
        }
    }
}
