// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Reflection;
using Jint;
using Newtonsoft.Json;

namespace Microsoft.PowerApps.TestEngine.Tests.PowerApps
{
    /// <summary>
    /// Regression tests for the CWE-94 code injection fix in CanvasAppSdk.js.
    ///
    /// interactWithControl() builds a script string that executePublishedAppScript() marshals into
    /// the published app and evaluates there. The property name comes from the test plan
    /// (.fx.yaml), so it has to be escaped with JSON.stringify() instead of being wrapped in hand
    /// written quotes, otherwise a crafted name can close the object literal and the argument list
    /// and append arbitrary statements to the script that crosses into the app.
    /// </summary>
    public class CanvasAppSdkInjectionTests
    {
        private const string SdkResourceName = "testengine.provider.canvas.tests.CanvasAppSdk.js";

        /// <summary>
        /// Property name that closes the generated object literal and the enclosing argument list,
        /// appends a statement, then reopens both so the injected script still parses.
        /// </summary>
        private const string InjectionPropertyName = "a\":0});injected = true;({\"b";

        private static string GetCanvasSdkSource()
        {
            var assembly = Assembly.GetExecutingAssembly();

            using (var stream = assembly.GetManifestResourceStream(SdkResourceName))
            {
                Assert.True(stream != null, $"Embedded resource {SdkResourceName} was not found");

                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// Loads the shipped SDK and replaces executePublishedAppScript so the script that would be
        /// sent to the published app can be inspected instead of dispatched.
        /// </summary>
        /// <param name="forceScalarBranch">
        /// Object.values() always returns an array, so the scalar branch of interactWithControl is
        /// only reachable when isArray is stubbed out.
        /// </param>
        private static Engine CreateSdkEngine(bool forceScalarBranch)
        {
            var engine = new Engine();

            // debugInfo is evaluated while the SDK loads and reads the published app telemetry
            engine.Execute("var Core = { Telemetry: {} };");
            engine.Execute("var capturedScript = null; var injected = false; var probedKeys = null;");

            engine.Execute(GetCanvasSdkSource());

            engine.Execute("executePublishedAppScript = function (scriptToExecute) { capturedScript = scriptToExecute; return scriptToExecute; };");

            if (forceScalarBranch)
            {
                engine.Execute("isArray = function () { return false; };");
            }

            return engine;
        }

        private static string ItemPathJson(string propertyName)
        {
            return JsonConvert.SerializeObject(new { controlName = "Label1", propertyName });
        }

        private static string BuildScript(Engine engine, string propertyName, string valueLiteral)
        {
            engine.Execute($"interactWithControl({ItemPathJson(propertyName)}, {valueLiteral});");

            return engine.Evaluate("capturedScript").AsString();
        }

        /// <summary>
        /// Stands in for the published app side implementation so the captured script can be run.
        /// </summary>
        private static void InstallProbe(Engine engine)
        {
            engine.Execute("interactWithControl = function (itemPath, value) { probedKeys = Object.keys(value); return true; };");
        }

        [Theory]
        [InlineData(true, "{ Value: 1 }")]
        [InlineData(false, "1")]
        public void InteractWithControlEscapesPropertyNameInGeneratedScript(bool useArrayBranch, string valueLiteral)
        {
            // Arrange
            var engine = CreateSdkEngine(forceScalarBranch: !useArrayBranch);

            // Act
            var script = BuildScript(engine, InjectionPropertyName, valueLiteral);

            // Assert - the quote that would terminate the key is escaped
            Assert.Contains("a\\\":0});injected", script);

            // Assert - the payload stays inert when the script reaches the published app
            InstallProbe(engine);
            engine.Execute($"eval({JsonConvert.SerializeObject(script)});");
            Assert.False(engine.Evaluate("injected").AsBoolean());
        }

        [Theory]
        [InlineData(true, "{ Value: 1 }")]
        [InlineData(false, "1")]
        public void InteractWithControlDeliversPropertyNameAsSingleKey(bool useArrayBranch, string valueLiteral)
        {
            // Arrange
            var engine = CreateSdkEngine(forceScalarBranch: !useArrayBranch);
            var script = BuildScript(engine, InjectionPropertyName, valueLiteral);

            InstallProbe(engine);

            // Act
            engine.Execute($"eval({JsonConvert.SerializeObject(script)});");

            // Assert - the whole payload arrives as one inert key rather than as extra statements
            Assert.False(engine.Evaluate("injected").AsBoolean());
            Assert.Equal(1, (int)engine.Evaluate("probedKeys.length").AsNumber());
            Assert.Equal(InjectionPropertyName, engine.Evaluate("probedKeys[0]").AsString());
        }

        [Theory]
        [InlineData(true, "{ Value: 1 }")]
        [InlineData(false, "1")]
        public void InteractWithControlIsUnchangedForOrdinaryPropertyNames(bool useArrayBranch, string valueLiteral)
        {
            // Arrange
            var engine = CreateSdkEngine(forceScalarBranch: !useArrayBranch);

            // Act
            var script = BuildScript(engine, "Text", valueLiteral);

            // Assert - escaping the key must not alter the script produced for a normal plan
            var expected = "interactWithControl({\"controlName\":\"Label1\",\"propertyName\":\"Text\"}, {\"Text\":1})";

            Assert.Equal(expected, script);
        }

        [Fact]
        public void SetPropertyValueDoesNotExecuteInjectedPropertyName()
        {
            // Arrange - setPropertyValue routes object values through interactWithControl
            var engine = CreateSdkEngine(forceScalarBranch: false);

            // Act
            engine.Execute($"PowerAppsTestEngine.setPropertyValue({ItemPathJson(InjectionPropertyName)}, {{ Value: 1 }});");
            var script = engine.Evaluate("capturedScript").AsString();

            InstallProbe(engine);
            engine.Execute($"eval({JsonConvert.SerializeObject(script)});");

            // Assert
            Assert.False(engine.Evaluate("injected").AsBoolean());
            Assert.Equal(InjectionPropertyName, engine.Evaluate("probedKeys[0]").AsString());
        }

        [Fact]
        public void SourceDoesNotInterpolatePropertyNameUnescaped()
        {
            // Arrange
            var source = GetCanvasSdkSource();

            // Assert - guards against reintroducing the hand written quoting of the property name
            Assert.DoesNotContain("{\"${itemPath.propertyName}\"", source);
            Assert.Contains("JSON.stringify(itemPath.propertyName)", source);
        }
    }
}
