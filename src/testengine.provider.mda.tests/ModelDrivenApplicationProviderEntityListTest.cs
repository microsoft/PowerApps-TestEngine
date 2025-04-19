// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Dynamic;
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
    public class ModelDrivenApplicationProviderEntityListTests
    {
        private Mock<ITestInfraFunctions> MockTestInfraFunctions;
        private Mock<ITestState> MockTestState;
        private Mock<ISingleTestInstanceState> MockSingleTestInstanceState;
        private Mock<ILogger> MockLogger;
        private Mock<IBrowserContext> MockBrowserContext;
        private JSObjectModel JsObjectModel;

        public ModelDrivenApplicationProviderEntityListTests()
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
        public void EntirtListType()
        {
            // Arrange
            var engine = new Engine();
            engine.Execute(Common.MockJavaScript("mockPageType = 'entitylist'", "entitylist"));

            // Act
            var result = engine.Evaluate("PowerAppsTestEngine.pageType()").AsString();

            // Assert
            Assert.Equal("entitylist", result);
        }

        [Theory]
        [InlineData("getCurrentXrmStatus", "typeof getCurrentXrmStatus()")]
        public void ValidateTestState(string scenario, string javaScript, object expected = null)
        {
            // Arrange
            var engine = new Engine();
            engine.Execute(Common.MockJavaScript("mockPageType = 'entitylist'", "entitylist"));
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

            // Assert
            Assert.Equal(expected, outcome);
        }

        // TODO: Complete implementation
        [Theory(Skip = "true")]
        [InlineData("JSON.stringify(PowerAppsTestEngine.buildControlObjectModel())")]
        public void CanBuildControls(string javaScript)
        {
            // Arrange
            var engine = new Engine();
            engine.Execute(Common.MockJavaScript("mockPageType = 'entitylist'", "custom"));

            // Act
            var result = engine.Evaluate(javaScript).AsString();

            // Assert
            Assert.True(result.Length > 0);
        }

        // TODO: Complete implementation
        [Theory(Skip = "true")]
        [MemberData(nameof(GetPropertyValueFromControlData))]
        public void GetPropertyValueFromControl(string javaScript, string controlName, string propertyName, int index, object expectedResult)
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
            var result = provider.GetPropertyValueFromControl<string>(new ItemPath() { ControlName = controlName, PropertyName = propertyName, Index = index });
            dynamic dynamicValue = JsonConvert.DeserializeObject<ExpandoObject>(result);

            // Assert

            Assert.Equal(expectedResult, dynamicValue.PropertyValue);
        }

        public static IEnumerable<object[]> GetPropertyValueFromControlData()
        {
            // Special case text should use the getValue()
            yield return new object[] {
                    Common.MockJavaScript("mockPageType = 'entitylist';mockValue = 'Hello'", "entitylist"),
                    "test",
                    "Text",
                    1,
                    "Hello"
            };

        }

        // TODO: Complete implementation
        [Theory(Skip = "true")]
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
                    Common.MockJavaScript("mockPageType = 'entitylist';mockValue = 'Hello'", "custom"),
                    "test",
                    "{ Text: 'Hello' }"
            };
        }
    }
}
