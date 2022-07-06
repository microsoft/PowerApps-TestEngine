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
            var selectOneParamFunction = new SelectOneParamFunction(MockPowerAppFunctions.Object, () => Task.CompletedTask);
            var selectTwoParamsFunction = new SelectTwoParamsFunction(MockPowerAppFunctions.Object, () => Task.CompletedTask);
            var selectThreeParamsFunction = new SelectThreeParamsFunction(MockPowerAppFunctions.Object, () => Task.CompletedTask);

            Assert.ThrowsAny<Exception>(() => selectOneParamFunction.Execute(null));
            Assert.ThrowsAny<Exception>(() => selectTwoParamsFunction.Execute(null, null));
            Assert.ThrowsAny<Exception>(() => selectThreeParamsFunction.Execute(null, null, null));
        }

        [Fact]
        public void SelectFunctionThrowsOnNonPowerAppsRecordValuetTest()
        {
            var recordType = new RecordType().Add("Text", FormulaType.String);
            var selectOneParamFunction = new SelectOneParamFunction(MockPowerAppFunctions.Object, () => Task.CompletedTask);
            var selectTwoParamsFunction = new SelectTwoParamsFunction(MockPowerAppFunctions.Object, () => Task.CompletedTask);
            var selectThreeParamsFunction = new SelectThreeParamsFunction(MockPowerAppFunctions.Object, () => Task.CompletedTask);

            var someOtherRecordValue = new SomeOtherRecordValue(recordType);
            Assert.ThrowsAny<Exception>(() => selectOneParamFunction.Execute(someOtherRecordValue));
            Assert.ThrowsAny<Exception>(() => selectTwoParamsFunction.Execute(someOtherRecordValue, It.IsAny<NumberValue>()));
            Assert.ThrowsAny<Exception>(() => selectThreeParamsFunction.Execute(someOtherRecordValue, It.IsAny<NumberValue>(), It.IsAny<RecordValue>()));
        }

        [Fact]
        public void SelectOneParamFunctionTest()
        {
            MockPowerAppFunctions.Setup(x => x.SelectControlAsync(It.IsAny<ItemPath>())).Returns(Task.FromResult(true));
            var recordType = new RecordType().Add("Text", FormulaType.String);

            var recordValue = new ControlRecordValue(recordType, MockPowerAppFunctions.Object, "Button1");

            var updaterFunctionCallCount = 0;
            var updaterFunction = () => {
                updaterFunctionCallCount++;
                return Task.CompletedTask;
            };
            var selectFunction = new SelectOneParamFunction(MockPowerAppFunctions.Object, updaterFunction);
            var result = selectFunction.Execute(recordValue);
            Assert.IsType<BlankValue>(result);
            MockPowerAppFunctions.Verify(x => x.SelectControlAsync(It.Is<ItemPath>((item) => item.ControlName == recordValue.Name)), Times.Once());
            Assert.Equal(1, updaterFunctionCallCount);
        }

        [Fact]
        public void SelectTwoParamFunctionTest()
        {
            MockPowerAppFunctions.Setup(x => x.SelectControlAsync(It.IsAny<ItemPath>())).Returns(Task.FromResult(true));
            var recordType = new RecordType().Add("Gallery1", new RecordType());

            var recordValue = new ControlRecordValue(recordType, MockPowerAppFunctions.Object, "Gallery1");
            var rowOrColumn = NumberValue.New(1.0);

            var updaterFunctionCallCount = 0;
            var updaterFunction = () => {
                updaterFunctionCallCount++;
                return Task.CompletedTask;
            };
            var selectFunction = new SelectTwoParamsFunction(MockPowerAppFunctions.Object, updaterFunction);           

            var result = selectFunction.Execute(recordValue, rowOrColumn);
            Assert.IsType<BlankValue>(result);
            MockPowerAppFunctions.Verify(x => x.SelectControlAsync(It.Is<ItemPath>((item) => item.ControlName == recordValue.Name)), Times.Once());
            Assert.Equal(1, updaterFunctionCallCount);
        }

        [Fact]
        public void SelectThreeParamFunctionTest()
        {
            MockPowerAppFunctions.Setup(x => x.SelectControlAsync(It.IsAny<ItemPath>())).Returns(Task.FromResult(true));
            var parentRecordType = new RecordType().Add("Gallery1", new RecordType());
            var childRecordType = new RecordType().Add("Button1", new RecordType());

            var parentValue = new ControlRecordValue(parentRecordType, MockPowerAppFunctions.Object, "Gallery1");
            var rowOrColumn = NumberValue.New(1.0);
            var childValue = new ControlRecordValue(childRecordType, MockPowerAppFunctions.Object, "Button1");

            var updaterFunctionCallCount = 0;
            var updaterFunction = () => {
                updaterFunctionCallCount++;
                return Task.CompletedTask;
            };
            var selectFunction = new SelectThreeParamsFunction(MockPowerAppFunctions.Object, updaterFunction);
            var result = selectFunction.Execute(parentValue, rowOrColumn, childValue);
            Assert.IsType<BlankValue>(result);
            MockPowerAppFunctions.Verify(x => x.SelectControlAsync(It.Is<ItemPath>((item) => item.ControlName == childValue.Name)), Times.Once());
            Assert.Equal(1, updaterFunctionCallCount);
        }

        [Fact]
        public void SelectOneParamFunctionFailsTest()
        {
            MockPowerAppFunctions.Setup(x => x.SelectControlAsync(It.IsAny<ItemPath>())).Returns(Task.FromResult(false));
            var recordType = new RecordType().Add("Text", FormulaType.String);

            var recordValue = new ControlRecordValue(recordType, MockPowerAppFunctions.Object, "Button1");

            var updaterFunctionCallCount = 0;
            var updaterFunction = () => {
                updaterFunctionCallCount++;
                return Task.CompletedTask;
            };

            var selectFunction = new SelectOneParamFunction(MockPowerAppFunctions.Object, updaterFunction); 
            Assert.ThrowsAny<Exception>(() => selectFunction.Execute(recordValue));
            MockPowerAppFunctions.Verify(x => x.SelectControlAsync(It.Is<ItemPath>((item) => item.ControlName == recordValue.Name)), Times.Once());
            Assert.Equal(0, updaterFunctionCallCount);
        }                

        [Fact]
        public void SelectTwoParamFunctionFailsTest()
        {
            MockPowerAppFunctions.Setup(x => x.SelectControlAsync(It.IsAny<ItemPath>())).Returns(Task.FromResult(false));
            var recordType = new RecordType().Add("Gallery1", new RecordType());

            var recordValue = new ControlRecordValue(recordType, MockPowerAppFunctions.Object, "Gallery1");
            var rowOrColumn = NumberValue.New(1.0);

            var updaterFunctionCallCount = 0;
            var updaterFunction = () => {
                updaterFunctionCallCount++;
                return Task.CompletedTask;
            };
            var selectFunction = new SelectTwoParamsFunction(MockPowerAppFunctions.Object, updaterFunction);

            Assert.ThrowsAny<Exception>(() => selectFunction.Execute(recordValue, rowOrColumn));
            MockPowerAppFunctions.Verify(x => x.SelectControlAsync(It.Is<ItemPath>((item) => item.ControlName == recordValue.Name)), Times.Once());
            Assert.Equal(0, updaterFunctionCallCount);
        }       

        [Fact]
        public void SelectThreeParamFunctionFailsTest()
        {
            MockPowerAppFunctions.Setup(x => x.SelectControlAsync(It.IsAny<ItemPath>())).Returns(Task.FromResult(false));
            var parentRecordType = new RecordType().Add("Gallery1", new RecordType());
            var childRecordType = new RecordType().Add("Button1", new RecordType());

            var parentValue = new ControlRecordValue(parentRecordType, MockPowerAppFunctions.Object, "Gallery1");
            var rowOrColumn = NumberValue.New(1.0);
            var childValue = new ControlRecordValue(childRecordType, MockPowerAppFunctions.Object, "Button1");

            var updaterFunctionCallCount = 0;
            var updaterFunction = () => {
                updaterFunctionCallCount++;
                return Task.CompletedTask;
            };
            var selectFunction = new SelectThreeParamsFunction(MockPowerAppFunctions.Object, updaterFunction);
            Assert.ThrowsAny<Exception>(() => selectFunction.Execute(parentValue, rowOrColumn, childValue));
            MockPowerAppFunctions.Verify(x => x.SelectControlAsync(It.Is<ItemPath>((item) => item.ControlName == childValue.Name)), Times.Once());
            Assert.Equal(0, updaterFunctionCallCount);
        }
    }
}
