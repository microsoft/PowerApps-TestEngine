// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
using Microsoft.PowerApps.TestEngine.PowerApps;
using Microsoft.PowerApps.TestEngine.PowerApps.PowerFxModel;
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
            Mock<Microsoft.Extensions.Logging.ILogger> MockLogger = new Mock<Microsoft.Extensions.Logging.ILogger>(MockBehavior.Loose);
            var recordType = new RecordType().Add("Text", FormulaType.String).Add("X", FormulaType.Number);
            var mockPowerAppFunctions = new Mock<IPowerAppFunctions>(MockBehavior.Strict);
            var controlName = "Label1";
            var propertyValue = Guid.NewGuid().ToString();
            var numberPropertyValue = 11;
            mockPowerAppFunctions.Setup(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((x) => x.PropertyName == "Text"), MockLogger.Object))
                .Returns(JsonConvert.SerializeObject(new JSPropertyValueModel() { PropertyValue = propertyValue }));
            mockPowerAppFunctions.Setup(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((x) => x.PropertyName == "X"), MockLogger.Object))
                .Returns(JsonConvert.SerializeObject(new JSPropertyValueModel() { PropertyValue = numberPropertyValue.ToString() }));

            var controlRecordValue = new ControlRecordValue(recordType, mockPowerAppFunctions.Object, MockLogger.Object, controlName);
            Assert.Equal(controlName, controlRecordValue.Name);
            Assert.Equal(recordType, controlRecordValue.Type);

            Assert.Equal(controlName, controlRecordValue.GetItemPath().ControlName);
            Assert.Null(controlRecordValue.GetItemPath().Index);
            Assert.Null(controlRecordValue.GetItemPath().PropertyName);
            Assert.Null(controlRecordValue.GetItemPath().ParentControl);
            Assert.Equal("Text", controlRecordValue.GetItemPath("Text").PropertyName);

            Assert.Equal(propertyValue, (controlRecordValue.GetField("Text") as StringValue).Value);
            Assert.Equal(numberPropertyValue, (controlRecordValue.GetField("X") as NumberValue).Value);

            mockPowerAppFunctions.Verify(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((x) => x.PropertyName == "Text" && x.ControlName == controlName), MockLogger.Object), Times.Once());
            mockPowerAppFunctions.Verify(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((x) => x.PropertyName == "X" && x.ControlName == controlName), MockLogger.Object), Times.Once());
        }

        [Fact]
        public void GalleryControlRecordValueTest()
        {
            Mock<Microsoft.Extensions.Logging.ILogger> MockLogger = new Mock<Microsoft.Extensions.Logging.ILogger>(MockBehavior.Loose);
            var labelRecordType = new RecordType().Add("Text", FormulaType.String);
            var labelName = "Label1";
            var galleryAllItemsTableType = new TableType().Add(new NamedFormulaType(labelName, labelRecordType));
            var allItemsName = "AllItems";
            var galleryRecordType = new RecordType().Add(allItemsName, galleryAllItemsTableType);
            var galleryName = "Gallery1";
            var labelText = Guid.NewGuid().ToString();
            var mockPowerAppFunctions = new Mock<IPowerAppFunctions>(MockBehavior.Strict);
            mockPowerAppFunctions.Setup(x => x.GetPropertyValueFromControl<string>(It.IsAny<ItemPath>(), MockLogger.Object))
                .Returns(JsonConvert.SerializeObject(new JSPropertyValueModel() { PropertyValue = labelText }));

            var itemCount = 4;
            mockPowerAppFunctions.Setup(x => x.GetItemCount(It.IsAny<ItemPath>(), MockLogger.Object)).Returns(itemCount);

            var galleryRecordValue = new ControlRecordValue(galleryRecordType, mockPowerAppFunctions.Object, MockLogger.Object, galleryName);
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
            mockPowerAppFunctions.Verify(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((x) => x.PropertyName == "Text" && x.ControlName == labelName), MockLogger.Object), Times.Exactly(itemCount));

        }
        [Fact]
        public void ComponentsControlRecordValueTest()
        {
            Mock<Microsoft.Extensions.Logging.ILogger> MockLogger = new Mock<Microsoft.Extensions.Logging.ILogger>(MockBehavior.Loose);
            var labelRecordType = new RecordType().Add("Text", FormulaType.String);
            var labelName = "Label1";
            var componentRecordType = new RecordType().Add(labelName, labelRecordType);
            var componentName = "Component1";
            var mockPowerAppFunctions = new Mock<IPowerAppFunctions>(MockBehavior.Strict);
            var propertyValue = Guid.NewGuid().ToString();

            mockPowerAppFunctions.Setup(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((x) => x.PropertyName == "Text"), MockLogger.Object))
                .Returns(JsonConvert.SerializeObject(new JSPropertyValueModel() { PropertyValue = propertyValue }));

            var controlRecordValue = new ControlRecordValue(componentRecordType, mockPowerAppFunctions.Object, MockLogger.Object, componentName);
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

            mockPowerAppFunctions.Verify(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((x) => x.PropertyName == "Text" && x.ControlName == labelName), MockLogger.Object), Times.Once());
        }
    }
}
