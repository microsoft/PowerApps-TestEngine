// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.PowerApps;
using Microsoft.PowerApps.TestEngine.PowerFx.Functions;
using Microsoft.PowerApps.TestEngine.Tests.Helpers;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Public.Values;
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
            var selectFunction = new SelectFunction(MockPowerAppFunctions.Object, () => Task.CompletedTask);
            Assert.ThrowsAny<Exception>(() => selectFunction.Execute(null));
        }

        [Fact]
        public void SelectFunctionThrowsOnNonPowerAppObjectObjectTest()
        {
            var selectFunction = new SelectFunction(MockPowerAppFunctions.Object, () => Task.CompletedTask);

            var someOtherUntypedObject = new SomeOtherUntypedObject();
            var untypedObject = FormulaValue.New(someOtherUntypedObject);
            Assert.ThrowsAny<Exception>(() => selectFunction.Execute(untypedObject));
        }

        [Fact]
        public void SelectFunctionTest()
        {
            MockPowerAppFunctions.Setup(x => x.SelectControlAsync(It.IsAny<ItemPath>())).Returns(Task.FromResult(true));

            var powerAppObject = new PowerAppControlModel("Label1", TestData.CreateSamplePropertiesDictionary(), MockPowerAppFunctions.Object, MockTestState.Object);
            var untypedObject = FormulaValue.New(powerAppObject);

            var mockUpdaterCallCount = 0;
            var mockUpdater = () => {
                mockUpdaterCallCount++;
                return Task.CompletedTask;
            };

            var selectFunction = new SelectFunction(MockPowerAppFunctions.Object, mockUpdater);
            var result = selectFunction.Execute(untypedObject);
            Assert.IsType<BlankValue>(result);
            MockPowerAppFunctions.Verify(x => x.SelectControlAsync(It.Is<ItemPath>((item) => item.ControlName == powerAppObject.Name)), Times.Once());
            Assert.Equal(1, mockUpdaterCallCount);
        }

        [Fact]
        public void SelectFunctionFailsTest()
        {
            MockPowerAppFunctions.Setup(x => x.SelectControlAsync(It.IsAny<ItemPath>())).Returns(Task.FromResult(false));

            var powerAppObject = new PowerAppControlModel("Label1", TestData.CreateSamplePropertiesDictionary(), MockPowerAppFunctions.Object, MockTestState.Object);
            var untypedObject = FormulaValue.New(powerAppObject);

            var mockUpdaterCallCount = 0;
            var mockUpdater = () => {
                mockUpdaterCallCount++;
                return Task.CompletedTask;
            };

            var selectFunction = new SelectFunction(MockPowerAppFunctions.Object, mockUpdater);
            Assert.ThrowsAny<Exception>(() => selectFunction.Execute(untypedObject));
            MockPowerAppFunctions.Verify(x => x.SelectControlAsync(It.Is<ItemPath>((item) => item.ControlName == powerAppObject.Name)), Times.Once());
            Assert.Equal(0, mockUpdaterCallCount);
        }
    }
}
