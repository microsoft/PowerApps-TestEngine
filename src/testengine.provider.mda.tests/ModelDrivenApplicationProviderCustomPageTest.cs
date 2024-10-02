// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Dynamic;
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
    public class ModelDrivenApplicationProviderCustomPageTests
    {
        private Mock<ITestInfraFunctions> MockTestInfraFunctions;
        private Mock<ITestState> MockTestState;
        private Mock<ISingleTestInstanceState> MockSingleTestInstanceState;
        private Mock<ILogger> MockLogger;
        private Mock<IBrowserContext> MockBrowserContext;
        private JSObjectModel JsObjectModel;

        public ModelDrivenApplicationProviderCustomPageTests()
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

        [Fact]
        public void CustomPageType()
        {
            // Arrange
            var engine = new Engine();
            engine.Execute(Common.MockJavaScript("mockPageType = 'custom'", "custom"));

            // Act
            var result = engine.Evaluate("PowerAppsTestEngine.pageType()").AsString();

            // Assert
            Assert.Equal("custom", result);
        }

        [Theory]
        [InlineData("getAppMagic", "typeof PowerAppsModelDrivenCanvas.getAppMagic")]
        [InlineData("mockAppMagic", "typeof mockAppMagic")]
        [InlineData("GlobalContextManager", "typeof mockAppMagic.Controls.GlobalContextManager")]
        [InlineData("bindingContext", "typeof mockAppMagic.Controls.GlobalContextManager.bindingContext")]
        [InlineData("controlContexts", "typeof mockAppMagic.Controls.GlobalContextManager.bindingContext.controlContexts")]
        [InlineData("componentBindingContexts", "typeof mockAppMagic.Controls.GlobalContextManager.bindingContext.componentBindingContexts")]
        [InlineData("lookup", "typeof mockAppMagic.Controls.GlobalContextManager.bindingContext.componentBindingContexts.lookup")]
        [InlineData("getAppMagic", "typeof (PowerAppsModelDrivenCanvas.getAppMagic())")]
        [InlineData("isArray", "PowerAppsModelDrivenCanvas.isArray([])", true)]
        [InlineData("AppDependencyHandler", "typeof AppDependencyHandler")]
        [InlineData("getBindingContext", "typeof PowerAppsModelDrivenCanvas.getBindingContext({controlName:'TextInput1', propertyName:'Text'}) !== 'undefined'", true)]
        [InlineData("getBindingContext.controlContexts", "PowerAppsModelDrivenCanvas.getBindingContext({controlName:'TextInput1', propertyName:'Text'}).controlContexts !== null", true)]
        [InlineData("getValue", "mockValue = 'Hello';PowerAppsModelDrivenCanvas.getBindingContext({controlName:'TextInput1', propertyName:'Text'}).controlContexts['TextInput1'].modelProperties['Text'].getValue()", "Hello")]
        [InlineData("getPropertyValueFromControl", "mockValue = 'Hello';JSON.stringify(PowerAppsModelDrivenCanvas.getPropertyValueFromControl({controlName:'TextInput1', propertyName:'Text'}))", "{\"propertyValue\":\"Hello\"}")]
        [InlineData("parseControl InputText1", "var control = PowerAppsModelDrivenCanvas.getAppMagic().Controls.GlobalContextManager.bindingContext.controlContexts[\"TextInput1\"];PowerAppsModelDrivenCanvas.parseControl('TextInput1', control).length", 1)]
        [InlineData("AppMagic controlContexts", "Object.keys(PowerAppsModelDrivenCanvas.getAppMagic().Controls.GlobalContextManager.bindingContext.controlContexts).length", 1)]
        [InlineData("parseControl", "PowerAppsModelDrivenCanvas.parseControl('TextInput1', PowerAppsModelDrivenCanvas.getAppMagic().Controls.GlobalContextManager.bindingContext.controlContexts['TextInput1']).length", 1)]
        [InlineData("parseControl name", "PowerAppsModelDrivenCanvas.parseControl('TextInput1', PowerAppsModelDrivenCanvas.getAppMagic().Controls.GlobalContextManager.bindingContext.controlContexts['TextInput1'])[0].name", "TextInput1")]
        [InlineData("parseControl properties", "PowerAppsModelDrivenCanvas.parseControl('TextInput1', PowerAppsModelDrivenCanvas.getAppMagic().Controls.GlobalContextManager.bindingContext.controlContexts['TextInput1'])[0].properties.length", 3)]
        [InlineData("getControlProperties", "mockPageType='custom';PowerAppsModelDrivenCanvas.isArray(JSON.parse(PowerAppsTestEngine.getControlProperties({controlName:'TextInput1', propertyName:'Text'})))", true)]
        public void ValidateTestState(string scenario, string javaScript, object expected = null)
        {
            // Arrange
            var engine = new Engine();
            engine.Execute(Common.MockJavaScript("mockPageType = 'custom'", "custom", interfaceResourceNames: new List<string> { "testengine.provider.mda.PowerAppsTestEngineMDA.js", "testengine.provider.mda.PowerAppsTestEngineMDACustom.js" }));
            expected = expected == null ? "function" : expected;

            scenario += "";

            // Act
            object outcome = null;
            var result = engine.Evaluate(javaScript);

            if (result.IsBoolean())
            {
                outcome = result.AsBoolean();
            }

            if (result.IsString())
            {
                outcome = result.AsString();
            }

            if (result.IsNumber())
            {
                outcome = (int)result.AsNumber();
            }

            // Assert
            Assert.Equal(expected, outcome);
        }

        [Theory]
        [InlineData("JSON.stringify(PowerAppsTestEngine.buildControlObjectModel())")]
        public void CanBuildControls(string javaScript)
        {
            // Arrange
            var engine = new Engine();
            engine.Execute(Common.MockJavaScript("mockPageType = 'custom'", "custom", interfaceResourceNames: new List<string> { "testengine.provider.mda.PowerAppsTestEngineMDA.js", "testengine.provider.mda.PowerAppsTestEngineMDACustom.js" }));

            // Act
            var result = engine.Evaluate(javaScript).AsString();

            // Assert
            Assert.True(result.Length > 0);
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
                 async (string query) =>
                 {
                     var result = engine.Evaluate(query);
                     if (result.IsBoolean())
                     {
                         return (object)result.AsBoolean();
                     }
                     if (result.IsString())
                     {
                         return (object)result.AsString();
                     }
                     return (object)"undefined";
                 }
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
                    Common.MockJavaScript("mockPageType = 'custom';mockValue = 'Hello'", "custom", interfaceResourceNames: new List<string> { "testengine.provider.mda.PowerAppsTestEngineMDA.js", "testengine.provider.mda.PowerAppsTestEngineMDACustom.js"}),
                    "TextInput1",
                    "Text",
                    "Hello"
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
                Assert.Equal(fieldData[key], fieldData[key]);
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
                    Common.MockJavaScript("mockPageType = 'custom';mockValue = 'Hello'", "custom", interfaceResourceNames: new List<string> { "testengine.provider.mda.PowerAppsTestEngineMDA.js", "testengine.provider.mda.PowerAppsTestEngineMDACustom.js"}),
                    "TextInput1",
                    "{ Text: 'Hello' }"
            };
        }

        [Theory]
        [MemberData(nameof(GetSetValueData))]
        public async Task SetValue(string javaScript, string controlName, string propertyName, object value)
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

        }

        /// <summary>
        /// Test data for <see cref="SetValue"/>
        /// </summary>
        /// <returns>MemberData items</returns>
        public static IEnumerable<object[]> GetSetValueData()
        {
            // Default Values
            yield return new object[] {
                   Common.MockJavaScript("mockPageType = 'custom';mockValue = 'Hello'", "custom"),
                    "TextInput1",
                    "Text",
                    "Hello"
            };
        }
    }
}
