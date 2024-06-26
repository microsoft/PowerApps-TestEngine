// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Dynamic;
using System.Reflection;
using System.Text;
using Jint;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Providers;
using Microsoft.PowerApps.TestEngine.TestInfra;
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
        [InlineData("{\"controlName\":\"Label1\",\"index\":null,\"parentControl\":null,\"propertyName\":\"Text\"}", "", "'ABC'", "ABC")]
        [InlineData("{\"controlName\":\"Label1\",\"index\":null,\"parentControl\":null,\"propertyName\":\"Visible\"}", "[{Key:'Visible',Value: false}]", "", false)]
        [InlineData("{\"controlName\":\"Label1\",\"index\":null,\"parentControl\":null,\"propertyName\":\"Visible\"}", "[{Key:'Visible',Value: true}]", "", true)]

        public void GetPropertyValueValues(string itemPathString, string json, object inputValue, object expectedOutput)
        {
            // Arrange
            MockSingleTestInstanceState.Setup(m => m.GetLogger()).Returns(MockLogger.Object);
            var itemPath = JsonConvert.DeserializeObject<ItemPath>(itemPathString);
            var inputPropertyValue = "{PropertyValue:" + inputValue + "}";

            MockTestState.Setup(m => m.GetTimeout()).Returns(30000);
            MockSingleTestInstanceState.Setup(m => m.GetLogger()).Returns(MockLogger.Object);

            if (string.IsNullOrEmpty(json))
            {
                MockTestInfraFunctions.Setup(m => m.RunJavascriptAsync<string>(string.Format(ModelDrivenApplicationProvider.QueryFormField, itemPath.ControlName)))
                    .Returns(Task.FromResult(inputPropertyValue));
            }
            else
            {
                MockTestInfraFunctions.Setup(m => m.RunJavascriptAsync<string>(string.Format(ModelDrivenApplicationProvider.ControlPropertiesQuery, itemPath.ControlName)))
                    .Returns(Task.FromResult(json));
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
                    MockJavaScript("mockValue = 'Hello'", "entityrecord"),
                    "prefix_Name",
                    "Text",
                    "Hello"
            };

            // Disabled by default controlDescriptor property
            yield return new object[] {
                    MockJavaScript("", "entityrecord"),
                    "prefix_Name",
                    "Disabled",
                    false
            };

            // test Disabled in controlDescriptor property
            yield return new object[] {
                    MockJavaScript("mockControlDescriptor.Disabled = true", "entityrecord"),
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

            var fieldData = JsonConvert.DeserializeObject<Dictionary<string,object>>(fields);

            foreach ( var key in fieldData.Keys )
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
                    MockJavaScript("mockValue = 'Hello'", "entityrecord"),
                    "test",
                    "{ Disabled: false, Text: 'Hello', ShowLabel: true, Label: null, Visible: true, IsRequired: false }"
            };

            // Change control name
            yield return new object[] {
                    MockJavaScript("mockControlName = 'test2';mockValue = 'New value'", "entityrecord"),
                    "test2",
                    "{ Disabled: false, Text: 'New value', ShowLabel: true, Label: null, Visible: true, IsRequired: false }"
            };

            // Change Label
            yield return new object[] {
                    MockJavaScript("mockControlDescriptor.Label = 'Item'", "entityrecord"),
                    "test",
                    "{ Label: 'Item' }"
            };

            // Change Visible
            yield return new object[] {
                    MockJavaScript("mockControlDescriptor.Visible = false", "entityrecord"),
                    "test",
                    "{ Visible: false }"
            };

            // Change IsRequired
            yield return new object[] {
                    MockJavaScript("mockControlDescriptor.IsRequired = true", "entityrecord"),
                    "test",
                    "{ IsRequired: true }"
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




        private static string MockJavaScript(string text, string pageType)
        {
            StringBuilder javaScript = new StringBuilder();

            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "testengine.provider.mda.tests.ModelDrivenApplicationMock.js";
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                string mock = reader.ReadToEnd();

                javaScript.Append(mock + ";");
            }

            assembly = typeof(ModelDrivenApplicationProvider).Assembly;
            resourceName = "testengine.provider.mda.PowerAppsTestEngineMDA.js";
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                javaScript.Append(reader.ReadToEnd());

                javaScript.Append(text + ";");

                return javaScript.ToString();
            }
        }
    }
}
