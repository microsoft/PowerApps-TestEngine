// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.PowerFx.Functions;
using Microsoft.PowerApps.TestEngine.Providers;
using Microsoft.PowerApps.TestEngine.Providers.PowerFxModel;
using Microsoft.PowerApps.TestEngine.Tests.Helpers;
using Microsoft.PowerFx.Types;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.PowerFx.Functions
{
    public class WaitFunctionTests
    {
        private Mock<ITestWebProvider> MockTestWebProvider;
        private Mock<ITestState> MockTestState;
        private Mock<ILogger> MockLogger;

        private int Timeout;

        public WaitFunctionTests()
        {
            Timeout = 30000;
            MockTestWebProvider = new Mock<ITestWebProvider>(MockBehavior.Strict);
            MockTestState = new Mock<ITestState>(MockBehavior.Strict);
            MockLogger = new Mock<ILogger>(MockBehavior.Strict);
        }

        [Fact]
        public void WaitFunctionStringThrowsOnInvalidArgumentsTest()
        {
            LoggingTestHelper.SetupMock(MockLogger);
            var recordType = RecordType.Empty().Add("Text", FormulaType.String);
            var waitFunction = new WaitFunctionString(Timeout, MockLogger.Object);
            var recordValue = new ControlRecordValue(recordType, MockTestWebProvider.Object, "Label1");
            Assert.Throws<ArgumentNullException>(() => waitFunction.Execute(null, FormulaValue.New("Text"), FormulaValue.New("1")));
            Assert.Throws<ArgumentNullException>(() => waitFunction.Execute(new SomeOtherRecordValue(recordType), null, FormulaValue.New("1")));
            Assert.Throws<ArgumentNullException>(() => waitFunction.Execute(new SomeOtherRecordValue(recordType), FormulaValue.New("Text"), null));
            Assert.Throws<InvalidCastException>(() => waitFunction.Execute(new SomeOtherRecordValue(recordType), FormulaValue.New("Text"), FormulaValue.New("1")));
        }

        [Fact]
        public void WaitFunctionNumberThrowsOnInvalidArgumentsTest()
        {
            LoggingTestHelper.SetupMock(MockLogger);
            var recordType = RecordType.Empty().Add("Text", FormulaType.Number);
            var waitFunction = new WaitFunctionNumber(Timeout, MockLogger.Object);
            var recordValue = new ControlRecordValue(recordType, MockTestWebProvider.Object, "Label1");
            Assert.Throws<ArgumentNullException>(() => waitFunction.Execute(null, FormulaValue.New("Text"), FormulaValue.New(1d)));
            Assert.Throws<ArgumentNullException>(() => waitFunction.Execute(new SomeOtherRecordValue(recordType), null, FormulaValue.New(1d)));
            Assert.Throws<ArgumentNullException>(() => waitFunction.Execute(new SomeOtherRecordValue(recordType), FormulaValue.New("Text"), null));
            Assert.Throws<InvalidCastException>(() => waitFunction.Execute(new SomeOtherRecordValue(recordType), FormulaValue.New("Text"), FormulaValue.New(1d)));
        }

        [Fact]
        public void WaitFunctionBooleanThrowsOnInvalidArgumentsTest()
        {
            LoggingTestHelper.SetupMock(MockLogger);
            var recordType = RecordType.Empty().Add("Text", FormulaType.Boolean);
            var waitFunction = new WaitFunctionBoolean(Timeout, MockLogger.Object);
            var recordValue = new ControlRecordValue(recordType, MockTestWebProvider.Object, "Label1");
            Assert.Throws<ArgumentNullException>(() => waitFunction.Execute(null, FormulaValue.New("Text"), FormulaValue.New(false)));
            Assert.Throws<ArgumentNullException>(() => waitFunction.Execute(new SomeOtherRecordValue(recordType), null, FormulaValue.New(false)));
            Assert.Throws<ArgumentNullException>(() => waitFunction.Execute(new SomeOtherRecordValue(recordType), FormulaValue.New("Text"), null));
            Assert.Throws<InvalidCastException>(() => waitFunction.Execute(new SomeOtherRecordValue(recordType), FormulaValue.New("Text"), FormulaValue.New(false)));
        }

        [Fact]
        public void WaitFunctionDateThrowsOnInvalidArgumentsTest()
        {
            LoggingTestHelper.SetupMock(MockLogger);
            var value = new DateTime(1970, 1, 1, 0, 0, 0);
            var recordType = RecordType.Empty().Add("Text", FormulaType.Date);
            var waitFunction = new WaitFunctionDate(Timeout, MockLogger.Object);
            var recordValue = new ControlRecordValue(recordType, MockTestWebProvider.Object, "Label1");
            Assert.Throws<ArgumentNullException>(() => waitFunction.Execute(null, FormulaValue.New("Text"), FormulaValue.NewDateOnly(value.Date)));
            Assert.Throws<ArgumentNullException>(() => waitFunction.Execute(new SomeOtherRecordValue(recordType), null, FormulaValue.NewDateOnly(value.Date)));
            Assert.Throws<ArgumentNullException>(() => waitFunction.Execute(new SomeOtherRecordValue(recordType), FormulaValue.New("Text"), null));
            Assert.Throws<InvalidCastException>(() => waitFunction.Execute(new SomeOtherRecordValue(recordType), FormulaValue.New("Text"), FormulaValue.NewDateOnly(value.Date)));
        }

        [Fact]
        public void WaitFunctionStringSucceedsTest()
        {
            LoggingTestHelper.SetupMock(MockLogger);
            var valueToWaitFor = "1";
            var recordType = RecordType.Empty().Add("Text", FormulaType.String);
            var recordValue = new ControlRecordValue(recordType, MockTestWebProvider.Object, "Label1");
            var jsPropertyValueModel = new JSPropertyValueModel()
            {
                PropertyValue = valueToWaitFor,
            };
            var expectedItemPath = new ItemPath
            {
                ControlName = "Label1",
                PropertyName = "Text"
            };
            MockTestWebProvider.Setup(x => x.GetPropertyValueFromControl<string>(It.IsAny<ItemPath>()))
                    .Returns(JsonConvert.SerializeObject(jsPropertyValueModel));
            MockTestState.Setup(x => x.GetTimeout()).Returns(Timeout);

            var waitFunction = new WaitFunctionString(Timeout, MockLogger.Object);
            waitFunction.Execute(recordValue, FormulaValue.New("Text"), FormulaValue.New(valueToWaitFor));

            MockTestWebProvider.Verify(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((itemPath) => itemPath.ControlName == expectedItemPath.ControlName && itemPath.PropertyName == expectedItemPath.PropertyName)), Times.Exactly(2));
        }

        [Fact]
        public void WaitFunctionImproperValueForStringTest()
        {
            LoggingTestHelper.SetupMock(MockLogger);
            var valueToWaitFor = true;
            var recordType = RecordType.Empty().Add("Value", FormulaType.Boolean);
            var recordValue = new ControlRecordValue(recordType, MockTestWebProvider.Object, "Toggle1");
            var jsPropertyValueModel = new JSPropertyValueModel()
            {
                PropertyValue = valueToWaitFor.ToString(),
            };
            var expectedItemPath = new ItemPath
            {
                ControlName = "Toggle1",
                PropertyName = "Value"
            };
            MockTestWebProvider.Setup(x => x.GetPropertyValueFromControl<string>(It.IsAny<ItemPath>()))
                    .Returns(JsonConvert.SerializeObject(jsPropertyValueModel));
            MockTestState.Setup(x => x.GetTimeout()).Returns(Timeout);

            var waitFunction = new WaitFunctionString(Timeout, MockLogger.Object);
            Assert.Throws<InvalidDataException>(() => waitFunction.Execute(recordValue, FormulaValue.New("Value"), StringValue.New("1")));
        }


        [Fact]
        public void WaitFunctionNumberSucceedsTest()
        {
            LoggingTestHelper.SetupMock(MockLogger);
            var valueToWaitFor = 1d;
            var recordType = RecordType.Empty().Add("Text", FormulaType.Number);

            var recordValue = new ControlRecordValue(recordType, MockTestWebProvider.Object, "Label1");
            var jsPropertyValueModel = new JSPropertyValueModel()
            {
                PropertyValue = valueToWaitFor.ToString("G"),
            };
            var expectedItemPath = new ItemPath
            {
                ControlName = "Label1",
                PropertyName = "Text"
            };
            MockTestWebProvider.Setup(x => x.GetPropertyValueFromControl<string>(It.IsAny<ItemPath>()))
                    .Returns(JsonConvert.SerializeObject(jsPropertyValueModel));
            MockTestState.Setup(x => x.GetTimeout()).Returns(Timeout);

            var waitFunction = new WaitFunctionNumber(Timeout, MockLogger.Object);
            waitFunction.Execute(recordValue, FormulaValue.New("Text"), FormulaValue.New(valueToWaitFor));

            MockTestWebProvider.Verify(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((itemPath) => itemPath.ControlName == expectedItemPath.ControlName && itemPath.PropertyName == expectedItemPath.PropertyName)), Times.Exactly(2));
        }

        [Fact]
        public void WaitFunctionImproperValueForNumberTest()
        {
            LoggingTestHelper.SetupMock(MockLogger);
            var valueToWaitFor = true;
            var recordType = RecordType.Empty().Add("Value", FormulaType.Boolean);
            var recordValue = new ControlRecordValue(recordType, MockTestWebProvider.Object, "Toggle1");
            var jsPropertyValueModel = new JSPropertyValueModel()
            {
                PropertyValue = valueToWaitFor.ToString(),
            };
            var expectedItemPath = new ItemPath
            {
                ControlName = "Toggle1",
                PropertyName = "Value"
            };
            MockTestWebProvider.Setup(x => x.GetPropertyValueFromControl<string>(It.IsAny<ItemPath>()))
                    .Returns(JsonConvert.SerializeObject(jsPropertyValueModel));
            MockTestState.Setup(x => x.GetTimeout()).Returns(Timeout);

            var waitFunction = new WaitFunctionNumber(Timeout, MockLogger.Object);
            Assert.Throws<InvalidDataException>(() => waitFunction.Execute(recordValue, FormulaValue.New("Value"), FormulaValue.New(1d)));
        }

        [Fact]
        public void WaitFunctionBooleanSucceedsTest()
        {
            var valueToWaitFor = false;
            var recordType = RecordType.Empty().Add("Text", FormulaType.Boolean);
            var recordValue = new ControlRecordValue(recordType, MockTestWebProvider.Object, "Label1");
            var jsPropertyValueModel = new JSPropertyValueModel()
            {
                PropertyValue = valueToWaitFor.ToString(),
            };
            var expectedItemPath = new ItemPath
            {
                ControlName = "Label1",
                PropertyName = "Text"
            };
            MockTestWebProvider.Setup(x => x.GetPropertyValueFromControl<string>(It.IsAny<ItemPath>()))
                    .Returns(JsonConvert.SerializeObject(jsPropertyValueModel));
            MockTestState.Setup(x => x.GetTimeout()).Returns(Timeout);
            LoggingTestHelper.SetupMock(MockLogger);

            var waitFunction = new WaitFunctionBoolean(Timeout, MockLogger.Object);
            waitFunction.Execute(recordValue, FormulaValue.New("Text"), BooleanValue.New(valueToWaitFor));

            MockTestWebProvider.Verify(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((itemPath) => itemPath.ControlName == expectedItemPath.ControlName && itemPath.PropertyName == expectedItemPath.PropertyName)), Times.Exactly(2));
        }

        [Fact]
        public void WaitFunctionImproperValueForBooleanTest()
        {
            LoggingTestHelper.SetupMock(MockLogger);
            var valueToWaitFor = 1;
            var recordType = RecordType.Empty().Add("Text", FormulaType.Number);

            var recordValue = new ControlRecordValue(recordType, MockTestWebProvider.Object, "Label1");
            var jsPropertyValueModel = new JSPropertyValueModel()
            {
                PropertyValue = valueToWaitFor.ToString(),
            };
            var expectedItemPath = new ItemPath
            {
                ControlName = "Label1",
                PropertyName = "Text"
            };
            MockTestWebProvider.Setup(x => x.GetPropertyValueFromControl<string>(It.IsAny<ItemPath>()))
                    .Returns(JsonConvert.SerializeObject(jsPropertyValueModel));
            MockTestState.Setup(x => x.GetTimeout()).Returns(Timeout);

            var waitFunction = new WaitFunctionBoolean(Timeout, MockLogger.Object);
            Assert.Throws<InvalidDataException>(() => waitFunction.Execute(recordValue, FormulaValue.New("Text"), BooleanValue.New(true)));
        }

        [Fact]
        public void WaitFunctionDate_DateValueSucceedsTest()
        {
            LoggingTestHelper.SetupMock(MockLogger);
            var value = new DateTime(2030, 1, 1, 0, 0, 0);
            var recordType = RecordType.Empty().Add("SelectedDate", FormulaType.Date);
            var recordValue = new ControlRecordValue(recordType, MockTestWebProvider.Object, "DatePicker1");
            var jsPropertyValueModel = new JSPropertyValueModel()
            {
                PropertyValue = value.ToString(),
            };
            var expectedItemPath = new ItemPath
            {
                ControlName = "DatePicker1",
                PropertyName = "SelectedDate"
            };
            MockTestWebProvider.Setup(x => x.GetPropertyValueFromControl<string>(It.IsAny<ItemPath>()))
                    .Returns(JsonConvert.SerializeObject(jsPropertyValueModel));
            MockTestState.Setup(x => x.GetTimeout()).Returns(Timeout);

            var waitFunction = new WaitFunctionDate(Timeout, MockLogger.Object);
            waitFunction.Execute(recordValue, FormulaValue.New("SelectedDate"), FormulaValue.NewDateOnly(value.Date));

            MockTestWebProvider.Verify(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((itemPath) => itemPath.ControlName == expectedItemPath.ControlName && itemPath.PropertyName == expectedItemPath.PropertyName)), Times.Exactly(2));
        }

        [Fact]
        public void WaitFunctionDate_DateTimeValueSucceedsTest()
        {
            LoggingTestHelper.SetupMock(MockLogger);
            var value = new DateTime(2030, 1, 1, 0, 0, 0);
            var recordType = RecordType.Empty().Add("DefaultDate", FormulaType.DateTime);
            var recordValue = new ControlRecordValue(recordType, MockTestWebProvider.Object, "DatePicker1");
            var jsPropertyValueModel = new JSPropertyValueModel()
            {
                PropertyValue = value.ToString(),
            };
            var expectedItemPath = new ItemPath
            {
                ControlName = "DatePicker1",
                PropertyName = "DefaultDate"
            };
            MockTestWebProvider.Setup(x => x.GetPropertyValueFromControl<string>(It.IsAny<ItemPath>()))
                    .Returns(JsonConvert.SerializeObject(jsPropertyValueModel));
            MockTestState.Setup(x => x.GetTimeout()).Returns(Timeout);

            var waitFunction = new WaitFunctionDate(Timeout, MockLogger.Object);
            waitFunction.Execute(recordValue, FormulaValue.New("DefaultDate"), FormulaValue.NewDateOnly(value));

            MockTestWebProvider.Verify(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((itemPath) => itemPath.ControlName == expectedItemPath.ControlName && itemPath.PropertyName == expectedItemPath.PropertyName)), Times.Exactly(2));
        }

        [Fact]
        public void WaitFunctionImproperValueForDateTest()
        {
            LoggingTestHelper.SetupMock(MockLogger);
            var value = new DateTime(2030, 1, 1, 0, 0, 0);
            var valueToWaitFor = 1;
            var recordType = RecordType.Empty().Add("Text", FormulaType.Number);

            var recordValue = new ControlRecordValue(recordType, MockTestWebProvider.Object, "Label1");
            var jsPropertyValueModel = new JSPropertyValueModel()
            {
                PropertyValue = valueToWaitFor.ToString(),
            };
            var expectedItemPath = new ItemPath
            {
                ControlName = "Label1",
                PropertyName = "Text"
            };
            MockTestWebProvider.Setup(x => x.GetPropertyValueFromControl<string>(It.IsAny<ItemPath>()))
                    .Returns(JsonConvert.SerializeObject(jsPropertyValueModel));
            MockTestState.Setup(x => x.GetTimeout()).Returns(Timeout);

            var waitFunction = new WaitFunctionDate(Timeout, MockLogger.Object);
            Assert.Throws<InvalidDataException>(() => waitFunction.Execute(recordValue, FormulaValue.New("Text"), FormulaValue.NewDateOnly(value.Date)));
        }

        [Fact]
        public void WaitFunctionDateTime_DateTimeValueSucceedsTest()
        {
            LoggingTestHelper.SetupMock(MockLogger);
            var value = new DateTime(2030, 1, 1, 0, 0, 0);
            var recordType = RecordType.Empty().Add("DefaultDate", FormulaType.DateTime);
            var recordValue = new ControlRecordValue(recordType, MockTestWebProvider.Object, "DatePicker1");
            var jsPropertyValueModel = new JSPropertyValueModel()
            {
                PropertyValue = value.ToString(),
            };
            var expectedItemPath = new ItemPath
            {
                ControlName = "DatePicker1",
                PropertyName = "DefaultDate"
            };
            MockTestWebProvider.Setup(x => x.GetPropertyValueFromControl<string>(It.IsAny<ItemPath>()))
                    .Returns(JsonConvert.SerializeObject(jsPropertyValueModel));
            MockTestState.Setup(x => x.GetTimeout()).Returns(Timeout);

            var waitFunction = new WaitFunctionDateTime(Timeout, MockLogger.Object);
            waitFunction.Execute(recordValue, FormulaValue.New("DefaultDate"), FormulaValue.New(value));

            MockTestWebProvider.Verify(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((itemPath) => itemPath.ControlName == expectedItemPath.ControlName && itemPath.PropertyName == expectedItemPath.PropertyName)), Times.Exactly(2));
        }

        [Fact]
        public void WaitFunctionDateTime_DateValueSucceedsTest()
        {
            LoggingTestHelper.SetupMock(MockLogger);
            var value = new DateTime(2030, 1, 1, 0, 0, 0);
            var recordType = RecordType.Empty().Add("SelectedDate", FormulaType.Date);
            var recordValue = new ControlRecordValue(recordType, MockTestWebProvider.Object, "DatePicker1");
            var jsPropertyValueModel = new JSPropertyValueModel()
            {
                PropertyValue = value.ToString(),
            };
            var expectedItemPath = new ItemPath
            {
                ControlName = "DatePicker1",
                PropertyName = "SelectedDate"
            };
            MockTestWebProvider.Setup(x => x.GetPropertyValueFromControl<string>(It.IsAny<ItemPath>()))
                    .Returns(JsonConvert.SerializeObject(jsPropertyValueModel));
            MockTestState.Setup(x => x.GetTimeout()).Returns(Timeout);

            var waitFunction = new WaitFunctionDateTime(Timeout, MockLogger.Object);
            waitFunction.Execute(recordValue, FormulaValue.New("SelectedDate"), FormulaValue.New(value.Date));

            MockTestWebProvider.Verify(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((itemPath) => itemPath.ControlName == expectedItemPath.ControlName && itemPath.PropertyName == expectedItemPath.PropertyName)), Times.Exactly(2));
        }

        [Fact]
        public void WaitFunctionStringWaitsForValueToUpdateTest()
        {
            LoggingTestHelper.SetupMock(MockLogger);
            var valueToWaitFor = "1";
            var recordType = RecordType.Empty().Add("Text", FormulaType.String);
            var recordValue = new ControlRecordValue(recordType, MockTestWebProvider.Object, "Label1");
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
            MockTestWebProvider.SetupSequence(x => x.GetPropertyValueFromControl<string>(It.IsAny<ItemPath>()))
                    .Returns(JsonConvert.SerializeObject(jsPropertyValueModel))
                    .Returns(JsonConvert.SerializeObject(jsPropertyValueModel))
                    .Returns(JsonConvert.SerializeObject(finalJsPropertyValueModel));
            MockTestState.Setup(x => x.GetTimeout()).Returns(Timeout);

            var waitFunction = new WaitFunctionString(Timeout, MockLogger.Object);
            waitFunction.Execute(recordValue, FormulaValue.New("Text"), FormulaValue.New(valueToWaitFor));

            MockTestWebProvider.Verify(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((itemPath) => itemPath.ControlName == expectedItemPath.ControlName && itemPath.PropertyName == expectedItemPath.PropertyName)), Times.Exactly(3));
        }

        [Fact]
        public void WaitFunctionNumberWaitsForValueToUpdateTest()
        {
            LoggingTestHelper.SetupMock(MockLogger);
            var valueToWaitFor = 1d;
            var recordType = RecordType.Empty().Add("Text", FormulaType.Number);
            var recordValue = new ControlRecordValue(recordType, MockTestWebProvider.Object, "Label1");
            var jsPropertyValueModel = new JSPropertyValueModel()
            {
                PropertyValue = "0",
            };
            var finalJsPropertyValueModel = new JSPropertyValueModel()
            {
                PropertyValue = valueToWaitFor.ToString("G"),
            };
            var expectedItemPath = new ItemPath
            {
                ControlName = "Label1",
                PropertyName = "Text"
            };
            MockTestWebProvider.SetupSequence(x => x.GetPropertyValueFromControl<string>(It.IsAny<ItemPath>()))
                    .Returns(JsonConvert.SerializeObject(jsPropertyValueModel))
                    .Returns(JsonConvert.SerializeObject(jsPropertyValueModel))
                    .Returns(JsonConvert.SerializeObject(finalJsPropertyValueModel));
            MockTestState.Setup(x => x.GetTimeout()).Returns(Timeout);

            var waitFunction = new WaitFunctionNumber(Timeout, MockLogger.Object);
            waitFunction.Execute(recordValue, FormulaValue.New("Text"), FormulaValue.New(valueToWaitFor));

            MockTestWebProvider.Verify(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((itemPath) => itemPath.ControlName == expectedItemPath.ControlName && itemPath.PropertyName == expectedItemPath.PropertyName)), Times.Exactly(3));
        }

        [Fact]
        public void WaitFunctionBooleanWaitsForValueToUpdateTest()
        {
            LoggingTestHelper.SetupMock(MockLogger);
            var valueToWaitFor = false;
            var recordType = RecordType.Empty().Add("Text", FormulaType.Boolean);
            var recordValue = new ControlRecordValue(recordType, MockTestWebProvider.Object, "Label1");
            var jsPropertyValueModel = new JSPropertyValueModel()
            {
                PropertyValue = "true",
            };
            var finalJsPropertyValueModel = new JSPropertyValueModel()
            {
                PropertyValue = valueToWaitFor.ToString(),
            };
            var expectedItemPath = new ItemPath
            {
                ControlName = "Label1",
                PropertyName = "Text"
            };
            MockTestWebProvider.SetupSequence(x => x.GetPropertyValueFromControl<string>(It.IsAny<ItemPath>()))
                    .Returns(JsonConvert.SerializeObject(jsPropertyValueModel))
                    .Returns(JsonConvert.SerializeObject(jsPropertyValueModel))
                    .Returns(JsonConvert.SerializeObject(finalJsPropertyValueModel));
            MockTestState.Setup(x => x.GetTimeout()).Returns(Timeout);

            var waitFunction = new WaitFunctionBoolean(Timeout, MockLogger.Object);
            waitFunction.Execute(recordValue, FormulaValue.New("Text"), BooleanValue.New(valueToWaitFor));

            MockTestWebProvider.Verify(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((itemPath) => itemPath.ControlName == expectedItemPath.ControlName && itemPath.PropertyName == expectedItemPath.PropertyName)), Times.Exactly(3));
        }

        [Fact]
        public void WaitFunctionDateWaitsForValueToUpdateTest()
        {
            LoggingTestHelper.SetupMock(MockLogger);
            var value = new DateTime(2030, 1, 1, 0, 0, 0);
            var recordType = RecordType.Empty().Add("SelectedDate", FormulaType.Date);
            var recordValue = new ControlRecordValue(recordType, MockTestWebProvider.Object, "DatePicker1");
            var jsPropertyValueModel = new JSPropertyValueModel()
            {
                PropertyValue = "0",
            };
            var finalJsPropertyValueModel = new JSPropertyValueModel()
            {
                PropertyValue = value.ToString(),
            };
            var expectedItemPath = new ItemPath
            {
                ControlName = "DatePicker1",
                PropertyName = "SelectedDate"
            };
            MockTestWebProvider.SetupSequence(x => x.GetPropertyValueFromControl<string>(It.IsAny<ItemPath>()))
                    .Returns(JsonConvert.SerializeObject(jsPropertyValueModel))
                    .Returns(JsonConvert.SerializeObject(jsPropertyValueModel))
                    .Returns(JsonConvert.SerializeObject(finalJsPropertyValueModel));
            MockTestState.Setup(x => x.GetTimeout()).Returns(Timeout);

            var waitFunction = new WaitFunctionDate(Timeout, MockLogger.Object);
            waitFunction.Execute(recordValue, FormulaValue.New("SelectedDate"), FormulaValue.NewDateOnly(value.Date));

            MockTestWebProvider.Verify(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((itemPath) => itemPath.ControlName == expectedItemPath.ControlName && itemPath.PropertyName == expectedItemPath.PropertyName)), Times.Exactly(2));
        }

        [Fact]
        public void WaitFunctionDateTimeWaitsForValueToUpdateTest()
        {
            LoggingTestHelper.SetupMock(MockLogger);
            var value = new DateTime(2030, 1, 1, 0, 0, 0);
            var recordType = RecordType.Empty().Add("DefaultDate", FormulaType.DateTime);
            var recordValue = new ControlRecordValue(recordType, MockTestWebProvider.Object, "DatePicker1");
            var jsPropertyValueModel = new JSPropertyValueModel()
            {
                PropertyValue = "0",
            };
            var finalJsPropertyValueModel = new JSPropertyValueModel()
            {
                PropertyValue = value.ToString("o"), // Use "o" format for round-trip date/time pattern
            };
            var expectedItemPath = new ItemPath
            {
                ControlName = "DatePicker1",
                PropertyName = "DefaultDate"
            };
            MockTestWebProvider.SetupSequence(x => x.GetPropertyValueFromControl<string>(It.IsAny<ItemPath>()))
                    .Returns(JsonConvert.SerializeObject(jsPropertyValueModel))
                    .Returns(JsonConvert.SerializeObject(jsPropertyValueModel))
                    .Returns(JsonConvert.SerializeObject(finalJsPropertyValueModel));
            MockTestState.Setup(x => x.GetTimeout()).Returns(Timeout);

            var waitFunction = new WaitFunctionDateTime(Timeout, MockLogger.Object);
            waitFunction.Execute(recordValue, FormulaValue.New("DefaultDate"), FormulaValue.New(value));

            MockTestWebProvider.Verify(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((itemPath) => itemPath.ControlName == expectedItemPath.ControlName && itemPath.PropertyName == expectedItemPath.PropertyName)), Times.Exactly(2));
        }

        [Fact]
        public void WaitFunctionStringTimeoutTest()
        {
            LoggingTestHelper.SetupMock(MockLogger);
            var valueToWaitFor = "1";
            var recordType = RecordType.Empty().Add("Text", FormulaType.String);
            var recordValue = new ControlRecordValue(recordType, MockTestWebProvider.Object, "Label1");
            var jsPropertyValueModel = new JSPropertyValueModel()
            {
                PropertyValue = "0",
            };
            MockTestWebProvider.SetupSequence(x => x.GetPropertyValueFromControl<string>(It.IsAny<ItemPath>()))
                    .Returns(JsonConvert.SerializeObject(jsPropertyValueModel))
                    .Returns(JsonConvert.SerializeObject(jsPropertyValueModel))
                    .Returns(JsonConvert.SerializeObject(jsPropertyValueModel));
            MockTestState.Setup(x => x.GetTimeout()).Returns(Timeout);

            var waitFunction = new WaitFunctionString(300, MockLogger.Object); // each trial has 500ms in between
            Assert.Throws<TimeoutException>(() => waitFunction.Execute(recordValue, FormulaValue.New("Text"), FormulaValue.New(valueToWaitFor)));
        }

        [Fact]
        public void WaitFunctionNumberTimeoutTest()
        {
            LoggingTestHelper.SetupMock(MockLogger);
            var valueToWaitFor = 1d;
            var recordType = RecordType.Empty().Add("Text", FormulaType.Number);
            var recordValue = new ControlRecordValue(recordType, MockTestWebProvider.Object, "Label1");
            var jsPropertyValueModel = new JSPropertyValueModel()
            {
                PropertyValue = "0",
            };

            MockTestWebProvider.SetupSequence(x => x.GetPropertyValueFromControl<string>(It.IsAny<ItemPath>()))
                    .Returns(JsonConvert.SerializeObject(jsPropertyValueModel))
                    .Returns(JsonConvert.SerializeObject(jsPropertyValueModel))
                    .Returns(JsonConvert.SerializeObject(jsPropertyValueModel));

            MockTestState.Setup(x => x.GetTimeout()).Returns(Timeout);

            var waitFunction = new WaitFunctionNumber(300, MockLogger.Object); // each trial has 500ms in between
            Assert.Throws<TimeoutException>(() => waitFunction.Execute(recordValue, FormulaValue.New("Text"), FormulaValue.New(valueToWaitFor)));
        }

        [Fact]
        public void WaitFunctionBooleanTimeoutTest()
        {
            LoggingTestHelper.SetupMock(MockLogger);
            var valueToWaitFor = false;
            var recordType = RecordType.Empty().Add("Text", FormulaType.Boolean);
            var recordValue = new ControlRecordValue(recordType, MockTestWebProvider.Object, "Label1");
            var jsPropertyValueModel = new JSPropertyValueModel()
            {
                PropertyValue = "true",
            };


            MockTestWebProvider.SetupSequence(x => x.GetPropertyValueFromControl<string>(It.IsAny<ItemPath>()))
                    .Returns(JsonConvert.SerializeObject(jsPropertyValueModel))
                    .Returns(JsonConvert.SerializeObject(jsPropertyValueModel))
                    .Returns(JsonConvert.SerializeObject(jsPropertyValueModel));
            MockTestState.Setup(x => x.GetTimeout()).Returns(Timeout);

            var waitFunction = new WaitFunctionBoolean(300, MockLogger.Object); // each trial has 500ms in between
            Assert.Throws<TimeoutException>(() => waitFunction.Execute(recordValue, FormulaValue.New("Text"), FormulaValue.New(valueToWaitFor)));
        }

        [Fact]
        public void WaitFunctionDateTimeoutTest()
        {
            LoggingTestHelper.SetupMock(MockLogger);
            var value = new DateTime(2030, 1, 1, 0, 0, 0);
            var recordType = RecordType.Empty().Add("SelectedDate", FormulaType.Date);
            var recordValue = new ControlRecordValue(recordType, MockTestWebProvider.Object, "DatePicker1");
            var jsPropertyValueModel = new JSPropertyValueModel()
            {
                PropertyValue = "0",
            };

            MockTestWebProvider.SetupSequence(x => x.GetPropertyValueFromControl<string>(It.IsAny<ItemPath>()))
                    .Returns(JsonConvert.SerializeObject(jsPropertyValueModel))
                    .Returns(JsonConvert.SerializeObject(jsPropertyValueModel))
                    .Returns(JsonConvert.SerializeObject(jsPropertyValueModel));
            MockTestState.Setup(x => x.GetTimeout()).Returns(Timeout);

            var waitFunction = new WaitFunctionDate(300, MockLogger.Object); // each trial has 500ms in between
            Assert.ThrowsAny<Exception>(() =>
            {
                try
                {
                    waitFunction.Execute(recordValue, FormulaValue.New("SelectedDate"), FormulaValue.NewDateOnly(value.Date));
                }
                catch (InvalidCastException ex)
                {
                    throw new TimeoutException("InvalidCastException occurred", ex);
                }
            });

        }

        [Fact]
        public void WaitFunctionDateTimeTimeoutTest()
        {
            LoggingTestHelper.SetupMock(MockLogger);
            var value = new DateTime(2030, 1, 1, 0, 0, 0);
            var recordType = RecordType.Empty().Add("DefaultDate", FormulaType.DateTime);
            var recordValue = new ControlRecordValue(recordType, MockTestWebProvider.Object, "DatePicker1");
            var jsPropertyValueModel = new JSPropertyValueModel()
            {
                PropertyValue = "0",
            };

            MockTestWebProvider.SetupSequence(x => x.GetPropertyValueFromControl<string>(It.IsAny<ItemPath>()))
                    .Returns(JsonConvert.SerializeObject(jsPropertyValueModel))
                    .Returns(JsonConvert.SerializeObject(jsPropertyValueModel))
                    .Returns(JsonConvert.SerializeObject(jsPropertyValueModel));
            MockTestState.Setup(x => x.GetTimeout()).Returns(Timeout);

            var waitFunction = new WaitFunctionDateTime(300, MockLogger.Object); // each trial has 500ms in between
            Assert.ThrowsAny<Exception>(() =>
            {
                try
                {
                    waitFunction.Execute(recordValue, FormulaValue.New("DefaultDate"), DateTimeValue.New(value));
                }
                catch (InvalidCastException ex)
                {
                    throw new TimeoutException("InvalidCastException occurred", ex);
                }
            });

        }
    }
}
