// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Helpers;
using Microsoft.PowerApps.TestEngine.PowerApps;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerApps.TestEngine.Tests.Helpers;
using Microsoft.PowerFx.Types;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.PowerApps
{
    public class PowerAppFunctionsTest
    {
        private Mock<ITestInfraFunctions> MockTestInfraFunctions;
        private Mock<ITestState> MockTestState;
        private Mock<ISingleTestInstanceState> MockSingleTestInstanceState;
        private Mock<ILogger> MockLogger;
        private JSObjectModel JsObjectModel;

        public PowerAppFunctionsTest()
        {
            MockTestInfraFunctions = new Mock<ITestInfraFunctions>(MockBehavior.Strict);
            MockTestState = new Mock<ITestState>(MockBehavior.Strict);
            MockSingleTestInstanceState = new Mock<ISingleTestInstanceState>(MockBehavior.Strict);
            MockLogger = new Mock<ILogger>(MockBehavior.Strict);
            JsObjectModel = new JSObjectModel()
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
        }

        [Fact]
        public async Task SetPropertyStringAsyncTest()
        {
            MockTestInfraFunctions.Setup(x => x.RunJavascriptAsync<bool>(It.IsAny<string>())).Returns(Task.FromResult(true));
            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            string itemPathString = "{\"controlName\":\"Button1\",\"index\":null,\"parentControl\":null,\"propertyName\":\"Text\"}";
            var itemPath = JsonConvert.DeserializeObject<ItemPath>(itemPathString);
            var value = "A";
            var stringValue = StringValue.New(value);
            var jsonSerializedValue = JsonConvert.SerializeObject(stringValue.Value);
            var result = await powerAppFunctions.SetPropertyAsync(itemPath, stringValue);

            Assert.True(result);
            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<bool>($"PowerAppsTestEngine.setPropertyValue({itemPathString}, {jsonSerializedValue})"), Times.Once());
        }

        [Fact]
        public async Task SetPropertyNumberAsyncTest()
        {
            MockTestInfraFunctions.Setup(x => x.RunJavascriptAsync<bool>(It.IsAny<string>())).Returns(Task.FromResult(true));
            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            string itemPathString = "{\"controlName\":\"Rating1\",\"index\":null,\"parentControl\":null,\"propertyName\":\"Value\"}";
            var itemPath = JsonConvert.DeserializeObject<ItemPath>(itemPathString);
            var value = 5;
            var numberValue = NumberValue.New(value);
            var jsonSerializedValue = JsonConvert.SerializeObject(numberValue.Value);
            var result = await powerAppFunctions.SetPropertyAsync(itemPath, numberValue);

            Assert.True(result);
            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<bool>($"PowerAppsTestEngine.setPropertyValue({itemPathString}, {jsonSerializedValue})"), Times.Once());
        }

        [Fact]
        public async Task SetPropertyBooleanAsyncTest()
        {
            MockTestInfraFunctions.Setup(x => x.RunJavascriptAsync<bool>(It.IsAny<string>())).Returns(Task.FromResult(true));
            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            string itemPathString = "{\"controlName\":\"Toggle1\",\"index\":null,\"parentControl\":null,\"propertyName\":\"Value\"}";
            var itemPath = JsonConvert.DeserializeObject<ItemPath>(itemPathString);
            var value = true;
            var booleanValue = BooleanValue.New(value);
            var jsonSerializedValue = JsonConvert.SerializeObject(booleanValue.Value);
            var result = await powerAppFunctions.SetPropertyAsync(itemPath, booleanValue);

            Assert.True(result);
            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<bool>($"PowerAppsTestEngine.setPropertyValue({itemPathString}, {jsonSerializedValue})"), Times.Once());
        }

        [Fact]
        public async Task SetPropertyDateAsyncTest()
        {
            MockTestInfraFunctions.Setup(x => x.RunJavascriptAsync<bool>(It.IsAny<string>())).Returns(Task.FromResult(true));
            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0);
            string itemPathString = "{\"controlName\":\"DatePicker1\",\"index\":null,\"parentControl\":null,\"propertyName\":\"SelectedDate\"}";
            var itemPath = JsonConvert.DeserializeObject<ItemPath>(itemPathString);
            var result = await powerAppFunctions.SetPropertyAsync(itemPath, DateValue.NewDateOnly(dt.Date));

            Assert.True(result);
            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<bool>($"PowerAppsTestEngine.setPropertyValue({itemPathString},{{\"SelectedDate\":Date.parse(\"{((DateValue)DateValue.NewDateOnly(dt.Date)).GetConvertedValue(null)}\")}})"), Times.Once());
        }

        [Fact]
        public async Task SetPropertyRecordAsyncTest()
        {
            MockTestInfraFunctions.Setup(x => x.RunJavascriptAsync<bool>(It.IsAny<string>())).Returns(Task.FromResult(true));
            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);

            string itemPathString = "{\"controlName\":\"Dropdown1\",\"index\":null,\"parentControl\":null,\"propertyName\":\"Selected\"}";
            var itemPath = JsonConvert.DeserializeObject<ItemPath>(itemPathString);

            var pair = new KeyValuePair<string, FormulaValue>("Value", StringValue.New("2"));
            var nameValue = new NamedValue(pair);

            var result = await powerAppFunctions.SetPropertyAsync(itemPath, RecordValue.NewRecordFromFields(nameValue));
            var value = "{\"Value\":\"2\"}";

            Assert.True(result);
            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<bool>($"PowerAppsTestEngine.setPropertyValue({itemPathString},{{\"Selected\":{value}}})"), Times.Once());
        }

        [Fact]
        public async Task SetPropertyTableAsyncTest()
        {
            MockTestInfraFunctions.Setup(x => x.RunJavascriptAsync<bool>(It.IsAny<string>())).Returns(Task.FromResult(true));
            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);

            string itemPathString = "{\"controlName\":\"ComboBox1\",\"index\":null,\"parentControl\":null,\"propertyName\":\"SelectedItems\"}";
            var itemPath = JsonConvert.DeserializeObject<ItemPath>(itemPathString);

            //Record Type for table 
            var controlType = RecordType.Empty().Add("Value", FormulaType.String);

            //First record value for table 
            var pair1 = new KeyValuePair<string, FormulaValue>("Value", StringValue.New("2"));
            var name1Value = new NamedValue(pair1);
            var record1Value = RecordValue.NewRecordFromFields(name1Value);

            //Second record value for table 
            var pair2 = new KeyValuePair<string, FormulaValue>("Value", StringValue.New("3"));
            var name2Value = new NamedValue(pair2);
            var record2Value = RecordValue.NewRecordFromFields(name2Value);

            RecordValue[] values = new RecordValue[] { record1Value, record2Value };
            var result = await powerAppFunctions.SetPropertyAsync(itemPath, TableValue.NewTable(controlType, values));
            var value = "[{\"Value\":\"2\"},{\"Value\":\"3\"}]";

            Assert.True(result);
            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<bool>($"PowerAppsTestEngine.setPropertyValue({itemPathString},{{\"SelectedItems\":{value}}})"), Times.Once());
        }

        [Fact]
        public async Task SetPropertyAsyncItemPathTest()
        {
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            MockLogger.Setup(x => x.Log<It.IsAnyType>(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()));
            MockTestInfraFunctions.Setup(x => x.RunJavascriptAsync<bool>(It.IsAny<string>())).Returns(Task.FromResult(true));

            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);

            // Testing itempath controlname null case
            var itemPath = JsonConvert.DeserializeObject<ItemPath>("{\"controlName\":null,\"index\":null,\"parentControl\":null,\"propertyName\":\"Text\"}");
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await powerAppFunctions.SetPropertyAsync(itemPath, StringValue.New("A")));

            // Testing itempath propertyname null case
            itemPath = JsonConvert.DeserializeObject<ItemPath>("{\"controlName\":\"Button1\",\"index\":null,\"parentControl\":null,\"propertyName\":null}");
            var result = await powerAppFunctions.SetPropertyAsync(itemPath, StringValue.New("A"));
            Assert.True(result);

            // Testing itempath propertyname null case when index not null
            itemPath = JsonConvert.DeserializeObject<ItemPath>("{\"controlName\":\"Button1\",\"index\":1,\"parentControl\":null,\"propertyName\":null}");
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await powerAppFunctions.SetPropertyAsync(itemPath, StringValue.New("A")));
        }

        [Fact]
        public async Task SetPropertyAsyncThrowsOnInvalidFormulaValueTest()
        {
            var itemPath = JsonConvert.DeserializeObject<ItemPath>("{\"controlName\":\"Button1\",\"index\":null,\"parentControl\":null,\"propertyName\":\"Text\"}");
            Guid guid = new Guid("00000000-0000-0000-0000-000000000001");
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);

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
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);
            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            var itemPath = JsonConvert.DeserializeObject<ItemPath>(itemPathString);
            var result = powerAppFunctions.GetPropertyValueFromControl<string>(itemPath);
            Assert.Equal(expectedOutput, result);
            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<string>($"PowerAppsTestEngine.getPropertyValue({itemPathString}).then((propertyValue) => JSON.stringify(propertyValue))"), Times.Once());
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
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);
            var itemPath = JsonConvert.DeserializeObject<ItemPath>(itemPathString);
            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            Assert.Throws<ArgumentNullException>(() => powerAppFunctions.GetPropertyValueFromControl<string>(itemPath));
        }

        [Fact]
        public async Task LoadPowerAppsObjectModelAsyncTest()
        {
            var expectedFormulaTypes = TestData.CreateExpectedFormulaTypesForSampleJsPropertyModelList();
            var button1RecordType = RecordType.Empty();
            var label2RecordType = RecordType.Empty();
            var label3RecordType = RecordType.Empty();
            var button2RecordType = RecordType.Empty();
            var button3RecordType = RecordType.Empty();
            var gallery1RecordType = RecordType.Empty();
            foreach (var expectedFormulaType in expectedFormulaTypes)
            {
                button1RecordType = button1RecordType.Add(expectedFormulaType.Key, expectedFormulaType.Value);
                label2RecordType = label2RecordType.Add(expectedFormulaType.Key, expectedFormulaType.Value);
                label3RecordType = label3RecordType.Add(expectedFormulaType.Key, expectedFormulaType.Value);
                button2RecordType = button2RecordType.Add(expectedFormulaType.Key, expectedFormulaType.Value);
                gallery1RecordType = gallery1RecordType.Add(expectedFormulaType.Key, expectedFormulaType.Value);
                button3RecordType = button3RecordType.Add(expectedFormulaType.Key, expectedFormulaType.Value);
            }
            var allItemsType = TableType.Empty()
                                .Add(new NamedFormulaType("Button1", button1RecordType))
                                .Add(new NamedFormulaType("Label2", label2RecordType))
                                .Add(new NamedFormulaType("Label3", label3RecordType));
            expectedFormulaTypes.Add("AllItems", allItemsType);
            gallery1RecordType.Add("AllItems", allItemsType);
            var selectedItemType = RecordType.Empty()
                                .Add(new NamedFormulaType("Button1", button1RecordType))
                                .Add(new NamedFormulaType("Label2", label2RecordType))
                                .Add(new NamedFormulaType("Label3", label3RecordType));
            expectedFormulaTypes.Add("SelectedItem", selectedItemType);
            expectedFormulaTypes.Add("Button2", button2RecordType);
            expectedFormulaTypes.Add("AllItems2", TableType.Empty().Add(new NamedFormulaType("Gallery1", gallery1RecordType)).Add(new NamedFormulaType("Button3", button3RecordType)));
            expectedFormulaTypes.Add("SelectedItem2", RecordType.Empty().Add(new NamedFormulaType("Gallery1", gallery1RecordType)).Add(new NamedFormulaType("Button3", button3RecordType)));

            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            MockTestInfraFunctions.Setup(x => x.AddScriptTagAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            MockTestInfraFunctions.SetupSequence(x => x.RunJavascriptAsync<string>("typeof PowerAppsTestEngine")).Returns(Task.FromResult("object"));
            MockTestInfraFunctions.Setup(x => x.RunJavascriptAsync<string>("PowerAppsTestEngine.buildObjectModel().then((objectModel) => JSON.stringify(objectModel))")).Returns(Task.FromResult(JsonConvert.SerializeObject(JsObjectModel)));
            var testSettings = new TestSettings() { Timeout = 30000 };
            MockTestState.Setup(x => x.GetTestSettings()).Returns(testSettings);
            LoggingTestHelper.SetupMock(MockLogger);
            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            var objectModel = await powerAppFunctions.LoadPowerAppsObjectModelAsync();

            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<string>("PowerAppsTestEngine.buildObjectModel().then((objectModel) => JSON.stringify(objectModel))"), Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, (string)null, LogLevel.Debug, Times.Exactly(2));

            Assert.Equal(JsObjectModel.Controls.Count, objectModel.Count);
            foreach (var jsModel in JsObjectModel.Controls)
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
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            MockTestInfraFunctions.Setup(x => x.AddScriptTagAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            MockTestInfraFunctions.Setup(x => x.RunJavascriptAsync<string>("typeof PowerAppsTestEngine")).Returns(Task.FromResult("object"));
            MockTestInfraFunctions.Setup(x => x.RunJavascriptAsync<string>("PowerAppsTestEngine.buildObjectModel().then((objectModel) => JSON.stringify(objectModel))")).Returns(Task.FromResult(JsonConvert.SerializeObject(jsObjectModel)));
            var testSettings = new TestSettings() { Timeout = 30000 };
            MockTestState.Setup(x => x.GetTestSettings()).Returns(testSettings);
            LoggingTestHelper.SetupMock(MockLogger);
            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            var objectModel = await powerAppFunctions.LoadPowerAppsObjectModelAsync();

            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<string>("PowerAppsTestEngine.buildObjectModel().then((objectModel) => JSON.stringify(objectModel))"), Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, (string)"Control: Label1 already added", LogLevel.Trace, Times.Once());

            Assert.Single(objectModel);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("{}")]
        [InlineData("{ controls: [] }")]
        public async Task LoadPowerAppsObjectModelAsyncWithNoModelTest(string jsObjectModelString)
        {
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            MockTestInfraFunctions.Setup(x => x.AddScriptTagAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            MockTestInfraFunctions.Setup(x => x.RunJavascriptAsync<string>("typeof PowerAppsTestEngine")).Returns(Task.FromResult("object"));
            MockTestInfraFunctions.Setup(x => x.RunJavascriptAsync<string>("PowerAppsTestEngine.buildObjectModel().then((objectModel) => JSON.stringify(objectModel))")).Returns(Task.FromResult(jsObjectModelString));
            var testSettings = new TestSettings() { Timeout = 3000 };
            MockTestState.Setup(x => x.GetTestSettings()).Returns(testSettings);
            LoggingTestHelper.SetupMock(MockLogger);
            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            await Assert.ThrowsAsync<TimeoutException>(async () => { await powerAppFunctions.LoadPowerAppsObjectModelAsync(); });

            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<string>("PowerAppsTestEngine.buildObjectModel().then((objectModel) => JSON.stringify(objectModel))"), Times.AtLeastOnce());
            LoggingTestHelper.VerifyLogging(MockLogger, "Start to load power apps object model", LogLevel.Debug, Times.Once());
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
            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<bool>($"PowerAppsTestEngine.select({JsonConvert.SerializeObject(itemPath)})"), Times.Once());
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
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);
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
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);
            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            var itemPath = JsonConvert.DeserializeObject<ItemPath>(itemPathString);
            var result = powerAppFunctions.GetItemCount(itemPath);
            Assert.Equal(expectedOutput, result);
            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<int>($"PowerAppsTestEngine.getItemCount({JsonConvert.SerializeObject(itemPath)})"), Times.Once());
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
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);
            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            Assert.Throws<ArgumentNullException>(() => powerAppFunctions.GetItemCount(JsonConvert.DeserializeObject<ItemPath>(itemPathString)));
        }

        [Fact]
        public async Task LoadPowerAppsObjectModelAsyncWaitsForAppToLoad()
        {
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            MockTestInfraFunctions.Setup(x => x.AddScriptTagAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            MockTestInfraFunctions.SetupSequence(x => x.RunJavascriptAsync<string>("typeof PowerAppsTestEngine"))
                .Returns(Task.FromResult("object"));
            MockTestInfraFunctions.Setup(x => x.RunJavascriptAsync<string>("PowerAppsTestEngine.buildObjectModel().then((objectModel) => JSON.stringify(objectModel))")).Returns(Task.FromResult("{}"));
            var testSettings = new TestSettings() { Timeout = 6000 };
            MockTestState.Setup(x => x.GetTestSettings()).Returns(testSettings);
            LoggingTestHelper.SetupMock(MockLogger);
            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            await Assert.ThrowsAsync<TimeoutException>(async () => { await powerAppFunctions.LoadPowerAppsObjectModelAsync(); });

            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<string>("PowerAppsTestEngine.buildObjectModel().then((objectModel) => JSON.stringify(objectModel))"), Times.AtLeastOnce());
            LoggingTestHelper.VerifyLogging(MockLogger, "Start to load power apps object model", LogLevel.Debug, Times.Once());
        }

        [Fact]
        public async Task LoadPowerAppsObjectModelAsyncWaitsForAppToLoadWithExceptions()
        {
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            MockTestInfraFunctions.Setup(x => x.AddScriptTagAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            MockTestInfraFunctions.SetupSequence(x => x.RunJavascriptAsync<string>("typeof PowerAppsTestEngine"))
    .Returns(Task.FromResult("object"));

            MockTestInfraFunctions.Setup(x => x.RunJavascriptAsync<string>("PowerAppsTestEngine.buildObjectModel().then((objectModel) => JSON.stringify(objectModel))")).Returns(Task.FromResult("{}"));
            var testSettings = new TestSettings() { Timeout = 12000 };
            MockTestState.Setup(x => x.GetTestSettings()).Returns(testSettings);
            LoggingTestHelper.SetupMock(MockLogger);
            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            await Assert.ThrowsAsync<TimeoutException>(async () => { await powerAppFunctions.LoadPowerAppsObjectModelAsync(); });

            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<string>("PowerAppsTestEngine.buildObjectModel().then((objectModel) => JSON.stringify(objectModel))"), Times.AtLeastOnce());
            LoggingTestHelper.VerifyLogging(MockLogger, "Start to load power apps object model", LogLevel.Debug, Times.Once());
        }

        [Fact]
        public async Task LoadPowerAppsObjectModelAsyncEmbedJSUndefined()
        {
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            MockTestInfraFunctions.Setup(x => x.AddScriptTagAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            MockTestInfraFunctions.SetupSequence(x => x.RunJavascriptAsync<string>("typeof PowerAppsTestEngine"))
                .Returns(Task.FromResult("undefined"));
            MockTestInfraFunctions.Setup(x => x.RunJavascriptAsync<string>("PowerAppsTestEngine.buildObjectModel().then((objectModel) => JSON.stringify(objectModel))")).Returns(Task.FromResult("{}"));
            var testSettings = new TestSettings() { Timeout = 5000 };
            MockTestState.Setup(x => x.GetTestSettings()).Returns(testSettings);
            LoggingTestHelper.SetupMock(MockLogger);

            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            await Assert.ThrowsAsync<TimeoutException>(async () => { await powerAppFunctions.LoadPowerAppsObjectModelAsync(); });

            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<string>("PowerAppsTestEngine.buildObjectModel().then((objectModel) => JSON.stringify(objectModel))"), Times.AtLeastOnce());
            LoggingTestHelper.VerifyLogging(MockLogger, "Start to load power apps object model", LogLevel.Debug, Times.Once());
        }

        [Fact]
        public async Task LoadPowerAppsObjectModelAsyncEmbedJSDefined()
        {
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            MockTestInfraFunctions.Setup(x => x.AddScriptTagAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            MockTestInfraFunctions.SetupSequence(x => x.RunJavascriptAsync<string>("typeof PowerAppsTestEngine"))
                .Returns(Task.FromResult("defined"));
            MockTestInfraFunctions.Setup(x => x.RunJavascriptAsync<string>("PowerAppsTestEngine.buildObjectModel().then((objectModel) => JSON.stringify(objectModel))")).Returns(Task.FromResult("{}"));
            var testSettings = new TestSettings() { Timeout = 5000 };
            MockTestState.Setup(x => x.GetTestSettings()).Returns(testSettings);
            LoggingTestHelper.SetupMock(MockLogger);

            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            await Assert.ThrowsAsync<TimeoutException>(async () => { await powerAppFunctions.LoadPowerAppsObjectModelAsync(); });

            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<string>("PowerAppsTestEngine.buildObjectModel().then((objectModel) => JSON.stringify(objectModel))"), Times.AtLeastOnce());
            LoggingTestHelper.VerifyLogging(MockLogger, "Start to load power apps object model", LogLevel.Debug, Times.Once());
        }

        [Fact]
        public async Task GetDebugInfoReturnsObject()
        {
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);

            var newSession = new ExpandoObject();
            newSession.TryAdd("appId", "someAppId");
            newSession.TryAdd("appVersion", "someAppVersionId");
            newSession.TryAdd("environmentId", "someEnvironmentId");
            newSession.TryAdd("sessionId", "someSessionId");

            MockTestInfraFunctions.Setup(x => x.AddScriptTagAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            MockTestInfraFunctions.SetupSequence(x => x.RunJavascriptAsync<object>("PowerAppsTestEngine.debugInfo"))
    .Returns(Task.FromResult((object)newSession));
            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            var actualValue = await powerAppFunctions.GetDebugInfo();

            Assert.Equal(actualValue, (object)newSession);
            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<object>("PowerAppsTestEngine.debugInfo"), Times.Once());
        }

        [Fact]
        public async Task GetDebugInfoReturnsNull()
        {
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            MockTestInfraFunctions.Setup(x => x.AddScriptTagAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            MockTestInfraFunctions.SetupSequence(x => x.RunJavascriptAsync<object>("PowerAppsTestEngine.debugInfo"))
    .Returns(Task.FromResult<object>(null));

            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            var actualValue = await powerAppFunctions.GetDebugInfo();

            Assert.Null(actualValue);
            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<object>("PowerAppsTestEngine.debugInfo"), Times.Once());
        }

        [Fact]
        public async Task TestEngineReadyReturnsTrue()
        {
            // Arrange
            MockTestInfraFunctions.Setup(x => x.RunJavascriptAsync<string>(PowerAppFunctions.CheckPowerAppsTestEngineReadyFunction))
                .Returns(Task.FromResult("function"));
            MockTestInfraFunctions.Setup(x => x.RunJavascriptAsync<bool>("PowerAppsTestEngine.testEngineReady()"))
                .Returns(Task.FromResult(true));
            MockTestInfraFunctions.Setup(x => x.AddScriptTagAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

            // Act
            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            var readyValue = await powerAppFunctions.TestEngineReady();

            // Assertion
            Assert.True(readyValue);
            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<string>(PowerAppFunctions.CheckPowerAppsTestEngineReadyFunction), Times.Once());
            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<bool>("PowerAppsTestEngine.testEngineReady()"), Times.Once());
        }

        [Fact]
        public async Task TestEngineReadyUndefinedWebplayerReturnsTrue()
        {
            // Arrange
            // Mock to return undefined >> scenario where webplayer JSSDK does not have testEngineReady function
            MockTestInfraFunctions.Setup(x => x.RunJavascriptAsync<string>(PowerAppFunctions.CheckPowerAppsTestEngineReadyFunction))
                .Returns(Task.FromResult("undefined"));
            MockTestInfraFunctions.Setup(x => x.AddScriptTagAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

            // Act
            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            var readyValue = await powerAppFunctions.TestEngineReady();

            // Assertion
            Assert.True(readyValue);
            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<string>(PowerAppFunctions.CheckPowerAppsTestEngineReadyFunction), Times.Once());
            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<bool>("PowerAppsTestEngine.testEngineReady()"), Times.Never());
        }

        [Fact]
        public async Task TestEngineReadyPublishedAppWithoutJSSDKErrorCodeReturnsTrue()
        {
            // Arrange
            MockTestInfraFunctions.Setup(x => x.RunJavascriptAsync<string>(PowerAppFunctions.CheckPowerAppsTestEngineReadyFunction))
                .Returns(Task.FromResult("function"));
            // Mock to return error code 1
            // Scenario where error thrown is PublishedAppWithoutJSSDKErrorCode
            MockTestInfraFunctions.Setup(x => x.RunJavascriptAsync<bool>("PowerAppsTestEngine.testEngineReady()"))
                .Throws(new Exception("1"));
            MockTestInfraFunctions.Setup(x => x.AddScriptTagAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

            // Act
            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            var readyValue = await powerAppFunctions.TestEngineReady();

            // Assertion
            // readyValue still returns true >> supporting old msapps without ready function
            Assert.True(readyValue);
            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<string>(PowerAppFunctions.CheckPowerAppsTestEngineReadyFunction), Times.Once());
            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<bool>("PowerAppsTestEngine.testEngineReady()"), Times.Once());
        }

        [Fact]
        public async Task TestEngineReadyNonPublishedAppWithoutJSSDKErrorCodeThrows()
        {
            // Arrange
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);            
            MockTestInfraFunctions.Setup(x => x.RunJavascriptAsync<string>(PowerAppFunctions.CheckPowerAppsTestEngineReadyFunction))
                .Returns(Task.FromResult("function"));
            // Mock to return error code 0 
            // Scenario where error thrown from app for reason other than PublishedAppWithoutJSSDKErrorCode
            MockTestInfraFunctions.Setup(x => x.RunJavascriptAsync<bool>("PowerAppsTestEngine.testEngineReady()"))
                .Throws(new Exception("0"));
            MockTestInfraFunctions.Setup(x => x.AddScriptTagAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            LoggingTestHelper.SetupMock(MockLogger);

            // Act and Assertion
            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            await Assert.ThrowsAsync<Exception>(() => powerAppFunctions.TestEngineReady());
            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<string>(PowerAppFunctions.CheckPowerAppsTestEngineReadyFunction), Times.Once());
            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<bool>("PowerAppsTestEngine.testEngineReady()"), Times.Once());
            MockLogger.Verify();
        }

        // Start Published App JSSDK not found tests
        [Fact]
        public async Task LoadPowerAppsObjectModelAsyncNoPublishedAppFunction()
        {
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            MockTestInfraFunctions.Setup(x => x.AddScriptTagAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            MockTestInfraFunctions.SetupSequence(x => x.RunJavascriptAsync<string>("typeof PowerAppsTestEngine"))
                .Returns(Task.FromResult("object"));
            MockTestInfraFunctions.Setup(x => x.RunJavascriptAsync<string>("PowerAppsTestEngine.buildObjectModel().then((objectModel) => JSON.stringify(objectModel))")).Throws(new Exception("1"));

            var testSettings = new TestSettings() { Timeout = 12000 };
            MockTestState.Setup(x => x.GetTestSettings()).Returns(testSettings);
            LoggingTestHelper.SetupMock(MockLogger);
            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            await Assert.ThrowsAsync<Exception>(async () => { await powerAppFunctions.LoadPowerAppsObjectModelAsync(); });

            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<string>("PowerAppsTestEngine.buildObjectModel().then((objectModel) => JSON.stringify(objectModel))"), Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, "Start to load power apps object model", LogLevel.Debug, Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, ExceptionHandlingHelper.PublishedAppWithoutJSSDKMessage, LogLevel.Error, Times.Once());
        }

        [Fact]
        public async Task SelectControlAsyncFailsNoPublishedAppFunction()
        {
            var itemPath = JsonConvert.DeserializeObject<ItemPath>("{\"controlName\":\"Label1\",\"index\":null,\"parentControl\":null,\"propertyName\":null}");

            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            MockTestInfraFunctions.Setup(x => x.RunJavascriptAsync<bool>(It.IsAny<string>()))
                .Throws(new Exception("1"));

            LoggingTestHelper.SetupMock(MockLogger);
            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            await Assert.ThrowsAsync<Exception>(async () => { await powerAppFunctions.SelectControlAsync(itemPath); });

            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<bool>(It.IsAny<string>()), Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, ExceptionHandlingHelper.PublishedAppWithoutJSSDKMessage, LogLevel.Error, Times.Once());
        }

        [Fact]
        public async Task SetPropertyRecordAsyncNoPublishedAppFunction()
        {
            string itemPathString = "{\"controlName\":\"Dropdown1\",\"index\":null,\"parentControl\":null,\"propertyName\":\"Selected\"}";
            var itemPath = JsonConvert.DeserializeObject<ItemPath>(itemPathString);

            var pair = new KeyValuePair<string, FormulaValue>("Value", StringValue.New("2"));
            var nameValue = new NamedValue(pair);

            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            MockTestInfraFunctions.Setup(x => x.RunJavascriptAsync<bool>(It.IsAny<string>()))
                .Throws(new Exception("1"));

            LoggingTestHelper.SetupMock(MockLogger);
            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            await Assert.ThrowsAsync<Exception>(async () => { await powerAppFunctions.SetPropertyRecordAsync(itemPath, RecordValue.NewRecordFromFields(nameValue)); });
            var value = "{\"Value\":\"2\"}";

            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<bool>($"PowerAppsTestEngine.setPropertyValue({itemPathString},{{\"Selected\":{value}}})"), Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, ExceptionHandlingHelper.PublishedAppWithoutJSSDKMessage, LogLevel.Error, Times.Once());
        }

        [Fact]
        public async Task SetPropertyTableAsyncNoPublishedAppFunction()
        {
            string itemPathString = "{\"controlName\":\"ComboBox1\",\"index\":null,\"parentControl\":null,\"propertyName\":\"SelectedItems\"}";
            var itemPath = JsonConvert.DeserializeObject<ItemPath>(itemPathString);

            //Record Type for table 
            var controlType = RecordType.Empty().Add("Value", FormulaType.String);

            //First record value for table 
            var pair1 = new KeyValuePair<string, FormulaValue>("Value", StringValue.New("2"));
            var name1Value = new NamedValue(pair1);
            var record1Value = RecordValue.NewRecordFromFields(name1Value);

            //Second record value for table 
            var pair2 = new KeyValuePair<string, FormulaValue>("Value", StringValue.New("3"));
            var name2Value = new NamedValue(pair2);
            var record2Value = RecordValue.NewRecordFromFields(name2Value);

            RecordValue[] values = new RecordValue[] { record1Value, record2Value };
            var value = "[{\"Value\":\"2\"},{\"Value\":\"3\"}]";

            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            MockTestInfraFunctions.Setup(x => x.RunJavascriptAsync<bool>(It.IsAny<string>()))
                .Throws(new Exception("1"));

            LoggingTestHelper.SetupMock(MockLogger);
            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            await Assert.ThrowsAsync<Exception>(async () => { await powerAppFunctions.SetPropertyTableAsync(itemPath, TableValue.NewTable(controlType, values)); });

            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<bool>($"PowerAppsTestEngine.setPropertyValue({itemPathString},{{\"SelectedItems\":{value}}})"), Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, ExceptionHandlingHelper.PublishedAppWithoutJSSDKMessage, LogLevel.Error, Times.Once());
        }

        [Fact]
        public async Task SetPropertyDateAsyncNoPublishedAppFunction()
        {
            DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0);
            string itemPathString = "{\"controlName\":\"DatePicker1\",\"index\":null,\"parentControl\":null,\"propertyName\":\"SelectedDate\"}";
            var itemPath = JsonConvert.DeserializeObject<ItemPath>(itemPathString);

            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            MockTestInfraFunctions.Setup(x => x.RunJavascriptAsync<bool>(It.IsAny<string>()))
                .Throws(new Exception("1"));

            LoggingTestHelper.SetupMock(MockLogger);
            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            await Assert.ThrowsAsync<Exception>(async () => { await powerAppFunctions.SetPropertyDateAsync(itemPath, DateValue.NewDateOnly(dt.Date)); });

            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<bool>($"PowerAppsTestEngine.setPropertyValue({itemPathString},{{\"SelectedDate\":Date.parse(\"{((DateValue)DateValue.NewDateOnly(dt.Date)).GetConvertedValue(null)}\")}})"), Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, ExceptionHandlingHelper.PublishedAppWithoutJSSDKMessage, LogLevel.Error, Times.Once());
        }

        [Fact]
        public async Task GetItemCountAsyncNoPublishedAppFunction()
        {
            var itemPath = JsonConvert.DeserializeObject<ItemPath>("{\"controlName\":\"Button1\",\"index\":null,\"parentControl\":null,\"propertyName\":null}");

            MockTestState.Setup(x => x.GetTimeout()).Returns(30000);
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            MockTestInfraFunctions.Setup(x => x.RunJavascriptAsync<int>(It.IsAny<string>()))
                .Throws(new Exception("1"));

            LoggingTestHelper.SetupMock(MockLogger);
            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);

            await Assert.ThrowsAsync<Exception>(async () => { powerAppFunctions.GetItemCount(itemPath); });
            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<int>($"PowerAppsTestEngine.getItemCount({JsonConvert.SerializeObject(itemPath)})"), Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, ExceptionHandlingHelper.PublishedAppWithoutJSSDKMessage, LogLevel.Error, Times.Once());
        }

        [Fact]
        public async Task GetPropertyAsyncNoPublishedAppFunction()
        {
            var itemPath = JsonConvert.DeserializeObject<ItemPath>("{\"controlName\":\"Label1\",\"index\":null,\"parentControl\":null,\"propertyName\":\"Text\"}");

            MockTestState.Setup(x => x.GetTimeout()).Returns(30000);
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            MockTestInfraFunctions.Setup(x => x.RunJavascriptAsync<string>(It.IsAny<string>()))
                .Throws(new Exception("1"));

            LoggingTestHelper.SetupMock(MockLogger);
            var powerAppFunctions = new PowerAppFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);

            await Assert.ThrowsAsync<Exception>(async () => { powerAppFunctions.GetPropertyValueFromControl<string>(itemPath); });
            MockTestInfraFunctions.Verify(x => x.RunJavascriptAsync<string>($"PowerAppsTestEngine.getPropertyValue({JsonConvert.SerializeObject(itemPath)}).then((propertyValue) => JSON.stringify(propertyValue))"), Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, ExceptionHandlingHelper.PublishedAppWithoutJSSDKMessage, LogLevel.Error, Times.Once());
        }

        // End Published App JSSDK not found tests
    }
}
