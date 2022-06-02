// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Castle.DynamicProxy.Generators.Emitters.SimpleAST;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.PowerApps;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerApps.TestEngine.Tests.Helpers;
using Microsoft.PowerFx.Core.Public.Types;
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
        [InlineData("{\"controlName\":\"Label1\",\"index\":null,\"childControl\":null,\"propertyName\":\"Text\"}", "Text")]
        [InlineData("{\"controlName\":\"Gallery1\",\"index\":2,\"childControl\":{\"controlName\":\"Label1\",\"index\":null,\"childControl\":null,\"propertyName\":\"Text\"},\"propertyName\":null}", "Text")]
        public async Task GetPropertyValueFromControlAsyncTest(string itemPathString, string expectedOutput)
        {
            // TODO: handle components and nested galleries
            MockTestInfraFunctions.Setup(x => x.RunJavascriptAsync<string>(It.IsAny<string>())).Returns(Task.FromResult(expectedOutput));
            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object);
            var itemPath = JsonConvert.DeserializeObject<ItemPath>(itemPathString);
            var result = await powerAppFunctions.GetPropertyValueFromControlAsync<string>(itemPath);
            Assert.Equal(expectedOutput, result);
            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<string>($"getPropertyValue({itemPathString})"), Times.Once());
        }

        [Theory]
        [InlineData("{\"controlName\":\"\",\"index\":null,\"childControl\":null,\"propertyName\":\"Text\"}")]
        [InlineData("{\"controlName\":null,\"index\":null,\"childControl\":null,\"propertyName\":\"Text\"}")]
        [InlineData("{\"controlName\":\"Label1\",\"index\":null,\"childControl\":null,\"propertyName\":\"\"}")]
        [InlineData("{\"controlName\":\"Label1\",\"index\":null,\"childControl\":null,\"propertyName\":null}")]
        [InlineData("{\"controlName\":\"\",\"index\":1,\"childControl\":{\"controlName\":\"Label1\",\"index\":null,\"childControl\":null,\"propertyName\":\"Text\"},\"propertyName\":null}")]
        [InlineData("{\"controlName\":null,\"index\":2,\"childControl\":{\"controlName\":\"Label1\",\"index\":null,\"childControl\":null,\"propertyName\":\"Text\"},\"propertyName\":null}")]
        [InlineData("{\"controlName\":\"Gallery1\",\"index\":3,\"childControl\":{\"controlName\":\"\",\"index\":null,\"childControl\":null,\"propertyName\":\"Text\"},\"propertyName\":null}")]
        [InlineData("{\"controlName\":\"Gallery1\",\"index\":4,\"childControl\":{\"controlName\":null,\"index\":null,\"childControl\":null,\"propertyName\":\"Text\"},\"propertyName\":null}")]
        [InlineData("{\"controlName\":\"Gallery1\",\"index\":5,\"childControl\":{\"controlName\":\"Label1\",\"index\":null,\"childControl\":null,\"propertyName\":\"\"},\"propertyName\":null}")]
        [InlineData("{\"controlName\":\"Gallery1\",\"index\":6,\"childControl\":{\"controlName\":\"Label1\",\"index\":null,\"childControl\":null,\"propertyName\":null},\"propertyName\":null}")]
        public async Task GetPropertyValueFromControlAsyncThrowsOnInvalidInputTest(string itemPathString)
        {
            // TODO: handle components and nested galleries
            var itemPath = JsonConvert.DeserializeObject<ItemPath>(itemPathString);
            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object);
            await Assert.ThrowsAsync<ArgumentNullException>(async() => await powerAppFunctions.GetPropertyValueFromControlAsync<string>(itemPath));
        }

        [Fact]
        public async Task LoadPowerAppsObjectModelAsyncTest()
        {
            // Handle nested galleries and components
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
                        Name = "Label1",
                        Properties = new string[] { "Text", "Color", "X", "Y"},
                        ItemCount = 0,
                        IsArray = false
                    },
                    new JSControlModel()
                    {
                        Name = "Gallery1",
                        Properties = new string[] { "AllItems", "X", "Y"},
                        ItemCount = 5,
                        IsArray = true,
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
                Assert.Equal(jsModel.IsArray ? jsModel.ItemCount : null, model.ItemCount);
                Assert.Null(model.SelectedIndex);
                Assert.Equal(jsModel.IsArray, (model.Type as ExternalType).Kind == ExternalTypeKind.Array);
                Assert.Equal(!jsModel.IsArray, (model.Type as ExternalType).Kind == ExternalTypeKind.Object);
                Assert.Null(model.ParentControl);

                if (jsModel.ChildrenControls != null)
                {
                    Assert.Equal(jsModel.ChildrenControls.Length, model.ChildControls.Count);
                    foreach (var jsChildModel in jsModel.ChildrenControls)
                    {
                        var childModel = model.ChildControls.Where(x => x.Name == jsChildModel.Name).FirstOrDefault();
                        Assert.NotNull(childModel);
                        Assert.Equal(jsChildModel.Name, childModel.Name);
                        Assert.Equal(jsChildModel.Properties?.Count(), childModel.Properties.Count);
                        foreach (var jsProperty in jsChildModel.Properties)
                        {
                            Assert.Contains(jsProperty, childModel.Properties);
                        }
                        Assert.Equal(jsChildModel.ItemCount, childModel.ItemCount);
                        Assert.Null(childModel.SelectedIndex);
                        Assert.Equal(jsChildModel.IsArray, (childModel.Type as ExternalType).Kind == ExternalTypeKind.Array);
                        Assert.Equal(!jsChildModel.IsArray, (childModel.Type as ExternalType).Kind == ExternalTypeKind.Object);
                        Assert.NotNull(childModel.ParentControl);
                        Assert.Equal(model.Name, childModel.ParentControl.Name);
                    }
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
        [InlineData("{\"controlName\":\"Button1\",\"index\":null,\"childControl\":null,\"propertyName\":null}", true)]
        [InlineData("{\"controlName\":\"Gallery1\",\"index\":2,\"childControl\":{\"controlName\":\"Button1\",\"index\":null,\"childControl\":null,\"propertyName\":null},\"propertyName\":null}", false)]
        public async Task SelectControlAsyncTest(string itemPathString, bool expectedOutput)
        {
            MockTestInfraFunctions.Setup(x => x.RunJavascriptAsync<bool>(It.IsAny<string>())).Returns(Task.FromResult(expectedOutput));
            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object);

            // TODO: Handle nested galleries and components
            var itemPath = JsonConvert.DeserializeObject<ItemPath>(itemPathString);
            var result = await powerAppFunctions.SelectControlAsync(itemPath);
            Assert.Equal(expectedOutput, result);
            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<bool>($"select({JsonConvert.SerializeObject(itemPath)})"), Times.Once());
        }

        [Theory]
        [InlineData("{\"controlName\":\"\",\"index\":null,\"childControl\":null,\"propertyName\":null}")]
        [InlineData("{\"controlName\":null,\"index\":null,\"childControl\":null,\"propertyName\":null}")]
        [InlineData("{\"controlName\":\"\",\"index\":2,\"childControl\":{\"controlName\":\"Button1\",\"index\":null,\"childControl\":null,\"propertyName\":null},\"propertyName\":null}")]
        [InlineData("{\"controlName\":null,\"index\":2,\"childControl\":{\"controlName\":\"Button1\",\"index\":null,\"childControl\":null,\"propertyName\":null},\"propertyName\":null}")]
        [InlineData("{\"controlName\":\"Gallery1\",\"index\":2,\"childControl\":{\"controlName\":\"\",\"index\":null,\"childControl\":null,\"propertyName\":null},\"propertyName\":null}")]
        [InlineData("{\"controlName\":\"Gallery1\",\"index\":2,\"childControl\":{\"controlName\":null,\"index\":null,\"childControl\":null,\"propertyName\":null},\"propertyName\":null}")]
        public async Task SelectControlAsyncThrowsOnInvalidInputTest(string itemPathString)
        {
            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object);
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await powerAppFunctions.SelectControlAsync(JsonConvert.DeserializeObject<ItemPath>(itemPathString)));
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
