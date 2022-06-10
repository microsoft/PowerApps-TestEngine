// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.PowerApps;
using Microsoft.PowerApps.TestEngine.PowerApps.PowerFxModel;
using Microsoft.PowerApps.TestEngine.PowerFx;
using Microsoft.PowerApps.TestEngine.PowerFx.Functions;
using Microsoft.PowerApps.TestEngine.Tests.Helpers;
using Microsoft.PowerFx.Types;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.PowerFx.Functions
{
    public class SelectFunctionTests
    {
        private Mock<IPowerAppFunctions> MockPowerAppFunctions;
        private Mock<ITestState> MockTestState;

        public SelectFunctionTests()
        {
            MockPowerAppFunctions = new Mock<IPowerAppFunctions>(MockBehavior.Strict);
            MockTestState = new Mock<ITestState>(MockBehavior.Strict);
        }

        [Fact]
        public void SelectFunctionThrowsOnNullObjectTest()
        {
            var recordType = new RecordType().Add("Text", FormulaType.String);
            var selectFunction = new SelectFunction(MockPowerAppFunctions.Object, recordType);
            Assert.ThrowsAny<Exception>(() => selectFunction.Execute(null));
        }

        [Fact]
        public void SelectFunctionThrowsOnNonPowerAppsRecordValuetTest()
        {
            var recordType = new RecordType().Add("Text", FormulaType.String);
            var selectFunction = new SelectFunction(MockPowerAppFunctions.Object, recordType);

            var someOtherRecordValue = new SomeOtherRecordValue(recordType);
            Assert.ThrowsAny<Exception>(() => selectFunction.Execute(someOtherRecordValue));
        }

        [Fact]
        public void SelectFunctionTest()
        {
            MockPowerAppFunctions.Setup(x => x.SelectControlAsync(It.IsAny<ItemPath>())).Returns(Task.FromResult(true));
            var recordType = new RecordType().Add("Text", FormulaType.String);

            var recordValue = new ControlRecordValue(recordType, MockPowerAppFunctions.Object, "Button1");

            var selectFunction = new SelectFunction(MockPowerAppFunctions.Object, recordType);
            var result = selectFunction.Execute(recordValue);
            Assert.IsType<BlankValue>(result);
            MockPowerAppFunctions.Verify(x => x.SelectControlAsync(It.Is<ItemPath>((item) => item.ControlName == recordValue.Name)), Times.Once());
        }

        [Fact]
        public void SelectFunctionFailsTest()
        {
            MockPowerAppFunctions.Setup(x => x.SelectControlAsync(It.IsAny<ItemPath>())).Returns(Task.FromResult(false));
            var recordType = new RecordType().Add("Text", FormulaType.String);

            var recordValue = new ControlRecordValue(recordType, MockPowerAppFunctions.Object, "Button1");

            var selectFunction = new SelectFunction(MockPowerAppFunctions.Object, recordType); 
            Assert.ThrowsAny<Exception>(() => selectFunction.Execute(recordValue));
            MockPowerAppFunctions.Verify(x => x.SelectControlAsync(It.Is<ItemPath>((item) => item.ControlName == recordValue.Name)), Times.Once());
        }
    }
}
