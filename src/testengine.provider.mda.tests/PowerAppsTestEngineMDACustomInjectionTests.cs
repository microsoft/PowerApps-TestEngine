// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Jint;
using Microsoft.PowerApps.TestEngine.Providers;
using Newtonsoft.Json;

namespace Microsoft.PowerApps.TestEngine.Tests.PowerApps
{
    /// <summary>
    /// Regression tests for the CWE-94 code injection fix in PowerAppsTestEngineMDACustom.js.
    ///
    /// PowerAppsModelDrivenCanvas.interactWithControl() builds a script string that is handed to
    /// executePublishedAppScript(), which runs it through eval(). The property name comes from the
    /// test plan (.fx.yaml), so it has to be escaped with JSON.stringify() instead of being wrapped
    /// in hand written quotes, otherwise a crafted name can close the object literal and the
    /// argument list and append arbitrary statements to the evaluated script.
    /// </summary>
    public class PowerAppsTestEngineMDACustomInjectionTests
    {
        private const string CustomResourceName = "testengine.provider.mda.PowerAppsTestEngineMDACustom.js";

        /// <summary>
        /// Property name that closes the generated object literal and the enclosing argument list,
        /// appends a statement, then reopens both so the injected script still parses.
        /// </summary>
        private const string InjectionPropertyName = "a\":0});injected = true;({\"b";

        private static string GetCustomScriptSource()
        {
            var assembly = typeof(ModelDrivenApplicationProvider).Assembly;

            using (var stream = assembly.GetManifestResourceStream(CustomResourceName))
            {
                Assert.True(stream != null, $"Embedded resource {CustomResourceName} was not found");

                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// Returns the source of the script building interactWithControl overload.
        /// The class declares interactWithControl twice and the later in-app definition wins at
        /// runtime, so the builder has to be evaluated on its own in order to be exercised.
        /// </summary>
        private static string ExtractScriptBuildingInteractWithControl(string source)
        {
            var start = source.IndexOf("static interactWithControl", StringComparison.Ordinal);
            Assert.True(start >= 0, $"interactWithControl was not found in {CustomResourceName}");

            var open = source.IndexOf('{', start);
            Assert.True(open > start, "The body of interactWithControl could not be located");

            var depth = 0;
            for (var index = open; index < source.Length; index++)
            {
                if (source[index] == '{')
                {
                    depth++;
                }
                else if (source[index] == '}')
                {
                    depth--;

                    if (depth == 0)
                    {
                        return source.Substring(start, index - start + 1);
                    }
                }
            }

            Assert.Fail("The body of interactWithControl is not brace balanced");
            return string.Empty;
        }

        /// <summary>
        /// Hosts the real builder next to a stubbed executePublishedAppScript so the generated
        /// script can be inspected instead of evaluated straight away.
        /// </summary>
        /// <param name="treatValuesAsArray">Selects the array or the scalar branch of the builder.</param>
        private static Engine CreateBuilderEngine(bool treatValuesAsArray)
        {
            var builder = ExtractScriptBuildingInteractWithControl(GetCustomScriptSource());

            var harness = @"
var capturedScript = null;
var injected = false;
function isArray(value) { return " + (treatValuesAsArray ? "true" : "false") + @"; }
class PowerAppsModelDrivenCanvas {
    static executePublishedAppScript(scriptToExecute) {
        capturedScript = scriptToExecute;
        return scriptToExecute;
    }

    " + builder + @"
}";

            var engine = new Engine();
            engine.Execute(harness);
            return engine;
        }

        private static string BuildScript(Engine engine, string propertyName, string valueLiteral)
        {
            var itemPath = JsonConvert.SerializeObject(new { controlName = "TextInput1", propertyName });

            engine.Execute($"PowerAppsModelDrivenCanvas.interactWithControl({itemPath}, {valueLiteral});");

            return engine.Evaluate("capturedScript").AsString();
        }

        [Theory]
        [InlineData(true, "{ Value: 1 }")]
        [InlineData(false, "1")]
        public void InteractWithControlEscapesPropertyNameInGeneratedScript(bool treatValuesAsArray, string valueLiteral)
        {
            // Arrange
            var engine = CreateBuilderEngine(treatValuesAsArray);

            // Act
            var script = BuildScript(engine, InjectionPropertyName, valueLiteral);

            // Assert - the quote that would terminate the key is escaped
            Assert.Contains("a\\\":0});injected", script);

            // Assert - the payload stays inert when the generated script is evaluated
            engine.Execute("eval(capturedScript);");
            Assert.False(engine.Evaluate("injected").AsBoolean());
        }

        [Theory]
        [InlineData(true, "{ Value: 1 }")]
        [InlineData(false, "1")]
        public void InteractWithControlDeliversPropertyNameAsSingleKey(bool treatValuesAsArray, string valueLiteral)
        {
            // Arrange
            var engine = CreateBuilderEngine(treatValuesAsArray);
            var script = BuildScript(engine, InjectionPropertyName, valueLiteral);

            // Swap the builder for a probe so the evaluated script reports what it actually passes on
            engine.Execute("var receivedKeys = null;");
            engine.Execute("PowerAppsModelDrivenCanvas.interactWithControl = function (itemPath, value) { receivedKeys = Object.keys(value); return true; };");

            // Act
            engine.Execute($"eval({JsonConvert.SerializeObject(script)});");

            // Assert - the whole payload arrives as one inert key rather than as extra statements
            Assert.False(engine.Evaluate("injected").AsBoolean());
            Assert.Equal(1, (int)engine.Evaluate("receivedKeys.length").AsNumber());
            Assert.Equal(InjectionPropertyName, engine.Evaluate("receivedKeys[0]").AsString());
        }

        [Theory]
        [InlineData(true, "{ Value: 1 }", "{\"Text\":1}")]
        [InlineData(false, "1", "{\"Text\":1}")]
        public void InteractWithControlIsUnchangedForOrdinaryPropertyNames(bool treatValuesAsArray, string valueLiteral, string expectedValueJson)
        {
            // Arrange
            var engine = CreateBuilderEngine(treatValuesAsArray);

            // Act
            var script = BuildScript(engine, "Text", valueLiteral);

            // Assert - escaping the key must not alter the script produced for a normal plan
            var expected = "PowerAppsModelDrivenCanvas.interactWithControl("
                + "{\"controlName\":\"TextInput1\",\"propertyName\":\"Text\"}, "
                + expectedValueJson
                + ")";

            Assert.Equal(expected, script);
        }

        [Fact]
        public void SetPropertyValueDoesNotExecuteInjectedPropertyName()
        {
            // Arrange
            var engine = new Engine();
            engine.Execute(Common.MockJavaScript(
                "mockPageType = 'custom'; var injected = false",
                "custom",
                interfaceResourceNames: new List<string>
                {
                    "testengine.provider.mda.PowerAppsTestEngineMDA.js",
                    "testengine.provider.mda.PowerAppsTestEngineMDACustom.js"
                }));

            var itemPath = JsonConvert.SerializeObject(new { controlName = "TextInput1", propertyName = InjectionPropertyName });

            // Act
            engine.Execute($"PowerAppsTestEngine.setPropertyValue({itemPath}, {{ Value: 1 }});");

            // Assert
            Assert.False(engine.Evaluate("injected").AsBoolean());
        }

        [Fact]
        public void SourceDoesNotInterpolatePropertyNameUnescaped()
        {
            // Arrange
            var source = GetCustomScriptSource();

            // Assert - guards against reintroducing the hand written quoting of the property name
            Assert.DoesNotContain("{\"${itemPath.propertyName}\"", source);
            Assert.Contains("JSON.stringify(itemPath.propertyName)", source);
        }
    }
}
