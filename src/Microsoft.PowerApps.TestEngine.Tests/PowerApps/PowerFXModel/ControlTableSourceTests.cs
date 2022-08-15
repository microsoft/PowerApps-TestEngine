// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerApps.TestEngine.PowerApps;
using Microsoft.PowerApps.TestEngine.PowerApps.PowerFxModel;
using Microsoft.PowerFx.Types;
using Moq;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.PowerApps.PowerFXModel
{
    public class ControlTableSourceTests
    {
        [Fact]
        public void TableSourceTest()
        {
            var mockPowerAppFunctions = new Mock<IPowerAppFunctions>(MockBehavior.Strict);
            var itemPath = new ItemPath()
            {
                ControlName = "Gallery1",
                PropertyName = "AllItems"
            };

            var itemCount = 3;
            mockPowerAppFunctions.Setup(x => x.GetItemCount(It.IsAny<ItemPath>())).Returns(itemCount);
            var recordType = RecordType.Empty().Add("Label1", RecordType.Empty().Add("Text", FormulaType.String));
            var controlTableSource = new ControlTableSource(mockPowerAppFunctions.Object, itemPath, recordType);
            Assert.Equal(itemCount, controlTableSource.Count);

            for (var i = 0; i < itemCount; i++)
            {
                var row = controlTableSource[i];
                Assert.Equal(i, row.ItemPath.Index);
                Assert.Equal(itemPath.ControlName, row.ItemPath.ControlName);
                Assert.Equal(itemPath.PropertyName, row.ItemPath.PropertyName);
            }
        }
    }
}
