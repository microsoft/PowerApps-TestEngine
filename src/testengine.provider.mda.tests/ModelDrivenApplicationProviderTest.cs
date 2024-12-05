// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Dynamic;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
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

        //adding this to reset the behavior after each test case for the assembly static function
        private readonly Func<Assembly> originalGetExecutingAssembly = () => typeof(ModelDrivenApplicationProvider).Assembly;

        public ModelDrivenApplicationProviderTests()
        {
            MockTestInfraFunctions = new Mock<ITestInfraFunctions>(MockBehavior.Strict);
            MockTestState = new Mock<ITestState>(MockBehavior.Strict);
            MockTestState.Setup(m => m.GetDomain()).Returns(String.Empty);
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
        public void ValidJavaScript(string javaScript, bool includeMocks, bool includeInterface, List<string> interfaceResourceNames = null)
        {
            var engine = new Jint.Engine();
            if (interfaceResourceNames == null)
            {
                engine.Evaluate(Common.MockJavaScript(javaScript, "custom", includeMocks, includeInterface));
            }
            else
            {
                engine.Evaluate(Common.MockJavaScript(javaScript, "custom", includeMocks, includeInterface, interfaceResourceNames));
            }
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

            yield return new object[] { "PowerAppsModelDrivenCanvas.getAppMagic()", true, true, new List<string> { "testengine.provider.mda.PowerAppsTestEngineMDACustom.js" } };

            yield return new object[] { "PowerAppsModelDrivenCanvas.parseControl('TextInput1', mockCanvasControl)", true, true, new List<string> { "testengine.provider.mda.PowerAppsTestEngineMDACustom.js" } };

            yield return new object[] { "PowerAppsModelDrivenCanvas.getAppMagic().Controls?.GlobalContextManager?.bindingContext", true, true, new List<string> { "testengine.provider.mda.PowerAppsTestEngineMDACustom.js" } };

            yield return new object[] { "PowerAppsModelDrivenCanvas.getAppMagic().Controls.GlobalContextManager.bindingContext.componentBindingContexts.lookup('TextInput1')", true, true, new List<string> { "testengine.provider.mda.PowerAppsTestEngineMDACustom.js" } };

            yield return new object[] { "PowerAppsModelDrivenCanvas.getBindingContext({controlName: 'TextInput1', propertyName: 'Text'})", true, true, new List<string> { "testengine.provider.mda.PowerAppsTestEngineMDACustom.js" } };

            yield return new object[] { "PowerAppsModelDrivenCanvas.getPropertyValueFromControl({controlName: 'TextInput1', propertyName: 'Text'})", true, true, new List<string> { "testengine.provider.mda.PowerAppsTestEngineMDACustom.js" } };
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
        public async Task IsIdle(string javaScript, bool expectedIdle)
        {
            var engine = new Jint.Engine();
            engine.Execute(javaScript);

            MockSingleTestInstanceState.Setup(m => m.GetLogger()).Returns(MockLogger.Object);
            MockTestInfraFunctions.Setup(m => m.RunJavascriptAsync<string>(It.IsAny<string>())).Returns(
                async (string query) => engine.Evaluate(query).AsString()
            );

            // Act
            var provider = new ModelDrivenApplicationProvider(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            var result = await provider.CheckIsIdleAsync();

            // Assert
            Assert.Equal(expectedIdle, result);
        }

        [Theory]
        [InlineData("", "", false)]
        [InlineData("class UCWorkBlockTracker {}", "", false)]
        [InlineData("class UCWorkBlockTracker { static isAppIdle() { return false; } }", "", false)]
        [InlineData("class UCWorkBlockTracker { static isAppIdle() { return true; } }", "", false)]
        [InlineData("class UCWorkBlockTracker { static isAppIdle() { return true; } }", "class PowerAppsTestEngine { }", true)]
        public async Task IsReady(string javaScript, string extraCode, bool expectedIdle)
        {
            var engine = new Jint.Engine();
            engine.Execute(javaScript);

            if (!string.IsNullOrEmpty(extraCode))
            {
                engine.Execute(extraCode);
            }

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
            var result = await provider.TestEngineReady();

            // Assert
            Assert.Equal(expectedIdle, result);
        }

        [Theory]
        [MemberData(nameof(GetPropertyValueFromControlData))]
        public void GetPropertyValueFromControl(string javaScript, string controlName, string propertyName, object expectedResult)
        {
            var engine = new Jint.Engine();
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
                    Common.MockJavaScript("mockValue = 'Hello'", "entityrecord", interfaceResourceNames: new List<string> { "testengine.provider.mda.PowerAppsTestEngineMDA.js", "testengine.provider.mda.PowerAppsTestEngineMDAEntityRecord.js"}),
                    "prefix_Name",
                    "Text",
                    "Hello"
            };

            // Disabled by default controlDescriptor property
            yield return new object[] {
                    Common.MockJavaScript("", "entityrecord", interfaceResourceNames: new List<string> { "testengine.provider.mda.PowerAppsTestEngineMDA.js", "testengine.provider.mda.PowerAppsTestEngineMDAEntityRecord.js"}),
                    "prefix_Name",
                    "Disabled",
                    false
            };

            // test Disabled in controlDescriptor property
            yield return new object[] {
                    Common.MockJavaScript("mockControlDescriptor.Disabled = true", "entityrecord", interfaceResourceNames: new List<string> { "testengine.provider.mda.PowerAppsTestEngineMDA.js", "testengine.provider.mda.PowerAppsTestEngineMDAEntityRecord.js"}),
                    "prefix_Name",
                    "Disabled",
                    true
            };
        }

        [Theory]
        [MemberData(nameof(GetControlData))]
        public async Task BuildEntityRecordControls(string javaScript, string controlName, string fields)
        {
            var engine = new Jint.Engine();
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
                    Common.MockJavaScript("mockValue = 'Hello'", "entityrecord", interfaceResourceNames: new List<string> { "testengine.provider.mda.PowerAppsTestEngineMDA.js", "testengine.provider.mda.PowerAppsTestEngineMDAEntityRecord.js"}),
                    "test",
                    "{ Disabled: false, Text: 'Hello', ShowLabel: true, Label: 'Text Input', Visible: true, IsRequired: false }"
            };

            // Change control name
            yield return new object[] {
                    Common.MockJavaScript("mockControlName = 'test2';mockValue = 'New value'", "entityrecord", interfaceResourceNames: new List<string> { "testengine.provider.mda.PowerAppsTestEngineMDA.js", "testengine.provider.mda.PowerAppsTestEngineMDAEntityRecord.js"}),
                    "test2",
                    "{ Disabled: false, Text: 'New value', ShowLabel: true, Label: 'Text Input', Visible: true, IsRequired: false }"
            };

            // Change Label
            yield return new object[] {
                    Common.MockJavaScript("mockControlDescriptor.Label = 'Item'", "entityrecord", interfaceResourceNames: new List<string> { "testengine.provider.mda.PowerAppsTestEngineMDA.js", "testengine.provider.mda.PowerAppsTestEngineMDAEntityRecord.js"}),
                    "test",
                    "{ Label: 'Item' }"
            };

            // Change Visible
            yield return new object[] {
                    Common.MockJavaScript("mockControlDescriptor.Visible = false", "entityrecord", interfaceResourceNames: new List<string> { "testengine.provider.mda.PowerAppsTestEngineMDA.js", "testengine.provider.mda.PowerAppsTestEngineMDAEntityRecord.js"}),
                    "test",
                    "{ Visible: false }"
            };

            // Change IsRequired
            yield return new object[] {
                    Common.MockJavaScript("mockControlDescriptor.IsRequired = true", "entityrecord", interfaceResourceNames: new List<string> { "testengine.provider.mda.PowerAppsTestEngineMDA.js", "testengine.provider.mda.PowerAppsTestEngineMDAEntityRecord.js"}),
                    "test",
                    "{ IsRequired: true }"
            };
        }

        [Theory]
        [MemberData(nameof(GetSetValueData))]
        public async Task SetValue(string javaScript, string controlName, string propertyName, string targetProperty, object value)
        {
            var engine = new Jint.Engine();
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
                    Common.MockJavaScript("mockValue = 'Hello'", "entityrecord", interfaceResourceNames: new List<string> { "testengine.provider.mda.PowerAppsTestEngineMDA.js", "testengine.provider.mda.PowerAppsTestEngineMDAEntityRecord.js"}),
                    "test",
                    "Text",
                    "value",
                    "Hello"
            };

            // Visible Values
            yield return new object[] {
                    Common.MockJavaScript("mockValue = 'Hello'", "entityrecord", interfaceResourceNames: new List<string> { "testengine.provider.mda.PowerAppsTestEngineMDA.js", "testengine.provider.mda.PowerAppsTestEngineMDAEntityRecord.js"}),
                    "test",
                    "Visible",
                    "visible",
                    true
            };

            // Disabled Values
            yield return new object[] {
                    Common.MockJavaScript("", "entityrecord", interfaceResourceNames: new List<string> { "testengine.provider.mda.PowerAppsTestEngineMDA.js", "testengine.provider.mda.PowerAppsTestEngineMDAEntityRecord.js"}),
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
            MockTestInfraFunctions.Setup(f => f.Page.RouteAsync(It.IsAny<string>(), It.IsAny<Func<IRoute, Task>>(), null)).Returns(Task.CompletedTask);
            var mockElementHandle = new Mock<IElementHandle>();
            MockTestInfraFunctions.Setup(f => f.Page.AddScriptTagAsync(It.IsAny<PageAddScriptTagOptions>())).ReturnsAsync(mockElementHandle.Object);

            MockSingleTestInstanceState.Setup(m => m.GetLogger()).Returns(MockLogger.Object);

            MockTestState.Setup(m => m.GetTimeout()).Returns(1000);

            // Act
            var provider = new ModelDrivenApplicationProvider(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            await provider.CheckProviderAsync();

            // Assert

        }

        [Fact]
        public async Task CheckProviderAsync_ShouldCloseBlankPage_IfExists()
        {
            // Arrange
            var MockTestInfraFunctions1 = new Mock<ITestInfraFunctions>();
            var blankPage = new Mock<IPage>();
            blankPage.Setup(p => p.Url).Returns("about:blank");
            var MockPage = new Mock<IPage>(MockBehavior.Strict);
            MockPage.Setup(p => p.Url).Returns("https://example.com");
            MockBrowserContext.Setup(c => c.Pages).Returns(new[] { blankPage.Object, MockPage.Object });
            MockSingleTestInstanceState.Setup(m => m.GetLogger()).Returns(MockLogger.Object);
            MockTestInfraFunctions1.Setup(m => m.GetContext()).Returns(MockBrowserContext.Object);
            MockTestInfraFunctions1.Setup(m => m.Page).Returns(MockPage.Object);
            MockTestInfraFunctions1.Setup(m => m.RunJavascriptAsync<bool>(It.IsAny<string>())).Returns(Task.FromResult(false));
            MockTestState.Setup(m => m.GetTimeout()).Returns(1000);
            MockTestInfraFunctions1.Setup(f => f.Page.RouteAsync(It.IsAny<string>(), It.IsAny<Func<IRoute, Task>>(), null)).Returns(Task.CompletedTask);
            var mockElementHandle = new Mock<IElementHandle>();
            MockTestInfraFunctions1.Setup(f => f.Page.AddScriptTagAsync(It.IsAny<PageAddScriptTagOptions>())).ReturnsAsync(mockElementHandle.Object);
            var provider = new ModelDrivenApplicationProvider(MockTestInfraFunctions1.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            // Act
            await provider.CheckProviderAsync();

            // Assert
            blankPage.Verify(p => p.CloseAsync(null), Times.Once);
            MockTestInfraFunctions1.VerifySet(f => f.Page = MockPage.Object);
        }

        [Fact]
        public async Task CheckProviderAsync_ShouldNotClosePage_IfNoBlankPageExists()
        {
            // Arrange
            var MockTestInfraFunctions1 = new Mock<ITestInfraFunctions>();

            var otherPage = new Mock<IPage>();
            otherPage.Setup(p => p.Url).Returns("http://example.com");
            MockSingleTestInstanceState.Setup(m => m.GetLogger()).Returns(MockLogger.Object);
            MockTestInfraFunctions1.Setup(m => m.GetContext()).Returns(MockBrowserContext.Object);
            MockTestInfraFunctions1.Setup(m => m.RunJavascriptAsync<bool>(It.IsAny<string>())).Returns(Task.FromResult(false));
            MockTestState.Setup(m => m.GetTimeout()).Returns(1000);
            MockTestInfraFunctions1.Setup(f => f.Page.RouteAsync(It.IsAny<string>(), It.IsAny<Func<IRoute, Task>>(), null)).Returns(Task.CompletedTask);

            MockBrowserContext.Setup(c => c.Pages).Returns(new[] { otherPage.Object });
            var provider = new ModelDrivenApplicationProvider(MockTestInfraFunctions1.Object, MockSingleTestInstanceState.Object, MockTestState.Object);

            // Act
            await provider.CheckProviderAsync();

            // Assert
            otherPage.Verify(p => p.CloseAsync(null), Times.Never);
            MockTestInfraFunctions1.VerifySet(f => f.Page = otherPage.Object);
        }


        [Fact]
        public async Task CheckProviderAsync_ShouldLogCorrectly()
        {
            // Arrange
            MockTestInfraFunctions.Setup(m => m.GetContext()).Returns(MockBrowserContext.Object);
            MockSingleTestInstanceState.Setup(m => m.GetLogger()).Returns(MockLogger.Object);
            var mockElementHandle = new Mock<IElementHandle>();
            MockTestInfraFunctions.Setup(f => f.Page.AddScriptTagAsync(It.IsAny<PageAddScriptTagOptions>())).ReturnsAsync(mockElementHandle.Object);
            MockTestState.Setup(m => m.GetTimeout()).Returns(1000);
            MockBrowserContext.Setup(m => m.Pages).Returns(new List<IPage>());
            MockTestInfraFunctions.Setup(m => m.RunJavascriptAsync<bool>(It.IsAny<string>())).Returns(Task.FromResult(false));

            MockTestInfraFunctions.Setup(f => f.Page.RouteAsync(It.IsAny<string>(), It.IsAny<Func<IRoute, Task>>(), null)).Returns(Task.CompletedTask);
            MockTestInfraFunctions.Setup(m => m.AddScriptContentAsync(It.IsAny<string>())).Returns(() => Task.CompletedTask);
            var provider = new ModelDrivenApplicationProvider(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);

            // Act
            await provider.CheckProviderAsync();

            // Assert
            MockLogger.Verify(l => l.Log(LogLevel.Debug, It.IsAny<EventId>(), It.Is<It.IsAnyType>((o, t) => o.ToString() == "Start to load PowerAppsTestEngine"), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
            MockLogger.Verify(l => l.Log(LogLevel.Debug, It.IsAny<EventId>(), It.Is<It.IsAnyType>((o, t) => o.ToString() == "Finish loading PowerAppsTestEngine."), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }


        [Theory]
        [MemberData(nameof(DebugInfoProperties))]
        public async Task DebugInfo(string propertyName, object expectedValue, string javaScript)
        {
            var engine = new Jint.Engine();
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
            var result = (IDictionary<string, object>)(await provider.GetDebugInfo());

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

        [Fact]
        public async Task EmbedMDAJSScripts_ResourceStreamIsNull_ThrowsException()
        {
            // Arrange
            MockSingleTestInstanceState.Setup(m => m.GetLogger()).Returns(MockLogger.Object);
            MockTestState.Setup(m => m.GetTimeout()).Returns(1000);
            string resourceName = "invalidResource";
            var _assemblyMock = new Mock<Assembly>();
            _assemblyMock.Setup(a => a.GetManifestResourceStream(resourceName))
                .Returns((Stream)null); // Simulate invalid resource name

            var provider = new ModelDrivenApplicationProvider(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            provider.GetExecutingAssembly = () => _assemblyMock.Object;

            // Act & Assert

            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await provider.EmbedMDAJSScripts(resourceName, "testScript.js");
            });
        }

        [Fact]
        public async Task EmbedMDAJSScripts_StreamUnreadable_ThrowsException()
        {
            // Arrange
            MockSingleTestInstanceState.Setup(m => m.GetLogger()).Returns(MockLogger.Object);
            MockTestState.Setup(m => m.GetTimeout()).Returns(1000);
            var resourceName = "testResource";
            var embeddedScriptName = "testScript.js";

            // Mock a stream that is unreadable
            var streamMock = new Mock<Stream>();
            streamMock.Setup(s => s.CanRead).Returns(false); // Simulate an unreadable stream

            var assemblyMock = new Mock<Assembly>();
            assemblyMock.Setup(a => a.GetManifestResourceStream(resourceName))
                .Returns(streamMock.Object); // Return the unreadable stream

            var provider = new ModelDrivenApplicationProvider(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            provider.GetExecutingAssembly = () => assemblyMock.Object;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await provider.EmbedMDAJSScripts(resourceName, embeddedScriptName);
            });
        }

        [Fact]
        public async Task EmbedMDAJSScripts_RouteAsyncFails_ThrowsException()
        {
            // Arrange
            MockSingleTestInstanceState.Setup(m => m.GetLogger()).Returns(MockLogger.Object);
            MockTestState.Setup(m => m.GetTimeout()).Returns(1000);
            var resourceName = "testResource";
            var embeddedScriptName = "testScript.js";
            var streamMock = new Mock<Stream>();
            streamMock.Setup(s => s.CanRead).Returns(true);

            var assemblyMock = new Mock<Assembly>();
            assemblyMock.Setup(a => a.GetManifestResourceStream(resourceName))
                .Returns(streamMock.Object);

            var pageMock = new Mock<IPage>();

            // Simulate failure during RouteAsync
            pageMock.Setup(p => p.RouteAsync(It.IsAny<string>(), It.IsAny<Func<IRoute, Task>>(), null))
                .ThrowsAsync(new InvalidOperationException("RouteAsync failed"));

            MockTestInfraFunctions.Setup(f => f.Page).Returns(pageMock.Object);

            var provider = new ModelDrivenApplicationProvider(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            provider.GetExecutingAssembly = () => assemblyMock.Object;

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await provider.EmbedMDAJSScripts(resourceName, embeddedScriptName);
            });
        }

        [Fact]
        public async Task EmbedMDAJSScripts_AddScriptTagFails_ThrowsException()
        {
            // Arrange
            MockSingleTestInstanceState.Setup(m => m.GetLogger()).Returns(MockLogger.Object);
            MockTestState.Setup(m => m.GetTimeout()).Returns(1000);
            var resourceName = "testResource";
            var embeddedScriptName = "testScript.js";
            var streamMock = new Mock<Stream>();
            streamMock.Setup(s => s.CanRead).Returns(true);

            var assemblyMock = new Mock<Assembly>();
            assemblyMock.Setup(a => a.GetManifestResourceStream(resourceName))
                .Returns(streamMock.Object);

            var pageMock = new Mock<IPage>();

            // Simulate failure during AddScriptTagAsync
            pageMock.Setup(p => p.AddScriptTagAsync(It.IsAny<PageAddScriptTagOptions>()))
                .ThrowsAsync(new InvalidOperationException("AddScriptTagAsync failed"));

            MockTestInfraFunctions.Setup(f => f.Page).Returns(pageMock.Object);

            var provider = new ModelDrivenApplicationProvider(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            provider.GetExecutingAssembly = () => assemblyMock.Object;

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await provider.EmbedMDAJSScripts(resourceName, embeddedScriptName);
            });
        }

        [Fact]
        public async Task EmbedMDAJSScripts_EmbedsScriptSuccessfully()
        {
            // Arrange
            MockSingleTestInstanceState.Setup(m => m.GetLogger()).Returns(MockLogger.Object);
            MockTestState.Setup(m => m.GetTimeout()).Returns(1000);

            var resourceName = "testResource";
            var embeddedScriptName = "testScript.js";
            string scriptContent = "console.log('test');"; // The content of the embedded JS file

            // Create a mock memory stream to simulate reading the embedded resource
            var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(scriptContent));

            // Mock the assembly to return the memory stream for the resource
            var assemblyMock = new Mock<Assembly>();
            assemblyMock.Setup(a => a.GetManifestResourceStream(resourceName))
                        .Returns(memoryStream);

            // Calculate the expected script hash
            string expectedScriptHash = "sha256-" + Convert.ToBase64String(SHA256.HashData(memoryStream.ToArray()));
            string expectedScriptUrl = $"/{embeddedScriptName}?hash={expectedScriptHash}";

            // Mocking the page and route fulfillment
            var mockPage = new Mock<IPage>();
            MockTestInfraFunctions.Setup(f => f.Page).Returns(mockPage.Object);
            mockPage.Setup(p => p.RouteAsync(It.IsAny<string>(), It.IsAny<Func<IRoute, Task>>(), null)).Returns(Task.CompletedTask);
            var mockElementHandle = new Mock<IElementHandle>();
            mockPage.Setup(p => p.AddScriptTagAsync(It.IsAny<PageAddScriptTagOptions>())).ReturnsAsync(mockElementHandle.Object);

            // Create the ModelDrivenApplicationProvider with mocked dependencies
            var provider = new ModelDrivenApplicationProvider(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            provider.GetExecutingAssembly = () => assemblyMock.Object;

            // Act
            await provider.EmbedMDAJSScripts(resourceName, embeddedScriptName);

            // Assert
            // Verify that the RouteAsync method was called with the correct script URL containing the hash
            mockPage.Verify(p => p.RouteAsync(It.Is<string>(url => url.Contains(expectedScriptHash)), It.IsAny<Func<IRoute, Task>>(), null), Times.Once);

            // Verify that AddScriptTagAsync was called with the correct PageAddScriptTagOptions
            mockPage.Verify(p => p.AddScriptTagAsync(It.Is<PageAddScriptTagOptions>(opt => opt.Url.Contains(expectedScriptHash))), Times.Once);
        }
    }
}
