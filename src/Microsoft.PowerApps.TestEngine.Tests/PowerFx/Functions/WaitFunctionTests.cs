// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.PowerApps;
using Microsoft.PowerApps.TestEngine.PowerApps.PowerFxModel;
using Microsoft.PowerApps.TestEngine.PowerFx.Functions;
using Microsoft.PowerApps.TestEngine.Tests.Helpers;
using Microsoft.PowerFx.Types;
using Moq;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.PowerFx.Functions
{
    public class WaitFunctionTests
    {
        private Mock<IPowerAppFunctions> MockPowerAppFunctions;
        private Mock<ITestState> MockTestState;
        
        private int Timeout;

        public WaitFunctionTests()
        {
            Timeout = 30000;
            MockPowerAppFunctions = new Mock<IPowerAppFunctions>(MockBehavior.Strict);
            MockTestState = new Mock<ITestState>(MockBehavior.Strict);
        }

        [Fact]
        public void WaitFunctionThrowsOnInvalidArgumentsTest()
        {
            var recordType = new RecordType().Add("Text", FormulaType.String);
            var waitFunction = new WaitFunction(Timeout, recordType);
            var recordValue = new ControlRecordValue(recordType, MockPowerAppFunctions.Object, "Label1");
            Assert.Throws<ArgumentNullException>(() => waitFunction.Execute(null, FormulaValue.New("Text"), FormulaValue.New("1")));
            Assert.Throws<ArgumentNullException>(() => waitFunction.Execute(new SomeOtherRecordValue(recordType), null, FormulaValue.New("1")));
            Assert.Throws<ArgumentNullException>(() => waitFunction.Execute(new SomeOtherRecordValue(recordType), FormulaValue.New("Text"), null));
            Assert.Throws<InvalidCastException>(() => waitFunction.Execute(new SomeOtherRecordValue(recordType), FormulaValue.New("Text"), FormulaValue.New("1")));
        }

        [Fact]
        public void WaitFunctionSucceedsTest()
        {
            var textToWaitFor = "1";
            var recordType = new RecordType().Add("Text", FormulaType.String);
            var recordValue = new ControlRecordValue(recordType, MockPowerAppFunctions.Object, "Label1");
            var jsPropertyValueModel = new JSPropertyValueModel()
            {
                PropertyValue = textToWaitFor,
            };
            var expectedItemPath = new ItemPath
            {
                ControlName = "Label1",
                PropertyName = "Text"
            };
            MockPowerAppFunctions.Setup(x => x.GetPropertyValueFromControl<string>(It.IsAny<ItemPath>()))
                    .Returns(JsonConvert.SerializeObject(jsPropertyValueModel));
            MockTestState.Setup(x => x.GetTimeout()).Returns(Timeout);

            var waitFunction = new WaitFunction(Timeout, recordType);
            waitFunction.Execute(recordValue, FormulaValue.New("Text"), FormulaValue.New(textToWaitFor));
            
            MockPowerAppFunctions.Verify(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((itemPath) => itemPath.ControlName == expectedItemPath.ControlName && itemPath.PropertyName == expectedItemPath.PropertyName)), Times.Once());
        }

        [Fact]
        public void WaitFunctionWaitsForValueToUpdateTest()
        {
            var textToWaitFor = "1";
            var recordType = new RecordType().Add("Text", FormulaType.String);
            var recordValue = new ControlRecordValue(recordType, MockPowerAppFunctions.Object, "Label1");
            var jsPropertyValueModel = new JSPropertyValueModel()
            {
                PropertyValue = "0",
            };
            var finalJsPropertyValueModel = new JSPropertyValueModel()
            {
                PropertyValue = textToWaitFor,
            };
            var expectedItemPath = new ItemPath
            {
                ControlName = "Label1",
                PropertyName = "Text"
            };
            MockPowerAppFunctions.SetupSequence(x => x.GetPropertyValueFromControl<string>(It.IsAny<ItemPath>()))
                    .Returns(JsonConvert.SerializeObject(jsPropertyValueModel))
                    .Returns(JsonConvert.SerializeObject(jsPropertyValueModel))
                    .Returns(JsonConvert.SerializeObject(finalJsPropertyValueModel));
            MockTestState.Setup(x => x.GetTimeout()).Returns(Timeout);

            var waitFunction = new WaitFunction(Timeout, recordType);
            waitFunction.Execute(recordValue, FormulaValue.New("Text"), FormulaValue.New(textToWaitFor));

            MockPowerAppFunctions.Verify(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((itemPath) => itemPath.ControlName == expectedItemPath.ControlName && itemPath.PropertyName == expectedItemPath.PropertyName)), Times.Exactly(3));
        }

        [Fact]
        public void WaitFunctionTimeoutTest()
        {
            var textToWaitFor = "1";
            var recordType = new RecordType().Add("Text", FormulaType.String);
            var recordValue = new ControlRecordValue(recordType, MockPowerAppFunctions.Object, "Label1");
            var jsPropertyValueModel = new JSPropertyValueModel()
            {
                PropertyValue = "0",
            };
            MockPowerAppFunctions.SetupSequence(x => x.GetPropertyValueFromControl<string>(It.IsAny<ItemPath>()))
                    .Returns(JsonConvert.SerializeObject(jsPropertyValueModel))
                    .Returns(JsonConvert.SerializeObject(jsPropertyValueModel))
                    .Returns(JsonConvert.SerializeObject(jsPropertyValueModel));
            MockTestState.Setup(x => x.GetTimeout()).Returns(Timeout);

            var waitFunction = new WaitFunction(300, recordType); // each trial has 500ms in between
            Assert.Throws<TimeoutException>(() => waitFunction.Execute(recordValue, FormulaValue.New("Text"), FormulaValue.New(textToWaitFor)));
        }
    }
}
