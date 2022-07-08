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
    public class SetPropertyFunctionTests
    {
        private Mock<IPowerAppFunctions> MockPowerAppFunctions;
        private Mock<ITestState> MockTestState;

        public SetPropertyFunctionTests()
        {
            MockPowerAppFunctions = new Mock<IPowerAppFunctions>(MockBehavior.Strict);
            MockTestState = new Mock<ITestState>(MockBehavior.Strict);
        }

        [Fact]
        public void SetPropertyFunctionThrowsOnNonPowerAppsRecordValueTest()
        {
            var recordType = new RecordType().Add("Text", FormulaType.String);
            var SetPropertyFunction = new SetPropertyFunction(MockPowerAppFunctions.Object, FormulaType.String);
            var someOtherRecordValue = new SomeOtherRecordValue(recordType);

            Assert.ThrowsAny<Exception>(() => SetPropertyFunction.Execute(someOtherRecordValue, StringValue.New("Test"), StringValue.New("10")));
        }

        [Fact]
        public void SetPropertyFunctionTest()
        {
            MockPowerAppFunctions.Setup(x => x.SetPropertyAsync(It.IsAny<ItemPath>(), It.IsAny<StringValue>())).Returns(Task.FromResult(true));

            // Make setPropertyFunction contain a text component called Button1
            var recordType = new RecordType().Add("Text", FormulaType.String);
            var recordValue = new ControlRecordValue(recordType, MockPowerAppFunctions.Object, "Button1");
            var setPropertyFunction = new SetPropertyFunction(MockPowerAppFunctions.Object, FormulaType.String);

            // Set the value of Button1's 'Text' property to 5
            var result = setPropertyFunction.Execute(recordValue, StringValue.New("Text"), StringValue.New("5"));

            // check to see if the value of Button1's 'Text' property is 5
            Assert.IsType<BlankValue>(result);
            MockPowerAppFunctions.Verify(x => x.SetPropertyAsync(It.Is<ItemPath>((item) => item.ControlName == recordValue.Name), It.Is<StringValue>(stringVal => stringVal.Value == "5")), Times.Once());

            // Set the value of Button1's 'Text' property to 10 
            result = setPropertyFunction.Execute(recordValue, StringValue.New("Text"), StringValue.New("10"));

            // check to see if the value of Button1's 'Text' property is 10
            Assert.IsType<BlankValue>(result);
            MockPowerAppFunctions.Verify(x => x.SetPropertyAsync(It.Is<ItemPath>((item) => item.ControlName == recordValue.Name), It.Is<StringValue>(stringVal => stringVal.Value == "10")), Times.Once());

            // Set the value of Button1's 'Text' property to 'abc'
            result = setPropertyFunction.Execute(recordValue, StringValue.New("Text"), StringValue.New("abc"));

            // check to see if the value of Button1's 'Text' property is abc
            Assert.IsType<BlankValue>(result);
            MockPowerAppFunctions.Verify(x => x.SetPropertyAsync(It.Is<ItemPath>((item) => item.ControlName == recordValue.Name), It.Is<StringValue>(stringVal => stringVal.Value == "abc")), Times.Once());
        }
    }
}
