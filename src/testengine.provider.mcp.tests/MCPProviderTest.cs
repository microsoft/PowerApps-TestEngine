// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.PowerFx;
using Microsoft.PowerApps.TestEngine.Providers;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerApps.TestEngine.Tests.Helpers;
using Microsoft.PowerFx;
using Moq;

namespace Microsoft.PowerApps.TestEngine.Tests.PowerApps
{
    public class MCPProviderTest
    {
        private Mock<ITestInfraFunctions> MockTestInfraFunctions;
        private Mock<ITestState> MockTestState;
        private Mock<ISingleTestInstanceState> MockSingleTestInstanceState;
        private Mock<ILogger> MockLogger;
        private Mock<IFileSystem> MockFileSystem;

        private MCPProvider _provider = null;

        public MCPProviderTest()
        {
            MockTestInfraFunctions = new Mock<ITestInfraFunctions>(MockBehavior.Strict);
            MockTestState = new Mock<ITestState>(MockBehavior.Strict);
            MockSingleTestInstanceState = new Mock<ISingleTestInstanceState>(MockBehavior.Strict);
            MockLogger = new Mock<ILogger>(MockBehavior.Strict);
            MockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);

            MockSingleTestInstanceState.Setup(m => m.GetLogger()).Returns(MockLogger.Object);
            _provider = new MCPProvider(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
        }

        [Fact]
        public async Task CheckNamespace()
        {
            // Arrange
            

            // Act
            var result = _provider.Namespaces;

            // Assert
            Assert.Single(result);
            Assert.Equal("Preview", result[0]);
        }

        [Fact]
        public async Task CheckProviderName()
        {
            // Arrange
            
            // Act
            var result = _provider.Name;

            // Assert
            Assert.Equal("mcp", result);
        }

        [Fact]
        public async Task CheckIsIdleAsync_ReturnsTrue()
        {
            // Arrange
            
            // Act
            var result = await _provider.CheckIsIdleAsync();

            // Assert
            Assert.True(result);
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
            _provider.Engine = new Microsoft.PowerFx.RecalcEngine();
            MockTestState.Setup(m => m.GetTestSettings()).Returns(new TestSettings());
            LoggingTestHelper.SetupMock(MockLogger);

            // Act
            var result = _provider.ValidatePowerFx(expression);

            // Assert
            if (expectedIsValid)
            {
                Assert.Contains("\"IsValid\":true", result);
                Assert.Contains("\"Errors\":[]", result);
            }
            else
            {
                Assert.Contains("\"IsValid\":false", result);
                Assert.DoesNotContain("\"Errors\":[]", result);
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

            if (!string.IsNullOrWhiteSpace(userDefinedTypeName))
            {
                settings.PowerFxTestTypes = new List<PowerFxTestType> { new PowerFxTestType { Name = userDefinedTypeName, Value = userDefinedType } };
            }

            if (!string.IsNullOrWhiteSpace(userDefinedFunction))
            {
                settings.TestFunctions = new List<TestFunction> { new TestFunction { Code = userDefinedFunction } };
            }

            LoggingTestHelper.SetupMock(MockLogger);

            PowerFxEngine.ConditionallyRegisterTestTypes(settings, config);

            _provider.Engine = new RecalcEngine(config);

            PowerFxEngine.ConditionallyRegisterTestFunctions(settings, config, MockLogger.Object, _provider.Engine);

            MockTestState.Setup(m => m.GetTestSettings()).Returns(settings);
            

            // Act
            var result = _provider.ValidatePowerFx(expression);

            // Assert
            if (expectedIsValid)
            {
                Assert.Contains("\"IsValid\":true", result);
                Assert.Contains("\"Errors\":[]", result);
            }
            else
            {
                Assert.Contains("\"IsValid\":false", result);
                Assert.DoesNotContain("\"Errors\":[]", result);
            }
        }

        [Fact]
        public async Task SetupContext_InitializesState()
        {
            // Arrange
            
            // Act
            await _provider.SetupContext();

            // Assert
            Assert.NotNull(_provider.TestState);
            Assert.NotNull(_provider.SingleTestInstanceState);
            Assert.NotNull(_provider.TestInfraFunctions);
        }

        [Fact]
        public void NodeJsHash()
        {
            // Arrange
            string appJsFileName = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location), "..", "..", "..", "src", "testengine.mcp", "app.js"));
            
            // Act & Asssert
            Assert.True(_provider.NodeJsHashValidator(MCPProvider.ComputeFileHash(appJsFileName)));
        }
    }
}
