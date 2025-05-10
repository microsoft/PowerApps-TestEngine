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
            MockLogger = new Mock<ILogger>();
            MockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);

            MockSingleTestInstanceState.Setup(m => m.GetLogger()).Returns(MockLogger.Object);

            // Use StubOrganizationService for testing
            _provider = new MCPProvider(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object)
            {
                GetOrganizationService = () => new StubOrganizationService()
            };
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
        public async Task HandleRequest_ValidatePowerFx()
        {
            // Arrange
            var mockContext = new Mock<IHttpContext>();
            var mockRequest = new Mock<IHttpRequest>();
            var mockResponse = new Mock<IHttpResponse>();
            var inputStream = new MemoryStream();
            var outputStream = new MemoryStream();

            using (var wrier = new StreamWriter(inputStream, leaveOpen: true))
            {
                wrier.WriteLine("\"1=1\"");
            }
            inputStream.Position = 0;

            mockRequest.Setup(r => r.HttpMethod).Returns("POST");
            mockRequest.Setup(r => r.ContentType).Returns("application/json");
            mockRequest.Setup(r => r.Url).Returns(new Uri("http://localhost/validate"));
            mockRequest.Setup(r => r.InputStream).Returns(inputStream);
            mockResponse.Setup(r => r.OutputStream).Returns(outputStream);

            mockContext.Setup(c => c.Request).Returns(mockRequest.Object);
            mockContext.Setup(c => c.Response).Returns(mockResponse.Object);

            var provider = new MCPProvider
            {
                GetOrganizationService = () => new StubOrganizationService(),
                Engine = new RecalcEngine(),
                TestState = MockTestState.Object,
                SingleTestInstanceState = MockSingleTestInstanceState.Object
            };

            MockTestState.Setup(m => m.GetTestSettings()).Returns(new TestSettings());
            MockSingleTestInstanceState.Setup(m => m.GetLogger()).Returns(MockLogger.Object);

            // Act
            await provider.HandleRequest(mockContext.Object);

            // Assert
            mockResponse.VerifySet(r => r.StatusCode = 200, Times.Once);
            outputStream.Position = 0;
            var responseBody = new StreamReader(outputStream).ReadToEnd();
        }

        [Fact]
        public async Task HandleRequest_ReturnsPlans()
        {
            // Arrange
            var mockContext = new Mock<IHttpContext>();
            var mockRequest = new Mock<IHttpRequest>();
            var mockResponse = new Mock<IHttpResponse>();
            var outputStream = new MemoryStream();

            mockRequest.Setup(r => r.HttpMethod).Returns("GET");
            mockRequest.Setup(r => r.Url).Returns(new Uri("http://localhost/plans"));
            mockResponse.Setup(r => r.OutputStream).Returns(outputStream);

            mockContext.Setup(c => c.Request).Returns(mockRequest.Object);
            mockContext.Setup(c => c.Response).Returns(mockResponse.Object);

            var provider = new MCPProvider
            {
                GetOrganizationService = () => new StubOrganizationService()
            };

            // Act
            await provider.HandleRequest(mockContext.Object);

            // Assert
            mockResponse.VerifySet(r => r.StatusCode = 200, Times.Once);
            outputStream.Position = 0;
            var responseBody = new StreamReader(outputStream).ReadToEnd();
            Assert.Contains("Business Flight Requests", responseBody);
        }
    }
}
