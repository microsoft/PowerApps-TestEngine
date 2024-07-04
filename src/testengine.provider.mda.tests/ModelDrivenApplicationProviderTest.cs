// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Dynamic;
using System.Reflection;
using System.Reflection.PortableExecutable;
using Jint;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Providers;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx.Types;
using Moq;
using Newtonsoft.Json;

namespace Microsoft.PowerApps.TestEngine.Tests.PowerApps
{
    public class ModelDrivenApplicationProviderTests
    {
        private Mock<ITestInfraFunctions> MockTestInfraFunctions;
        private Mock<ITestState> MockTestState;
        private Mock<ISingleTestInstanceState> MockSingleTestInstanceState;
        private Mock<ILogger> MockLogger;
        private Mock<IBrowserContext> MockBrowserContext;
        private JSObjectModel JsObjectModel;

        public ModelDrivenApplicationProviderTests()
        {
            MockTestInfraFunctions = new Mock<ITestInfraFunctions>(MockBehavior.Strict);
            MockTestState = new Mock<ITestState>(MockBehavior.Strict);
            MockSingleTestInstanceState = new Mock<ISingleTestInstanceState>(MockBehavior.Strict);
            MockLogger = new Mock<ILogger>(MockBehavior.Loose);
            MockBrowserContext = new Mock<IBrowserContext>(MockBehavior.Strict);
            JsObjectModel = new JSObjectModel()
            {
                Controls = new List<JSControlModel>()
            };
        }

        [Theory]
        [MemberData(nameof(TestJavaScript))]
        public void ValidJavaScript(string javaScript, bool includeMocks, bool includeInterface)
        {
            var engine = new Engine();
            engine.Evaluate(Common.MockJavaScript(javaScript, "custom", includeMocks, includeInterface));
        }

        public static IEnumerable<object[]> TestJavaScript()
        {
            yield return new object[] { "var a = 1", false, false };

            yield return new object[] { "", false, false };

            yield return new object[] { "", true, false };

            yield return new object[] { "", false, true };

            yield return new object[] { "", true, true };

            yield return new object[] { "mockValue = 'test'", true, true };

            yield return new object[] { "PowerAppsTestEngine.pageType()", true, true };

            yield return new object[] { "PowerAppsModelDrivenCanvas.getAppMagic()", true, true };

            yield return new object[] { "PowerAppsModelDrivenCanvas.parseControl('TextInput1', mockCanvasControl)", true, true };

            yield return new object[] { "PowerAppsModelDrivenCanvas.getAppMagic().Controls?.GlobalContextManager?.bindingContext", true, true };

            yield return new object[] { "PowerAppsModelDrivenCanvas.getAppMagic().Controls.GlobalContextManager.bindingContext.componentBindingContexts.lookup('TextInput1')", true, true };

            yield return new object[] { "PowerAppsModelDrivenCanvas.getBindingContext({controlName: 'TextInput1', propertyName: 'Text'})", true, true };

            yield return new object[] { "PowerAppsModelDrivenCanvas.getPropertyValueFromControl({controlName: 'TextInput1', propertyName: 'Text'})", true, true };
        }

        [Theory]
        [InlineData("{\"controlName\":\"Label1\",\"index\":null,\"parentControl\":null,\"propertyName\":\"Text\"}", "", "'ABC'", "entityrecord", "ABC")]
        [InlineData("{\"controlName\":\"Label1\",\"index\":null,\"parentControl\":null,\"propertyName\":\"Visible\"}", "[{Key:'Visible',Value: false}]", "", "entityrecord", false)]
        [InlineData("{\"controlName\":\"Label1\",\"index\":null,\"parentControl\":null,\"propertyName\":\"Visible\"}", "[{Key:'Visible',Value: true}]", "", "entityrecord", true)]

        public void GetPropertyValue(string itemPathString, string json, object inputValue, string pageType, object expectedOutput)
        {
            // Arrange
            MockSingleTestInstanceState.Setup(m => m.GetLogger()).Returns(MockLogger.Object);
            var itemPath = JsonConvert.DeserializeObject<ItemPath>(itemPathString);
            var inputPropertyValue = "{PropertyValue:" + inputValue + "}";

            if (itemPath.PropertyName == "Text")
            {
                MockTestInfraFunctions.Setup(m => m.RunJavascriptAsync<string>("PowerAppsTestEngine.pageType()")).Returns(Task.FromResult("entityrecord"));
            }

            MockTestState.Setup(m => m.GetTimeout()).Returns(30000);
            MockSingleTestInstanceState.Setup(m => m.GetLogger()).Returns(MockLogger.Object);

            if (!string.IsNullOrEmpty(pageType))
            {
                MockTestInfraFunctions.Setup(m => m.RunJavascriptAsync<object>("PowerAppsTestEngine.pageType()"))
                    .Returns(Task.FromResult((object)pageType));
            }

            if (string.IsNullOrEmpty(json))
            {
                MockTestInfraFunctions.Setup(m => m.RunJavascriptAsync<object>(string.Format(ModelDrivenApplicationProvider.QueryFormField, itemPath.ControlName)))
                    .Returns(Task.FromResult((object)inputPropertyValue));
            }
            else
            {
                MockTestInfraFunctions.Setup(m => m.RunJavascriptAsync<object>(string.Format(ModelDrivenApplicationProvider.ControlPropertiesQuery, JsonConvert.SerializeObject(itemPath))))
                    .Returns(Task.FromResult((object)json));
            }

            // Act
            var provider = new ModelDrivenApplicationProvider(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            var result = provider.GetPropertyValueFromControl<string>(itemPath);

            // Assert
            var propertryValue = JsonConvert.DeserializeObject<JSPropertyValueModel>(result);

            Assert.Equal(expectedOutput.ToString().ToLower(), propertryValue.PropertyValue.ToLower());
        }

        [Theory]
        [InlineData("", false)]
        [InlineData("class UCWorkBlockTracker {}", false)]
        [InlineData("class UCWorkBlockTracker { static isAppIdle() { return false; } }", false)]
        [InlineData("class UCWorkBlockTracker { static isAppIdle() { return true; } }", true)]
        public void IsIdle(string javaScript, bool expectedIdle)
        {
            var engine = new Engine();
            engine.Execute(javaScript);

            MockSingleTestInstanceState.Setup(m => m.GetLogger()).Returns(MockLogger.Object);
            MockTestInfraFunctions.Setup(m => m.RunJavascriptAsync<string>(It.IsAny<string>())).Returns(
                async (string query) => engine.Evaluate(query).AsString()
            );

            // Act
            var provider = new ModelDrivenApplicationProvider(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            var result = provider.CheckIsIdleAsync().Result;

            // Assert
            Assert.Equal(expectedIdle, result);
        }

        [Theory]
        [InlineData("", false)]
        [InlineData("class UCWorkBlockTracker {}", false)]
        [InlineData("class UCWorkBlockTracker { static isAppIdle() { return false; } }", false)]
        [InlineData("class UCWorkBlockTracker { static isAppIdle() { return true; } }", true)]
        public void IsReady(string javaScript, bool expectedIdle)
        {
            var engine = new Engine();
            engine.Execute(javaScript);

            MockSingleTestInstanceState.Setup(m => m.GetLogger()).Returns(MockLogger.Object);

            MockTestInfraFunctions = new Mock<ITestInfraFunctions>();
            MockTestInfraFunctions.Setup(m => m.RunJavascriptAsync<string>(It.IsAny<string>()))
            .Returns(
                async (string query) => engine.Evaluate(query).AsString()
            );

            MockTestInfraFunctions.Setup(m => m.RunJavascriptAsync<bool>(It.IsAny<string>()))
            .Returns(
                async (string query) => engine.Evaluate(query).AsBoolean()
            );

            // Act
            var provider = new ModelDrivenApplicationProvider(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            var result = provider.TestEngineReady().Result;

            // Assert
            Assert.Equal(expectedIdle, result);
        }

        [Theory]
        [MemberData(nameof(GetPropertyValueFromControlData))]
        public void GetPropertyValueFromControl(string javaScript, string controlName, string propertyName, object expectedResult)
        {
            var engine = new Engine();
            engine.Execute(javaScript);

            MockTestState.Setup(m => m.GetTimeout()).Returns(1000);

            MockSingleTestInstanceState.Setup(m => m.GetLogger()).Returns(MockLogger.Object);

            MockTestInfraFunctions = new Mock<ITestInfraFunctions>();

            MockTestInfraFunctions.Setup(m => m.RunJavascriptAsync<string>(It.IsAny<string>()))
            .Returns(
                async (string query) => engine.Evaluate(query).AsString()
            );

            MockTestInfraFunctions.Setup(m => m.RunJavascriptAsync<object>(It.IsAny<string>()))
            .Returns(
                async (string query) => engine.Evaluate(query).AsString()
            );

            MockTestInfraFunctions.Setup(m => m.RunJavascriptAsync<bool>(It.IsAny<string>()))
            .Returns(
                async (string query) => engine.Evaluate(query).AsBoolean()
            );

            // Act
            var provider = new ModelDrivenApplicationProvider(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            var result = provider.GetPropertyValueFromControl<string>(new ItemPath() { ControlName = controlName, PropertyName = propertyName });
            dynamic dynamicValue = JsonConvert.DeserializeObject<ExpandoObject>(result);

            // Assert

            Assert.Equal(expectedResult, dynamicValue.PropertyValue);
        }

        public static IEnumerable<object[]> GetPropertyValueFromControlData()
        {
            // Special case text should use the getValue()
            yield return new object[] {
                    Common.MockJavaScript("mockValue = 'Hello'", "entityrecord"),
                    "prefix_Name",
                    "Text",
                    "Hello"
            };

            // Disabled by default controlDescriptor property
            yield return new object[] {
                    Common.MockJavaScript("", "entityrecord"),
                    "prefix_Name",
                    "Disabled",
                    false
            };

            // test Disabled in controlDescriptor property
            yield return new object[] {
                    Common.MockJavaScript("mockControlDescriptor.Disabled = true", "entityrecord"),
                    "prefix_Name",
                    "Disabled",
                    true
            };
        }

        [Theory]
        [MemberData(nameof(GetControlData))]
        public async Task BuildEntityRecordControls(string javaScript, string controlName, string fields)
        {
            var engine = new Engine();
            engine.Execute(javaScript);

            MockTestState.Setup(m => m.GetTimeout()).Returns(1000);

            MockSingleTestInstanceState.Setup(m => m.GetLogger()).Returns(MockLogger.Object);

            MockTestInfraFunctions = new Mock<ITestInfraFunctions>();
            MockTestInfraFunctions.Setup(m => m.RunJavascriptAsync<string>("PowerAppsTestEngine.buildControlObjectModel()"))
            .Returns(
                async (string query) => engine.Evaluate(query).AsString()
            );

            MockTestInfraFunctions.Setup(m => m.RunJavascriptAsync<object>(It.IsNotIn("PowerAppsTestEngine.buildControlObjectModel()")))
            .Returns(
                async (string query) => (object)engine.Evaluate(query).AsString()
            );


            MockTestInfraFunctions.Setup(m => m.RunJavascriptAsync<bool>(It.IsAny<string>()))
            .Returns(
                async (string query) => engine.Evaluate(query).AsBoolean()
            );

            // Act
            var provider = new ModelDrivenApplicationProvider(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            var result = await provider.LoadObjectModelAsync();

            // Assert
            Assert.Single(result);
            Assert.True(result.ContainsKey(controlName));

            var controlFields = result[controlName].Fields;

            var fieldData = JsonConvert.DeserializeObject<Dictionary<string, object>>(fields);

            foreach (var key in fieldData.Keys)
            {
                var match = controlFields.Where(f => f.Name == key).FirstOrDefault();
                Assert.True(match != null, $"Field {key} not found");
                Assert.Equal(fieldData[key], match.Value.ToObject());
            }
        }

        /// <summary>
        /// Test data for <see cref="BuildControls"/>
        /// </summary>
        /// <returns>MemberData items</returns>
        public static IEnumerable<object[]> GetControlData()
        {
            // Default Values
            yield return new object[] {
                    Common.MockJavaScript("mockValue = 'Hello'", "entityrecord"),
                    "test",
                    "{ Disabled: false, Text: 'Hello', ShowLabel: true, Label: 'Text Input', Visible: true, IsRequired: false }"
            };

            // Change control name
            yield return new object[] {
                    Common.MockJavaScript("mockControlName = 'test2';mockValue = 'New value'", "entityrecord"),
                    "test2",
                    "{ Disabled: false, Text: 'New value', ShowLabel: true, Label: 'Text Input', Visible: true, IsRequired: false }"
            };

            // Change Label
            yield return new object[] {
                    Common.MockJavaScript("mockControlDescriptor.Label = 'Item'", "entityrecord"),
                    "test",
                    "{ Label: 'Item' }"
            };

            // Change Visible
            yield return new object[] {
                    Common.MockJavaScript("mockControlDescriptor.Visible = false", "entityrecord"),
                    "test",
                    "{ Visible: false }"
            };

            // Change IsRequired
            yield return new object[] {
                    Common.MockJavaScript("mockControlDescriptor.IsRequired = true", "entityrecord"),
                    "test",
                    "{ IsRequired: true }"
            };
        }

        [Theory]
        [MemberData(nameof(GetSetValueData))]
        public async Task SetValue(string javaScript, string controlName, string propertyName, string targetProperty, object value)
        {
            var engine = new Engine();
            engine.Execute(javaScript);

            MockTestState.Setup(m => m.GetTimeout()).Returns(1000);

            MockSingleTestInstanceState.Setup(m => m.GetLogger()).Returns(MockLogger.Object);

            MockTestInfraFunctions = new Mock<ITestInfraFunctions>();
            MockTestInfraFunctions.Setup(m => m.RunJavascriptAsync<string>(It.IsAny<string>()))
            .Returns(
                async (string query) => engine.Evaluate(query).AsString()
            );

            MockTestInfraFunctions.Setup(m => m.RunJavascriptAsync<object>(It.IsAny<string>()))
            .Returns(
                async (string query) => {
                    var result = engine.Evaluate(query);
                    if ( result.IsBoolean() )
                    {
                        return result.AsBoolean();
                    }
                    if (result.IsString())
                    {
                        return result.AsString();
                    }
                    return "undefined";
                }
            );

            MockTestInfraFunctions.Setup(m => m.RunJavascriptAsync<bool>(It.IsAny<string>()))
            .Returns(
                async (string query) => engine.Evaluate(query).AsBoolean()
            );

            FormulaValue providerValue = null;
            if (value is string)
            {
                providerValue = StringValue.New((string)value);
            }

            if (value is bool)
            {
                providerValue = BooleanValue.New((bool)value);
            }


            // Act
            var provider = new ModelDrivenApplicationProvider(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            var result = await provider.SetPropertyAsync(new ItemPath { ControlName = controlName, PropertyName = propertyName }, providerValue);

            // Assert
            object controlValue = null;
            if (value is string)
            {
                controlValue = engine.Evaluate($"mockControl.{targetProperty}").AsString();

                var changed = engine.Evaluate($"mockControl.changed").AsBoolean();
                Assert.True(changed);
            }
            if (value is bool)
            {
                controlValue = engine.Evaluate($"mockControl.{targetProperty}").AsBoolean();
            }

            Assert.Equal(value, controlValue);
        }

        /// <summary>
        /// Test data for <see cref="SetValue"/>
        /// </summary>
        /// <returns>MemberData items</returns>
        public static IEnumerable<object[]> GetSetValueData()
        {
            // Default Values
            yield return new object[] {
                    Common.MockJavaScript("mockValue = 'Hello'", "entityrecord"),
                    "test",
                    "Text",
                    "value",
                    "Hello"
            };

            // Visible Values
            yield return new object[] {
                    Common.MockJavaScript("mockValue = 'Hello'", "entityrecord"),
                    "test",
                    "Visible",
                    "visible",
                    true
            };

            // Disabled Values
            yield return new object[] {
                    Common.MockJavaScript("", "entityrecord"),
                    "test",
                    "Disabled",
                    "disabled",
                    true
            };
        }

        [Fact]
        public async Task CheckProviderAsync()
        {
            // Arrange
            MockTestInfraFunctions.Setup(m => m.GetContext()).Returns(MockBrowserContext.Object);
            MockTestInfraFunctions.Setup(m => m.AddScriptContentAsync(It.IsAny<string>()))
                .Returns(() => Task.CompletedTask);
            MockTestInfraFunctions.Setup(m => m.RunJavascriptAsync<bool>(It.IsAny<string>())).Returns(Task.FromResult(false));

            MockBrowserContext.Setup(m => m.Pages).Returns(new List<IPage>());

            MockSingleTestInstanceState.Setup(m => m.GetLogger()).Returns(MockLogger.Object);

            MockTestState.Setup(m => m.GetTimeout()).Returns(1000);

            // Act
            var provider = new ModelDrivenApplicationProvider(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            await provider.CheckProviderAsync();

            // Assert

        }

        [Theory]
        [MemberData(nameof(DebugInfoProperties))]
        public async Task DebugInfo(string propertyName, object expectedValue, string javaScript)
        {
            var engine = new Engine();
            engine.Execute(javaScript);

            MockTestState.Setup(m => m.GetTimeout()).Returns(1000);

            MockSingleTestInstanceState.Setup(m => m.GetLogger()).Returns(MockLogger.Object);

            MockBrowserContext.Setup(m => m.Pages).Returns(new List<IPage>());

            MockTestInfraFunctions = new Mock<ITestInfraFunctions>();

            MockTestInfraFunctions.Setup(m => m.GetContext()).Returns(MockBrowserContext.Object);
            MockTestInfraFunctions.Setup(m => m.RunJavascriptAsync<string>(It.IsAny<string>()))
            .Returns(
                async (string query) => engine.Evaluate(query).AsString()
            );

            MockTestInfraFunctions.Setup(m => m.RunJavascriptAsync<bool>(It.IsAny<string>()))
            .Returns(
                async (string query) => engine.Evaluate(query).AsBoolean()
            );

            // Act
            var provider = new ModelDrivenApplicationProvider(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            var result = (IDictionary<string, object>) (await provider.GetDebugInfo());

            // Assert
            Assert.Equal(expectedValue, result[propertyName]);
        }

        /// <summary>
        /// Test data for <see cref="DebugInfo"/>
        /// </summary>
        /// <returns>MemberData items</returns>
        public static IEnumerable<object[]> DebugInfoProperties()
        {
            yield return new object[] {
                    "PageType",
                    "entityrecord",
                     Common.MockJavaScript("", "entityrecord"),
            };

            yield return new object[] {
                    "PageCount",
                    0,
                    Common.MockJavaScript("", "entityrecord"),
            };
        }
    }
}
