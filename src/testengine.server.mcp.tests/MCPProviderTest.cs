// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Castle.Core.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.PowerFx;
using Microsoft.PowerApps.TestEngine.Providers;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.Tests.Helpers;
using Microsoft.PowerFx;
using Moq;


namespace testengine.server.mcp.tests
{
    public class MCPProviderTest
    {
        private Mock<IFileSystem> MockFileSystem;

        private MCPProvider _provider = null;

        public MCPProviderTest()
        {

            MockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);

            // Use StubOrganizationService for testing
            _provider = new MCPProvider()
            {
                GetOrganizationService = () => new StubOrganizationService(),
                FileSystem = MockFileSystem.Object
            };
        }

        [Theory]
        [InlineData("If(1=1,2,3)", true)] // Valid Power Fx expression
        [InlineData("InvalidExpression", false)] // Invalid Power Fx expression
        [InlineData(null, true)] // Null input
        [InlineData("", true)] // Empty input
        [InlineData("Assert(DoesNotExist)", false)] // Invalid function or symbol
        public void ValidatePowerFx_ParameterizedTests(string? expression, bool expectedIsValid)
        {
            // Arrange
            _provider.Engine = new RecalcEngine();

            // Act
            var result = _provider.ValidatePowerFx(expression);

            // Assert
            if (expectedIsValid)
            {
                Assert.Contains("isValid: true", result);
                Assert.Contains("errors: []", result);
            }
            else
            {
                Assert.Contains("isValid: false", result);
                Assert.DoesNotContain("errors: []", result);
            }
        }

        [Theory]
        [InlineData("", "", "", "If(1=1,2,3)", true)] // Valid Power Fx expression
        [InlineData("", "", "Total(a:Number, b:Number): Number = a + b", "Total(2,3)", true)]
        [InlineData("NumberValue", "{a:Number,b:Number}", "Total(x:NumberValue): Number = x.a + x.b", "Total({a:2,b:3})", true)] // Record Value
        [InlineData("NumberValueCollection", "[{a:Number,b:Number}]", "Total(x: NumberValueCollection): Number = 1", "Total([{a:2,b:3}])", true)] // Table Value
        public void UserDefined(string userDefinedTypeName, string userDefinedType, string userDefinedFunction, string? expression, bool expectedIsValid)
        {
            // Arrange
            var config = new PowerFxConfig();
            var settings = new TestSettings();

            _provider.MCPTestSettings = settings;

            if (!string.IsNullOrWhiteSpace(userDefinedTypeName))
            {
                settings.PowerFxTestTypes = new List<PowerFxTestType> { new PowerFxTestType { Name = userDefinedTypeName, Value = userDefinedType } };
            }

            if (!string.IsNullOrWhiteSpace(userDefinedFunction))
            {
                settings.TestFunctions = new List<TestFunction> { new TestFunction { Code = userDefinedFunction } };
            }

            PowerFxEngine.ConditionallyRegisterTestTypes(settings, config);

            _provider.Engine = new RecalcEngine(config);

            PowerFxEngine.ConditionallyRegisterTestFunctions(settings, config, null, _provider.Engine);

            // Act
            var result = _provider.ValidatePowerFx(expression);

            // Assert
            if (expectedIsValid)
            {
                Assert.Contains("isValid: true", result);
                Assert.Contains("errors: []", result);
            }
            else
            {
                Assert.Contains("isValid: false", result);
                Assert.DoesNotContain("errors: []", result);
            }
        }

        [Fact]
        public async Task HandleRequest_ValidatePowerFx()
        {
            // Arrange
            var request = new MCPRequest { Method = "POST", Endpoint = "validate", Body = "\"\\\"1=1\\\"\"", ContentType = "application/json" };

            var provider = new MCPProvider
            {
                GetOrganizationService = () => new StubOrganizationService(),
                Engine = new RecalcEngine()
            };

            // Act
            var response = await provider.HandleRequest(request);

            // Assert
            Assert.Equal(200, response.StatusCode);
            Assert.Equal("application/x-yaml", response.ContentType);
        }

        [Fact]
        public async Task HandleRequest_ReturnsPlans()
        {
            // Arrange
            var request = new MCPRequest { Method = "GET", Endpoint = "plans", ContentType = "application/json" };

            var provider = new MCPProvider
            {
                GetOrganizationService = () => new StubOrganizationService(),
                Engine = new RecalcEngine()
            };

            // Act
            var response = await provider.HandleRequest(request);

            // Assert
            Assert.Equal(200, response.StatusCode);
        }

        [Theory]
        [InlineData("application/json", "\"If(1=1,2,3)\"", true)] // Valid Power Fx in JSON
        [InlineData("application/json", "\"InvalidExpression\"", false)] // Invalid Power Fx in JSON
        [InlineData("application/x-yaml", "If(1=1,2,3)", true)] // Valid Power Fx in YAML
        [InlineData("application/x-yaml", "InvalidExpression", false)] // Invalid Power Fx in YAML
        [InlineData("application/json", null, false)] // Null input in JSON
        [InlineData("application/x-yaml", "", true)] // Empty input in YAML
        public async Task HandleRequest_ValidatePowerFx_Parameterized(string contentType, string? body, bool expectedIsValid)
        {
            // Arrange
            var request = new MCPRequest
            {
                Method = "POST",
                Endpoint = "validate",
                Body = body,
                ContentType = contentType
            };

            var provider = new MCPProvider
            {
                GetOrganizationService = () => new StubOrganizationService(),
                Engine = new RecalcEngine()
            };

            // Act
            var response = await provider.HandleRequest(request);

            // Assert
            Assert.Equal(200, response.StatusCode);
            Assert.Equal("application/x-yaml", response.ContentType);

            // Deserialize the YAML response
            var deserializer = new YamlDotNet.Serialization.DeserializerBuilder()
                .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention.Instance)
                .Build();

            // Handle the >+ syntax
            var rawYaml = response.Body.StartsWith(">+") ? deserializer.Deserialize<string>(response.Body) : response.Body;
            var result = deserializer.Deserialize<ValidationResult>(rawYaml.Trim());

            // Validate the deserialized result
            Assert.Equal(expectedIsValid, result.isValid);
            if (expectedIsValid)
            {
                Assert.Empty(result.errors);
            }
            else
            {
                Assert.NotEmpty(result.errors);
            }
        }

        [Theory]
        [InlineData("RecordItem", "{Value: Number}", "IsZero(item: RecordItem): Boolean = item.Value = 0", "application/json", "\"IsZero({Value:0})\"", true)]
        [InlineData("", "", "IsZero(item: Number): Boolean = item = 0", "application/json", "\"IsZero(0)\"", true)]
        public async Task HandleRequest_ValidatePowerFx_Typed(string typeName, string typeValue, string functionValue, string contentType, string? body, bool expectedIsValid)
        {
            // Arrange
            var request = new MCPRequest
            {
                Method = "POST",
                Endpoint = "validate",
                Body = body,
                ContentType = contentType
            };

            var settings = new TestSettings();

            var provider = new MCPProvider
            {
                GetOrganizationService = () => new StubOrganizationService(),
                Engine = new RecalcEngine(),
                MCPTestSettings = settings
            };

            if (!string.IsNullOrEmpty(typeName))
            {
                settings.PowerFxTestTypes = new List<PowerFxTestType> { new PowerFxTestType { Name = typeName, Value = typeValue } };
            }

            if (!string.IsNullOrEmpty(functionValue))
            {
                settings.TestFunctions = new List<TestFunction> { new TestFunction { Code = functionValue } };
            }

            // Act
            var response = await provider.HandleRequest(request);

            // Assert
            Assert.Equal(200, response.StatusCode);
            Assert.Equal("application/x-yaml", response.ContentType);

            // Deserialize the YAML response
            var deserializer = new YamlDotNet.Serialization.DeserializerBuilder()
                .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention.Instance)
                .Build();

            // Handle the >+ syntax
            var rawYaml = response.Body.StartsWith(">+") ? deserializer.Deserialize<string>(response.Body) : response.Body;
            var result = deserializer.Deserialize<ValidationResult>(rawYaml.Trim());

            // Validate the deserialized result
            Assert.Equal(expectedIsValid, result.isValid);
            if (expectedIsValid)
            {
                Assert.Empty(result.errors);
            }
            else
            {
                Assert.NotEmpty(result.errors);
            }
        }

        // Helper class for deserializing the validation result
        public class ValidationResult
        {
            public bool isValid { get; set; }
            public List<string> errors { get; set; } = new List<string>();
        }
    }
}
