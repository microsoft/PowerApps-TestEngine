// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Castle.DynamicProxy.Generators.Emitters.SimpleAST;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.PowerApps;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerApps.TestEngine.Tests.Helpers;
using Microsoft.PowerFx.Types;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
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

namespace Microsoft.PowerApps.TestEngine.Tests.PowerApps
{
    public class PowerAppFunctionsTest
    {
        private Mock<ITestInfraFunctions> MockTestInfraFunctions;
        private Mock<ITestState> MockTestState;
        private Mock<ISingleTestInstanceState> MockSingleTestInstanceState;
        private Mock<ILogger> MockLogger;

        public PowerAppFunctionsTest()
        {
            MockTestInfraFunctions = new Mock<ITestInfraFunctions>(MockBehavior.Strict);
            MockTestState = new Mock<ITestState>(MockBehavior.Strict);
            MockSingleTestInstanceState = new Mock<ISingleTestInstanceState>(MockBehavior.Strict);
            MockLogger = new Mock<ILogger>(MockBehavior.Strict);
        }


        [Theory]
        [InlineData("{\"controlName\":\"Button1\",\"index\":null,\"parentControl\":null,\"propertyName\":\"Text\"}", StringValue.New("A"), true)]
        [InlineData("{\"controlName\":\"Rating1\",\"index\":null,\"parentControl\":null,\"propertyName\":\"Value\"}", NumberValue.New(5), true)]
        [InlineData("{\"controlName\":\"Toggle1\",\"index\":null,\"parentControl\":null,\"propertyName\":\"Value\"}", BooleanValue.New(true), true)]
        [InlineData("{\"controlName\":\"DatePicker1\",\"index\":null,\"parentControl\":null,\"propertyName\":\"DefaultDate\"}", DateValue.New(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).ToLocalTime()), true)]
        public async Task SetPropertyAsyncTest(string itemPathString, FormulaValue value, bool expectedOutput)
        {
            MockTestInfraFunctions.Setup(x => x.RunJavascriptAsync<bool>(It.IsAny<string>())).Returns(Task.FromResult(expectedOutput));

            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            var itemPath = JsonConvert.DeserializeObject<ItemPath>(itemPathString);
            var result = await powerAppFunctions.SetPropertyAsync(itemPath, value);

            Assert.Equal(expectedOutput, result);

            Object? objectValue = null;

            switch (value.Type)
            {
                case (NumberType):
                    objectValue = ((NumberValue)value).Value;
                    break;
                case (StringType):
                    objectValue = ((StringValue)value).Value;
                    break;
                case (BooleanType):
                    objectValue = ((BooleanValue)value).Value;
                    break;
                case (DateType):
                    objectValue = ((DateValue)value).Value;
                    break;
            }

            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<bool>($"setPropertyValue({itemPathString}, \"{objectValue}\")"), Times.Once());
        }

        [Fact]
        public async Task SetPropertyAsyncThrowsOnInvalidFormulaValueTest()
        {
            var itemPath = JsonConvert.DeserializeObject<ItemPath>("{\"controlName\":\"Button1\",\"index\":null,\"parentControl\":null,\"propertyName\":\"Text\"}");
            Guid guid = new Guid("a");

            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            await Assert.ThrowsAsync<ArgumentException>(async () => await powerAppFunctions.SetPropertyAsync(itemPath, GuidValue.New(guid)));
        }

        [Theory]
        [InlineData("{\"controlName\":\"Label1\",\"index\":null,\"parentControl\":null,\"propertyName\":\"Text\"}", "123")]
        [InlineData("{\"controlName\":\"Label1\",\"index\":null,\"parentControl\":{\"controlName\":\"Gallery1\",\"index\":2,\"parentControl\":null,\"propertyName\":\"AllItems\"},\"propertyName\":\"Text\"}", "456")]
        [InlineData("{\"controlName\":\"Label1\",\"index\":null,\"parentControl\":{\"controlName\":\"Gallery2\",\"index\":3,\"parentControl\":{\"controlName\":\"Gallery1\",\"index\":2,\"parentControl\":null,\"propertyName\":\"AllItems\"},\"propertyName\":\"AllItems\"},\"propertyName\":\"Text\"}", "789")]
        [InlineData("{\"controlName\":\"Label1\",\"index\":null,\"parentControl\":{\"controlName\":\"Component1\",\"index\":null,\"parentControl\":null,\"propertyName\":null},\"propertyName\":\"Text\"}", "012")]
        public void GetPropertyValueFromControlTest(string itemPathString, string expectedOutput)
        {
            MockTestInfraFunctions.Setup(x => x.RunJavascriptAsync<string>(It.IsAny<string>())).Returns(Task.FromResult(expectedOutput));
            MockTestState.Setup(x => x.GetTimeout()).Returns(30000);
            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            var itemPath = JsonConvert.DeserializeObject<ItemPath>(itemPathString);
            var result = powerAppFunctions.GetPropertyValueFromControl<string>(itemPath);
            Assert.Equal(expectedOutput, result);
            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<string>($"getPropertyValue({itemPathString})"), Times.Once());
        }

        [Theory]
        [InlineData("{\"controlName\":\"\",\"index\":null,\"parentControl\":null,\"propertyName\":\"Text\"}")]
        [InlineData("{\"controlName\":null,\"index\":null,\"parentControl\":null,\"propertyName\":\"Text\"}")]
        [InlineData("{\"controlName\":\"Label1\",\"index\":null,\"parentControl\":null,\"propertyName\":\"\"}")]
        [InlineData("{\"controlName\":\"Label1\",\"index\":null,\"parentControl\":null,\"propertyName\":null}")]
        [InlineData("{\"controlName\":\"\",\"index\":null,\"parentControl\":{\"controlName\":\"Gallery1\",\"index\":2,\"parentControl\":null,\"propertyName\":\"AllItems\"},\"propertyName\":\"Text\"}")]
        [InlineData("{\"controlName\":null,\"index\":null,\"parentControl\":{\"controlName\":\"Gallery1\",\"index\":2,\"parentControl\":null,\"propertyName\":\"AllItems\"},\"propertyName\":\"Text\"}")]
        [InlineData("{\"controlName\":\"Label1\",\"index\":null,\"parentControl\":{\"controlName\":\"Gallery1\",\"index\":2,\"parentControl\":null,\"propertyName\":\"AllItems\"},\"propertyName\":\"\"}")]
        [InlineData("{\"controlName\":\"Label1\",\"index\":null,\"parentControl\":{\"controlName\":\"Gallery1\",\"index\":2,\"parentControl\":null,\"propertyName\":\"AllItems\"},\"propertyName\":null}")]
        [InlineData("{\"controlName\":\"Label1\",\"index\":null,\"parentControl\":{\"controlName\":\"\",\"index\":2,\"parentControl\":null,\"propertyName\":\"AllItems\"},\"propertyName\":\"Text\"}")]
        [InlineData("{\"controlName\":\"Label1\",\"index\":null,\"parentControl\":{\"controlName\":null,\"index\":2,\"parentControl\":null,\"propertyName\":\"AllItems\"},\"propertyName\":\"Text\"}")]
        [InlineData("{\"controlName\":\"Label1\",\"index\":null,\"parentControl\":{\"controlName\":\"Gallery1\",\"index\":2,\"parentControl\":null,\"propertyName\":\"\"},\"propertyName\":\"Text\"}")]
        [InlineData("{\"controlName\":\"Label1\",\"index\":null,\"parentControl\":{\"controlName\":\"Gallery1\",\"index\":2,\"parentControl\":null,\"propertyName\":null},\"propertyName\":\"Text\"}")]
        [InlineData("{\"controlName\":\"\",\"index\":null,\"parentControl\":{\"controlName\":\"Gallery2\",\"index\":3,\"parentControl\":{\"controlName\":\"Gallery1\",\"index\":2,\"parentControl\":null,\"propertyName\":\"AllItems\"},\"propertyName\":\"AllItems\"},\"propertyName\":\"Text\"}")]
        [InlineData("{\"controlName\":null,\"index\":null,\"parentControl\":{\"controlName\":\"Gallery2\",\"index\":3,\"parentControl\":{\"controlName\":\"Gallery1\",\"index\":2,\"parentControl\":null,\"propertyName\":\"AllItems\"},\"propertyName\":\"AllItems\"},\"propertyName\":\"Text\"}")]
        [InlineData("{\"controlName\":\"Label1\",\"index\":null,\"parentControl\":{\"controlName\":\"Gallery2\",\"index\":3,\"parentControl\":{\"controlName\":\"Gallery1\",\"index\":2,\"parentControl\":null,\"propertyName\":\"AllItems\"},\"propertyName\":\"AllItems\"},\"propertyName\":\"\"}")]
        [InlineData("{\"controlName\":\"Label1\",\"index\":null,\"parentControl\":{\"controlName\":\"Gallery2\",\"index\":3,\"parentControl\":{\"controlName\":\"Gallery1\",\"index\":2,\"parentControl\":null,\"propertyName\":\"AllItems\"},\"propertyName\":\"AllItems\"},\"propertyName\":null}")]
        [InlineData("{\"controlName\":\"Label1\",\"index\":null,\"parentControl\":{\"controlName\":\"\",\"index\":3,\"parentControl\":{\"controlName\":\"Gallery1\",\"index\":2,\"parentControl\":null,\"propertyName\":\"AllItems\"},\"propertyName\":\"AllItems\"},\"propertyName\":\"Text\"}")]
        [InlineData("{\"controlName\":\"Label1\",\"index\":null,\"parentControl\":{\"controlName\":null,\"index\":3,\"parentControl\":{\"controlName\":\"Gallery1\",\"index\":2,\"parentControl\":null,\"propertyName\":\"AllItems\"},\"propertyName\":\"AllItems\"},\"propertyName\":\"Text\"}")]
        [InlineData("{\"controlName\":\"Label1\",\"index\":null,\"parentControl\":{\"controlName\":\"Gallery2\",\"index\":3,\"parentControl\":{\"controlName\":\"Gallery1\",\"index\":2,\"parentControl\":null,\"propertyName\":\"AllItems\"},\"propertyName\":\"\"},\"propertyName\":\"Text\"}")]
        [InlineData("{\"controlName\":\"Label1\",\"index\":null,\"parentControl\":{\"controlName\":\"Gallery2\",\"index\":3,\"parentControl\":{\"controlName\":\"Gallery1\",\"index\":2,\"parentControl\":null,\"propertyName\":\"AllItems\"},\"propertyName\":null},\"propertyName\":\"Text\"}")]
        [InlineData("{\"controlName\":\"Label1\",\"index\":null,\"parentControl\":{\"controlName\":\"Gallery2\",\"index\":3,\"parentControl\":{\"controlName\":\"\",\"index\":2,\"parentControl\":null,\"propertyName\":\"AllItems\"},\"propertyName\":\"AllItems\"},\"propertyName\":\"Text\"}")]
        [InlineData("{\"controlName\":\"Label1\",\"index\":null,\"parentControl\":{\"controlName\":\"Gallery2\",\"index\":3,\"parentControl\":{\"controlName\":null,\"index\":2,\"parentControl\":null,\"propertyName\":\"AllItems\"},\"propertyName\":\"AllItems\"},\"propertyName\":\"Text\"}")]
        [InlineData("{\"controlName\":\"Label1\",\"index\":null,\"parentControl\":{\"controlName\":\"Gallery2\",\"index\":3,\"parentControl\":{\"controlName\":\"Gallery1\",\"index\":2,\"parentControl\":null,\"propertyName\":\"\"},\"propertyName\":\"AllItems\"},\"propertyName\":\"Text\"}")]
        [InlineData("{\"controlName\":\"Label1\",\"index\":null,\"parentControl\":{\"controlName\":\"Gallery2\",\"index\":3,\"parentControl\":{\"controlName\":\"Gallery1\",\"index\":2,\"parentControl\":null,\"propertyName\":null},\"propertyName\":\"AllItems\"},\"propertyName\":\"Text\"}")]
        [InlineData("{\"controlName\":\"\",\"index\":null,\"parentControl\":{\"controlName\":\"Component1\",\"index\":null,\"parentControl\":null,\"propertyName\":null},\"propertyName\":\"Text\"}")]
        [InlineData("{\"controlName\":null,\"index\":null,\"parentControl\":{\"controlName\":\"Component1\",\"index\":null,\"parentControl\":null,\"propertyName\":null},\"propertyName\":\"Text\"}")]
        [InlineData("{\"controlName\":\"Label1\",\"index\":null,\"parentControl\":{\"controlName\":\"\",\"index\":null,\"parentControl\":null,\"propertyName\":null},\"propertyName\":\"Text\"}")]
        [InlineData("{\"controlName\":\"Label1\",\"index\":null,\"parentControl\":{\"controlName\":null,\"index\":null,\"parentControl\":null,\"propertyName\":null},\"propertyName\":\"Text\"}")]
        [InlineData("{\"controlName\":\"Label1\",\"index\":null,\"parentControl\":{\"controlName\":\"Component1\",\"index\":null,\"parentControl\":null,\"propertyName\":null},\"propertyName\":\"\"}")]
        [InlineData("{\"controlName\":\"Label1\",\"index\":null,\"parentControl\":{\"controlName\":\"Component1\",\"index\":null,\"parentControl\":null,\"propertyName\":null},\"propertyName\":null}")]
        public void GetPropertyValueFromControlThrowsOnInvalidInputTest(string itemPathString)
        {
            MockTestState.Setup(x => x.GetTimeout()).Returns(30000);
            var itemPath = JsonConvert.DeserializeObject<ItemPath>(itemPathString);
            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            Assert.Throws<ArgumentNullException>(() => powerAppFunctions.GetPropertyValueFromControl<string>(itemPath));
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
                        Properties = TestData.CreateSampleJsPropertyModelList()
                    },
                    new JSControlModel()
                    {
                        Name = "Label3",
                        Properties = TestData.CreateSampleJsPropertyModelList(),
                    },
                    new JSControlModel()
                    {
                        Name = "Label2",
                        Properties = TestData.CreateSampleJsPropertyModelList()
                    },
                    new JSControlModel()
                    {
                        Name = "Button1",
                        Properties = TestData.CreateSampleJsPropertyModelList()
                    },
                    new JSControlModel()
                    {
                        Name = "Button2",
                        Properties = TestData.CreateSampleJsPropertyModelList()
                    },
                    new JSControlModel()
                    {
                        Name = "Button3",
                        Properties = TestData.CreateSampleJsPropertyModelList()
                    },
                    new JSControlModel()
                    {
                        Name = "Gallery1",
                        Properties = TestData.CreateSampleJsPropertyModelList(
                            new JSPropertyModel[] 
                            {
                                new JSPropertyModel() { PropertyName = "AllItems", PropertyType = "*[Button1:v, Label2:v, Label3:v]" },
                                new JSPropertyModel() { PropertyName = "SelectedItem", PropertyType = "![Button1:v, Label2:v, Label3:v]" }
                            })
                    },
                    new JSControlModel()
                    {
                        Name = "Component1",
                        Properties = TestData.CreateSampleJsPropertyModelList(
                            new JSPropertyModel[]
                            {
                                new JSPropertyModel() { PropertyName = "Button2", PropertyType = "Button2" },
                            })
                    },
                    new JSControlModel()
                    {
                        Name = "Gallery2",
                        Properties = TestData.CreateSampleJsPropertyModelList(
                            new JSPropertyModel[]
                            {
                                new JSPropertyModel() { PropertyName = "AllItems2", PropertyType = "*[Gallery1:v, Button3:v]" },
                                new JSPropertyModel() { PropertyName = "SelectedItem2", PropertyType = "![Gallery1:v, Button3:v]" }
                            })
                    },
                }
            };

            var expectedFormulaTypes = TestData.CreateExpectedFormulaTypesForSampleJsPropertyModelList();
            var button1RecordType = new RecordType();
            var label2RecordType = new RecordType();
            var label3RecordType = new RecordType();
            var button2RecordType = new RecordType();
            var button3RecordType = new RecordType();
            var gallery1RecordType = new RecordType();
            foreach (var expectedFormulaType in expectedFormulaTypes)
            {
                button1RecordType = button1RecordType.Add(expectedFormulaType.Key, expectedFormulaType.Value);
                label2RecordType = label2RecordType.Add(expectedFormulaType.Key, expectedFormulaType.Value);
                label3RecordType = label3RecordType.Add(expectedFormulaType.Key, expectedFormulaType.Value);
                button2RecordType = button2RecordType.Add(expectedFormulaType.Key, expectedFormulaType.Value);
                gallery1RecordType = gallery1RecordType.Add(expectedFormulaType.Key, expectedFormulaType.Value);
                button3RecordType = button3RecordType.Add(expectedFormulaType.Key, expectedFormulaType.Value);
            }
            var allItemsType = new TableType()
                                .Add(new NamedFormulaType("Button1", button1RecordType))
                                .Add(new NamedFormulaType("Label2", label2RecordType))
                                .Add(new NamedFormulaType("Label3", label3RecordType));
            expectedFormulaTypes.Add("AllItems", allItemsType);
            gallery1RecordType.Add("AllItems", allItemsType);
            var selectedItemType = new RecordType()
                                .Add(new NamedFormulaType("Button1", button1RecordType))
                                .Add(new NamedFormulaType("Label2", label2RecordType))
                                .Add(new NamedFormulaType("Label3", label3RecordType));
            expectedFormulaTypes.Add("SelectedItem", selectedItemType);
            expectedFormulaTypes.Add("Button2", button2RecordType);
            expectedFormulaTypes.Add("AllItems2", new TableType().Add(new NamedFormulaType("Gallery1", gallery1RecordType)).Add(new NamedFormulaType("Button3", button3RecordType)));
            expectedFormulaTypes.Add("SelectedItem2", new RecordType().Add(new NamedFormulaType("Gallery1", gallery1RecordType)).Add(new NamedFormulaType("Button3", button3RecordType)));

            var publishedAppIframeName = "fullscreen-app-host";
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            MockTestInfraFunctions.Setup(x => x.AddScriptTagAsync(It.IsAny<string>(), It.IsAny<string?>())).Returns(Task.CompletedTask);
            MockTestInfraFunctions.Setup(x => x.RunJavascriptAsync<string>("getAppStatus()")).Returns(Task.FromResult("Idle"));
            MockTestInfraFunctions.Setup(x => x.RunJavascriptAsync<string>("buildObjectModel().then((objectModel) => JSON.stringify(objectModel));")).Returns(Task.FromResult(JsonConvert.SerializeObject(jsObjectModel)));
            var testSettings = new TestSettings(){Timeout = 30000};
            MockTestState.Setup(x => x.GetTestSettings()).Returns(testSettings);
            LoggingTestHelper.SetupMock(MockLogger);
            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            var objectModel = await powerAppFunctions.LoadPowerAppsObjectModelAsync();

            MockTestInfraFunctions.Verify(x => x.AddScriptTagAsync(It.Is<string>((scriptTag) => scriptTag.Contains("CanvasAppSdk.js")), null), Times.Once());
            MockTestInfraFunctions.Verify(x => x.AddScriptTagAsync(It.Is<string>((scriptTag) => scriptTag.Contains("PublishedAppTesting.js")), publishedAppIframeName), Times.Once());
            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<string>("getAppStatus()"), Times.Once());
            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<string>("buildObjectModel().then((objectModel) => JSON.stringify(objectModel));"), Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, (string)null, LogLevel.Debug, Times.Never());

            Assert.Equal(jsObjectModel.Controls.Count, objectModel.Count);
            foreach (var jsModel in jsObjectModel.Controls)
            {
                var model = objectModel[jsModel.Name];
                Assert.NotNull(model);
                Assert.Equal(jsModel.Name, model.Name);
                foreach (var jsProperty in jsModel.Properties)
                {
                    var fieldType = model.Type.GetFieldType(jsProperty.PropertyName);
                    Assert.NotNull(fieldType);

                    var expectedType = expectedFormulaTypes[jsProperty.PropertyName];
                    Assert.Equal(JsonConvert.SerializeObject(expectedType), JsonConvert.SerializeObject(fieldType));
                }
            }
        }

        [Fact]
        public async Task LoadPowerAppsObjectModelAsyncWithDuplicatesDoesNotThrowTest()
        {
            var jsObjectModel = new JSObjectModel()
            {
                Controls = new List<JSControlModel>()
                {
                    new JSControlModel()
                    {
                        Name = "Label1",
                        Properties = TestData.CreateSampleJsPropertyModelList()
                    },
                    new JSControlModel()
                    {
                        Name = "Label1",
                        Properties = TestData.CreateSampleJsPropertyModelList()
                    }
                }
            };
            var publishedAppIframeName = "fullscreen-app-host";
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            MockTestInfraFunctions.Setup(x => x.AddScriptTagAsync(It.IsAny<string>(), It.IsAny<string?>())).Returns(Task.CompletedTask);
            MockTestInfraFunctions.Setup(x => x.RunJavascriptAsync<string>("getAppStatus()")).Returns(Task.FromResult("Idle"));
            MockTestInfraFunctions.Setup(x => x.RunJavascriptAsync<string>("buildObjectModel().then((objectModel) => JSON.stringify(objectModel));")).Returns(Task.FromResult(JsonConvert.SerializeObject(jsObjectModel)));
            var testSettings = new TestSettings() { Timeout = 30000 };
            MockTestState.Setup(x => x.GetTestSettings()).Returns(testSettings);
            LoggingTestHelper.SetupMock(MockLogger);
            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            var objectModel = await powerAppFunctions.LoadPowerAppsObjectModelAsync();

            MockTestInfraFunctions.Verify(x => x.AddScriptTagAsync(It.Is<string>((scriptTag) => scriptTag.Contains("CanvasAppSdk.js")), null), Times.Once());
            MockTestInfraFunctions.Verify(x => x.AddScriptTagAsync(It.Is<string>((scriptTag) => scriptTag.Contains("PublishedAppTesting.js")), publishedAppIframeName), Times.Once());
            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<string>("getAppStatus()"), Times.Once());
            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<string>("buildObjectModel().then((objectModel) => JSON.stringify(objectModel));"), Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, (string)"Control: Label1 already added", LogLevel.Debug, Times.Once());

            Assert.Single(objectModel);
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
            var testSettings = new TestSettings(){Timeout = 30000};
            MockTestState.Setup(x => x.GetTestSettings()).Returns(testSettings);
            LoggingTestHelper.SetupMock(MockLogger);
            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            var objectModel = await powerAppFunctions.LoadPowerAppsObjectModelAsync();
            Assert.Empty(objectModel);

            MockTestInfraFunctions.Verify(x => x.AddScriptTagAsync(It.Is<string>((scriptTag) => scriptTag.Contains("CanvasAppSdk.js")), null), Times.Once());
            MockTestInfraFunctions.Verify(x => x.AddScriptTagAsync(It.Is<string>((scriptTag) => scriptTag.Contains("PublishedAppTesting.js")), publishedAppIframeName), Times.Once());
            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<string>("getAppStatus()"), Times.Once());
            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<string>("buildObjectModel().then((objectModel) => JSON.stringify(objectModel));"), Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, "No control model was found", LogLevel.Error, Times.Once());
        }

        [Theory]
        [InlineData("{\"controlName\":\"Button1\",\"index\":null,\"parentControl\":null,\"propertyName\":null}", true)]
        [InlineData("{\"controlName\":\"Button1\",\"index\":null,\"parentControl\":{\"controlName\":\"Gallery1\",\"index\":2,\"parentControl\":null,\"propertyName\":\"AllItems\"},\"propertyName\":null}", false)]
        [InlineData("{\"controlName\":\"Button1\",\"index\":null,\"parentControl\":{\"controlName\":\"Gallery2\",\"index\":3,\"parentControl\":{\"controlName\":\"Gallery1\",\"index\":2,\"parentControl\":null,\"propertyName\":\"AllItems\"},\"propertyName\":\"AllItems\"},\"propertyName\":null}", true)]
        [InlineData("{\"controlName\":\"Button1\",\"index\":null,\"parentControl\":{\"controlName\":\"Component1\",\"index\":null,\"parentControl\":null,\"propertyName\":null},\"propertyName\":null}", false)]
        public async Task SelectControlAsyncTest(string itemPathString, bool expectedOutput)
        {
            MockTestInfraFunctions.Setup(x => x.RunJavascriptAsync<bool>(It.IsAny<string>())).Returns(Task.FromResult(expectedOutput));
            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            var itemPath = JsonConvert.DeserializeObject<ItemPath>(itemPathString);
            var result = await powerAppFunctions.SelectControlAsync(itemPath);
            Assert.Equal(expectedOutput, result);
            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<bool>($"select({JsonConvert.SerializeObject(itemPath)})"), Times.Once());
        }

        [Theory]
        [InlineData("{\"controlName\":\"\",\"index\":null,\"parentControl\":null,\"propertyName\":null}")]
        [InlineData("{\"controlName\":null,\"index\":null,\"parentControl\":null,\"propertyName\":null}")]
        [InlineData("{\"controlName\":\"\",\"index\":null,\"parentControl\":{\"controlName\":\"Gallery1\",\"index\":2,\"parentControl\":null,\"propertyName\":\"AllItems\"},\"propertyName\":null}")]
        [InlineData("{\"controlName\":null,\"index\":null,\"parentControl\":{\"controlName\":\"Gallery1\",\"index\":2,\"parentControl\":null,\"propertyName\":\"AllItems\"},\"propertyName\":null}")]
        [InlineData("{\"controlName\":\"Button1\",\"index\":null,\"parentControl\":{\"controlName\":\"\",\"index\":2,\"parentControl\":null,\"propertyName\":\"AllItems\"},\"propertyName\":null}")]
        [InlineData("{\"controlName\":\"Button1\",\"index\":null,\"parentControl\":{\"controlName\":null,\"index\":2,\"parentControl\":null,\"propertyName\":\"AllItems\"},\"propertyName\":null}")]
        [InlineData("{\"controlName\":\"Button1\",\"index\":null,\"parentControl\":{\"controlName\":\"Gallery1\",\"index\":2,\"parentControl\":null,\"propertyName\":\"\"},\"propertyName\":null}")]
        [InlineData("{\"controlName\":\"Button1\",\"index\":null,\"parentControl\":{\"controlName\":\"Gallery1\",\"index\":2,\"parentControl\":null,\"propertyName\":null},\"propertyName\":null}")]
        [InlineData("{\"controlName\":\"\",\"index\":null,\"parentControl\":{\"controlName\":\"Gallery2\",\"index\":3,\"parentControl\":{\"controlName\":\"Gallery1\",\"index\":2,\"parentControl\":null,\"propertyName\":\"AllItems\"},\"propertyName\":\"AllItems\"},\"propertyName\":null}")]
        [InlineData("{\"controlName\":null,\"index\":null,\"parentControl\":{\"controlName\":\"Gallery2\",\"index\":3,\"parentControl\":{\"controlName\":\"Gallery1\",\"index\":2,\"parentControl\":null,\"propertyName\":\"AllItems\"},\"propertyName\":\"AllItems\"},\"propertyName\":null}")]
        [InlineData("{\"controlName\":\"Button1\",\"index\":null,\"parentControl\":{\"controlName\":\"\",\"index\":3,\"parentControl\":{\"controlName\":\"Gallery1\",\"index\":2,\"parentControl\":null,\"propertyName\":\"AllItems\"},\"propertyName\":\"AllItems\"},\"propertyName\":null}")]
        [InlineData("{\"controlName\":\"Button1\",\"index\":null,\"parentControl\":{\"controlName\":null,\"index\":3,\"parentControl\":{\"controlName\":\"Gallery1\",\"index\":2,\"parentControl\":null,\"propertyName\":\"AllItems\"},\"propertyName\":\"AllItems\"},\"propertyName\":null}")]
        [InlineData("{\"controlName\":\"Button1\",\"index\":null,\"parentControl\":{\"controlName\":\"Gallery2\",\"index\":3,\"parentControl\":{\"controlName\":\"Gallery1\",\"index\":2,\"parentControl\":null,\"propertyName\":\"\"},\"propertyName\":\"AllItems\"},\"propertyName\":null}")]
        [InlineData("{\"controlName\":\"Button1\",\"index\":null,\"parentControl\":{\"controlName\":\"Gallery2\",\"index\":3,\"parentControl\":{\"controlName\":\"Gallery1\",\"index\":2,\"parentControl\":null,\"propertyName\":null},\"propertyName\":\"AllItems\"},\"propertyName\":null}")]
        [InlineData("{\"controlName\":\"Button1\",\"index\":null,\"parentControl\":{\"controlName\":\"Gallery2\",\"index\":3,\"parentControl\":{\"controlName\":\"\",\"index\":2,\"parentControl\":null,\"propertyName\":\"AllItems\"},\"propertyName\":\"AllItems\"},\"propertyName\":null}")]
        [InlineData("{\"controlName\":\"Button1\",\"index\":null,\"parentControl\":{\"controlName\":\"Gallery2\",\"index\":3,\"parentControl\":{\"controlName\":null,\"index\":2,\"parentControl\":null,\"propertyName\":\"AllItems\"},\"propertyName\":\"AllItems\"},\"propertyName\":null}")]
        [InlineData("{\"controlName\":\"Button1\",\"index\":null,\"parentControl\":{\"controlName\":\"Gallery2\",\"index\":3,\"parentControl\":{\"controlName\":\"Gallery1\",\"index\":2,\"parentControl\":null,\"propertyName\":\"AllItems\"},\"propertyName\":\"\"},\"propertyName\":null}")]
        [InlineData("{\"controlName\":\"Button1\",\"index\":null,\"parentControl\":{\"controlName\":\"Gallery2\",\"index\":3,\"parentControl\":{\"controlName\":\"Gallery1\",\"index\":2,\"parentControl\":null,\"propertyName\":\"AllItems\"},\"propertyName\":null},\"propertyName\":null}")]
        [InlineData("{\"controlName\":\"\",\"index\":null,\"parentControl\":{\"controlName\":\"Component1\",\"index\":null,\"parentControl\":null,\"propertyName\":null},\"propertyName\":null}")]
        [InlineData("{\"controlName\":null,\"index\":null,\"parentControl\":{\"controlName\":\"Component1\",\"index\":null,\"parentControl\":null,\"propertyName\":null},\"propertyName\":null}")]
        [InlineData("{\"controlName\":\"Button1\",\"index\":null,\"parentControl\":{\"controlName\":\"\",\"index\":null,\"parentControl\":null,\"propertyName\":null},\"propertyName\":null}")]
        [InlineData("{\"controlName\":\"Button1\",\"index\":null,\"parentControl\":{\"controlName\":null,\"index\":null,\"parentControl\":null,\"propertyName\":null},\"propertyName\":null}")]
        public async Task SelectControlAsyncThrowsOnInvalidInputTest(string itemPathString)
        {
            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await powerAppFunctions.SelectControlAsync(JsonConvert.DeserializeObject<ItemPath>(itemPathString)));
        }

        [Theory]
        [InlineData("{\"controlName\":\"Button1\",\"index\":null,\"parentControl\":null,\"propertyName\":null}", 1)]
        [InlineData("{\"controlName\":\"Button1\",\"index\":null,\"parentControl\":{\"controlName\":\"Gallery1\",\"index\":2,\"parentControl\":null,\"propertyName\":\"AllItems\"},\"propertyName\":null}", 2)]
        [InlineData("{\"controlName\":\"Button1\",\"index\":null,\"parentControl\":{\"controlName\":\"Gallery2\",\"index\":3,\"parentControl\":{\"controlName\":\"Gallery1\",\"index\":2,\"parentControl\":null,\"propertyName\":\"AllItems\"},\"propertyName\":\"AllItems\"},\"propertyName\":null}", 3)]
        [InlineData("{\"controlName\":\"Button1\",\"index\":null,\"parentControl\":{\"controlName\":\"Component1\",\"index\":null,\"parentControl\":null,\"propertyName\":null},\"propertyName\":null}", 4)]
        public void GetItemCountTest(string itemPathString, int expectedOutput)
        {
            MockTestInfraFunctions.Setup(x => x.RunJavascriptAsync<int>(It.IsAny<string>())).Returns(Task.FromResult(expectedOutput));
            MockTestState.Setup(x => x.GetTimeout()).Returns(30000);
            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            var itemPath = JsonConvert.DeserializeObject<ItemPath>(itemPathString);
            var result = powerAppFunctions.GetItemCount(itemPath);
            Assert.Equal(expectedOutput, result);
            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<int>($"getItemCount({JsonConvert.SerializeObject(itemPath)})"), Times.Once());
        }

        [Theory]
        [InlineData("{\"controlName\":\"\",\"index\":null,\"parentControl\":null,\"propertyName\":null}")]
        [InlineData("{\"controlName\":null,\"index\":null,\"parentControl\":null,\"propertyName\":null}")]
        [InlineData("{\"controlName\":\"\",\"index\":null,\"parentControl\":{\"controlName\":\"Gallery1\",\"index\":2,\"parentControl\":null,\"propertyName\":\"AllItems\"},\"propertyName\":null}")]
        [InlineData("{\"controlName\":null,\"index\":null,\"parentControl\":{\"controlName\":\"Gallery1\",\"index\":2,\"parentControl\":null,\"propertyName\":\"AllItems\"},\"propertyName\":null}")]
        [InlineData("{\"controlName\":\"Button1\",\"index\":null,\"parentControl\":{\"controlName\":\"\",\"index\":2,\"parentControl\":null,\"propertyName\":\"AllItems\"},\"propertyName\":null}")]
        [InlineData("{\"controlName\":\"Button1\",\"index\":null,\"parentControl\":{\"controlName\":null,\"index\":2,\"parentControl\":null,\"propertyName\":\"AllItems\"},\"propertyName\":null}")]
        [InlineData("{\"controlName\":\"Button1\",\"index\":null,\"parentControl\":{\"controlName\":\"Gallery1\",\"index\":2,\"parentControl\":null,\"propertyName\":\"\"},\"propertyName\":null}")]
        [InlineData("{\"controlName\":\"Button1\",\"index\":null,\"parentControl\":{\"controlName\":\"Gallery1\",\"index\":2,\"parentControl\":null,\"propertyName\":null},\"propertyName\":null}")]
        [InlineData("{\"controlName\":\"\",\"index\":null,\"parentControl\":{\"controlName\":\"Gallery2\",\"index\":3,\"parentControl\":{\"controlName\":\"Gallery1\",\"index\":2,\"parentControl\":null,\"propertyName\":\"AllItems\"},\"propertyName\":\"AllItems\"},\"propertyName\":null}")]
        [InlineData("{\"controlName\":null,\"index\":null,\"parentControl\":{\"controlName\":\"Gallery2\",\"index\":3,\"parentControl\":{\"controlName\":\"Gallery1\",\"index\":2,\"parentControl\":null,\"propertyName\":\"AllItems\"},\"propertyName\":\"AllItems\"},\"propertyName\":null}")]
        [InlineData("{\"controlName\":\"Button1\",\"index\":null,\"parentControl\":{\"controlName\":\"\",\"index\":3,\"parentControl\":{\"controlName\":\"Gallery1\",\"index\":2,\"parentControl\":null,\"propertyName\":\"AllItems\"},\"propertyName\":\"AllItems\"},\"propertyName\":null}")]
        [InlineData("{\"controlName\":\"Button1\",\"index\":null,\"parentControl\":{\"controlName\":null,\"index\":3,\"parentControl\":{\"controlName\":\"Gallery1\",\"index\":2,\"parentControl\":null,\"propertyName\":\"AllItems\"},\"propertyName\":\"AllItems\"},\"propertyName\":null}")]
        [InlineData("{\"controlName\":\"Button1\",\"index\":null,\"parentControl\":{\"controlName\":\"Gallery2\",\"index\":3,\"parentControl\":{\"controlName\":\"Gallery1\",\"index\":2,\"parentControl\":null,\"propertyName\":\"\"},\"propertyName\":\"AllItems\"},\"propertyName\":null}")]
        [InlineData("{\"controlName\":\"Button1\",\"index\":null,\"parentControl\":{\"controlName\":\"Gallery2\",\"index\":3,\"parentControl\":{\"controlName\":\"Gallery1\",\"index\":2,\"parentControl\":null,\"propertyName\":null},\"propertyName\":\"AllItems\"},\"propertyName\":null}")]
        [InlineData("{\"controlName\":\"Button1\",\"index\":null,\"parentControl\":{\"controlName\":\"Gallery2\",\"index\":3,\"parentControl\":{\"controlName\":\"\",\"index\":2,\"parentControl\":null,\"propertyName\":\"AllItems\"},\"propertyName\":\"AllItems\"},\"propertyName\":null}")]
        [InlineData("{\"controlName\":\"Button1\",\"index\":null,\"parentControl\":{\"controlName\":\"Gallery2\",\"index\":3,\"parentControl\":{\"controlName\":null,\"index\":2,\"parentControl\":null,\"propertyName\":\"AllItems\"},\"propertyName\":\"AllItems\"},\"propertyName\":null}")]
        [InlineData("{\"controlName\":\"Button1\",\"index\":null,\"parentControl\":{\"controlName\":\"Gallery2\",\"index\":3,\"parentControl\":{\"controlName\":\"Gallery1\",\"index\":2,\"parentControl\":null,\"propertyName\":\"AllItems\"},\"propertyName\":\"\"},\"propertyName\":null}")]
        [InlineData("{\"controlName\":\"Button1\",\"index\":null,\"parentControl\":{\"controlName\":\"Gallery2\",\"index\":3,\"parentControl\":{\"controlName\":\"Gallery1\",\"index\":2,\"parentControl\":null,\"propertyName\":\"AllItems\"},\"propertyName\":null},\"propertyName\":null}")]
        [InlineData("{\"controlName\":\"\",\"index\":null,\"parentControl\":{\"controlName\":\"Component1\",\"index\":null,\"parentControl\":null,\"propertyName\":null},\"propertyName\":null}")]
        [InlineData("{\"controlName\":null,\"index\":null,\"parentControl\":{\"controlName\":\"Component1\",\"index\":null,\"parentControl\":null,\"propertyName\":null},\"propertyName\":null}")]
        [InlineData("{\"controlName\":\"Button1\",\"index\":null,\"parentControl\":{\"controlName\":\"\",\"index\":null,\"parentControl\":null,\"propertyName\":null},\"propertyName\":null}")]
        [InlineData("{\"controlName\":\"Button1\",\"index\":null,\"parentControl\":{\"controlName\":null,\"index\":null,\"parentControl\":null,\"propertyName\":null},\"propertyName\":null}")]
        public void GetItemCountThrowsOnInvalidInputTest(string itemPathString)
        {
            MockTestState.Setup(x => x.GetTimeout()).Returns(30000);
            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            Assert.Throws<ArgumentNullException>(() => powerAppFunctions.GetItemCount(JsonConvert.DeserializeObject<ItemPath>(itemPathString)));
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
            var testSettings = new TestSettings(){Timeout = 30000};
            MockTestState.Setup(x => x.GetTestSettings()).Returns(testSettings);
            LoggingTestHelper.SetupMock(MockLogger);
            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
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
            var testSettings = new TestSettings(){Timeout = 30000};
            MockTestState.Setup(x => x.GetTestSettings()).Returns(testSettings);
            LoggingTestHelper.SetupMock(MockLogger);
            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
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
            var testSettings = new TestSettings(){Timeout = 30000};
            MockTestState.Setup(x => x.GetTestSettings()).Returns(testSettings);
            LoggingTestHelper.SetupMock(MockLogger);
            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
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
            var testSettings = new TestSettings(){Timeout = 5000};
            MockTestState.Setup(x => x.GetTestSettings()).Returns(testSettings);
            LoggingTestHelper.SetupMock(MockLogger);
            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            var objectModel = await powerAppFunctions.LoadPowerAppsObjectModelAsync();
            Assert.Empty(objectModel);

            MockTestInfraFunctions.Verify(x => x.AddScriptTagAsync(It.Is<string>((scriptTag) => scriptTag.Contains("CanvasAppSdk.js")), null), Times.Exactly(2));
            MockTestInfraFunctions.Verify(x => x.AddScriptTagAsync(It.Is<string>((scriptTag) => scriptTag.Contains("PublishedAppTesting.js")), publishedAppIframeName), Times.Once());
            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<string>("getAppStatus()"), Times.Exactly(4));
            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<string>("buildObjectModel().then((objectModel) => JSON.stringify(objectModel));"), Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, "No control model was found", LogLevel.Error, Times.Once());
        }

        [Fact]
        public async Task LoadPowerAppsObjectModelAsyncWaitsForAppToBeIdleTimeout()
        {
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            MockTestInfraFunctions.Setup(x => x.AddScriptTagAsync(It.IsAny<string>(), It.IsAny<string?>())).Returns(Task.CompletedTask);
            MockTestInfraFunctions.SetupSequence(x => x.RunJavascriptAsync<string>("getAppStatus()"))
                .Returns(Task.FromResult("Busy"))
                .Returns(Task.FromResult("Busy"))
                .Returns(Task.FromResult("Busy"));
            MockTestInfraFunctions.Setup(x => x.RunJavascriptAsync<string>("buildObjectModel().then((objectModel) => JSON.stringify(objectModel));")).Returns(Task.FromResult("{}"));
            var testSettings = new TestSettings(){Timeout = 15};
            MockTestState.Setup(x => x.GetTestSettings()).Returns(testSettings);
            LoggingTestHelper.SetupMock(MockLogger);
            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            await Assert.ThrowsAsync<TimeoutException>(async() => {await powerAppFunctions.LoadPowerAppsObjectModelAsync();});
        }
    }
}
