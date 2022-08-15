// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using Microsoft.PowerApps.TestEngine.PowerApps.PowerFxModel;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.PowerApps.PowerFXModel
{
    public class TypeMappingTests
    {
        [Fact]
        public void TypeMappingSetsUpDefaultTypes()
        {
            var typeMapping = new TypeMapping();
            Assert.True(typeMapping.TryGetType("s", out var formulaType));
            Assert.Equal(FormulaType.String, formulaType);

            Assert.True(typeMapping.TryGetType("b", out formulaType));
            Assert.Equal(FormulaType.Boolean, formulaType);

            Assert.True(typeMapping.TryGetType("d", out formulaType));
            Assert.Equal(FormulaType.DateTime, formulaType);

            Assert.True(typeMapping.TryGetType("D", out formulaType));
            Assert.Equal(FormulaType.Date, formulaType);

            Assert.True(typeMapping.TryGetType("h", out formulaType));
            Assert.Equal(FormulaType.Hyperlink, formulaType);

            Assert.True(typeMapping.TryGetType("c", out formulaType));
            Assert.Equal(FormulaType.Color, formulaType);

            Assert.True(typeMapping.TryGetType("n", out formulaType));
            Assert.Equal(FormulaType.Number, formulaType);

            Assert.True(typeMapping.TryGetType("Z", out formulaType));
            Assert.Equal(FormulaType.DateTimeNoTimeZone, formulaType);

            Assert.True(typeMapping.TryGetType("g", out formulaType));
            Assert.Equal(FormulaType.Guid, formulaType);
        }

        [Fact]
        public void TryGetTypeFailsForNonExistentTypeTest()
        {
            var typeMapping = new TypeMapping();
            Assert.False(typeMapping.TryGetType(Guid.NewGuid().ToString(), out var formulaType));
            Assert.Null(formulaType);

            Assert.False(typeMapping.TryGetType(null, out formulaType));
            Assert.Null(formulaType);

            Assert.False(typeMapping.TryGetType("", out formulaType));
            Assert.Null(formulaType);
        }

        [Fact]
        public void GetTypeThatWasAddedTest()
        {
            var typeMapping = new TypeMapping();

            var recordType = RecordType.Empty().Add("Text", FormulaType.String).Add("X", FormulaType.Number);

            typeMapping.AddMapping("Label1", recordType);

            Assert.True(typeMapping.TryGetType("Label1", out var formulaType));
            Assert.Equal(recordType, formulaType);
        }

        [Fact]
        public void GetTableTypeTest()
        {
            var typeMapping = new TypeMapping();
            var labelType = RecordType.Empty().Add("Text", FormulaType.String).Add("X", FormulaType.Number).Add(Guid.NewGuid().ToString(), FormulaType.Guid);
            var buttonType = RecordType.Empty().Add("Text", FormulaType.String).Add("X", FormulaType.Number).Add(Guid.NewGuid().ToString(), FormulaType.Guid);
            var imageType = RecordType.Empty().Add("Text", FormulaType.String).Add("X", FormulaType.Number).Add(Guid.NewGuid().ToString(), FormulaType.Guid);
            typeMapping.AddMapping("Label1", labelType);
            typeMapping.AddMapping("Button1", buttonType);
            typeMapping.AddMapping("Image1", imageType);

            Assert.True(typeMapping.TryGetType("*[Label1:v, Button1:v, Image1:v]", out var formulaType));
            Assert.NotNull(formulaType);
            var tableType = formulaType as TableType;
            Assert.NotNull(tableType);
            Assert.Equal(labelType, tableType.GetFieldType("Label1"));
            Assert.Equal(buttonType, tableType.GetFieldType("Button1"));
            Assert.Equal(imageType, tableType.GetFieldType("Image1"));

            Assert.True(typeMapping.TryGetType("*[Label1:v, Button1:v]", out formulaType));
            Assert.NotNull(formulaType);
            tableType = formulaType as TableType;
            Assert.NotNull(tableType);
            Assert.Equal(labelType, tableType.GetFieldType("Label1"));
            Assert.Equal(buttonType, tableType.GetFieldType("Button1"));
            Assert.ThrowsAny<Exception>(() => tableType.GetFieldType("Image1"));

            Assert.True(typeMapping.TryGetType("*[Button1:v]", out formulaType));
            Assert.NotNull(formulaType);
            tableType = formulaType as TableType;
            Assert.NotNull(tableType);
            Assert.ThrowsAny<Exception>(() => tableType.GetFieldType("Label1"));
            Assert.Equal(buttonType, tableType.GetFieldType("Button1"));
            Assert.ThrowsAny<Exception>(() => tableType.GetFieldType("Image1"));

            // Empty table
            Assert.True(typeMapping.TryGetType("*[]", out formulaType));
            Assert.Equal(RecordType.Empty().ToTable(),formulaType);
        }

        [Fact]
        public void GetRecordTypeTest()
        {
            var typeMapping = new TypeMapping();
            var labelType = RecordType.Empty().Add("Text", FormulaType.String).Add("X", FormulaType.Number).Add(Guid.NewGuid().ToString(), FormulaType.Guid);
            var buttonType = RecordType.Empty().Add("Text", FormulaType.String).Add("X", FormulaType.Number).Add(Guid.NewGuid().ToString(), FormulaType.Guid);
            var imageType = RecordType.Empty().Add("Text", FormulaType.String).Add("X", FormulaType.Number).Add(Guid.NewGuid().ToString(), FormulaType.Guid);
            typeMapping.AddMapping("Label1", labelType);
            typeMapping.AddMapping("Button1", buttonType);
            typeMapping.AddMapping("Image1", imageType);

            Assert.True(typeMapping.TryGetType("![Label1:v, Button1:v, Image1:v]", out var formulaType));
            Assert.NotNull(formulaType);
            var recordType = formulaType as RecordType;
            Assert.NotNull(recordType);
            Assert.Equal(labelType, recordType.GetFieldType("Label1"));
            Assert.Equal(buttonType, recordType.GetFieldType("Button1"));
            Assert.Equal(imageType, recordType.GetFieldType("Image1"));

            Assert.True(typeMapping.TryGetType("![Label1:v, Button1:v]", out formulaType));
            Assert.NotNull(formulaType);
            recordType = formulaType as RecordType;
            Assert.NotNull(recordType);
            Assert.Equal(labelType, recordType.GetFieldType("Label1"));
            Assert.Equal(buttonType, recordType.GetFieldType("Button1"));
            Assert.ThrowsAny<Exception>(() => recordType.GetFieldType("Image1"));

            Assert.True(typeMapping.TryGetType("![Button1:v]", out formulaType));
            Assert.NotNull(formulaType);
            recordType = formulaType as RecordType;
            Assert.NotNull(recordType);
            Assert.ThrowsAny<Exception>(() => recordType.GetFieldType("Label1"));
            Assert.Equal(buttonType, recordType.GetFieldType("Button1"));
            Assert.ThrowsAny<Exception>(() => recordType.GetFieldType("Image1"));

            // Empty table
            Assert.True(typeMapping.TryGetType("![]", out formulaType));
            Assert.Equal(RecordType.Empty(),formulaType);
        }

        [Fact]
        public void GetComplexTypeWithMissingSubType()
        {
            var typeMapping = new TypeMapping();
            var labelType = RecordType.Empty().Add("Text", FormulaType.String).Add("X", FormulaType.Number).Add(Guid.NewGuid().ToString(), FormulaType.Guid);
            var buttonType = RecordType.Empty().Add("Text", FormulaType.String).Add("X", FormulaType.Number).Add(Guid.NewGuid().ToString(), FormulaType.Guid);
            var imageType = RecordType.Empty().Add("Text", FormulaType.String).Add("X", FormulaType.Number).Add(Guid.NewGuid().ToString(), FormulaType.Guid);
            typeMapping.AddMapping("Label1", labelType);
            typeMapping.AddMapping("Button1", buttonType);
            typeMapping.AddMapping("Image1", imageType);

            Assert.False(typeMapping.TryGetType($"*[Label1:v, Button1:v, Image1:v, {Guid.NewGuid().ToString()}:v]", out var formulaType));
            Assert.Null(formulaType);

            Assert.False(typeMapping.TryGetType($"![Label1:v, Button1:v, Image1:v, {Guid.NewGuid().ToString()}:v]", out formulaType));
            Assert.Null(formulaType);
        }
    }
}
