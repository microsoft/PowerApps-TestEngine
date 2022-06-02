// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerApps.TestEngine.PowerApps;
using Microsoft.PowerApps.TestEngine.PowerFx.Functions;
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

        public SelectFunctionTests()
        {
            MockPowerAppFunctions = new Mock<IPowerAppFunctions>(MockBehavior.Strict);
        }

        [Fact]
        public void SelectFunctionThrowsOnNullObjectTest()
        {
            var selectFunction = new SelectFunction(MockPowerAppFunctions.Object);
            Assert.ThrowsAny<Exception>(() => selectFunction.Execute(null));
        }

        [Fact]
        public void SelectFunctionThrowsOnNonPowerAppObjectObjectTest()
        {
            var selectFunction = new SelectFunction(MockPowerAppFunctions.Object);

            var someOtherUntypedObject = new SomeOtherUntypedObject();
            var untypedObject = FormulaValue.New(someOtherUntypedObject);
            Assert.ThrowsAny<Exception>(() => selectFunction.Execute(untypedObject));
        }

        [Fact]
        public void SelectFunctionTest()
        {
            MockPowerAppFunctions.Setup(x => x.SelectControlAsync(It.IsAny<ItemPath>())).Returns(Task.FromResult(true));

            var powerAppObject = new PowerAppControlModel("Label1", new Dictionary<string, FormulaType>() { { "Text", FormulaType.String } }, MockPowerAppFunctions.Object);
            var untypedObject = FormulaValue.New(powerAppObject);

            var selectFunction = new SelectFunction(MockPowerAppFunctions.Object);
            var result = selectFunction.Execute(untypedObject);
            Assert.IsType<BlankValue>(result);
            MockPowerAppFunctions.Verify(x => x.SelectControlAsync(It.Is<ItemPath>((item) => item.ControlName == powerAppObject.Name)), Times.Once());
        }

        [Fact]
        public void SelectFunctionFailsTest()
        {
            MockPowerAppFunctions.Setup(x => x.SelectControlAsync(It.IsAny<ItemPath>())).Returns(Task.FromResult(false));

            var powerAppObject = new PowerAppControlModel("Label1", new Dictionary<string, FormulaType>() { { "Text", FormulaType.String } }, MockPowerAppFunctions.Object);
            var untypedObject = FormulaValue.New(powerAppObject);

            var selectFunction = new SelectFunction(MockPowerAppFunctions.Object);
            Assert.ThrowsAny<Exception>(() => selectFunction.Execute(untypedObject));
            MockPowerAppFunctions.Verify(x => x.SelectControlAsync(It.Is<ItemPath>((item) => item.ControlName == powerAppObject.Name)), Times.Once());
        }
    }
}
