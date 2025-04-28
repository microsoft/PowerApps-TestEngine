// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Modules;
using Microsoft.PowerApps.TestEngine.PowerFx;
using Microsoft.PowerApps.TestEngine.Providers;
using Microsoft.PowerApps.TestEngine.Providers.PowerFxModel;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerApps.TestEngine.Tests.Helpers;
using Microsoft.PowerFx;
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
        private Mock<ITestWebProvider> MockTestWebProvider;
        private Mock<IFileSystem> MockFileSystem;
        private Mock<ISingleTestInstanceState> MockSingleTestInstanceState;
        private Mock<IEnvironmentVariable> MockEnvironmentVariable;
        private Mock<Microsoft.Extensions.Logging.ILogger> MockLogger;
        private TestSettings testSettings = new TestSettings();

        public PowerFxEngineTests()
        {
            MockTestInfraFunctions = new Mock<ITestInfraFunctions>(MockBehavior.Strict);
            MockTestState = new Mock<ITestState>(MockBehavior.Strict);
            MockTestWebProvider = new Mock<ITestWebProvider>(MockBehavior.Strict);
            MockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            MockSingleTestInstanceState = new Mock<ISingleTestInstanceState>(MockBehavior.Strict);
            MockLogger = new Mock<Microsoft.Extensions.Logging.ILogger>(MockBehavior.Strict);
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            MockTestState.Setup(x => x.GetTimeout()).Returns(30000);
            MockEnvironmentVariable = new Mock<IEnvironmentVariable>(MockBehavior.Strict);
            LoggingTestHelper.SetupMock(MockLogger);
            MockTestState.Setup(x => x.TestProvider).Returns((ITestWebProvider)null);
            testSettings = new TestSettings();
        }

        [Fact]
        public void SetupDoesNotThrow()
        {
            MockTestState.Setup(x => x.GetTestSettings()).Returns(testSettings);
            MockTestState.Setup(x => x.GetTestEngineModules()).Returns(new List<ITestEngineModule>());
            MockTestState.Setup(x => x.GetDomain()).Returns("https://contoso.crm.dynamics.com");

            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockTestWebProvider.Object, MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object, MockEnvironmentVariable.Object);

            powerFxEngine.GetAzureCliHelper = () => null;
            powerFxEngine.Setup(testSettings);
        }

        [Fact]
        public async Task ExecuteThrowsOnNoSetupTest()
        {
            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockTestWebProvider.Object, MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object, MockEnvironmentVariable.Object);
            await Assert.ThrowsAsync<InvalidOperationException>(() => powerFxEngine.ExecuteAsync("", It.IsAny<CultureInfo>()));
            LoggingTestHelper.VerifyLogging(MockLogger, "Engine is null, make sure to call Setup first", LogLevel.Error, Times.Once());
        }

        [Fact]
        public async Task UpdatePowerFxModelAsyncThrowsOnNoSetupTest()
        {
            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockTestWebProvider.Object, MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object, MockEnvironmentVariable.Object);
            await Assert.ThrowsAsync<InvalidOperationException>(() => powerFxEngine.UpdatePowerFxModelAsync());
            LoggingTestHelper.VerifyLogging(MockLogger, "Engine is null, make sure to call Setup first", LogLevel.Error, Times.Once());
        }

        [Fact]
        public async Task UpdatePowerFxModelAsyncThrowsOnCantGetAppStatusTest()
        {
            var recordType = RecordType.Empty().Add("Text", FormulaType.String);
            var button1 = new ControlRecordValue(recordType, MockTestWebProvider.Object, "Button1");
            MockTestWebProvider.Setup(x => x.SelectControlAsync(It.IsAny<ItemPath>(), null)).Returns(Task.FromResult(true));
            MockTestWebProvider.Setup(x => x.LoadObjectModelAsync()).Returns(Task.FromResult(new Dictionary<string, ControlRecordValue>() { { "Button1", button1 } }));
            MockTestWebProvider.Setup(x => x.CheckIsIdleAsync()).Returns(Task.FromResult(false));

            var testSettings = new TestSettings() { Timeout = 3000 };
            MockTestState.Setup(x => x.GetTestSettings()).Returns(testSettings);
            MockTestState.Setup(x => x.GetTestEngineModules()).Returns(new List<ITestEngineModule>());
            MockTestState.Setup(x => x.GetDomain()).Returns("https://contoso.crm.dynamics.com");

            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockTestWebProvider.Object, MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object, MockEnvironmentVariable.Object);

            powerFxEngine.GetAzureCliHelper = () => null;
            powerFxEngine.Setup(testSettings);
            await Assert.ThrowsAsync<TimeoutException>(() => powerFxEngine.UpdatePowerFxModelAsync());
            LoggingTestHelper.VerifyLogging(MockLogger, "Something went wrong when Test Engine tried to get App status.", LogLevel.Error, Times.Once());
        }

        [Fact]
        public async Task RunRequirementsCheckAsyncTest()
        {
            MockTestState.Setup(x => x.GetTestSettings()).Returns(new TestSettings());
            MockTestState.Setup(x => x.GetTestEngineModules()).Returns(new List<ITestEngineModule>());
            MockTestState.Setup(x => x.GetDomain()).Returns("https://contoso.crm.dynamics.com");

            MockTestWebProvider.Setup(x => x.CheckProviderAsync()).Returns(Task.CompletedTask);
            MockTestWebProvider.Setup(x => x.TestEngineReady()).Returns(Task.FromResult(true));

            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockTestWebProvider.Object, MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object, MockEnvironmentVariable.Object);

            powerFxEngine.GetAzureCliHelper = () => null;
            powerFxEngine.Setup(testSettings);

            await powerFxEngine.RunRequirementsCheckAsync();

            MockTestWebProvider.Verify(x => x.CheckProviderAsync(), Times.Once());
            MockTestWebProvider.Verify(x => x.TestEngineReady(), Times.Once());
        }

        [Fact]
        public async Task RunRequirementsCheckAsyncThrowsOnCheckAndHandleIfLegacyPlayerTest()
        {
            MockTestState.Setup(x => x.GetTestSettings()).Returns(new TestSettings());
            MockTestState.Setup(x => x.GetTestEngineModules()).Returns(new List<ITestEngineModule>());
            MockTestState.Setup(x => x.GetDomain()).Returns("https://contoso.crm.dynamics.com");

            MockTestWebProvider.Setup(x => x.CheckProviderAsync()).Throws(new Exception());
            MockTestWebProvider.Setup(x => x.TestEngineReady()).Returns(Task.FromResult(true));

            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockTestWebProvider.Object, MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object, MockEnvironmentVariable.Object);

            powerFxEngine.GetAzureCliHelper = () => null;
            powerFxEngine.Setup(testSettings);

            await Assert.ThrowsAsync<Exception>(() => powerFxEngine.RunRequirementsCheckAsync());

            MockTestWebProvider.Verify(x => x.CheckProviderAsync(), Times.Once());
            MockTestWebProvider.Verify(x => x.TestEngineReady(), Times.Never());
        }

        [Fact]
        public async Task RunRequirementsCheckAsyncThrowsOnTestEngineReadyTest()
        {
            MockTestState.Setup(x => x.GetTestSettings()).Returns(new TestSettings());
            MockTestState.Setup(x => x.GetTestEngineModules()).Returns(new List<ITestEngineModule>());
            MockTestState.Setup(x => x.GetDomain()).Returns("https://contoso.crm.dynamics.com");

            MockTestWebProvider.Setup(x => x.CheckProviderAsync()).Returns(Task.CompletedTask);
            MockTestWebProvider.Setup(x => x.TestEngineReady()).Throws(new Exception());

            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockTestWebProvider.Object, MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object, MockEnvironmentVariable.Object);

            powerFxEngine.GetAzureCliHelper = () => null;
            powerFxEngine.Setup(testSettings);

            await Assert.ThrowsAsync<Exception>(() => powerFxEngine.RunRequirementsCheckAsync());

            MockTestWebProvider.Verify(x => x.CheckProviderAsync(), Times.Once());
            MockTestWebProvider.Verify(x => x.TestEngineReady(), Times.Once());
        }

        [Fact]
        public async Task ExecuteOneFunctionTest()
        {
            MockTestState.Setup(x => x.GetTestSettings()).Returns(new TestSettings());
            MockTestState.Setup(x => x.GetTestEngineModules()).Returns(new List<ITestEngineModule>());
            MockTestState.SetupGet(x => x.ExecuteStepByStep).Returns(false);
            MockTestState.Setup(x => x.OnBeforeTestStepExecuted(It.IsAny<TestStepEventArgs>()));
            MockTestState.Setup(x => x.OnAfterTestStepExecuted(It.IsAny<TestStepEventArgs>()));
            MockTestState.Setup(x => x.GetDomain()).Returns("https://contoso.crm.dynamics.com");

            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockTestWebProvider.Object, MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object, MockEnvironmentVariable.Object);
            MockTestState.Setup(x => x.GetTestSettings()).Returns<TestSettings>(null);
            powerFxEngine.GetAzureCliHelper = () => null;
            powerFxEngine.Setup(testSettings);
            var result = await powerFxEngine.ExecuteAsync("1+1", new CultureInfo("en-US"));
            Assert.Equal(2, ((NumberValue)result).Value);
            LoggingTestHelper.VerifyLogging(MockLogger, "Attempting:\n\n{\n1+1}", LogLevel.Trace, Times.Once());

            result = await powerFxEngine.ExecuteAsync("=1+1", new CultureInfo("en-US"));
            Assert.Equal(2, ((NumberValue)result).Value);
            LoggingTestHelper.VerifyLogging(MockLogger, "Attempting:\n\n{\n1+1}", LogLevel.Trace, Times.Exactly(2));
        }

        [Fact]
        public async Task ExecuteMultipleFunctionsTest()
        {
            MockTestState.Setup(x => x.GetTestSettings()).Returns(new TestSettings());
            MockTestState.Setup(x => x.GetTestEngineModules()).Returns(new List<ITestEngineModule>());
            MockTestState.SetupGet(x => x.ExecuteStepByStep).Returns(false);
            MockTestState.Setup(x => x.OnBeforeTestStepExecuted(It.IsAny<TestStepEventArgs>()));
            MockTestState.Setup(x => x.OnAfterTestStepExecuted(It.IsAny<TestStepEventArgs>()));
            MockTestState.SetupGet(x => x.ExecuteStepByStep).Returns(false);
            MockTestState.Setup(x => x.OnBeforeTestStepExecuted(It.IsAny<TestStepEventArgs>()));
            MockTestState.Setup(x => x.OnAfterTestStepExecuted(It.IsAny<TestStepEventArgs>()));
            MockTestState.Setup(x => x.GetDomain()).Returns("https://contoso.crm.dynamics.com");

            var powerFxExpression = "1+1; //some comment \n 2+2;\n Concatenate(\"hello\", \"world\");";
            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockTestWebProvider.Object, MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object, MockEnvironmentVariable.Object);
            MockTestState.Setup(x => x.GetTestSettings()).Returns<TestSettings>(null);
            powerFxEngine.GetAzureCliHelper = () => null;
            powerFxEngine.Setup(testSettings);
            var result = await powerFxEngine.ExecuteAsync(powerFxExpression, It.IsAny<CultureInfo>());
            Assert.Equal("helloworld", ((StringValue)result).Value);
            LoggingTestHelper.VerifyLogging(MockLogger, $"Attempting:\n\n{{\n{powerFxExpression}}}", LogLevel.Trace, Times.Once());

            result = await powerFxEngine.ExecuteAsync($"={powerFxExpression}", It.IsAny<CultureInfo>());
            Assert.Equal("helloworld", ((StringValue)result).Value);
            LoggingTestHelper.VerifyLogging(MockLogger, $"Attempting:\n\n{{\n{powerFxExpression}}}", LogLevel.Trace, Times.Exactly(2));
        }

        [Fact]
        public async Task ExecuteMultipleFunctionsWithDifferentLocaleTest()
        {
            MockTestState.Setup(x => x.GetTestSettings()).Returns(new TestSettings());
            MockTestState.Setup(x => x.GetTestEngineModules()).Returns(new List<ITestEngineModule>());
            MockTestState.SetupGet(x => x.ExecuteStepByStep).Returns(false);
            MockTestState.Setup(x => x.OnBeforeTestStepExecuted(It.IsAny<TestStepEventArgs>()));
            MockTestState.Setup(x => x.OnAfterTestStepExecuted(It.IsAny<TestStepEventArgs>()));
            MockTestState.Setup(x => x.GetDomain()).Returns("https://contoso.crm.dynamics.com");

            // en-US locale
            var culture = new CultureInfo("en-US");
            var enUSpowerFxExpression = "1+1;2+2;";
            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockTestWebProvider.Object, MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object, MockEnvironmentVariable.Object);
            powerFxEngine.GetAzureCliHelper = () => null;
            powerFxEngine.Setup(testSettings);
            var enUSResult = await powerFxEngine.ExecuteAsync(enUSpowerFxExpression, culture);

            // fr locale
            culture = new CultureInfo("fr");
            var frpowerFxExpression = "1+1;;2+2;;";
            powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockTestWebProvider.Object, MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object, MockEnvironmentVariable.Object);

            powerFxEngine.GetAzureCliHelper = () => null;
            powerFxEngine.Setup(testSettings);
            var frResult = await powerFxEngine.ExecuteAsync(frpowerFxExpression, culture);

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
            var label1 = new ControlRecordValue(recordType, MockTestWebProvider.Object, "Label1");
            var label2 = new ControlRecordValue(recordType, MockTestWebProvider.Object, "Label2");
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

            MockTestWebProvider.Setup(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((itemPath) => itemPath.ControlName == "Label1")))
                .Returns(JsonConvert.SerializeObject(label1JsProperty));
            MockTestWebProvider.Setup(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((itemPath) => itemPath.ControlName == "Label2")))
                .Returns(JsonConvert.SerializeObject(label2JsProperty));
            MockTestWebProvider.Setup(x => x.LoadObjectModelAsync()).Returns(Task.FromResult(new Dictionary<string, ControlRecordValue>() { { "Label1", label1 }, { "Label2", label2 } }));
            MockTestWebProvider.Setup(x => x.CheckProviderAsync()).Returns(Task.FromResult(true));
            MockTestWebProvider.Setup(x => x.CheckIsIdleAsync()).Returns(Task.FromResult(true));

            var testSettings = new TestSettings() { Timeout = 3000 };
            MockTestState.Setup(x => x.GetTestSettings()).Returns(testSettings);
            MockTestState.Setup(x => x.GetTestEngineModules()).Returns(new List<ITestEngineModule>());
            MockTestState.SetupGet(x => x.ExecuteStepByStep).Returns(false);
            MockTestState.Setup(x => x.OnBeforeTestStepExecuted(It.IsAny<TestStepEventArgs>()));
            MockTestState.Setup(x => x.OnAfterTestStepExecuted(It.IsAny<TestStepEventArgs>()));
            MockTestState.Setup(x => x.GetDomain()).Returns("https://contoso.crm.dynamics.com");

            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockTestWebProvider.Object, MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object, MockEnvironmentVariable.Object);

            powerFxEngine.GetAzureCliHelper = () => null;
            powerFxEngine.Setup(testSettings);
            await powerFxEngine.UpdatePowerFxModelAsync();

            MockTestWebProvider.Verify(x => x.LoadObjectModelAsync(), Times.Once());

            var result = await powerFxEngine.ExecuteAsync(powerFxExpression, It.IsAny<CultureInfo>());
            Assert.Equal($"{label1Text}{label2Text}", ((StringValue)result).Value);
            LoggingTestHelper.VerifyLogging(MockLogger, $"Attempting:\n\n{{\n{powerFxExpression}}}", LogLevel.Trace, Times.Once());
            MockTestWebProvider.Verify(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((itemPath) => itemPath.ControlName == label1ItemPath.ControlName && itemPath.PropertyName == label1ItemPath.PropertyName)), Times.Once());
            MockTestWebProvider.Verify(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((itemPath) => itemPath.ControlName == label2ItemPath.ControlName && itemPath.PropertyName == label2ItemPath.PropertyName)), Times.Once());

            result = await powerFxEngine.ExecuteAsync($"={powerFxExpression}", It.IsAny<CultureInfo>());
            Assert.Equal($"{label1Text}{label2Text}", ((StringValue)result).Value);
            LoggingTestHelper.VerifyLogging(MockLogger, $"Attempting:\n\n{{\n{powerFxExpression}}}", LogLevel.Trace, Times.Exactly(2));
            MockTestWebProvider.Verify(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((itemPath) => itemPath.ControlName == label1ItemPath.ControlName && itemPath.PropertyName == label1ItemPath.PropertyName)), Times.Exactly(2));
            MockTestWebProvider.Verify(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((itemPath) => itemPath.ControlName == label2ItemPath.ControlName && itemPath.PropertyName == label2ItemPath.PropertyName)), Times.Exactly(2));
        }

        [Fact]
        public async Task ExecuteFailsWhenPowerFXThrowsTest()
        {
            MockTestState.Setup(x => x.GetTestSettings()).Returns(new TestSettings());
            MockTestState.Setup(x => x.GetTestEngineModules()).Returns(new List<ITestEngineModule>());
            MockTestState.SetupGet(x => x.ExecuteStepByStep).Returns(false);
            MockTestState.Setup(x => x.OnBeforeTestStepExecuted(It.IsAny<TestStepEventArgs>()));
            MockTestState.Setup(x => x.OnAfterTestStepExecuted(It.IsAny<TestStepEventArgs>()));
            MockTestState.Setup(x => x.GetDomain()).Returns("https://contoso.crm.dynamics.com");
            MockTestWebProvider.Setup(m => m.GenerateTestUrl("https://contoso.crm.dynamics.com", "")).Returns("https://contoso.crm.dynamics.com");

            var powerFxExpression = "someNonExistentPowerFxFunction(1, 2, 3)";
            MockTestWebProvider.Setup(x => x.LoadObjectModelAsync()).Returns(Task.FromResult(new Dictionary<string, ControlRecordValue>()));
            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockTestWebProvider.Object, MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object, MockEnvironmentVariable.Object);
            powerFxEngine.GetAzureCliHelper = () => null;
            powerFxEngine.Setup(testSettings);
            await Assert.ThrowsAsync<InvalidOperationException>(() => powerFxEngine.ExecuteWithRetryAsync(powerFxExpression, It.IsAny<CultureInfo>()));
        }

        [Fact]
        public async Task ExecuteFailsWhenUsingNonExistentVariableTest()
        {
            MockTestState.Setup(x => x.GetTestSettings()).Returns(new TestSettings());
            MockTestState.Setup(x => x.GetTestEngineModules()).Returns(new List<ITestEngineModule>());
            MockTestState.SetupGet(x => x.ExecuteStepByStep).Returns(false);
            MockTestState.Setup(x => x.OnBeforeTestStepExecuted(It.IsAny<TestStepEventArgs>()));
            MockTestState.Setup(x => x.OnAfterTestStepExecuted(It.IsAny<TestStepEventArgs>()));
            MockTestState.Setup(x => x.GetDomain()).Returns("https://contoso.crm.dynamics.com");

            var powerFxExpression = "Concatenate(Label1.Text, Label2.Text)";
            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockTestWebProvider.Object, MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object, MockEnvironmentVariable.Object);
            powerFxEngine.GetAzureCliHelper = () => null;
            powerFxEngine.Setup(testSettings);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await powerFxEngine.ExecuteWithRetryAsync(powerFxExpression, It.IsAny<CultureInfo>()));
        }

        [Fact]
        public async Task ExecuteAssertFunctionTest()
        {
            MockTestState.Setup(x => x.GetTestSettings()).Returns(new TestSettings());
            MockTestState.Setup(x => x.GetTestEngineModules()).Returns(new List<ITestEngineModule>());
            MockTestState.SetupGet(x => x.ExecuteStepByStep).Returns(false);
            MockTestState.Setup(x => x.OnBeforeTestStepExecuted(It.IsAny<TestStepEventArgs>()));
            MockTestState.Setup(x => x.OnAfterTestStepExecuted(It.IsAny<TestStepEventArgs>()));
            MockTestState.Setup(x => x.GetDomain()).Returns("https://contoso.crm.dynamics.com");

            var powerFxExpression = "Assert(1+1=2, \"Adding 1 + 1\")";
            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockTestWebProvider.Object, MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object, MockEnvironmentVariable.Object);
            powerFxEngine.GetAzureCliHelper = () => null;
            powerFxEngine.Setup(testSettings);
            var result = await powerFxEngine.ExecuteAsync(powerFxExpression, It.IsAny<CultureInfo>());
            Assert.IsType<BlankValue>(result);

            var failingPowerFxExpression = "Assert(1+1=3, \"Supposed to fail\")";
            await Assert.ThrowsAnyAsync<Exception>(() => powerFxEngine.ExecuteAsync(failingPowerFxExpression, It.IsAny<CultureInfo>()));
        }

        [Fact]
        public async Task ExecuteScreenshotFunctionTest()
        {
            MockTestState.Setup(x => x.GetTestSettings()).Returns(new TestSettings());
            MockTestState.Setup(x => x.GetTestEngineModules()).Returns(new List<ITestEngineModule>());
            MockTestState.SetupGet(x => x.ExecuteStepByStep).Returns(false);
            MockTestState.Setup(x => x.OnBeforeTestStepExecuted(It.IsAny<TestStepEventArgs>()));
            MockTestState.Setup(x => x.OnAfterTestStepExecuted(It.IsAny<TestStepEventArgs>()));
            MockTestState.Setup(x => x.GetDomain()).Returns("https://contoso.crm.dynamics.com");

            MockSingleTestInstanceState.Setup(x => x.GetTestResultsDirectory()).Returns("C:\\testResults");
            MockFileSystem.Setup(x => x.Exists(It.IsAny<string>())).Returns(true);
            MockTestInfraFunctions.Setup(x => x.ScreenshotAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            var powerFxExpression = "Screenshot(\"1.jpg\")";
            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockTestWebProvider.Object, MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object, MockEnvironmentVariable.Object);
            powerFxEngine.GetAzureCliHelper = () => null;
            powerFxEngine.Setup(testSettings);
            var result = await powerFxEngine.ExecuteAsync(powerFxExpression, It.IsAny<CultureInfo>());
            Assert.IsType<BlankValue>(result);

            var failingPowerFxExpression = "Screenshot(\"1.txt\")";
            await Assert.ThrowsAsync<TargetInvocationException>(() => powerFxEngine.ExecuteAsync(failingPowerFxExpression, It.IsAny<CultureInfo>()));
        }

        [Fact]
        public async Task ExecuteSelectFunctionTest()
        {
            var recordType = RecordType.Empty().Add("Text", FormulaType.String);
            var button1 = new ControlRecordValue(recordType, MockTestWebProvider.Object, "Button1");
            MockTestWebProvider.Setup(x => x.CheckProviderAsync()).Returns(Task.FromResult(true));
            MockTestWebProvider.Setup(x => x.SelectControlAsync(It.IsAny<ItemPath>(), null)).Returns(Task.FromResult(true));
            MockTestWebProvider.Setup(x => x.LoadObjectModelAsync()).Returns(Task.FromResult(new Dictionary<string, ControlRecordValue>() { { "Button1", button1 } }));
            MockTestWebProvider.Setup(x => x.CheckIsIdleAsync()).Returns(Task.FromResult(true));
            MockTestState.Setup(x => x.GetDomain()).Returns("https://contoso.crm.dynamics.com");

            var testSettings = new TestSettings() { Timeout = 3000 };
            MockTestState.Setup(x => x.GetTestSettings()).Returns(testSettings);
            MockTestState.Setup(x => x.GetTestEngineModules()).Returns(new List<ITestEngineModule>());
            MockTestState.SetupGet(x => x.ExecuteStepByStep).Returns(false);
            MockTestState.Setup(x => x.OnBeforeTestStepExecuted(It.IsAny<TestStepEventArgs>()));
            MockTestState.Setup(x => x.OnAfterTestStepExecuted(It.IsAny<TestStepEventArgs>()));

            var powerFxExpression = "Select(Button1)";
            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockTestWebProvider.Object, MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object, MockEnvironmentVariable.Object);
            powerFxEngine.GetAzureCliHelper = () => null;
            powerFxEngine.Setup(testSettings);
            await powerFxEngine.UpdatePowerFxModelAsync();
            await powerFxEngine.ExecuteWithRetryAsync(powerFxExpression, It.IsAny<CultureInfo>());
            var result = await powerFxEngine.ExecuteAsync(powerFxExpression, It.IsAny<CultureInfo>());
            Assert.IsType<BlankValue>(result);
            MockTestWebProvider.Verify(x => x.LoadObjectModelAsync(), Times.Exactly(3));
        }

        [Fact]
        public async Task ExecuteSelectFunctionFailingTest()
        {
            MockTestWebProvider.Setup(x => x.SelectControlAsync(It.IsAny<ItemPath>(), null)).Returns(Task.FromResult(false));
            MockTestWebProvider.Setup(x => x.CheckProviderAsync()).Returns(Task.FromResult(true));
            MockTestWebProvider.Setup(x => x.CheckIsIdleAsync()).Returns(Task.FromResult(true));
            MockTestState.Setup(x => x.ExecuteStepByStep).Returns(false);
            MockTestState.Setup(x => x.OnBeforeTestStepExecuted(It.IsAny<TestStepEventArgs>()));
            MockTestState.Setup(x => x.OnAfterTestStepExecuted(It.IsAny<TestStepEventArgs>()));
            MockTestState.Setup(x => x.GetDomain()).Returns("https://contoso.crm.dynamics.com");

            var testSettings = new TestSettings() { Timeout = 3000 };
            MockTestState.Setup(x => x.GetTestSettings()).Returns(testSettings);
            MockTestState.Setup(x => x.GetTestEngineModules()).Returns(new List<ITestEngineModule>());

            var recordType = RecordType.Empty().Add("Text", FormulaType.String);
            var button1 = new ControlRecordValue(recordType, MockTestWebProvider.Object, "Button1");
            MockTestWebProvider.Setup(x => x.LoadObjectModelAsync()).Returns(Task.FromResult(new Dictionary<string, ControlRecordValue>() { { "Button1", button1 } }));

            var powerFxExpression = "Select(Button1)";
            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockTestWebProvider.Object, MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object, MockEnvironmentVariable.Object);
            powerFxEngine.GetAzureCliHelper = () => null;
            powerFxEngine.Setup(testSettings);
            await powerFxEngine.UpdatePowerFxModelAsync();
            await Assert.ThrowsAsync<TargetInvocationException>(() => powerFxEngine.ExecuteAsync(powerFxExpression, It.IsAny<CultureInfo>()));
            MockTestWebProvider.Verify(x => x.LoadObjectModelAsync(), Times.Once());
        }

        [Fact]
        public async Task ExecuteSelectFunctionThrowsOnDifferentRecordTypeTest()
        {
            var recordType = RecordType.Empty().Add("Text", FormulaType.String);
            var otherRecordType = RecordType.Empty().Add("Foo", FormulaType.String);
            var button1 = new ControlRecordValue(otherRecordType, MockTestWebProvider.Object, "Button1");
            MockTestWebProvider.Setup(x => x.LoadObjectModelAsync()).Returns(Task.FromResult(new Dictionary<string, ControlRecordValue>() { { "Button1", button1 } }));
            MockTestWebProvider.Setup(x => x.CheckProviderAsync()).Returns(Task.FromResult(true));
            MockTestWebProvider.Setup(x => x.CheckIsIdleAsync()).Returns(Task.FromResult(true));
            MockTestState.Setup(x => x.ExecuteStepByStep).Returns(false);
            MockTestState.Setup(x => x.OnBeforeTestStepExecuted(It.IsAny<TestStepEventArgs>()));
            MockTestState.Setup(x => x.OnAfterTestStepExecuted(It.IsAny<TestStepEventArgs>()));
            MockTestState.Setup(x => x.GetDomain()).Returns("https://contoso.crm.dynamics.com");

            var testSettings = new TestSettings() { Timeout = 3000 };
            MockTestState.Setup(x => x.GetTestSettings()).Returns(testSettings);
            MockTestState.Setup(x => x.GetTestEngineModules()).Returns(new List<ITestEngineModule>());

            var powerFxExpression = "Select(Button1)";
            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockTestWebProvider.Object, MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object, MockEnvironmentVariable.Object);
            powerFxEngine.GetAzureCliHelper = () => null;
            powerFxEngine.Setup(testSettings);
            await powerFxEngine.UpdatePowerFxModelAsync();
            await Assert.ThrowsAsync<TargetInvocationException>(() => powerFxEngine.ExecuteAsync(powerFxExpression, It.IsAny<CultureInfo>()));
            MockTestWebProvider.Verify(x => x.LoadObjectModelAsync(), Times.Once());
        }

        [Fact]
        public async Task ExecuteSetPropertyFunctionTest()
        {
            var recordType = RecordType.Empty().Add("Text", FormulaType.String);
            var button1 = new ControlRecordValue(recordType, MockTestWebProvider.Object, "Button1");

            MockTestWebProvider.Setup(x => x.CheckProviderAsync()).Returns(Task.CompletedTask);
            MockTestWebProvider.Setup(x => x.SetPropertyAsync(It.IsAny<ItemPath>(), It.IsAny<StringValue>())).Returns(Task.FromResult(true));
            MockTestWebProvider.Setup(x => x.LoadObjectModelAsync()).Returns(Task.FromResult(new Dictionary<string, ControlRecordValue>() { { "Button1", button1 } }));
            MockTestWebProvider.Setup(x => x.CheckIsIdleAsync()).Returns(Task.FromResult(true));

            var testSettings = new TestSettings() { Timeout = 3000 };
            MockTestState.Setup(x => x.GetTestSettings()).Returns(testSettings);
            MockTestState.Setup(x => x.GetTestEngineModules()).Returns(new List<ITestEngineModule>());
            MockTestState.SetupGet(x => x.ExecuteStepByStep).Returns(false);
            MockTestState.Setup(x => x.OnBeforeTestStepExecuted(It.IsAny<TestStepEventArgs>()));
            MockTestState.Setup(x => x.OnAfterTestStepExecuted(It.IsAny<TestStepEventArgs>()));
            MockTestState.Setup(x => x.GetDomain()).Returns("https://contoso.crm.dynamics.com");

            var powerFxExpression = "SetProperty(Button1.Text, \"10\")";
            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockTestWebProvider.Object, MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object, MockEnvironmentVariable.Object);

            powerFxEngine.GetAzureCliHelper = () => null;
            powerFxEngine.Setup(testSettings);
            await powerFxEngine.UpdatePowerFxModelAsync();
            await powerFxEngine.ExecuteWithRetryAsync(powerFxExpression, It.IsAny<CultureInfo>());
            var result = await powerFxEngine.ExecuteAsync(powerFxExpression, It.IsAny<CultureInfo>());
            Assert.IsType<BooleanValue>(result);
            MockTestWebProvider.Verify(x => x.LoadObjectModelAsync(), Times.Once());
        }

        [Fact]
        public async Task ExecuteSetPropertyFunctionThrowsOnDifferentRecordTypeTest()
        {
            var wrongRecordType = RecordType.Empty().Add("Foo", FormulaType.String);
            var button1 = new ControlRecordValue(wrongRecordType, MockTestWebProvider.Object, "Button1");

            MockTestWebProvider.Setup(x => x.LoadObjectModelAsync()).Returns(Task.FromResult(new Dictionary<string, ControlRecordValue>() { { "Button1", button1 } }));
            MockTestWebProvider.Setup(x => x.CheckProviderAsync()).Returns(Task.CompletedTask);
            MockTestWebProvider.Setup(x => x.CheckIsIdleAsync()).Returns(Task.FromResult(true));

            var testSettings = new TestSettings() { Timeout = 3000 };
            MockTestState.Setup(x => x.GetTestSettings()).Returns(testSettings);
            MockTestState.Setup(x => x.GetTestEngineModules()).Returns(new List<ITestEngineModule>());
            MockTestState.Setup(x => x.ExecuteStepByStep).Returns(false);
            MockTestState.Setup(x => x.OnBeforeTestStepExecuted(It.IsAny<TestStepEventArgs>()));
            MockTestState.Setup(x => x.OnAfterTestStepExecuted(It.IsAny<TestStepEventArgs>()));
            MockTestState.Setup(x => x.GetDomain()).Returns("https://contoso.crm.dynamics.com");

            var powerFxExpression = "SetProperty(Button1.Text, \"10\")";
            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockTestWebProvider.Object, MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object, MockEnvironmentVariable.Object);

            powerFxEngine.GetAzureCliHelper = () => null;
            powerFxEngine.Setup(testSettings);
            await powerFxEngine.UpdatePowerFxModelAsync();
            await Assert.ThrowsAsync<InvalidOperationException>(() => powerFxEngine.ExecuteAsync(powerFxExpression, It.IsAny<CultureInfo>()));
            MockTestWebProvider.Verify(x => x.LoadObjectModelAsync(), Times.Once());
        }

        [Fact]
        public async Task ExecuteWaitFunctionTest()
        {
            var recordType = RecordType.Empty().Add("Text", FormulaType.String);
            var label1 = new ControlRecordValue(recordType, MockTestWebProvider.Object, "Label1");
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

            MockTestWebProvider.Setup(x => x.CheckProviderAsync()).Returns(Task.CompletedTask);
            MockTestWebProvider.Setup(x => x.GetPropertyValueFromControl<string>(It.Is<ItemPath>((input) => itemPath.ControlName == input.ControlName && itemPath.PropertyName == input.PropertyName)))
                .Returns(JsonConvert.SerializeObject(label1JsProperty));
            MockTestWebProvider.Setup(x => x.LoadObjectModelAsync()).Returns(Task.FromResult(new Dictionary<string, ControlRecordValue>() { { "Label1", label1 } }));
            MockTestWebProvider.Setup(x => x.CheckIsIdleAsync()).Returns(Task.FromResult(true));

            var testSettings = new TestSettings() { Timeout = 3000 };
            MockTestState.Setup(x => x.GetTestSettings()).Returns(testSettings);
            MockTestState.Setup(x => x.GetTestEngineModules()).Returns(new List<ITestEngineModule>());
            MockTestState.SetupGet(x => x.ExecuteStepByStep).Returns(false);
            MockTestState.Setup(x => x.OnBeforeTestStepExecuted(It.IsAny<TestStepEventArgs>()));
            MockTestState.Setup(x => x.OnAfterTestStepExecuted(It.IsAny<TestStepEventArgs>()));
            MockTestState.Setup(x => x.GetDomain()).Returns("https://contoso.crm.dynamics.com");

            var powerFxExpression = "Wait(Label1, \"Text\", \"1\")";
            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockTestWebProvider.Object, MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object, MockEnvironmentVariable.Object);

            powerFxEngine.GetAzureCliHelper = () => null;
            powerFxEngine.Setup(testSettings);
            await powerFxEngine.UpdatePowerFxModelAsync();
            await powerFxEngine.ExecuteWithRetryAsync(powerFxExpression, It.IsAny<CultureInfo>());
            var result = await powerFxEngine.ExecuteAsync(powerFxExpression, It.IsAny<CultureInfo>());
            Assert.IsType<BlankValue>(result);
            MockTestWebProvider.Verify(x => x.LoadObjectModelAsync(), Times.Once());
        }

        [Fact]
        public async Task ExecuteWaitFunctionThrowsOnDifferentRecordTypeTest()
        {
            var recordType = RecordType.Empty().Add("Text", FormulaType.String);
            var otherRecordType = RecordType.Empty().Add("Foo", FormulaType.String);
            var label1 = new ControlRecordValue(otherRecordType, MockTestWebProvider.Object, "Label1");
            MockTestWebProvider.Setup(x => x.LoadObjectModelAsync()).Returns(Task.FromResult(new Dictionary<string, ControlRecordValue>() { { "Label1", label1 } }));
            MockTestWebProvider.Setup(x => x.CheckProviderAsync()).Returns(Task.CompletedTask);
            MockTestWebProvider.Setup(x => x.CheckIsIdleAsync()).Returns(Task.FromResult(true));

            var testSettings = new TestSettings() { Timeout = 3000 };
            MockTestState.Setup(x => x.GetTestSettings()).Returns(testSettings);
            MockTestState.Setup(x => x.GetTestEngineModules()).Returns(new List<ITestEngineModule>());
            MockTestState.Setup(x => x.ExecuteStepByStep).Returns(false);
            MockTestState.Setup(x => x.OnBeforeTestStepExecuted(It.IsAny<TestStepEventArgs>()));
            MockTestState.Setup(x => x.OnAfterTestStepExecuted(It.IsAny<TestStepEventArgs>()));
            MockTestState.Setup(x => x.GetDomain()).Returns("https://contoso.crm.dynamics.com");

            var powerFxExpression = "Wait(Label1, \"Text\", \"1\")";
            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockTestWebProvider.Object, MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object, MockEnvironmentVariable.Object);

            powerFxEngine.GetAzureCliHelper = () => null;
            powerFxEngine.Setup(testSettings);
            await powerFxEngine.UpdatePowerFxModelAsync();
            await Assert.ThrowsAsync<TargetInvocationException>(() => powerFxEngine.ExecuteAsync(powerFxExpression, It.IsAny<CultureInfo>()));
            MockTestWebProvider.Verify(x => x.LoadObjectModelAsync(), Times.Once());
        }

        private async Task TestStepByStep()
        {
            // Arrange
            var powerFxEngine = GetPowerFxEngine();
            int updateCounter = 0;
            var otherRecordType = RecordType.Empty().Add("Foo", FormulaType.String);
            var label1 = new ControlRecordValue(otherRecordType, MockTestWebProvider.Object, "Label1");
            var label2 = new ControlRecordValue(otherRecordType, MockTestWebProvider.Object, "Label2");
            var label3 = new ControlRecordValue(otherRecordType, MockTestWebProvider.Object, "Label3");
            MockTestWebProvider.Setup(x => x.LoadObjectModelAsync()).Returns(() =>
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
            MockTestWebProvider.Setup(x => x.CheckProviderAsync()).Returns(Task.CompletedTask);
            MockTestWebProvider.Setup(x => x.CheckIsIdleAsync()).Returns(Task.FromResult(true));
            MockTestWebProvider.Setup(x => x.SelectControlAsync(It.IsAny<ItemPath>(), null)).Returns(Task.FromResult(true));

            var oldUICulture = CultureInfo.CurrentUICulture;
            var frenchCulture = new CultureInfo("fr");
            CultureInfo.CurrentUICulture = frenchCulture;
            powerFxEngine.Setup(testSettings);
            var expression = "Select(Label1/*Label;;22*/);;\"Just stirng \n;literal\";;Select(Label2)\n;;Select(Label3);;Assert(1=1; \"Supposed to pass;;\");;Max(1,2)";

            // Act 

            // Engine.Eval should throw an exception when none of the used first names exist in the underlying symbol table yet.
            // This confirms that we would be hitting goStepByStep branch
            await Assert.ThrowsAsync<Exception>(() => powerFxEngine.ExecuteAsync(expression, frenchCulture));
            await powerFxEngine.UpdatePowerFxModelAsync();
            var result = await powerFxEngine.ExecuteAsync(expression, frenchCulture);

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
            return new PowerFxEngine(MockTestInfraFunctions.Object, MockTestWebProvider.Object, MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object, MockEnvironmentVariable.Object);
        }

        [Fact]
        public async Task ExecuteFooFromModuleFunction()
        {
            var testSettings = new TestSettings() { ExtensionModules = new TestSettingExtensions { Enable = true } };
            MockTestState.SetupGet(x => x.ExecuteStepByStep).Returns(false);
            MockTestState.Setup(x => x.OnBeforeTestStepExecuted(It.IsAny<TestStepEventArgs>()));
            MockTestState.Setup(x => x.OnAfterTestStepExecuted(It.IsAny<TestStepEventArgs>()));
            MockTestState.Setup(x => x.GetDomain()).Returns("https://contoso.crm.dynamics.com");

            var mockModule = new Mock<ITestEngineModule>();
            var modules = new List<ITestEngineModule>() { mockModule.Object };

            mockModule.Setup(x => x.RegisterPowerFxFunction(It.IsAny<PowerFxConfig>(), It.IsAny<ITestInfraFunctions>(), It.IsAny<ITestWebProvider>(), It.IsAny<ISingleTestInstanceState>(), It.IsAny<ITestState>(), It.IsAny<IFileSystem>()))
                .Callback((PowerFxConfig powerFxConfig, ITestInfraFunctions functions, ITestWebProvider apps, ISingleTestInstanceState instanceState, ITestState state, IFileSystem fileSystem) =>
                {
                    powerFxConfig.AddFunction(new FooFunction());
                });

            MockTestState.Setup(x => x.GetTestSettings()).Returns(testSettings);
            MockTestState.Setup(x => x.GetTestEngineModules()).Returns(modules);

            MockTestWebProvider.Setup(x => x.CheckProviderAsync()).Returns(Task.CompletedTask);
            MockTestWebProvider.Setup(x => x.CheckIsIdleAsync()).Returns(Task.FromResult(true));
            MockTestWebProvider.Setup(x => x.LoadObjectModelAsync()).Returns(Task.FromResult(new Dictionary<string, ControlRecordValue>() { }));

            var powerFxExpression = "Foo()";
            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockTestWebProvider.Object, MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object, MockEnvironmentVariable.Object);
            powerFxEngine.GetAzureCliHelper = () => null;
            powerFxEngine.Setup(testSettings);
            await powerFxEngine.UpdatePowerFxModelAsync();
            await powerFxEngine.ExecuteAsync(powerFxExpression, CultureInfo.CurrentCulture);
        }

        [Theory]
        [MemberData(nameof(PowerFxTypeTest))]
        public async Task SetupPowerFxType(string type, string sample, string check, int expected)
        {
            // Act
            var testSettings = new TestSettings()
            {
                PowerFxTestTypes = new List<PowerFxTestType> { new PowerFxTestType { Name = "Test", Value = type } },
                TestFunctions = new List<TestFunction> { new TestFunction { Code = check } }
            };

            MockTestState.Setup(m => m.GetTestEngineModules()).Returns(new List<ITestEngineModule>());
            MockTestState.Setup(m => m.GetTestSettings()).Returns(testSettings);

            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockTestWebProvider.Object, MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object, MockEnvironmentVariable.Object);
            powerFxEngine.GetAzureCliHelper = () => null;

            // Act
            powerFxEngine.Setup(testSettings);

            // Assert
            var functionText = $"Foo({sample})";

            var result = await powerFxEngine.Engine.EvalAsync(functionText, CancellationToken.None);

            Assert.True(result is NumberValue);

            NumberValue numberResult = result as NumberValue;

            Assert.Equal(expected, numberResult.Value);
        }

        public static IEnumerable<object[]> PowerFxTypeTest()
        {
            yield return new object[] { "{Name: Text}", "{Name: \"Other\"}", "Foo(x: Test): Number = Len(x.Name);", 5 };
            yield return new object[] { "[{Name: Text}]", "[{Name: \"Other\"}]", "Foo(x: Test): Number = CountRows(x);", 1 };
            yield return new object[] { "{Size: Number}", "{Size: 1}", "Foo(x: Test): Number = x.Size;", 1 };
            yield return new object[] { "{IsOn: Boolean}", "{IsOn: true}", "Foo(x: Test): Number = If(x.IsOn,1,0);", 1 };
            yield return new object[] { "{IsOn: Boolean}", "{IsOn: false}", "Foo(x: Test): Number = If(x.IsOn,1,0);", 0 };
            yield return new object[] { "{When: DateTime}", "{When: Now()}", "Foo(x: Test): Number = If(x.When > Date(1970,1,1),1,0);", 1 };
            yield return new object[] { "[{Value: Number}]", "[{Value: 1},{Value: 1},{Value: 1}]", "Foo(x: Test): Number = Sum(ForAll(x As Item,Item.Value),Value)", 3 };
            yield return new object[] { "{Size: Number}", "{Size: 0}", "Foo(x: Test): Number = If(IsError(AssertNotError(1/x.Size, \"Test\")),1,0);", 1 };
            yield return new object[] { "[{Size: Number}]", "[{Size: 0},{Size: 1},{Size: 2}]", "Foo(x: Test): Number = Sum(ForAll(x,If(IsError(AssertNotError(1=ThisRecord.Size,\"Test\")),{Value: 1},{Value:0})),Value);", 2 };
        }
    }

    public class FooFunction : ReflectionFunction
    {
        public FooFunction() : base("Foo", FormulaType.Blank)
        {
        }

        public BlankValue Execute()
        {
            return BlankValue.NewBlank();
        }
    }
}
