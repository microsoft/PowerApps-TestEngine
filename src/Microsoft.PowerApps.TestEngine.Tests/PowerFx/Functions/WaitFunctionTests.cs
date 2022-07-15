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
        public void WaitFunctionStringThrowsOnInvalidArgumentsTest()
        {
            var recordType = new RecordType().Add("Text", FormulaType.String);
            var waitFunction = new WaitFunctionString(Timeout);
            var recordValue = new ControlRecordValue(recordType, MockPowerAppFunctions.Object, "Label1");
            Assert.Throws<ArgumentNullException>(() => waitFunction.Execute(null, FormulaValue.New("Text"), FormulaValue.New("1")));
            Assert.Throws<ArgumentNullException>(() => waitFunction.Execute(new SomeOtherRecordValue(recordType), null, FormulaValue.New("1")));
            Assert.Throws<ArgumentNullException>(() => waitFunction.Execute(new SomeOtherRecordValue(recordType), FormulaValue.New("Text"), null));
            Assert.Throws<InvalidCastException>(() => waitFunction.Execute(new SomeOtherRecordValue(recordType), FormulaValue.New("Text"), FormulaValue.New("1")));
        }

        [Fact]
        public void WaitFunctionNumberThrowsOnInvalidArgumentsTest()
        {
            var recordType = new RecordType().Add("Text", FormulaType.String);
            var waitFunction = new WaitFunctionNumber(Timeout);
            var recordValue = new ControlRecordValue(recordType, MockPowerAppFunctions.Object, "Label1");
            Assert.Throws<ArgumentNullException>(() => waitFunction.Execute(null, FormulaValue.New("Text"), FormulaValue.New(1)));
            Assert.Throws<ArgumentNullException>(() => waitFunction.Execute(new SomeOtherRecordValue(recordType), null, FormulaValue.New(1)));
            Assert.Throws<ArgumentNullException>(() => waitFunction.Execute(new SomeOtherRecordValue(recordType), FormulaValue.New("Text"), null));
            Assert.Throws<InvalidCastException>(() => waitFunction.Execute(new SomeOtherRecordValue(recordType), FormulaValue.New("Text"), FormulaValue.New(1)));
        }

        [Fact]
        public void WaitFunctionBooleanThrowsOnInvalidArgumentsTest()
        {
            var recordType = new RecordType().Add("Text", FormulaType.String);
            var waitFunction = new WaitFunctionBoolean(Timeout);
            var recordValue = new ControlRecordValue(recordType, MockPowerAppFunctions.Object, "Label1");
            Assert.Throws<ArgumentNullException>(() => waitFunction.Execute(null, FormulaValue.New("Text"), FormulaValue.New(false)));
            Assert.Throws<ArgumentNullException>(() => waitFunction.Execute(new SomeOtherRecordValue(recordType), null, FormulaValue.New(false)));
            Assert.Throws<ArgumentNullException>(() => waitFunction.Execute(new SomeOtherRecordValue(recordType), FormulaValue.New("Text"), null));
            Assert.Throws<InvalidCastException>(() => waitFunction.Execute(new SomeOtherRecordValue(recordType), FormulaValue.New("Text"), FormulaValue.New(false)));
        }

        [Fact]
        public void WaitFunctionDateThrowsOnInvalidArgumentsTest()
        {
            var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).ToLocalTime();
            var recordType = new RecordType().Add("Text", FormulaType.String);
            var waitFunction = new WaitFunctionDate(Timeout);
            var recordValue = new ControlRecordValue(recordType, MockPowerAppFunctions.Object, "Label1");
            Assert.Throws<ArgumentNullException>(() => waitFunction.Execute(null, FormulaValue.New("Text"), FormulaValue.NewDateOnly(dateTime)));
            Assert.Throws<ArgumentNullException>(() => waitFunction.Execute(new SomeOtherRecordValue(recordType), null, FormulaValue.NewDateOnly(dateTime)));
            Assert.Throws<ArgumentNullException>(() => waitFunction.Execute(new SomeOtherRecordValue(recordType), FormulaValue.New("Text"), null));
            Assert.Throws<InvalidCastException>(() => waitFunction.Execute(new SomeOtherRecordValue(recordType), FormulaValue.New("Text"), FormulaValue.NewDateOnly(dateTime)));
        }

        [Fact]
        public void WaitFunctionStringSucceedsTest()
        {
            var valueToWaitFor = "1";
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

            var waitFunction = new WaitFunctionString(Timeout);
            waitFunction.Execute(recordValue, FormulaValue.New("Text"), FormulaValue.New(valueToWaitFor));
            
            MockPowerAppFunctions.Verify(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((itemPath) => itemPath.ControlName == expectedItemPath.ControlName && itemPath.PropertyName == expectedItemPath.PropertyName)), Times.Once());
        }

        [Fact]
        public void WaitFunctionNumberSucceedsTest()
        {
            var valueToWaitFor = 1;
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

            var waitFunction = new WaitFunctionNumber(Timeout);
            waitFunction.Execute(recordValue, FormulaValue.New("Text"), FormulaValue.New(valueToWaitFor));
            
            MockPowerAppFunctions.Verify(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((itemPath) => itemPath.ControlName == expectedItemPath.ControlName && itemPath.PropertyName == expectedItemPath.PropertyName)), Times.Once());
        }

        [Fact]
        public void WaitFunctionBooleanSucceedsTest()
        {
            var valueToWaitFor = false;
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

            var waitFunction = new WaitFunctionBoolean(Timeout);
            waitFunction.Execute(recordValue, FormulaValue.New("Text"), FormulaValue.New(valueToWaitFor));
            
            MockPowerAppFunctions.Verify(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((itemPath) => itemPath.ControlName == expectedItemPath.ControlName && itemPath.PropertyName == expectedItemPath.PropertyName)), Times.Once());
        }

        [Fact]
        public void WaitFunctionDateSucceedsTest()
        {
            var valueToWaitFor =  new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).ToLocalTime();
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

            var waitFunction = new WaitFunctionDate(Timeout);
            waitFunction.Execute(recordValue, FormulaValue.New("Text"), FormulaValue.NewDateOnly(valueToWaitFor));
            
            MockPowerAppFunctions.Verify(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((itemPath) => itemPath.ControlName == expectedItemPath.ControlName && itemPath.PropertyName == expectedItemPath.PropertyName)), Times.Once());
        }

        [Fact]
        public void WaitFunctionStringWaitsForValueToUpdateTest()
        {
            var valueToWaitFor = "1";
            var recordType = new RecordType().Add("Text", FormulaType.String);
            var recordValue = new ControlRecordValue(recordType, MockPowerAppFunctions.Object, "Label1");
            var jsPropertyValueModel = new JSPropertyValueModel()
            {
                PropertyValue = "0",
            };
            var finalJsPropertyValueModel = new JSPropertyValueModel()
            {
                PropertyValue = valueToWaitFor,
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

            var waitFunction = new WaitFunctionString(Timeout);
            waitFunction.Execute(recordValue, FormulaValue.New("Text"), FormulaValue.New(valueToWaitFor));

            MockPowerAppFunctions.Verify(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((itemPath) => itemPath.ControlName == expectedItemPath.ControlName && itemPath.PropertyName == expectedItemPath.PropertyName)), Times.Exactly(3));
        }

        
        [Fact]
        public void WaitFunctionNumberWaitsForValueToUpdateTest()
        {
            var valueToWaitFor = 1;
            var recordType = new RecordType().Add("Text", FormulaType.String);
            var recordValue = new ControlRecordValue(recordType, MockPowerAppFunctions.Object, "Label1");
            var jsPropertyValueModel = new JSPropertyValueModel()
            {
                PropertyValue = "0",
            };
            var finalJsPropertyValueModel = new JSPropertyValueModel()
            {
                PropertyValue = valueToWaitFor,
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

            var waitFunction = new WaitFunctionNumber(Timeout);
            waitFunction.Execute(recordValue, FormulaValue.New("Text"), FormulaValue.New(valueToWaitFor));

            MockPowerAppFunctions.Verify(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((itemPath) => itemPath.ControlName == expectedItemPath.ControlName && itemPath.PropertyName == expectedItemPath.PropertyName)), Times.Exactly(3));
        }

        
        [Fact]
        public void WaitFunctionBooleanWaitsForValueToUpdateTest()
        {
            var valueToWaitFor = false;
            var recordType = new RecordType().Add("Text", FormulaType.String);
            var recordValue = new ControlRecordValue(recordType, MockPowerAppFunctions.Object, "Label1");
            var jsPropertyValueModel = new JSPropertyValueModel()
            {
                PropertyValue = "0",
            };
            var finalJsPropertyValueModel = new JSPropertyValueModel()
            {
                PropertyValue = valueToWaitFor,
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

            var waitFunction = new WaitFunctionBoolean(Timeout);
            waitFunction.Execute(recordValue, FormulaValue.New("Text"), FormulaValue.New(valueToWaitFor));

            MockPowerAppFunctions.Verify(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((itemPath) => itemPath.ControlName == expectedItemPath.ControlName && itemPath.PropertyName == expectedItemPath.PropertyName)), Times.Exactly(3));
        }

        
        [Fact]
        public void WaitFunctionDateWaitsForValueToUpdateTest()
        {
            var valueToWaitFor =  new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).ToLocalTime();
            var recordType = new RecordType().Add("Text", FormulaType.String);
            var recordValue = new ControlRecordValue(recordType, MockPowerAppFunctions.Object, "Label1");
            var jsPropertyValueModel = new JSPropertyValueModel()
            {
                PropertyValue = "0",
            };
            var finalJsPropertyValueModel = new JSPropertyValueModel()
            {
                PropertyValue = valueToWaitFor,
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

            var waitFunction = new WaitFunctionDate(Timeout);
            waitFunction.Execute(recordValue, FormulaValue.New("Text"), FormulaValue.NewDateOnly(valueToWaitFor));

            MockPowerAppFunctions.Verify(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((itemPath) => itemPath.ControlName == expectedItemPath.ControlName && itemPath.PropertyName == expectedItemPath.PropertyName)), Times.Exactly(3));
        }

        [Fact]
        public void WaitFunctionStringTimeoutTest()
        {
            var valueToWaitFor = "1";
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

            var waitFunction = new WaitFunctionString(300); // each trial has 500ms in between
            Assert.Throws<TimeoutException>(() => waitFunction.Execute(recordValue, FormulaValue.New("Text"), FormulaValue.New(valueToWaitFor)));
        }


        [Fact]
        public void WaitFunctionNumberTimeoutTest()
        {
            var valueToWaitFor = 1;
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

            var waitFunction = new WaitFunctionNumber(300); // each trial has 500ms in between
            Assert.Throws<TimeoutException>(() => waitFunction.Execute(recordValue, FormulaValue.New("Text"), FormulaValue.New(valueToWaitFor)));
        }

        [Fact]
        public void WaitFunctionBooleanTimeoutTest()
        {
            var valueToWaitFor = false;
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

            var waitFunction = new WaitFunctionBoolean(300); // each trial has 500ms in between
            Assert.Throws<TimeoutException>(() => waitFunction.Execute(recordValue, FormulaValue.New("Text"), FormulaValue.New(valueToWaitFor)));
        }


        [Fact]
        public void WaitFunctionDateTimeoutTest()
        {
            var valueToWaitFor =  new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).ToLocalTime();
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

            var waitFunction = new WaitFunctionDate(300); // each trial has 500ms in between
            Assert.Throws<TimeoutException>(() => waitFunction.Execute(recordValue, FormulaValue.New("Text"), FormulaValue.NewDateOnly(valueToWaitFor)));
        }
    }
}
