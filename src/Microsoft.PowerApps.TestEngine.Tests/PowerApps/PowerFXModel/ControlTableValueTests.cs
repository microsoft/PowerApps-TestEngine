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
    public class ControlTableValueTests
    {
        [Fact]
        public void TableTest()
        {
            var control1Name = Guid.NewGuid().ToString();
            var control2Name = Guid.NewGuid().ToString();
            var control1PropName = Guid.NewGuid().ToString();
            var control2PropName = Guid.NewGuid().ToString();
            var control1Type = RecordType.Empty().Add(control1PropName, FormulaType.String);
            var control2Type = RecordType.Empty().Add(control2PropName, FormulaType.String);
            var tableType = TableType.Empty().Add(new NamedFormulaType(control1Name, control1Type)).Add(new NamedFormulaType(control2Name, control2Type));
            var mockPowerAppFunctions = new Mock<IPowerAppFunctions>(MockBehavior.Strict);
            var tableCount = 5;
            var control1PropertyValue = Guid.NewGuid().ToString();
            var control2PropertyValue = Guid.NewGuid().ToString();
            mockPowerAppFunctions.Setup(x => x.GetItemCount(It.IsAny<ItemPath>())).Returns(tableCount);
            mockPowerAppFunctions.Setup(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>(x => x.PropertyName == control1PropName)))
                .Returns(JsonConvert.SerializeObject(new JSPropertyValueModel() { PropertyValue = control1PropertyValue }));
            mockPowerAppFunctions.Setup(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>(x => x.PropertyName == control2PropName)))
                .Returns(JsonConvert.SerializeObject(new JSPropertyValueModel() { PropertyValue = control2PropertyValue }));

            var itemPath = new ItemPath()
            {
                ControlName = "Gallery1",
                PropertyName = "AllItems"
            };

            var recordType = tableType.ToRecord();
            var tableSource = new ControlTableSource(mockPowerAppFunctions.Object, itemPath, recordType);
            var tableValue = new ControlTableValue(recordType, tableSource, mockPowerAppFunctions.Object);

            Assert.Equal(recordType, tableValue.RecordType);
            Assert.Equal(tableCount, tableValue.Rows.Count());

            for (var i = 0; i < tableCount; i++)
            {
                var row = tableValue.Rows.ToArray()[i];
                Assert.Equal(recordType, row.Value.Type);
                var rowRecordValue = row.Value as ControlRecordValue;
                Assert.NotNull(rowRecordValue);
                var rowItemPath = rowRecordValue.GetItemPath();
                Assert.NotNull(rowItemPath.ParentControl);
                Assert.Equal(i, rowItemPath.ParentControl.Index);
                Assert.Equal(itemPath.ControlName, rowItemPath.ParentControl.ControlName);
                Assert.Equal(itemPath.PropertyName, rowItemPath.ParentControl.PropertyName);
                Assert.Null(rowRecordValue.Name);
                Assert.Null(rowItemPath.ControlName);

                var control1Value = rowRecordValue.GetField(control1Name);
                Assert.NotNull(control1Value);
                var control1RecordValue = control1Value as ControlRecordValue;
                Assert.NotNull(control1RecordValue);
                var control1PropValue = control1RecordValue.GetField(control1PropName);
                Assert.NotNull(control1PropValue);
                Assert.Equal(control1PropertyValue, (control1PropValue as StringValue).Value);

                var control2Value = rowRecordValue.GetField(control2Name);
                Assert.NotNull(control2Value);
                var control2RecordValue = control2Value as ControlRecordValue;
                Assert.NotNull(control2RecordValue);
                var control2PropValue = control2RecordValue.GetField(control2PropName);
                Assert.NotNull(control2PropValue);
                Assert.Equal(control2PropertyValue, (control2PropValue as StringValue).Value);
            }

            mockPowerAppFunctions.Verify(x => x.GetItemCount(It.IsAny<ItemPath>()), Times.AtLeastOnce());
            mockPowerAppFunctions.Verify(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>(x => x.PropertyName == control1PropName)), Times.Exactly(tableCount));
            mockPowerAppFunctions.Verify(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>(x => x.PropertyName == control2PropName)), Times.Exactly(tableCount));
        }
    }
}
