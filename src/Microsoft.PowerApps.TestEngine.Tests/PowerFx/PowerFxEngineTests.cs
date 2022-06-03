// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.PowerApps;
using Microsoft.PowerApps.TestEngine.PowerFx;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerApps.TestEngine.Tests.Helpers;
using Microsoft.PowerApps.TestEngine.Tests.PowerFx.Functions;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Public.Values;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.PowerFx
{
    public class PowerFxEngineTests
    {
        private Mock<ITestInfraFunctions> MockTestInfraFunctions;
        private Mock<IPowerAppFunctions> MockPowerAppFunctions;
        private Mock<IFileSystem> MockFileSystem;
        private Mock<ISingleTestInstanceState> MockSingleTestInstanceState;
        private Mock<ILogger> MockLogger;

        public PowerFxEngineTests()
        {
            MockTestInfraFunctions = new Mock<ITestInfraFunctions>(MockBehavior.Strict);
            MockPowerAppFunctions = new Mock<IPowerAppFunctions>(MockBehavior.Strict);
            MockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            MockSingleTestInstanceState = new Mock<ISingleTestInstanceState>(MockBehavior.Strict);
            MockLogger = new Mock<ILogger>(MockBehavior.Strict);
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);
        }

        [Fact]
        public void SetupDoesNotThrow()
        {
            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockPowerAppFunctions.Object, MockSingleTestInstanceState.Object, MockFileSystem.Object);
            powerFxEngine.Setup();
        }

        [Fact]
        public void ExecuteThrowsOnNoSetupTest()
        {
            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockPowerAppFunctions.Object, MockSingleTestInstanceState.Object, MockFileSystem.Object);
            Assert.Throws<InvalidOperationException>(() => powerFxEngine.Execute(""));
            LoggingTestHelper.VerifyLogging(MockLogger, "Engine is null, make sure to call Setup first", LogLevel.Error, Times.Once());
        }

        [Fact]
        public void UpdatePowerFXModelAsyncThrowsOnNoSetupTest()
        {
            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockPowerAppFunctions.Object, MockSingleTestInstanceState.Object, MockFileSystem.Object);
            Assert.ThrowsAsync<InvalidOperationException>(() => powerFxEngine.UpdatePowerFXModelAsync());
            LoggingTestHelper.VerifyLogging(MockLogger, "Engine is null, make sure to call Setup first", LogLevel.Error, Times.Once());
        }

        [Fact]
        public void ExecuteOneFunctionTest()
        {
            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockPowerAppFunctions.Object, MockSingleTestInstanceState.Object, MockFileSystem.Object);
            powerFxEngine.Setup();
            var result = powerFxEngine.Execute("1+1");
            Assert.Equal(2, ((NumberValue)result).Value);
            LoggingTestHelper.VerifyLogging(MockLogger, "Executing 1+1", LogLevel.Information, Times.Once());

            result = powerFxEngine.Execute("=1+1");
            Assert.Equal(2, ((NumberValue)result).Value);
            LoggingTestHelper.VerifyLogging(MockLogger, "Executing 1+1", LogLevel.Information, Times.Exactly(2));
        }

        [Fact]
        public void ExecuteMultipleFunctionsTest()
        {
            var powerFxExpression = "1+1; //some comment \n 2+2;\n Concatenate(\"hello\", \"world\");";
            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockPowerAppFunctions.Object, MockSingleTestInstanceState.Object, MockFileSystem.Object);
            powerFxEngine.Setup();
            var result = powerFxEngine.Execute(powerFxExpression);
            Assert.Equal("helloworld", ((StringValue)result).Value);
            LoggingTestHelper.VerifyLogging(MockLogger, $"Executing {powerFxExpression}", LogLevel.Information, Times.Once());

            result = powerFxEngine.Execute($"={powerFxExpression}");
            Assert.Equal("helloworld", ((StringValue)result).Value);
            LoggingTestHelper.VerifyLogging(MockLogger, $"Executing {powerFxExpression}", LogLevel.Information, Times.Exactly(2));
        }

        [Fact]
        public async Task ExecuteWithVariablesTest()
        {
            var label1 = new PowerAppControlModel("Label1", TestData.CreateSamplePropertiesDictionary(), MockPowerAppFunctions.Object);
            var label2 = new PowerAppControlModel("Label2", TestData.CreateSamplePropertiesDictionary(), MockPowerAppFunctions.Object);
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

            MockPowerAppFunctions.Setup(x => x.GetPropertyValueFromControlAsync<string>(It.Is<ItemPath>((itemPath) => itemPath.ControlName == "Label1")))
                .Returns(Task.FromResult(JsonConvert.SerializeObject(label1JsProperty)));
            MockPowerAppFunctions.Setup(x => x.GetPropertyValueFromControlAsync<string>(It.Is<ItemPath>((itemPath) => itemPath.ControlName == "Label2")))
                .Returns(Task.FromResult(JsonConvert.SerializeObject(label2JsProperty)));
            MockPowerAppFunctions.Setup(x => x.LoadPowerAppsObjectModelAsync()).Returns(Task.FromResult(new List<PowerAppControlModel> { label1, label2 }));

            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockPowerAppFunctions.Object, MockSingleTestInstanceState.Object, MockFileSystem.Object);
            powerFxEngine.Setup();
            await powerFxEngine.UpdatePowerFXModelAsync();

            MockPowerAppFunctions.Verify(x => x.LoadPowerAppsObjectModelAsync(), Times.Once());

            var result = powerFxEngine.Execute(powerFxExpression);
            Assert.Equal($"{label1Text}{label2Text}", ((StringValue)result).Value);
            LoggingTestHelper.VerifyLogging(MockLogger, $"Executing {powerFxExpression}", LogLevel.Information, Times.Once());
            MockPowerAppFunctions.Verify(x => x.GetPropertyValueFromControlAsync<string>(It.Is<ItemPath>((itemPath) => itemPath.ControlName == label1ItemPath.ControlName && itemPath.PropertyName == label1ItemPath.PropertyName)), Times.Once());
            MockPowerAppFunctions.Verify(x => x.GetPropertyValueFromControlAsync<string>(It.Is<ItemPath>((itemPath) => itemPath.ControlName == label2ItemPath.ControlName && itemPath.PropertyName == label2ItemPath.PropertyName)), Times.Once());

            result = powerFxEngine.Execute($"={powerFxExpression}");
            Assert.Equal($"{label1Text}{label2Text}", ((StringValue)result).Value);
            LoggingTestHelper.VerifyLogging(MockLogger, $"Executing {powerFxExpression}", LogLevel.Information, Times.Exactly(2));
            MockPowerAppFunctions.Verify(x => x.GetPropertyValueFromControlAsync<string>(It.Is<ItemPath>((itemPath) => itemPath.ControlName == label1ItemPath.ControlName && itemPath.PropertyName == label1ItemPath.PropertyName)), Times.Exactly(2));
            MockPowerAppFunctions.Verify(x => x.GetPropertyValueFromControlAsync<string>(It.Is<ItemPath>((itemPath) => itemPath.ControlName == label2ItemPath.ControlName && itemPath.PropertyName == label2ItemPath.PropertyName)), Times.Exactly(2));
        }

        [Fact]
        public void ExecuteFailsWhenPowerFXThrowsTest()
        {
            var powerFxExpression = "someNonExistentPowerFxFunction(1, 2, 3)";
            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockPowerAppFunctions.Object, MockSingleTestInstanceState.Object, MockFileSystem.Object);
            powerFxEngine.Setup();
            Assert.ThrowsAny<Exception>(() => powerFxEngine.Execute(powerFxExpression));
            LoggingTestHelper.VerifyLogging(MockLogger, $"Executing {powerFxExpression}", LogLevel.Information, Times.Once());
        }

        [Fact]
        public void ExecuteFailsWhenUsingNonExistentVariableTest()
        {
            var powerFxExpression = "Concatenate(Label1.Text, Label2.Text)";
            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockPowerAppFunctions.Object, MockSingleTestInstanceState.Object, MockFileSystem.Object);
            powerFxEngine.Setup();
            Assert.ThrowsAny<Exception>(() => powerFxEngine.Execute(powerFxExpression));
            LoggingTestHelper.VerifyLogging(MockLogger, $"Executing {powerFxExpression}", LogLevel.Information, Times.Once());
        }

        [Fact]
        public void ExecuteAssertFunctionTest()
        {
            var powerFxExpression = "Assert(1+1=2, \"Adding 1 + 1\")";
            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockPowerAppFunctions.Object, MockSingleTestInstanceState.Object, MockFileSystem.Object);
            powerFxEngine.Setup();
            var result = powerFxEngine.Execute(powerFxExpression);
            Assert.IsType<BlankValue>(result);

            var failingPowerFxExpression = "Assert(1+1=3, \"Supposed to fail\")";
            Assert.ThrowsAny<Exception>(() => powerFxEngine.Execute(failingPowerFxExpression));
        }

        [Fact]
        public void ExecuteScreenshotFunctionTest()
        {
            MockSingleTestInstanceState.Setup(x => x.GetTestResultsDirectory()).Returns("C:\\testResults");
            MockFileSystem.Setup(x => x.IsValidFilePath(It.IsAny<string>())).Returns(true);
            MockTestInfraFunctions.Setup(x => x.ScreenshotAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            var powerFxExpression = "Screenshot(\"1.jpg\")";
            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockPowerAppFunctions.Object, MockSingleTestInstanceState.Object, MockFileSystem.Object);
            powerFxEngine.Setup();
            var result = powerFxEngine.Execute(powerFxExpression);
            Assert.IsType<BlankValue>(result);

            var failingPowerFxExpression = "Screenshot(\"1.txt\")";
            Assert.ThrowsAny<Exception>(() => powerFxEngine.Execute(failingPowerFxExpression));
        }

        [Fact]
        public async Task ExecuteSelectFunctionTest()
        {
            var button1 = new PowerAppControlModel("Button1", TestData.CreateSamplePropertiesDictionary(), MockPowerAppFunctions.Object);
            MockPowerAppFunctions.Setup(x => x.SelectControlAsync(It.IsAny<ItemPath>())).Returns(Task.FromResult(true));
            MockPowerAppFunctions.Setup(x => x.LoadPowerAppsObjectModelAsync()).Returns(Task.FromResult(new List<PowerAppControlModel>() {  button1 }));

            var powerFxExpression = "Select(Button1)";
            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockPowerAppFunctions.Object, MockSingleTestInstanceState.Object, MockFileSystem.Object);
            powerFxEngine.Setup();
            await powerFxEngine.UpdatePowerFXModelAsync();
            var result = powerFxEngine.Execute(powerFxExpression);
            Assert.IsType<BlankValue>(result);
            MockPowerAppFunctions.Verify(x => x.LoadPowerAppsObjectModelAsync(), Times.Exactly(2));
        }

        [Fact]
        public async Task ExecuteSelectFunctionFailingTest()
        {
            MockPowerAppFunctions.Setup(x => x.SelectControlAsync(It.IsAny<ItemPath>())).Returns(Task.FromResult(false));

            var button1 = new PowerAppControlModel("Button1", TestData.CreateSamplePropertiesDictionary(), MockPowerAppFunctions.Object);
            MockPowerAppFunctions.Setup(x => x.LoadPowerAppsObjectModelAsync()).Returns(Task.FromResult(new List<PowerAppControlModel>() { button1 }));

            var powerFxExpression = "Select(Button1)";
            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockPowerAppFunctions.Object, MockSingleTestInstanceState.Object, MockFileSystem.Object);
            powerFxEngine.Setup();
            await powerFxEngine.UpdatePowerFXModelAsync();
            Assert.ThrowsAny<Exception>(() => powerFxEngine.Execute(powerFxExpression));
            MockPowerAppFunctions.Verify(x => x.LoadPowerAppsObjectModelAsync(), Times.Once());
        }

        [Fact]
        public async Task ExecuteWaitFunctionTest()
        {
            var label1 = new PowerAppControlModel("Label1", TestData.CreateSamplePropertiesDictionary(), MockPowerAppFunctions.Object);
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

            MockPowerAppFunctions.Setup(x => x.GetPropertyValueFromControlAsync<string>(It.Is<ItemPath>((input) => itemPath.ControlName == input.ControlName && itemPath.PropertyName == input.PropertyName)))
                .Returns(Task.FromResult(JsonConvert.SerializeObject(label1JsProperty)));
            MockPowerAppFunctions.Setup(x => x.LoadPowerAppsObjectModelAsync()).Returns(Task.FromResult(new List<PowerAppControlModel>() { label1 }));
            var powerFxExpression = "Wait(Label1, \"Text\", \"1\")";
            var powerFxEngine = new PowerFxEngine(MockTestInfraFunctions.Object, MockPowerAppFunctions.Object, MockSingleTestInstanceState.Object, MockFileSystem.Object);
            powerFxEngine.Setup();
            await powerFxEngine.UpdatePowerFXModelAsync();
            var result = powerFxEngine.Execute(powerFxExpression);
            Assert.IsType<BlankValue>(result);
            MockPowerAppFunctions.Verify(x => x.LoadPowerAppsObjectModelAsync(), Times.Once());
        }
    }
}
