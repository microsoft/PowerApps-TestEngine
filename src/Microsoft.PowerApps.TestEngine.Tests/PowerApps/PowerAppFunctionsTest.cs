// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Castle.DynamicProxy.Generators.Emitters.SimpleAST;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.PowerApps;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerApps.TestEngine.Tests.Helpers;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.PowerApps
{
    public class PowerAppFunctionsTest
    {
        private Mock<ITestInfraFunctions> MockTestInfraFunctions;
        private Mock<ISingleTestInstanceState> MockSingleTestInstanceState;
        private Mock<ILogger> MockLogger;

        public PowerAppFunctionsTest()
        {
            MockTestInfraFunctions = new Mock<ITestInfraFunctions>(MockBehavior.Strict);
            MockSingleTestInstanceState = new Mock<ISingleTestInstanceState>(MockBehavior.Strict);
            MockLogger = new Mock<ILogger>(MockBehavior.Strict);
        }

        [Theory]
        [InlineData("Label1", "Text", null, null, "Hello")]
        [InlineData("Button1", "Text", "Component1", null, "Click Me")]
        [InlineData("TextInput1", "Text", "Gallery1", 3, "Enter text here")]
        public async Task GetPropertyValueFromControlAsyncTest(string controlName, string propertyName, string? parentControlName, int? rowOrColumnNumber, string expectedOutput)
        {
            // TODO: handle components and galleries
            MockTestInfraFunctions.Setup(x => x.RunJavascriptAsync<string>(It.IsAny<string>())).Returns(Task.FromResult(expectedOutput));
            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object);
            var itemPath = new ItemPath
            {
                ControlName = controlName,
                PropertyName = propertyName
            };
            var result = await powerAppFunctions.GetPropertyValueFromControlAsync<string>(itemPath);
            Assert.Equal(expectedOutput, result);
            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<string>($"getPropertyValue({JsonConvert.SerializeObject(itemPath)})"), Times.Once());
        }

        [Theory]
        [InlineData("", "Text")]
        [InlineData(null, "Text")]
        [InlineData("Label1", "")]
        [InlineData("Label1", null)]
        public async Task GetPropertyValueFromControlAsyncThrowsOnInvalidInputTest(string controlName, string propertyName)
        {
            var itemPath = new ItemPath
            {
                ControlName = controlName,
                PropertyName = propertyName
            };
            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object);
            await Assert.ThrowsAsync<ArgumentNullException>(async() => await powerAppFunctions.GetPropertyValueFromControlAsync<string>(itemPath));
        }

        [Fact]
        public async Task LoadPowerAppsObjectModelAsyncTest()
        {
            var jsObjectModel = new JSObjectModel()
            {
                Controls = new List<JSControlModel>()
                {
                    new JSControlModel()
                    {
                        Name = "Label1",
                        Properties = new string[] { "Text", "Color", "X", "Y"}
                    },
                    new JSControlModel()
                    {
                        Name = "Gallery1",
                        Properties = new string[] { "AllItems", "X", "Y"},
                        ItemCount = 5,
                        ChildrenControls = new JSControlModel[]
                        {
                            new JSControlModel()
                            {
                                Name = "Label2",
                                Properties = new string[] { "Text", "Color", "X", "Y"}
                            },
                            new JSControlModel()
                            {
                                Name = "Button1",
                                Properties = new string[] { "Text", "Color", "X", "Y"}
                            }
                        }
                    }
                }
            };

            var publishedAppIframeName = "fullscreen-app-host";
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            MockTestInfraFunctions.Setup(x => x.AddScriptTagAsync(It.IsAny<string>(), It.IsAny<string?>())).Returns(Task.CompletedTask);
            MockTestInfraFunctions.Setup(x => x.RunJavascriptAsync<string>("getAppStatus()")).Returns(Task.FromResult("Idle"));
            MockTestInfraFunctions.Setup(x => x.RunJavascriptAsync<string>("buildObjectModel().then((objectModel) => JSON.stringify(objectModel));")).Returns(Task.FromResult(JsonConvert.SerializeObject(jsObjectModel)));
            LoggingTestHelper.SetupMock(MockLogger);
            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object);
            var objectModel = await powerAppFunctions.LoadPowerAppsObjectModelAsync();
            Assert.Equal(jsObjectModel.Controls.Count, objectModel.Count);
            foreach(var jsModel in jsObjectModel.Controls)
            {
                var model = objectModel.Where(x => x.Name == jsModel.Name).FirstOrDefault();
                Assert.NotNull(model);
                Assert.Equal(jsModel.Name, model.Name);
                Assert.Equal(jsModel.Properties?.Count(), model.Properties.Count);
                foreach (var jsProperty in jsModel.Properties)
                {
                    Assert.Contains(jsProperty, model.Properties);
                }
            }

            MockTestInfraFunctions.Verify(x => x.AddScriptTagAsync(It.Is<string>((scriptTag) => scriptTag.Contains("CanvasAppSdk.js")), null), Times.Once());
            MockTestInfraFunctions.Verify(x => x.AddScriptTagAsync(It.Is<string>((scriptTag) => scriptTag.Contains("PublishedAppTesting.js")), publishedAppIframeName), Times.Once());
            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<string>("getAppStatus()"), Times.Once());
            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<string>("buildObjectModel().then((objectModel) => JSON.stringify(objectModel));"), Times.Once());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("{}")]
        [InlineData("{ controls: [] }")]
        public async Task LoadPowerAppsObjectModelAsyncWithNoModelTest(string? jsObjectModelString)
        {
            var publishedAppIframeName = "fullscreen-app-host";
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            MockTestInfraFunctions.Setup(x => x.AddScriptTagAsync(It.IsAny<string>(), It.IsAny<string?>())).Returns(Task.CompletedTask);
            MockTestInfraFunctions.Setup(x => x.RunJavascriptAsync<string>("getAppStatus()")).Returns(Task.FromResult("Idle"));
            MockTestInfraFunctions.Setup(x => x.RunJavascriptAsync<string>("buildObjectModel().then((objectModel) => JSON.stringify(objectModel));")).Returns(Task.FromResult(jsObjectModelString));
            LoggingTestHelper.SetupMock(MockLogger);
            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object);
            var objectModel = await powerAppFunctions.LoadPowerAppsObjectModelAsync();
            Assert.Empty(objectModel);

            MockTestInfraFunctions.Verify(x => x.AddScriptTagAsync(It.Is<string>((scriptTag) => scriptTag.Contains("CanvasAppSdk.js")), null), Times.Once());
            MockTestInfraFunctions.Verify(x => x.AddScriptTagAsync(It.Is<string>((scriptTag) => scriptTag.Contains("PublishedAppTesting.js")), publishedAppIframeName), Times.Once());
            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<string>("getAppStatus()"), Times.Once());
            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<string>("buildObjectModel().then((objectModel) => JSON.stringify(objectModel));"), Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, "No control model was found", LogLevel.Error, Times.Once());
        }



        [Theory]
        [InlineData("Label1", null, null, true)]
        [InlineData("Button1", "Component1", null, false)]
        [InlineData("TextInput1", "Gallery1", 3, true)]
        public async Task SelectControlAsyncTest(string controlName, string? parentControlName, int? rowOrColumnNumber, bool expectedOutput)
        {
            MockTestInfraFunctions.Setup(x => x.RunJavascriptAsync<bool>(It.IsAny<string>())).Returns(Task.FromResult(expectedOutput));
            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object);

            // TODO: Handle galleries and components
            var itemPath = new ItemPath()
            {
                ControlName = controlName,
            };
            var result = await powerAppFunctions.SelectControlAsync(itemPath);
            Assert.Equal(expectedOutput, result);
            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<bool>($"select({JsonConvert.SerializeObject(itemPath)})"), Times.Once());
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task SelectControlAsyncThrowsOnInvalidInputTest(string controlName)
        {
            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object);
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await powerAppFunctions.SelectControlAsync(new ItemPath() {  ControlName = controlName }));
        }

        [Fact]
        public async Task LoadPowerAppsObjectModelAsyncWaitsForAppToLoad()
        {
            var publishedAppIframeName = "fullscreen-app-host";
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            MockTestInfraFunctions.Setup(x => x.AddScriptTagAsync(It.IsAny<string>(), It.IsAny<string?>())).Returns(Task.CompletedTask);
            MockTestInfraFunctions.SetupSequence(x => x.RunJavascriptAsync<string>("getAppStatus()"))
                .Returns(Task.FromResult("Loading"))
                .Returns(Task.FromResult("Loading"))
                .Returns(Task.FromResult("Idle"));
            MockTestInfraFunctions.Setup(x => x.RunJavascriptAsync<string>("buildObjectModel().then((objectModel) => JSON.stringify(objectModel));")).Returns(Task.FromResult("{}"));
            LoggingTestHelper.SetupMock(MockLogger);
            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object);
            var objectModel = await powerAppFunctions.LoadPowerAppsObjectModelAsync();
            Assert.Empty(objectModel);

            MockTestInfraFunctions.Verify(x => x.AddScriptTagAsync(It.Is<string>((scriptTag) => scriptTag.Contains("CanvasAppSdk.js")), null), Times.Once());
            MockTestInfraFunctions.Verify(x => x.AddScriptTagAsync(It.Is<string>((scriptTag) => scriptTag.Contains("PublishedAppTesting.js")), publishedAppIframeName), Times.Once());
            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<string>("getAppStatus()"), Times.Exactly(3));
            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<string>("buildObjectModel().then((objectModel) => JSON.stringify(objectModel));"), Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, "No control model was found", LogLevel.Error, Times.Once());
        }

        [Fact]
        public async Task LoadPowerAppsObjectModelAsyncWaitsForAppToLoadWithExceptions()
        {
            var publishedAppIframeName = "fullscreen-app-host";
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            MockTestInfraFunctions.Setup(x => x.AddScriptTagAsync(It.IsAny<string>(), It.IsAny<string?>())).Returns(Task.CompletedTask);
            MockTestInfraFunctions.SetupSequence(x => x.RunJavascriptAsync<string>("getAppStatus()"))
                .Throws(new Exception())
                .Returns(Task.FromResult("Loading"))
                .Returns(Task.FromResult("Loading"))
                .Returns(Task.FromResult("Idle"));
            MockTestInfraFunctions.Setup(x => x.RunJavascriptAsync<string>("buildObjectModel().then((objectModel) => JSON.stringify(objectModel));")).Returns(Task.FromResult("{}"));
            LoggingTestHelper.SetupMock(MockLogger);
            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object);
            var objectModel = await powerAppFunctions.LoadPowerAppsObjectModelAsync();
            Assert.Empty(objectModel);

            MockTestInfraFunctions.Verify(x => x.AddScriptTagAsync(It.Is<string>((scriptTag) => scriptTag.Contains("CanvasAppSdk.js")), null), Times.Exactly(2));
            MockTestInfraFunctions.Verify(x => x.AddScriptTagAsync(It.Is<string>((scriptTag) => scriptTag.Contains("PublishedAppTesting.js")), publishedAppIframeName), Times.Once());
            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<string>("getAppStatus()"), Times.Exactly(4));
            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<string>("buildObjectModel().then((objectModel) => JSON.stringify(objectModel));"), Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, "No control model was found", LogLevel.Error, Times.Once());
        }

        [Fact]
        public async Task LoadPowerAppsObjectModelAsyncWaitsForAppToBeIdle()
        {
            var publishedAppIframeName = "fullscreen-app-host";
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            MockTestInfraFunctions.Setup(x => x.AddScriptTagAsync(It.IsAny<string>(), It.IsAny<string?>())).Returns(Task.CompletedTask);
            MockTestInfraFunctions.SetupSequence(x => x.RunJavascriptAsync<string>("getAppStatus()"))
                .Returns(Task.FromResult("Busy"))
                .Returns(Task.FromResult("Busy"))
                .Returns(Task.FromResult("Idle"));
            MockTestInfraFunctions.Setup(x => x.RunJavascriptAsync<string>("buildObjectModel().then((objectModel) => JSON.stringify(objectModel));")).Returns(Task.FromResult("{}"));
            LoggingTestHelper.SetupMock(MockLogger);
            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object);
            var objectModel = await powerAppFunctions.LoadPowerAppsObjectModelAsync();
            Assert.Empty(objectModel);

            MockTestInfraFunctions.Verify(x => x.AddScriptTagAsync(It.Is<string>((scriptTag) => scriptTag.Contains("CanvasAppSdk.js")), null), Times.Once());
            MockTestInfraFunctions.Verify(x => x.AddScriptTagAsync(It.Is<string>((scriptTag) => scriptTag.Contains("PublishedAppTesting.js")), publishedAppIframeName), Times.Once());
            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<string>("getAppStatus()"), Times.Exactly(3));
            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<string>("buildObjectModel().then((objectModel) => JSON.stringify(objectModel));"), Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, "No control model was found", LogLevel.Error, Times.Once());
        }

        [Fact]
        public async Task LoadPowerAppsObjectModelAsyncWaitsForAppToBeIdleWithExceptions()
        {
            var publishedAppIframeName = "fullscreen-app-host";
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            MockTestInfraFunctions.Setup(x => x.AddScriptTagAsync(It.IsAny<string>(), It.IsAny<string?>())).Returns(Task.CompletedTask);
            MockTestInfraFunctions.SetupSequence(x => x.RunJavascriptAsync<string>("getAppStatus()"))
                .Throws(new Exception())
                .Returns(Task.FromResult("Busy"))
                .Returns(Task.FromResult("Busy"))
                .Returns(Task.FromResult("Idle"));
            MockTestInfraFunctions.Setup(x => x.RunJavascriptAsync<string>("buildObjectModel().then((objectModel) => JSON.stringify(objectModel));")).Returns(Task.FromResult("{}"));
            LoggingTestHelper.SetupMock(MockLogger);
            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object);
            var objectModel = await powerAppFunctions.LoadPowerAppsObjectModelAsync();
            Assert.Empty(objectModel);

            MockTestInfraFunctions.Verify(x => x.AddScriptTagAsync(It.Is<string>((scriptTag) => scriptTag.Contains("CanvasAppSdk.js")), null), Times.Exactly(2));
            MockTestInfraFunctions.Verify(x => x.AddScriptTagAsync(It.Is<string>((scriptTag) => scriptTag.Contains("PublishedAppTesting.js")), publishedAppIframeName), Times.Once());
            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<string>("getAppStatus()"), Times.Exactly(4));
            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<string>("buildObjectModel().then((objectModel) => JSON.stringify(objectModel));"), Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, "No control model was found", LogLevel.Error, Times.Once());
        }
    }
}
