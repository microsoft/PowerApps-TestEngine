// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.Decompiler.TypeSystem;
using Microsoft.PowerApps.TestEngine.Providers;
using Microsoft.PowerApps.TestEngine.Providers.PowerFxModel;
using Microsoft.PowerFx.Types;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.PowerApps.PowerFXModel
{
    public class ControlRecordValueTests
    {
        [Fact]
        public void SimpleControlRecordValueTest()
        {
            var recordType = RecordType.Empty().Add("Text", FormulaType.String).Add("X", FormulaType.Number).Add("SelectedDate", FormulaType.Date).Add("DefaultDate", FormulaType.DateTime);
            var mockTestWebProvider = new Mock<ITestWebProvider>(MockBehavior.Strict);
            var controlName = "Label1";
            var propertyValue = Guid.NewGuid().ToString();
            var numberPropertyValue = 11;
            var datePropertyValue = new DateTime(2030, 1, 1, 0, 0, 0).Date;
            var dateTimePropertyValue = new DateTime(2029, 12, 31, 18, 30, 0);
            var timezoneValue = "UTC";

            mockTestWebProvider.Setup(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((x) => x.PropertyName == "Text")))
                .Returns(JsonConvert.SerializeObject(new JSPropertyValueModel() { PropertyValue = propertyValue }));
            mockTestWebProvider.Setup(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((x) => x.PropertyName == "X")))
                .Returns(JsonConvert.SerializeObject(new JSPropertyValueModel() { PropertyValue = numberPropertyValue.ToString() }));
            mockTestWebProvider.Setup(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((x) => x.PropertyName == "SelectedDate")))
                .Returns(JsonConvert.SerializeObject(new JSPropertyValueModel() { PropertyValue = datePropertyValue.ToString() }));
            mockTestWebProvider.Setup(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((x) => x.PropertyName == "DefaultDate")))
                .Returns(JsonConvert.SerializeObject(new JSPropertyValueModel() { PropertyValue = dateTimePropertyValue.ToString() }));
            mockTestWebProvider.Setup(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((x) => x.PropertyName == "DateTimeZone")))
              .Returns(JsonConvert.SerializeObject(new JSPropertyValueModel() { PropertyValue = timezoneValue }));

            var controlRecordValue = new ControlRecordValue(recordType, mockTestWebProvider.Object, controlName);
            Assert.Equal(controlName, controlRecordValue.Name);
            Assert.Equal(recordType, controlRecordValue.Type);

            Assert.Equal(controlName, controlRecordValue.GetItemPath().ControlName);
            Assert.Null(controlRecordValue.GetItemPath().Index);
            Assert.Null(controlRecordValue.GetItemPath().PropertyName);
            Assert.Null(controlRecordValue.GetItemPath().ParentControl);
            Assert.Equal("Text", controlRecordValue.GetItemPath("Text").PropertyName);

            Assert.Equal(propertyValue, (controlRecordValue.GetField("Text") as StringValue).Value);
            Assert.Equal(numberPropertyValue, (controlRecordValue.GetField("X") as NumberValue).Value);
            Assert.Equal(datePropertyValue.ToString(), (controlRecordValue.GetField("SelectedDate") as DateValue).GetConvertedValue(null).ToString());

            // Adjust the assertion for DefaultDate based on the timezone value
            var controlDateTimeValue = (controlRecordValue.GetField("DefaultDate") as DateTimeValue).GetConvertedValue(null);
            var expectedDateTimeValue = timezoneValue.Equals("local", StringComparison.OrdinalIgnoreCase) ? dateTimePropertyValue.ToLocalTime() : dateTimePropertyValue;
            Assert.Equal(expectedDateTimeValue.ToString(), controlDateTimeValue.ToString());
            mockTestWebProvider.Verify(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((x) => x.PropertyName == "Text" && x.ControlName == controlName)), Times.Once());
            mockTestWebProvider.Verify(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((x) => x.PropertyName == "X" && x.ControlName == controlName)), Times.Once());
            mockTestWebProvider.Verify(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((x) => x.PropertyName == "SelectedDate" && x.ControlName == controlName)), Times.Once());
            mockTestWebProvider.Verify(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((x) => x.PropertyName == "DefaultDate" && x.ControlName == controlName)), Times.Once());
            mockTestWebProvider.Verify(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((x) => x.PropertyName == "DateTimeZone" && x.ControlName == controlName)), Times.Exactly(2));
        }

        [Fact]
        public void GalleryControlRecordValueTestMDA()
        {
            var labelRecordType = RecordType.Empty().Add("Text", FormulaType.String);
            var labelName = "Label1";
            var galleryAllItemsTableType = TableType.Empty().Add(new NamedFormulaType(labelName, labelRecordType));
            var allItemsName = "AllItems";
            var galleryRecordType = RecordType.Empty().Add(allItemsName, galleryAllItemsTableType);
            var galleryName = "Gallery1";
            var labelText = Guid.NewGuid().ToString();
            var mockTestWebProvider = new Mock<ITestWebProvider>(MockBehavior.Strict);
            //case of mda provider
            mockTestWebProvider.Setup(x => x.Name).Returns("mda");
            mockTestWebProvider.Setup(x => x.GetPropertyValueFromControl<string>(It.IsAny<ItemPath>()))
                .Returns(JsonConvert.SerializeObject(new JSPropertyValueModel() { PropertyValue = labelText }));

            var itemCount = 4;
            mockTestWebProvider.Setup(x => x.GetItemCount(It.IsAny<ItemPath>())).Returns(itemCount);

            var galleryRecordValue = new ControlRecordValue(galleryRecordType, mockTestWebProvider.Object, galleryName);
            Assert.Equal(galleryName, galleryRecordValue.Name);
            Assert.Equal(galleryRecordType, galleryRecordValue.Type);

            Assert.Equal(galleryName, galleryRecordValue.GetItemPath().ControlName);
            Assert.Null(galleryRecordValue.GetItemPath().Index);
            Assert.Null(galleryRecordValue.GetItemPath().PropertyName);
            Assert.Null(galleryRecordValue.GetItemPath().ParentControl);
            Assert.Equal("Text", galleryRecordValue.GetItemPath("Text").PropertyName);

            // Gallery1.AllItems
            var allItemsTableValue = galleryRecordValue.GetField(allItemsName) as TableValue;
            Assert.NotNull(allItemsTableValue);
            Assert.Equal(galleryAllItemsTableType, allItemsTableValue.Type);

            var rows = allItemsTableValue.Rows.ToArray();

            for (var i = 0; i < itemCount; i++)
            {
                // Index(Gallery1.AllItems, i)
                var row = rows[i];
                var rowControlRecordValue = row.Value as ControlRecordValue;
                Assert.NotNull(rowControlRecordValue.Name);
                Assert.Equal(galleryAllItemsTableType.ToRecord(), rowControlRecordValue.Type);

                Assert.NotNull(rowControlRecordValue.GetItemPath().ParentControl);
                Assert.Equal(galleryName, rowControlRecordValue.GetItemPath().ParentControl.ControlName);
                Assert.Equal(i, rowControlRecordValue.GetItemPath().ParentControl.Index);
                Assert.Equal(allItemsName, rowControlRecordValue.GetItemPath().ParentControl.PropertyName);

                // Index(Gallery1.AllItems, i).Label1
                var labelRecordValue = rowControlRecordValue.GetField(labelName) as ControlRecordValue;
                Assert.NotNull(labelRecordValue);
                Assert.Equal(labelName, labelRecordValue.Name);
                Assert.Equal(labelRecordType, labelRecordValue.Type);

                Assert.Equal(labelName, labelRecordValue.GetItemPath().ControlName);
                Assert.Null(labelRecordValue.GetItemPath().Index);
                Assert.Null(labelRecordValue.GetItemPath().PropertyName);
                Assert.NotNull(labelRecordValue.GetItemPath().ParentControl);
                Assert.Equal(galleryName, labelRecordValue.GetItemPath().ParentControl.ControlName);
                Assert.Equal(i, labelRecordValue.GetItemPath().ParentControl.Index);
                Assert.Equal(allItemsName, labelRecordValue.GetItemPath().ParentControl.PropertyName);

                // Index(Gallery1.AllItems, i).Label1.Text
                Assert.Equal(labelText, (labelRecordValue.GetField("Text") as StringValue).Value);
            }
            mockTestWebProvider.Verify(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((x) => x.PropertyName == "Text" && x.ControlName == labelName)), Times.Exactly(itemCount));

        }

        [Fact]
        public void GalleryControlRecordValueTestNonMDA()
        {
            var labelRecordType = RecordType.Empty().Add("Text", FormulaType.String);
            var labelName = "Label1";
            var galleryAllItemsTableType = TableType.Empty().Add(new NamedFormulaType(labelName, labelRecordType));
            var allItemsName = "AllItems";
            var galleryRecordType = RecordType.Empty().Add(allItemsName, galleryAllItemsTableType);
            var galleryName = "Gallery1";
            var labelText = Guid.NewGuid().ToString();
            var mockTestWebProvider = new Mock<ITestWebProvider>(MockBehavior.Strict);
            //case of non mda provider
            mockTestWebProvider.Setup(x => x.Name).Returns(string.Empty);
            mockTestWebProvider.Setup(x => x.GetPropertyValueFromControl<string>(It.IsAny<ItemPath>()))
                .Returns(JsonConvert.SerializeObject(new JSPropertyValueModel() { PropertyValue = labelText }));

            var itemCount = 4;
            mockTestWebProvider.Setup(x => x.GetItemCount(It.IsAny<ItemPath>())).Returns(itemCount);

            var galleryRecordValue = new ControlRecordValue(galleryRecordType, mockTestWebProvider.Object, galleryName);
            Assert.Equal(galleryName, galleryRecordValue.Name);
            Assert.Equal(galleryRecordType, galleryRecordValue.Type);

            Assert.Equal(galleryName, galleryRecordValue.GetItemPath().ControlName);
            Assert.Null(galleryRecordValue.GetItemPath().Index);
            Assert.Null(galleryRecordValue.GetItemPath().PropertyName);
            Assert.Null(galleryRecordValue.GetItemPath().ParentControl);
            Assert.Equal("Text", galleryRecordValue.GetItemPath("Text").PropertyName);

            // Gallery1.AllItems
            var allItemsTableValue = galleryRecordValue.GetField(allItemsName) as TableValue;
            Assert.NotNull(allItemsTableValue);
            Assert.Equal(galleryAllItemsTableType, allItemsTableValue.Type);

            var rows = allItemsTableValue.Rows.ToArray();

            for (var i = 0; i < itemCount; i++)
            {
                // Index(Gallery1.AllItems, i)
                var row = rows[i];
                var rowControlRecordValue = row.Value as ControlRecordValue;
                Assert.Null(rowControlRecordValue.Name);
                Assert.Equal(galleryAllItemsTableType.ToRecord(), rowControlRecordValue.Type);

                Assert.NotNull(rowControlRecordValue.GetItemPath().ParentControl);
                Assert.Equal(galleryName, rowControlRecordValue.GetItemPath().ParentControl.ControlName);
                Assert.Equal(i, rowControlRecordValue.GetItemPath().ParentControl.Index);
                Assert.Equal(allItemsName, rowControlRecordValue.GetItemPath().ParentControl.PropertyName);

                // Index(Gallery1.AllItems, i).Label1
                var labelRecordValue = rowControlRecordValue.GetField(labelName) as ControlRecordValue;
                Assert.NotNull(labelRecordValue);
                Assert.Equal(labelName, labelRecordValue.Name);
                Assert.Equal(labelRecordType, labelRecordValue.Type);

                Assert.Equal(labelName, labelRecordValue.GetItemPath().ControlName);
                Assert.Null(labelRecordValue.GetItemPath().Index);
                Assert.Null(labelRecordValue.GetItemPath().PropertyName);
                Assert.NotNull(labelRecordValue.GetItemPath().ParentControl);
                Assert.Equal(galleryName, labelRecordValue.GetItemPath().ParentControl.ControlName);
                Assert.Equal(i, labelRecordValue.GetItemPath().ParentControl.Index);
                Assert.Equal(allItemsName, labelRecordValue.GetItemPath().ParentControl.PropertyName);

                // Index(Gallery1.AllItems, i).Label1.Text
                Assert.Equal(labelText, (labelRecordValue.GetField("Text") as StringValue).Value);
            }
            mockTestWebProvider.Verify(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((x) => x.PropertyName == "Text" && x.ControlName == labelName)), Times.Exactly(itemCount));

        }

        [Fact]
        public void ComponentsControlRecordValueTest()
        {
            var labelRecordType = RecordType.Empty().Add("Text", FormulaType.String);
            var labelName = "Label1";
            var componentRecordType = RecordType.Empty().Add(labelName, labelRecordType);
            var componentName = "Component1";
            var mockTestWebProvider = new Mock<ITestWebProvider>(MockBehavior.Strict);
            var propertyValue = Guid.NewGuid().ToString();

            mockTestWebProvider.Setup(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((x) => x.PropertyName == "Text")))
                .Returns(JsonConvert.SerializeObject(new JSPropertyValueModel() { PropertyValue = propertyValue }));

            var controlRecordValue = new ControlRecordValue(componentRecordType, mockTestWebProvider.Object, componentName);
            Assert.Equal(componentName, controlRecordValue.Name);
            Assert.Equal(componentRecordType, controlRecordValue.Type);

            Assert.Equal(componentName, controlRecordValue.GetItemPath().ControlName);
            Assert.Null(controlRecordValue.GetItemPath().Index);
            Assert.Null(controlRecordValue.GetItemPath().PropertyName);
            Assert.Null(controlRecordValue.GetItemPath().ParentControl);
            Assert.Equal("Text", controlRecordValue.GetItemPath("Text").PropertyName);


            // Component1.Label1
            var labelRecordValue = controlRecordValue.GetField(labelName) as ControlRecordValue;
            Assert.NotNull(labelRecordValue);
            Assert.Equal(labelName, labelRecordValue.Name);
            Assert.Equal(labelRecordType, labelRecordValue.Type);

            Assert.Equal(labelName, labelRecordValue.GetItemPath().ControlName);
            Assert.Null(labelRecordValue.GetItemPath().Index);
            Assert.Null(labelRecordValue.GetItemPath().PropertyName);
            Assert.NotNull(labelRecordValue.GetItemPath().ParentControl);
            Assert.Equal(componentName, labelRecordValue.GetItemPath().ParentControl.ControlName);
            Assert.Null(labelRecordValue.GetItemPath().ParentControl.Index);
            Assert.Null(labelRecordValue.GetItemPath().ParentControl.PropertyName);

            // Component1.Label1.Text
            Assert.Equal(propertyValue, (labelRecordValue.GetField("Text") as StringValue).Value);

            mockTestWebProvider.Verify(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((x) => x.PropertyName == "Text" && x.ControlName == labelName)), Times.Once());
        }

        [Theory]
        [MemberData(nameof(GetFieldData))]
        public async Task GetPrimativeField(FormulaType formulaType, string json, object expected)
        {
            var mockTestWebProvider = new Mock<ITestWebProvider>(MockBehavior.Strict);
            var componentRecordType = RecordType.Empty().Add(new NamedFormulaType("Test", formulaType));
            var componentName = "Component1";
            var controlRecordValue = new ControlRecordValue(componentRecordType, mockTestWebProvider.Object, componentName);

            mockTestWebProvider.Setup(m => m.GetPropertyValueFromControl<string>(It.IsAny<ItemPath>())).Returns(json);

            // Act
            var result = await controlRecordValue.GetFieldAsync("Test", CancellationToken.None);

            // Assert
            result.TryGetPrimitiveValue(out object primativeValue);

            if (expected is DateTime expectedDate && primativeValue is DateTime primativeDate)
            {
                if (expectedDate.Kind == DateTimeKind.Unspecified)
                {
                    expectedDate = DateTime.SpecifyKind(expectedDate, DateTimeKind.Utc);
                }
                if (primativeDate.Kind == DateTimeKind.Unspecified)
                {
                    primativeDate = DateTime.SpecifyKind(primativeDate, DateTimeKind.Utc);
                }
                if (primativeDate.Kind == DateTimeKind.Utc)
                {
                    primativeDate = primativeDate.ToUniversalTime();
                }
                Assert.Equal(expectedDate, primativeDate);
            }
            else if (expected is long expectedLong && primativeValue is DateTime primativeDate2)
            {
                var expectedLong2 = new DateTime(1970, 1, 1).AddMilliseconds(expectedLong);
                if (expectedLong2.Kind == DateTimeKind.Unspecified)
                {
                    expectedLong2 = DateTime.SpecifyKind(expectedLong2, DateTimeKind.Utc);
                }
                if (primativeDate2.Kind == DateTimeKind.Unspecified)
                {
                    primativeDate2 = DateTime.SpecifyKind(primativeDate2, DateTimeKind.Utc);
                }
                if (primativeDate2.Kind == DateTimeKind.Utc)
                {
                    primativeDate2 = primativeDate2.ToUniversalTime();
                }
                Assert.Equal(expectedLong2, primativeDate2);
            }
            else
            {
                Assert.Equal(expected, primativeValue);
            }
        }

        public static IEnumerable<object[]> GetFieldData()
        {
            var guidValue = Guid.NewGuid();
            var dateTime = new DateTime(2023, 12, 10, 1, 2, 3, DateTimeKind.Local);
            var dateTimeValue = new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();

            var dateValue = new DateTime(2023, 12, 10, 0, 0, 0, DateTimeKind.Local);
            var dateUnixValue = new DateTimeOffset(dateValue).ToUnixTimeMilliseconds();

            yield return new object[] { BlankType.Blank, "{PropertyValue: null}", null }; // Happy path Blank
            yield return new object[] { StringType.String, "{PropertyValue: 'Test'}", "Test" }; // Happy path, text
            yield return new object[] { NumberType.Number, "{PropertyValue: 1}", (double)1 }; // Happy path, number
            yield return new object[] { GuidType.Guid, $"{{PropertyValue: '{guidValue.ToString()}'}}", guidValue }; // Happy path, GUID
            yield return new object[] { BooleanType.Boolean, $"{{PropertyValue: true}}", true }; // Happy path, Boolean
            yield return new object[] { BooleanType.Boolean, $"{{PropertyValue: false}}", false }; // Happy path, Boolean
            yield return new object[] { BooleanType.Boolean, $"{{PropertyValue: 'true'}}", true }; // Happy path, Boolean
            yield return new object[] { BooleanType.Boolean, $"{{PropertyValue: 'false'}}", false }; // Happy path, Boolean
            yield return new object[] { DateTimeType.DateTime, $"{{PropertyValue: {dateTimeValue}}}", dateTimeValue }; // Happy path, DateTime
            yield return new object[] { DateTimeType.Date, $"{{PropertyValue: {dateUnixValue}}}", dateValue }; // Happy path, Date
        }

        [Theory]
        [MemberData(nameof(GetTableData))]
        public async Task GetTable(FormulaType formulaType, string json, string expected, string providerType)
        {
            var mockTestWebProvider = new Mock<ITestWebProvider>(MockBehavior.Strict);
            //case of non mda provider
            mockTestWebProvider.Setup(x => x.Name).Returns(providerType);
            var componentRecordType = RecordType.Empty().Add(new NamedFormulaType("Test", formulaType));
            var componentName = "Component1";
            var controlRecordValue = new ControlRecordValue(componentRecordType, mockTestWebProvider.Object, componentName, new ItemPath { ControlName = "Gallery", PropertyName = "Items", Index = 0 });

            mockTestWebProvider.Setup(m => m.GetItemCount(It.IsAny<ItemPath>())).Returns(1);
            mockTestWebProvider.Setup(m => m.GetPropertyValueFromControl<string>(It.IsAny<ItemPath>())).Returns(json);

            // Act
            var result = await controlRecordValue.GetFieldAsync("Test", CancellationToken.None) as TableValue;

            // Assert
            Assert.Single(result.Rows);
            Assert.Equal(expected, FormatTableValue(result));
        }

        public static IEnumerable<object[]> GetTableData()
        {
            var guidValue = Guid.NewGuid();
            var dateTime = new DateTime(2023, 12, 10, 1, 2, 3, DateTimeKind.Utc);
            var dateTimeValue = new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();


            var dateValue = new DateTime(2023, 12, 10, 0, 0, 0, DateTimeKind.Utc);
            var dateUnixValue = new DateTimeOffset(dateValue).ToUnixTimeMilliseconds();

            yield return new object[] { TableType.Empty().Add(new NamedFormulaType("Test", StringType.String)), "{PropertyValue: 'A'}", "[{'Test': \"A\"}]", "mda" };
            yield return new object[] { TableType.Empty().Add(new NamedFormulaType("Test", NumberType.Number)), "{PropertyValue: 1}", "[{'Test': 1}]" , "mda"};
            yield return new object[] { TableType.Empty().Add(new NamedFormulaType("Test", NumberType.Decimal)), "{PropertyValue: 1.1}", "[{'Test': 1.1}]" , "mda" };
            yield return new object[] { TableType.Empty().Add(new NamedFormulaType("Test", BooleanType.Boolean)), "{PropertyValue: true}", "[{'Test': true}]", "mda" };
            yield return new object[] { TableType.Empty().Add(new NamedFormulaType("Test", BooleanType.Boolean)), "{PropertyValue: 'true'}", "[{'Test': true}]", "mda" };
            yield return new object[] { TableType.Empty().Add(new NamedFormulaType("Test", BooleanType.Boolean)), "{PropertyValue: false}", "[{'Test': false}]" , "mda" };
            yield return new object[] { TableType.Empty().Add(new NamedFormulaType("Test", BooleanType.Boolean)), "{PropertyValue: 'false'}", "[{'Test': false}]" , "mda" };

            yield return new object[] { TableType.Empty().Add(new NamedFormulaType("Test", DateTimeType.DateTime)), $"{{PropertyValue: {dateTimeValue}}}", $"[{{'Test': \"{dateTime.ToString("o")}\"}}]", "mda" };
            yield return new object[] { TableType.Empty().Add(new NamedFormulaType("Test", DateTimeType.Date)), $"{{PropertyValue: {dateUnixValue}}}", $"[{{'Test': \"{dateValue.ToString("o")}\"}}]", "mda" };

            yield return new object[] { TableType.Empty().Add(new NamedFormulaType("Test", StringType.String)), "{PropertyValue: 'A'}", "[{'Test': \"A\"}]", string.Empty};
            yield return new object[] { TableType.Empty().Add(new NamedFormulaType("Test", NumberType.Number)), "{PropertyValue: 1}", "[{'Test': 1}]", string.Empty };
            yield return new object[] { TableType.Empty().Add(new NamedFormulaType("Test", NumberType.Decimal)), "{PropertyValue: 1.1}", "[{'Test': 1.1}]", string.Empty };
            yield return new object[] { TableType.Empty().Add(new NamedFormulaType("Test", BooleanType.Boolean)), "{PropertyValue: true}", "[{'Test': true}]", "canvas" };
            yield return new object[] { TableType.Empty().Add(new NamedFormulaType("Test", BooleanType.Boolean)), "{PropertyValue: 'true'}", "[{'Test': true}]", "canvas" };
            yield return new object[] { TableType.Empty().Add(new NamedFormulaType("Test", BooleanType.Boolean)), "{PropertyValue: false}", "[{'Test': false}]", string.Empty };
            yield return new object[] { TableType.Empty().Add(new NamedFormulaType("Test", BooleanType.Boolean)), "{PropertyValue: 'false'}", "[{'Test': false}]", string.Empty };

            yield return new object[] { TableType.Empty().Add(new NamedFormulaType("Test", DateTimeType.DateTime)), $"{{PropertyValue: {dateTimeValue}}}", $"[{{'Test': \"{dateTime.ToString("o")}\"}}]", string.Empty };
            yield return new object[] { TableType.Empty().Add(new NamedFormulaType("Test", DateTimeType.Date)), $"{{PropertyValue: {dateUnixValue}}}", $"[{{'Test': \"{dateValue.ToString("o")}\"}}]", string.Empty };
        }

        [Theory]
        [MemberData(nameof(GetRecordData))]
        public async Task GetRecord(FormulaType formulaType, string json, string expected)
        {
            var mockTestWebProvider = new Mock<ITestWebProvider>(MockBehavior.Strict);
            var componentRecordType = RecordType.Empty().Add(new NamedFormulaType("Test", formulaType));
            var componentName = "Component1";
            var controlRecordValue = new ControlRecordValue(componentRecordType, mockTestWebProvider.Object, componentName);

            mockTestWebProvider.Setup(m => m.GetItemCount(It.IsAny<ItemPath>())).Returns(1);
            mockTestWebProvider.Setup(m => m.GetPropertyValueFromControl<string>(It.IsAny<ItemPath>())).Returns(json);

            // Act
            var result = await controlRecordValue.GetFieldAsync("Test", CancellationToken.None) as RecordValue;

            // Assert
            Assert.Equal(expected, FormatRecordValue(result));
        }

        public static IEnumerable<object[]> GetRecordData()
        {
            yield return new object[] { RecordType.Empty().Add(new NamedFormulaType("Test", StringType.String)), "{PropertyValue: 'A'}", "{'Test': \"A\"}" };
            yield return new object[] { RecordType.Empty().Add(new NamedFormulaType("Test", NumberType.Number)), "{PropertyValue: 1}", "{'Test': 1}" };
            yield return new object[] { RecordType.Empty().Add(new NamedFormulaType("Test", NumberType.Decimal)), "{PropertyValue: 1.1}", "{'Test': 1.1}" };
            yield return new object[] { RecordType.Empty().Add(new NamedFormulaType("Test", BooleanType.Boolean)), "{PropertyValue: true}", "{'Test': true}" };
            yield return new object[] { RecordType.Empty().Add(new NamedFormulaType("Test", BooleanType.Boolean)), "{PropertyValue: 'true'}", "{'Test': true}" };
            yield return new object[] { RecordType.Empty().Add(new NamedFormulaType("Test", BooleanType.Boolean)), "{PropertyValue: false}", "{'Test': false}" };
            yield return new object[] { RecordType.Empty().Add(new NamedFormulaType("Test", BooleanType.Boolean)), "{PropertyValue: 'false'}", "{'Test': false}" };
        }

        /// <summary>
        /// Convert the Power Fx table into string representation
        /// </summary>
        /// <param name="tableValue">The table to be converted</param>
        /// <returns>The string representation of all rows of the table</returns>
        private string FormatTableValue(TableValue tableValue)
        {
            var rows = tableValue.Rows.Select(row => FormatValue(row.Value));
            return $"[{string.Join(", ", rows)}]";
        }

        /// <summary>
        /// Convert a Power Fx object to String Representation of the Record
        /// </summary>
        /// <param name="recordValue">The record to be converted</param>
        /// <returns>Power Fx representation</returns>
        private string FormatRecordValue(RecordValue recordValue)
        {
            var fields = recordValue.Fields.Select(field => $"'{field.Name}': {FormatValue(field.Value)}");
            return $"{{{string.Join(", ", fields)}}}";
        }

        /// <summary>
        /// Convert Power Fx formula value to the string representation
        /// </summary>
        /// <param name="value">The vaue to convert</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private string FormatValue(FormulaValue value)
        {
            //TODO: Handle special case of DateTime As unix time to DateTime
            return value switch
            {
                BlankValue blankValue => "null",
                StringValue stringValue => $"\"{stringValue.Value}\"",
                DecimalValue decimalValue => decimalValue.Value.ToString(),
                NumberValue numberValue => numberValue.Value.ToString(),
                BooleanValue booleanValue => booleanValue.Value.ToString().ToLower(),
                // Assume all dates should be in UTC
                DateValue dateValue => $"\"{dateValue.GetConvertedValue(TimeZoneInfo.Utc).ToString("o")}\"", // ISO 8601 format
                DateTimeValue dateTimeValue => $"\"{dateTimeValue.GetConvertedValue(TimeZoneInfo.Utc).ToString("o")}\"", // ISO 8601 format
                RecordValue recordValue => FormatRecordValue(recordValue),
                TableValue tableValue => FormatTableValue(tableValue),
                _ => throw new ArgumentException("Unsupported FormulaValue type")
            };
        }

    }
}
