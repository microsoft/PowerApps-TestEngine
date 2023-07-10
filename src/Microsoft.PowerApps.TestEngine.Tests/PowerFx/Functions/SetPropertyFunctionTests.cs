// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.PowerApps;
using Microsoft.PowerApps.TestEngine.PowerApps.PowerFxModel;
using Microsoft.PowerApps.TestEngine.PowerFx.Functions;
using Microsoft.PowerApps.TestEngine.Tests.Helpers;
using Microsoft.PowerFx.Types;
using Moq;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.PowerFx.Functions
{
    public class SetPropertyFunctionTests
    {
        private Mock<IPowerAppFunctions> MockPowerAppFunctions;
        private Mock<ITestState> MockTestState;
        private Mock<ILogger> MockLogger;

        public SetPropertyFunctionTests()
        {
            MockPowerAppFunctions = new Mock<IPowerAppFunctions>(MockBehavior.Strict);
            MockTestState = new Mock<ITestState>(MockBehavior.Strict);
            MockLogger = new Mock<ILogger>(MockBehavior.Strict);
        }

        [Fact]
        public void SetPropertyFunctionThrowsOnNonPowerAppsRecordValueTest()
        {
            var recordType = RecordType.Empty().Add("Text", FormulaType.String);
            var SetPropertyFunctionString = new SetPropertyFunction(MockPowerAppFunctions.Object, MockLogger.Object);
            var someOtherRecordValue = new SomeOtherRecordValue(recordType);

            Assert.ThrowsAny<Exception>(() => SetPropertyFunctionString.Execute(someOtherRecordValue, StringValue.New("Test"), StringValue.New("10")));
        }

        [Fact]
        public void SetPropertyFunctionFailsTest()
        {
            MockPowerAppFunctions.Setup(x => x.SetPropertyAsync(It.IsAny<ItemPath>(), It.IsAny<StringValue>())).Returns(Task.FromResult(false));
            LoggingTestHelper.SetupMock(MockLogger);

            var recordType = RecordType.Empty().Add("Text", FormulaType.String);
            var recordValue = new ControlRecordValue(recordType, MockPowerAppFunctions.Object, "Button1");
            var setPropertyFunctionString = new SetPropertyFunction(MockPowerAppFunctions.Object, MockLogger.Object);

            // This tests the SetPropertyAsync returning false and hence throwing exception
            Assert.ThrowsAny<Exception>(() => setPropertyFunctionString.Execute(recordValue, StringValue.New("Text"), StringValue.New("5")));
            MockPowerAppFunctions.Verify(x => x.SetPropertyAsync(It.Is<ItemPath>((item) => item.ControlName == recordValue.Name), It.Is<StringValue>(stringVal => stringVal.Value == "5")), Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, (string)"Error occurred on DataType of type Microsoft.PowerFx.Types.StringValue", LogLevel.Debug, Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, (string)"Property name: Text", LogLevel.Trace, Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, (string)"Unable to set property with SetProperty function.", LogLevel.Error, Times.Once());
        }

        [Fact]
        public void SetPropertyStringFunctionTest()
        {
            MockPowerAppFunctions.Setup(x => x.SetPropertyAsync(It.IsAny<ItemPath>(), It.IsAny<StringValue>())).Returns(Task.FromResult(true));
            LoggingTestHelper.SetupMock(MockLogger);

            // Make setPropertyFunction contain a text component called Button1
            var recordType = RecordType.Empty().Add("Text", FormulaType.String);
            var recordValue = new ControlRecordValue(recordType, MockPowerAppFunctions.Object, "Button1");
            var setPropertyFunctionString = new SetPropertyFunction(MockPowerAppFunctions.Object, MockLogger.Object);

            // Set the value of Button1's 'Text' property to 5
            var result = setPropertyFunctionString.Execute(recordValue, StringValue.New("Text"), StringValue.New("5"));

            // check to see if the value of Button1's 'Text' property is 5
            Assert.IsType<BooleanValue>(result);
            MockPowerAppFunctions.Verify(x => x.SetPropertyAsync(It.Is<ItemPath>((item) => item.ControlName == recordValue.Name), It.Is<StringValue>(stringVal => stringVal.Value == "5")), Times.Once());

            // Set the value of Button1's 'Text' property to 10 
            result = setPropertyFunctionString.Execute(recordValue, StringValue.New("Text"), StringValue.New("10"));

            // check to see if the value of Button1's 'Text' property is 10
            Assert.IsType<BooleanValue>(result);
            MockPowerAppFunctions.Verify(x => x.SetPropertyAsync(It.Is<ItemPath>((item) => item.ControlName == recordValue.Name), It.Is<StringValue>(stringVal => stringVal.Value == "10")), Times.Once());

            // Set the value of Button1's 'Text' property to 'abc'
            result = setPropertyFunctionString.Execute(recordValue, StringValue.New("Text"), StringValue.New("abc"));

            // check to see if the value of Button1's 'Text' property is abc
            Assert.IsType<BooleanValue>(result);
            MockPowerAppFunctions.Verify(x => x.SetPropertyAsync(It.Is<ItemPath>((item) => item.ControlName == recordValue.Name), It.Is<StringValue>(stringVal => stringVal.Value == "abc")), Times.Once());
        }

        [Fact]
        public void SetPropertyNumberFunctionTest()
        {
            MockPowerAppFunctions.Setup(x => x.SetPropertyAsync(It.IsAny<ItemPath>(), It.IsAny<NumberValue>())).Returns(Task.FromResult(true));
            LoggingTestHelper.SetupMock(MockLogger);

            // Make setPropertyFunction contain a component called Rating1
            var recordType = RecordType.Empty().Add("Value", FormulaType.Number);
            var recordValue = new ControlRecordValue(recordType, MockPowerAppFunctions.Object, "Rating1");
            var setPropertyFunction = new SetPropertyFunction(MockPowerAppFunctions.Object, MockLogger.Object);

            // Set the value of Rating1's 'Value' property to 5
            var result = setPropertyFunction.Execute(recordValue, StringValue.New("Value"), NumberValue.New(5));

            // check to see if the value of Rating1's 'Value' property is 5
            Assert.IsType<BooleanValue>(result);
            MockPowerAppFunctions.Verify(x => x.SetPropertyAsync(It.Is<ItemPath>((item) => item.ControlName == recordValue.Name), It.Is<NumberValue>(numVal => numVal.Value == 5)), Times.Once());
        }

        [Fact]
        public void SetPropertyBooleanFunctionTest()
        {
            MockPowerAppFunctions.Setup(x => x.SetPropertyAsync(It.IsAny<ItemPath>(), It.IsAny<BooleanValue>())).Returns(Task.FromResult(true));
            LoggingTestHelper.SetupMock(MockLogger);

            // Make setPropertyFunction contain a component called Toggle1
            var recordType = RecordType.Empty().Add("Value", FormulaType.Boolean);
            var recordValue = new ControlRecordValue(recordType, MockPowerAppFunctions.Object, "Toggle1");
            var setPropertyFunction = new SetPropertyFunction(MockPowerAppFunctions.Object, MockLogger.Object);

            // Set the value of Toggle1's 'Value' property to true
            var result = setPropertyFunction.Execute(recordValue, StringValue.New("Value"), BooleanValue.New(true));

            // check to see if the value of Toggle1's 'Value' property is true
            Assert.IsType<BooleanValue>(result);
            MockPowerAppFunctions.Verify(x => x.SetPropertyAsync(It.Is<ItemPath>((item) => item.ControlName == recordValue.Name), It.Is<BooleanValue>(boolVal => boolVal.Value == true)), Times.Once());
        }

        [Fact]
        public void SetPropertyDateFunctionTest()
        {
            MockPowerAppFunctions.Setup(x => x.SetPropertyAsync(It.IsAny<ItemPath>(), It.IsAny<DateValue>())).Returns(Task.FromResult(true));
            LoggingTestHelper.SetupMock(MockLogger);

            // Make setPropertyFunction contain a component called DatePicker1
            var recordType = RecordType.Empty().Add("Value", FormulaType.Date);
            var recordValue = new ControlRecordValue(recordType, MockPowerAppFunctions.Object, "DatePicker1");
            var setPropertyFunction = new SetPropertyFunction(MockPowerAppFunctions.Object, MockLogger.Object);

            // Set the value of DatePicker1's 'Value' property to the datetime (01/01/2030)
            var dt = new DateTime(2030, 1, 1, 0, 0, 0);
            var result = setPropertyFunction.Execute(recordValue, StringValue.New("Value"), FormulaValue.NewDateOnly(dt.Date));

            // check to see if the value of DatePicker1's 'Value' property is the correct datetime (01/01/2030)
            Assert.IsType<BooleanValue>(result);
            MockPowerAppFunctions.Verify(x => x.SetPropertyAsync(It.Is<ItemPath>((item) => item.ControlName == recordValue.Name), It.Is<DateValue>(dateVal => dateVal.GetConvertedValue(null) == dt)), Times.Once());
        }

        [Fact]
        public void SetPropertyRecordFunctionTest()
        {
            MockPowerAppFunctions.Setup(x => x.SetPropertyAsync(It.IsAny<ItemPath>(), It.IsAny<RecordValue>())).Returns(Task.FromResult(true));
            LoggingTestHelper.SetupMock(MockLogger);

            // Make setPropertyFunction contain a component called Dropdown1
            var recordType = RecordType.Empty().Add("Selected", RecordType.Empty());
            var recordValue = new ControlRecordValue(recordType, MockPowerAppFunctions.Object, "Dropdown1");
            var setPropertyFunction = new SetPropertyFunction(MockPowerAppFunctions.Object, MockLogger.Object);

            // Set the value of Dropdown1's 'Selected' property to {"Value":"2"}
            var pair = new KeyValuePair<string, FormulaValue>("Value", StringValue.New("2"));
            var nameValue = new NamedValue(pair);
            var result = setPropertyFunction.Execute(recordValue, StringValue.New("Selected"), RecordValue.NewRecordFromFields(nameValue));

            // check to see if the value of Dropdown1's 'Selected' property is "2"
            Assert.IsType<BooleanValue>(result);
            MockPowerAppFunctions.Verify(x => x.SetPropertyAsync(It.Is<ItemPath>((item) => item.ControlName == recordValue.Name), It.Is<RecordValue>(recordVal => ((StringValue)recordVal.GetField("Value")).Value == "2")), Times.Once());
        }


        [Fact]
        public void SetPropertyTableFunctionTest()
        {
            MockPowerAppFunctions.Setup(x => x.SetPropertyAsync(It.IsAny<ItemPath>(), It.IsAny<TableValue>())).Returns(Task.FromResult(true));
            MockPowerAppFunctions.Setup(x => x.GetItemCount(It.IsAny<ItemPath>())).Returns(2);
            LoggingTestHelper.SetupMock(MockLogger);

            var setPropertyFunction = new SetPropertyFunction(MockPowerAppFunctions.Object, MockLogger.Object);
            var control1Name = Guid.NewGuid().ToString();
            var control2Name = Guid.NewGuid().ToString();
            var control1Type = RecordType.Empty().Add("Value", FormulaType.String);
            var control2Type = RecordType.Empty().Add("Value", FormulaType.String);
            var tableType = TableType.Empty().Add(new NamedFormulaType(control1Name, control1Type, "2")).Add(new NamedFormulaType(control2Name, control2Type, "3"));
            var itemPath = new ItemPath()
            {
                ControlName = "ComboBox1",
                PropertyName = "SelectedItems"
            };

            var recordType = tableType.ToRecord();
            var recordValue = new ControlRecordValue(recordType, MockPowerAppFunctions.Object, "ComboBox1");
            var tableSource = new ControlTableSource(MockPowerAppFunctions.Object, itemPath, recordType);
            var tableValue = new ControlTableValue(recordType, tableSource, MockPowerAppFunctions.Object);
            var result = setPropertyFunction.Execute(recordValue, StringValue.New("SelectedItems"), tableValue);

            Assert.IsType<BooleanValue>(result);
            Assert.Equal(2, tableSource.Count);
            MockPowerAppFunctions.Verify(x => x.SetPropertyAsync(It.Is<ItemPath>((item) => item.ControlName == recordValue.Name), It.Is<TableValue>(tableVal => tableVal.Count() == 2)), Times.Once());
        }
    }
}
