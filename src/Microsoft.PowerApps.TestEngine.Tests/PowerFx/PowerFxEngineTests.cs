// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.PowerApps;
using Microsoft.PowerApps.TestEngine.PowerApps.PowerFxModel;
using Microsoft.PowerApps.TestEngine.PowerFx;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerApps.TestEngine.Tests.Helpers;
using Microsoft.PowerFx.Types;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.PowerFx
{
    public class PowerFxEngineTests
    {
        private Mock<ITestInfraFunctions> MockTestInfraFunctions;
        private Mock<ITestState> MockTestState;
        private Mock<IPowerAppFunctions> MockPowerAppFunctions;
        private Mock<IFileSystem> MockFileSystem;
        private Mock<ISingleTestInstanceState> MockSingleTestInstanceState;
        private Mock<ILogger> MockLogger;

        public PowerFxEngineTests()
        {
            MockTestInfraFunctions = new Mock<ITestInfraFunctions>(MockBehavior.Strict);
            MockTestState = new Mock<ITestState>(MockBehavior.Strict);
            MockPowerAppFunctions = new Mock<IPowerAppFunctions>(MockBehavior.Strict);
            MockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            MockSingleTestInstanceState = new Mock<ISingleTestInstanceState>(MockBehavior.Strict);
            MockLogger = new Mock<ILogger>(MockBehavior.Strict);
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            MockTestState.Setup(x => x.GetTimeout()).Returns(30000);
            LoggingTestHelper.SetupMock(MockLogger);
        }

        [Fact]
        public void SetupDoesNotThrow()
        {
            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockPowerAppFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object);
            powerFxEngine.Setup();
        }

        [Fact]
        public void ExecuteThrowsOnNoSetupTest()
        {
            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockPowerAppFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object);
            Assert.Throws<InvalidOperationException>(() => powerFxEngine.Execute("", It.IsAny<CultureInfo>()));
            LoggingTestHelper.VerifyLogging(MockLogger, "Engine is null, make sure to call Setup first", LogLevel.Error, Times.Once());
        }

        [Fact]
        public void UpdatePowerFxModelAsyncThrowsOnNoSetupTest()
        {
            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockPowerAppFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object);
            Assert.ThrowsAsync<InvalidOperationException>(() => powerFxEngine.UpdatePowerFxModelAsync());
            LoggingTestHelper.VerifyLogging(MockLogger, "Engine is null, make sure to call Setup first", LogLevel.Error, Times.Once());
        }

        [Fact]
        public async void UpdatePowerFxModelAsyncThrowsOnCantGetAppStatusTest()
        {
            var recordType = RecordType.Empty().Add("Text", FormulaType.String);
            var button1 = new ControlRecordValue(recordType, MockPowerAppFunctions.Object, "Button1");
            MockPowerAppFunctions.Setup(x => x.SelectControlAsync(It.IsAny<ItemPath>())).Returns(Task.FromResult(true));
            MockPowerAppFunctions.Setup(x => x.LoadPowerAppsObjectModelAsync()).Returns(Task.FromResult(new Dictionary<string, ControlRecordValue>() { { "Button1", button1 } }));
            MockPowerAppFunctions.Setup(x => x.CheckIfAppIsIdleAsync()).Returns(Task.FromResult(false));

            var testSettings = new TestSettings() { Timeout = 3000 };
            MockTestState.Setup(x => x.GetTestSettings()).Returns(testSettings);

            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockPowerAppFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object);
            powerFxEngine.Setup();
            await Assert.ThrowsAsync<TimeoutException>(() => powerFxEngine.UpdatePowerFxModelAsync());
            LoggingTestHelper.VerifyLogging(MockLogger, "Something went wrong when Test Engine tried to get App status.", LogLevel.Error, Times.Once());
        }

        [Fact]
        public async void RunRequirementsCheckAsyncTest()
        {
            MockPowerAppFunctions.Setup(x => x.CheckAndHandleIfLegacyPlayerAsync()).Returns(Task.CompletedTask);
            MockPowerAppFunctions.Setup(x => x.TestEngineReady()).Returns(Task.FromResult(true));

            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockPowerAppFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object);
            powerFxEngine.Setup();

            await powerFxEngine.RunRequirementsCheckAsync();

            MockPowerAppFunctions.Verify(x => x.CheckAndHandleIfLegacyPlayerAsync(), Times.Once());
            MockPowerAppFunctions.Verify(x => x.TestEngineReady(), Times.Once());
        }

        [Fact]
        public async void RunRequirementsCheckAsyncThrowsOnCheckAndHandleIfLegacyPlayerTest()
        {
            MockPowerAppFunctions.Setup(x => x.CheckAndHandleIfLegacyPlayerAsync()).Throws(new Exception());
            MockPowerAppFunctions.Setup(x => x.TestEngineReady()).Returns(Task.FromResult(true));

            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockPowerAppFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object);
            powerFxEngine.Setup();

            await Assert.ThrowsAsync<Exception>(() => powerFxEngine.RunRequirementsCheckAsync());

            MockPowerAppFunctions.Verify(x => x.CheckAndHandleIfLegacyPlayerAsync(), Times.Once());
            MockPowerAppFunctions.Verify(x => x.TestEngineReady(), Times.Never());
        }

        [Fact]
        public async void RunRequirementsCheckAsyncThrowsOnTestEngineReadyTest()
        {
            MockPowerAppFunctions.Setup(x => x.CheckAndHandleIfLegacyPlayerAsync()).Returns(Task.CompletedTask);
            MockPowerAppFunctions.Setup(x => x.TestEngineReady()).Throws(new Exception());

            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockPowerAppFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object);
            powerFxEngine.Setup();

            await Assert.ThrowsAsync<Exception>(() => powerFxEngine.RunRequirementsCheckAsync());

            MockPowerAppFunctions.Verify(x => x.CheckAndHandleIfLegacyPlayerAsync(), Times.Once());
            MockPowerAppFunctions.Verify(x => x.TestEngineReady(), Times.Once());
        }

        [Fact]
        public void ExecuteOneFunctionTest()
        {
            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockPowerAppFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object);
            powerFxEngine.Setup();
            var result = powerFxEngine.Execute("1+1", new CultureInfo("en-US"));
            Assert.Equal(2, ((NumberValue)result).Value);
            LoggingTestHelper.VerifyLogging(MockLogger, "Attempting:\n\n{\n1+1}", LogLevel.Trace, Times.Once());

            result = powerFxEngine.Execute("=1+1", new CultureInfo("en-US"));
            Assert.Equal(2, ((NumberValue)result).Value);
            LoggingTestHelper.VerifyLogging(MockLogger, "Attempting:\n\n{\n1+1}", LogLevel.Trace, Times.Exactly(2));
        }

        [Fact]
        public void ExecuteMultipleFunctionsTest()
        {
            var powerFxExpression = "1+1; //some comment \n 2+2;\n Concatenate(\"hello\", \"world\");";
            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockPowerAppFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object);
            powerFxEngine.Setup();
            var result = powerFxEngine.Execute(powerFxExpression, It.IsAny<CultureInfo>());
            Assert.Equal("helloworld", ((StringValue)result).Value);
            LoggingTestHelper.VerifyLogging(MockLogger, $"Attempting:\n\n{{\n{powerFxExpression}}}", LogLevel.Trace, Times.Once());

            result = powerFxEngine.Execute($"={powerFxExpression}", It.IsAny<CultureInfo>());
            Assert.Equal("helloworld", ((StringValue)result).Value);
            LoggingTestHelper.VerifyLogging(MockLogger, $"Attempting:\n\n{{\n{powerFxExpression}}}", LogLevel.Trace, Times.Exactly(2));
        }

        [Fact]
        public void ExecuteMultipleFunctionsWithDifferentLocaleTest()
        {
            // en-US locale
            var culture = new CultureInfo("en-US");
            var enUSpowerFxExpression = "1+1;2+2;";
            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockPowerAppFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object);
            powerFxEngine.Setup();
            var enUSResult = powerFxEngine.Execute(enUSpowerFxExpression, culture);

            // fr locale
            culture = new CultureInfo("fr");
            var frpowerFxExpression = "1+1;;2+2;;";
            powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockPowerAppFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object);
            powerFxEngine.Setup();
            var frResult = powerFxEngine.Execute(frpowerFxExpression, culture);

            // Assertions
            Assert.Equal(4, ((NumberValue)enUSResult).Value);
            LoggingTestHelper.VerifyLogging(MockLogger, $"Attempting:\n\n{{\n{enUSpowerFxExpression}}}", LogLevel.Trace, Times.Once());
            Assert.Equal(4, ((NumberValue)frResult).Value);
            LoggingTestHelper.VerifyLogging(MockLogger, $"Attempting:\n\n{{\n{frpowerFxExpression}}}", LogLevel.Trace, Times.Once());
        }

        [Fact]
        public async Task ExecuteWithVariablesTest()
        {
            var recordType = RecordType.Empty().Add("Text", FormulaType.String);
            var label1 = new ControlRecordValue(recordType, MockPowerAppFunctions.Object, "Label1");
            var label2 = new ControlRecordValue(recordType, MockPowerAppFunctions.Object, "Label2");
            var powerFxExpression = "Concatenate(Text(Label1.Text), Text(Label2.Text))";
            var label1Text = "Hello";
            var label2Text = "World";
            var label1JsProperty = new JSPropertyValueModel()
            {
                PropertyValue = label1Text,
            };
            var label2JsProperty = new JSPropertyValueModel()
            {
                PropertyValue = label2Text,
            };
            var label1ItemPath = new ItemPath
            {
                ControlName = "Label1",
                PropertyName = "Text"
            };
            var label2ItemPath = new ItemPath
            {
                ControlName = "Label2",
                PropertyName = "Text"
            };

            MockPowerAppFunctions.Setup(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((itemPath) => itemPath.ControlName == "Label1")))
                .Returns(JsonConvert.SerializeObject(label1JsProperty));
            MockPowerAppFunctions.Setup(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((itemPath) => itemPath.ControlName == "Label2")))
                .Returns(JsonConvert.SerializeObject(label2JsProperty));
            MockPowerAppFunctions.Setup(x => x.LoadPowerAppsObjectModelAsync()).Returns(Task.FromResult(new Dictionary<string, ControlRecordValue>() { { "Label1", label1 }, { "Label2", label2 } }));
            MockPowerAppFunctions.Setup(x => x.CheckAndHandleIfLegacyPlayerAsync()).Returns(Task.FromResult(true));
            MockPowerAppFunctions.Setup(x => x.CheckIfAppIsIdleAsync()).Returns(Task.FromResult(true));

            var testSettings = new TestSettings() { Timeout = 3000 };
            MockTestState.Setup(x => x.GetTestSettings()).Returns(testSettings);

            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockPowerAppFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object);
            powerFxEngine.Setup();
            await powerFxEngine.UpdatePowerFxModelAsync();

            MockPowerAppFunctions.Verify(x => x.LoadPowerAppsObjectModelAsync(), Times.Once());

            var result = powerFxEngine.Execute(powerFxExpression, It.IsAny<CultureInfo>());
            Assert.Equal($"{label1Text}{label2Text}", ((StringValue)result).Value);
            LoggingTestHelper.VerifyLogging(MockLogger, $"Attempting:\n\n{{\n{powerFxExpression}}}", LogLevel.Trace, Times.Once());
            MockPowerAppFunctions.Verify(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((itemPath) => itemPath.ControlName == label1ItemPath.ControlName && itemPath.PropertyName == label1ItemPath.PropertyName)), Times.Once());
            MockPowerAppFunctions.Verify(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((itemPath) => itemPath.ControlName == label2ItemPath.ControlName && itemPath.PropertyName == label2ItemPath.PropertyName)), Times.Once());

            result = powerFxEngine.Execute($"={powerFxExpression}", It.IsAny<CultureInfo>());
            Assert.Equal($"{label1Text}{label2Text}", ((StringValue)result).Value);
            LoggingTestHelper.VerifyLogging(MockLogger, $"Attempting:\n\n{{\n{powerFxExpression}}}", LogLevel.Trace, Times.Exactly(2));
            MockPowerAppFunctions.Verify(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((itemPath) => itemPath.ControlName == label1ItemPath.ControlName && itemPath.PropertyName == label1ItemPath.PropertyName)), Times.Exactly(2));
            MockPowerAppFunctions.Verify(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((itemPath) => itemPath.ControlName == label2ItemPath.ControlName && itemPath.PropertyName == label2ItemPath.PropertyName)), Times.Exactly(2));
        }

        [Fact]
        public void ExecuteFailsWhenPowerFXThrowsTest()
        {
            var powerFxExpression = "someNonExistentPowerFxFunction(1, 2, 3)";
            MockPowerAppFunctions.Setup(x => x.LoadPowerAppsObjectModelAsync()).Returns(Task.FromResult(new Dictionary<string, ControlRecordValue>()));
            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockPowerAppFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object);
            powerFxEngine.Setup();
            Assert.ThrowsAsync<Exception>(async () => await powerFxEngine.ExecuteWithRetryAsync(powerFxExpression, It.IsAny<CultureInfo>()));
        }

        [Fact]
        public void ExecuteFailsWhenUsingNonExistentVariableTest()
        {
            var powerFxExpression = "Concatenate(Label1.Text, Label2.Text)";
            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockPowerAppFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object);
            powerFxEngine.Setup();
            Assert.ThrowsAsync<Exception>(async () => await powerFxEngine.ExecuteWithRetryAsync(powerFxExpression, It.IsAny<CultureInfo>()));
        }

        [Fact]
        public void ExecuteAssertFunctionTest()
        {
            var powerFxExpression = "Assert(1+1=2, \"Adding 1 + 1\")";
            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockPowerAppFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object);
            powerFxEngine.Setup();
            var result = powerFxEngine.Execute(powerFxExpression, It.IsAny<CultureInfo>());
            Assert.IsType<BlankValue>(result);

            var failingPowerFxExpression = "Assert(1+1=3, \"Supposed to fail\")";
            Assert.ThrowsAny<Exception>(() => powerFxEngine.Execute(failingPowerFxExpression, It.IsAny<CultureInfo>()));
        }

        [Fact]
        public async Task ExecuteScreenshotFunctionTest()
        {
            MockSingleTestInstanceState.Setup(x => x.GetTestResultsDirectory()).Returns("C:\\testResults");
            MockFileSystem.Setup(x => x.IsValidFilePath(It.IsAny<string>())).Returns(true);
            MockTestInfraFunctions.Setup(x => x.ScreenshotAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            var powerFxExpression = "Screenshot(\"1.jpg\")";
            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockPowerAppFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object);
            powerFxEngine.Setup();
            var result = powerFxEngine.Execute(powerFxExpression, It.IsAny<CultureInfo>());
            Assert.IsType<BlankValue>(result);

            var failingPowerFxExpression = "Screenshot(\"1.txt\")";
            Assert.ThrowsAny<Exception>(() => powerFxEngine.Execute(failingPowerFxExpression, It.IsAny<CultureInfo>()));
        }

        [Fact]
        public async Task ExecuteSelectFunctionTest()
        {
            var recordType = RecordType.Empty().Add("Text", FormulaType.String);
            var button1 = new ControlRecordValue(recordType, MockPowerAppFunctions.Object, "Button1");
            MockPowerAppFunctions.Setup(x => x.CheckAndHandleIfLegacyPlayerAsync()).Returns(Task.FromResult(true));
            MockPowerAppFunctions.Setup(x => x.SelectControlAsync(It.IsAny<ItemPath>())).Returns(Task.FromResult(true));
            MockPowerAppFunctions.Setup(x => x.LoadPowerAppsObjectModelAsync()).Returns(Task.FromResult(new Dictionary<string, ControlRecordValue>() { { "Button1", button1 } }));
            MockPowerAppFunctions.Setup(x => x.CheckIfAppIsIdleAsync()).Returns(Task.FromResult(true));

            var testSettings = new TestSettings() { Timeout = 3000 };
            MockTestState.Setup(x => x.GetTestSettings()).Returns(testSettings);

            var powerFxExpression = "Select(Button1)";
            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockPowerAppFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object);
            powerFxEngine.Setup();
            await powerFxEngine.UpdatePowerFxModelAsync();
            await powerFxEngine.ExecuteWithRetryAsync(powerFxExpression, It.IsAny<CultureInfo>());
            var result = powerFxEngine.Execute(powerFxExpression, It.IsAny<CultureInfo>());
            Assert.IsType<BlankValue>(result);
            MockPowerAppFunctions.Verify(x => x.LoadPowerAppsObjectModelAsync(), Times.Exactly(3));
        }

        [Fact]
        public async Task ExecuteSelectFunctionFailingTest()
        {
            MockPowerAppFunctions.Setup(x => x.SelectControlAsync(It.IsAny<ItemPath>())).Returns(Task.FromResult(false));
            MockPowerAppFunctions.Setup(x => x.CheckAndHandleIfLegacyPlayerAsync()).Returns(Task.FromResult(true));
            MockPowerAppFunctions.Setup(x => x.CheckIfAppIsIdleAsync()).Returns(Task.FromResult(true));

            var testSettings = new TestSettings() { Timeout = 3000 };
            MockTestState.Setup(x => x.GetTestSettings()).Returns(testSettings);

            var recordType = RecordType.Empty().Add("Text", FormulaType.String);
            var button1 = new ControlRecordValue(recordType, MockPowerAppFunctions.Object, "Button1");
            MockPowerAppFunctions.Setup(x => x.LoadPowerAppsObjectModelAsync()).Returns(Task.FromResult(new Dictionary<string, ControlRecordValue>() { { "Button1", button1 } }));

            var powerFxExpression = "Select(Button1)";
            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockPowerAppFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object);
            powerFxEngine.Setup();
            await powerFxEngine.UpdatePowerFxModelAsync();
            Assert.ThrowsAny<Exception>(() => powerFxEngine.Execute(powerFxExpression, It.IsAny<CultureInfo>()));
            MockPowerAppFunctions.Verify(x => x.LoadPowerAppsObjectModelAsync(), Times.Once());
        }

        [Fact]
        public async Task ExecuteSelectFunctionThrowsOnDifferentRecordTypeTest()
        {
            var recordType = RecordType.Empty().Add("Text", FormulaType.String);
            var otherRecordType = RecordType.Empty().Add("Foo", FormulaType.String);
            var button1 = new ControlRecordValue(otherRecordType, MockPowerAppFunctions.Object, "Button1");
            MockPowerAppFunctions.Setup(x => x.LoadPowerAppsObjectModelAsync()).Returns(Task.FromResult(new Dictionary<string, ControlRecordValue>() { { "Button1", button1 } }));
            MockPowerAppFunctions.Setup(x => x.CheckAndHandleIfLegacyPlayerAsync()).Returns(Task.FromResult(true));
            MockPowerAppFunctions.Setup(x => x.CheckIfAppIsIdleAsync()).Returns(Task.FromResult(true));

            var testSettings = new TestSettings() { Timeout = 3000 };
            MockTestState.Setup(x => x.GetTestSettings()).Returns(testSettings);

            var powerFxExpression = "Select(Button1)";
            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockPowerAppFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object);
            powerFxEngine.Setup();
            await powerFxEngine.UpdatePowerFxModelAsync();
            Assert.ThrowsAny<Exception>(() => powerFxEngine.Execute(powerFxExpression, It.IsAny<CultureInfo>()));
            MockPowerAppFunctions.Verify(x => x.LoadPowerAppsObjectModelAsync(), Times.Once());
        }

        [Fact]
        public async Task ExecuteSetPropertyFunctionTest()
        {
            var recordType = RecordType.Empty().Add("Text", FormulaType.String);
            var button1 = new ControlRecordValue(recordType, MockPowerAppFunctions.Object, "Button1");

            MockPowerAppFunctions.Setup(x => x.CheckAndHandleIfLegacyPlayerAsync()).Returns(Task.FromResult(true));
            MockPowerAppFunctions.Setup(x => x.SetPropertyAsync(It.IsAny<ItemPath>(), It.IsAny<StringValue>())).Returns(Task.FromResult(true));
            MockPowerAppFunctions.Setup(x => x.LoadPowerAppsObjectModelAsync()).Returns(Task.FromResult(new Dictionary<string, ControlRecordValue>() { { "Button1", button1 } }));
            MockPowerAppFunctions.Setup(x => x.CheckIfAppIsIdleAsync()).Returns(Task.FromResult(true));

            var testSettings = new TestSettings() { Timeout = 3000 };
            MockTestState.Setup(x => x.GetTestSettings()).Returns(testSettings);

            var powerFxExpression = "SetProperty(Button1.Text, \"10\")";
            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockPowerAppFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object);

            powerFxEngine.Setup();
            await powerFxEngine.UpdatePowerFxModelAsync();
            await powerFxEngine.ExecuteWithRetryAsync(powerFxExpression, It.IsAny<CultureInfo>());
            var result = powerFxEngine.Execute(powerFxExpression, It.IsAny<CultureInfo>());
            Assert.IsType<BooleanValue>(result);
            MockPowerAppFunctions.Verify(x => x.LoadPowerAppsObjectModelAsync(), Times.Once());
        }

        [Fact]
        public async Task ExecuteSetPropertyFunctionThrowsOnDifferentRecordTypeTest()
        {
            var wrongRecordType = RecordType.Empty().Add("Foo", FormulaType.String);
            var button1 = new ControlRecordValue(wrongRecordType, MockPowerAppFunctions.Object, "Button1");

            MockPowerAppFunctions.Setup(x => x.LoadPowerAppsObjectModelAsync()).Returns(Task.FromResult(new Dictionary<string, ControlRecordValue>() { { "Button1", button1 } }));
            MockPowerAppFunctions.Setup(x => x.CheckAndHandleIfLegacyPlayerAsync()).Returns(Task.FromResult(true));
            MockPowerAppFunctions.Setup(x => x.CheckIfAppIsIdleAsync()).Returns(Task.FromResult(true));

            var testSettings = new TestSettings() { Timeout = 3000 };
            MockTestState.Setup(x => x.GetTestSettings()).Returns(testSettings);

            var powerFxExpression = "SetProperty(Button1.Text, \"10\")";
            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockPowerAppFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object);

            powerFxEngine.Setup();
            await powerFxEngine.UpdatePowerFxModelAsync();
            Assert.ThrowsAny<Exception>(() => powerFxEngine.Execute(powerFxExpression, It.IsAny<CultureInfo>()));
            MockPowerAppFunctions.Verify(x => x.LoadPowerAppsObjectModelAsync(), Times.Once());
        }

        [Fact]
        public async Task ExecuteWaitFunctionTest()
        {
            var recordType = RecordType.Empty().Add("Text", FormulaType.String);
            var label1 = new ControlRecordValue(recordType, MockPowerAppFunctions.Object, "Label1");
            var label1Text = "1";
            var label1JsProperty = new JSPropertyValueModel()
            {
                PropertyValue = label1Text,
            };
            var itemPath = new ItemPath
            {
                ControlName = "Label1",
                PropertyName = "Text"
            };

            MockPowerAppFunctions.Setup(x => x.CheckAndHandleIfLegacyPlayerAsync()).Returns(Task.FromResult(true));
            MockPowerAppFunctions.Setup(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((input) => itemPath.ControlName == input.ControlName && itemPath.PropertyName == input.PropertyName)))
                .Returns(JsonConvert.SerializeObject(label1JsProperty));
            MockPowerAppFunctions.Setup(x => x.LoadPowerAppsObjectModelAsync()).Returns(Task.FromResult(new Dictionary<string, ControlRecordValue>() { { "Label1", label1 } }));
            MockPowerAppFunctions.Setup(x => x.CheckIfAppIsIdleAsync()).Returns(Task.FromResult(true));

            var testSettings = new TestSettings() { Timeout = 3000 };
            MockTestState.Setup(x => x.GetTestSettings()).Returns(testSettings);

            var powerFxExpression = "Wait(Label1, \"Text\", \"1\")";
            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockPowerAppFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object);
            powerFxEngine.Setup();
            await powerFxEngine.UpdatePowerFxModelAsync();
            await powerFxEngine.ExecuteWithRetryAsync(powerFxExpression, It.IsAny<CultureInfo>());
            var result = powerFxEngine.Execute(powerFxExpression, It.IsAny<CultureInfo>());
            Assert.IsType<BlankValue>(result);
            MockPowerAppFunctions.Verify(x => x.LoadPowerAppsObjectModelAsync(), Times.Once());
        }

        [Fact]
        public async Task ExecuteWaitFunctionThrowsOnDifferentRecordTypeTest()
        {
            var recordType = RecordType.Empty().Add("Text", FormulaType.String);
            var otherRecordType = RecordType.Empty().Add("Foo", FormulaType.String);
            var label1 = new ControlRecordValue(otherRecordType, MockPowerAppFunctions.Object, "Label1");
            MockPowerAppFunctions.Setup(x => x.LoadPowerAppsObjectModelAsync()).Returns(Task.FromResult(new Dictionary<string, ControlRecordValue>() { { "Label1", label1 } }));
            MockPowerAppFunctions.Setup(x => x.CheckAndHandleIfLegacyPlayerAsync()).Returns(Task.FromResult(true));
            MockPowerAppFunctions.Setup(x => x.CheckIfAppIsIdleAsync()).Returns(Task.FromResult(true));

            var testSettings = new TestSettings() { Timeout = 3000 };
            MockTestState.Setup(x => x.GetTestSettings()).Returns(testSettings);

            var powerFxExpression = "Wait(Label1, \"Text\", \"1\")";
            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockPowerAppFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object);
            powerFxEngine.Setup();
            await powerFxEngine.UpdatePowerFxModelAsync();
            Assert.ThrowsAny<Exception>(() => powerFxEngine.Execute(powerFxExpression, It.IsAny<CultureInfo>()));
            MockPowerAppFunctions.Verify(x => x.LoadPowerAppsObjectModelAsync(), Times.Once());
        }

        [Fact]
        public async Task TestStepByStep()
        {
            // Arrange
            var powerFxEngine = GetPowerFxEngine();
            int updateCounter = 0;
            var otherRecordType = RecordType.Empty().Add("Foo", FormulaType.String);
            var label1 = new ControlRecordValue(otherRecordType, MockPowerAppFunctions.Object, "Label1");
            var label2 = new ControlRecordValue(otherRecordType, MockPowerAppFunctions.Object, "Label2");
            var label3 = new ControlRecordValue(otherRecordType, MockPowerAppFunctions.Object, "Label3");
            MockPowerAppFunctions.Setup(x => x.LoadPowerAppsObjectModelAsync()).Returns(() =>
            {
                if (updateCounter == 0)
                {
                    ++updateCounter;
                    return Task.FromResult(new Dictionary<string, ControlRecordValue>() { { "Label1", label1 } });
                }
                else if (updateCounter == 1)
                {
                    ++updateCounter;
                    return Task.FromResult(new Dictionary<string, ControlRecordValue>() { { "Label2", label2 } });
                }
                else
                {
                    return Task.FromResult(new Dictionary<string, ControlRecordValue>() { { "Label3", label3 } });
                }
            });
            MockPowerAppFunctions.Setup(x => x.CheckAndHandleIfLegacyPlayerAsync()).Returns(Task.FromResult(true));
            MockPowerAppFunctions.Setup(x => x.CheckIfAppIsIdleAsync()).Returns(Task.FromResult(true));
            MockPowerAppFunctions.Setup(x => x.SelectControlAsync(It.IsAny<ItemPath>())).Callback(async () =>
            {
                await powerFxEngine.UpdatePowerFxModelAsync();
            }).Returns(Task.FromResult(true));

            var oldUICulture = CultureInfo.CurrentUICulture;
            var frenchCulture = new CultureInfo("fr");
            CultureInfo.CurrentUICulture = frenchCulture;
            powerFxEngine.Setup();
            var testSettings = new TestSettings() { Timeout = 3000 };
            MockTestState.Setup(x => x.GetTestSettings()).Returns(testSettings);
            var expression = "Select(Label1/*Label;;22*/);;\"Just stirng \n;literal\";;Select(Label2)\n;;Select(Label3);;Assert(1=1; \"Supposed to pass;;\");;Max(1,2)";

            // Act 

            // Engine.Eval should throw an exception when none of the used first names exist in the underlying symbol table yet.
            // This confirms that we would be hitting goStepByStep branch
            Assert.ThrowsAny<Exception>(() => powerFxEngine.Execute(expression, frenchCulture));
            await powerFxEngine.UpdatePowerFxModelAsync();
            var result = powerFxEngine.Execute(expression, frenchCulture);

            try
            {
                CultureInfo.CurrentUICulture = oldUICulture;
            }
            catch
            {
                // no op
            }

            // Assert
            Assert.Equal(2, updateCounter);
            Assert.Equal(FormulaType.Number, result.Type);
            Assert.Equal("1.2", (result as NumberValue).Value.ToString());
            LoggingTestHelper.VerifyLogging(MockLogger, $"Syntax check failed. Now attempting to execute lines step by step", LogLevel.Debug, Times.Exactly(2));
        }

        private PowerFxEngine GetPowerFxEngine()
        {
            return new PowerFxEngine(MockTestInfraFunctions.Object, MockPowerAppFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object);
        }
    }
}
