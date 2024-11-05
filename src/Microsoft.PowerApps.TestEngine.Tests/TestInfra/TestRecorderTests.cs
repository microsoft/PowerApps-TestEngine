// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Moq;
using Xunit;
using static Microsoft.PowerApps.TestEngine.Tests.TestInfra.NetworkMonitorTests;

namespace Microsoft.PowerApps.TestEngine.Tests.TestInfra
{
    public class TestRecorderTests
    {
        private Mock<ILogger> _mockLogger;
        private Mock<IBrowserContext> _mockBrowserContext;
        private Mock<ITestState> _mockTestState;
        private Mock<ITestInfraFunctions> _mockTestInfraFunctions;
        private Mock<IFileSystem> _mockFileSystem;
        private Mock<IPage> _mockPage;
        private Mock<IRequest> _mockRequest;
        private Mock<IResponse> _mockResponse;
        private RecalcEngine _recalcEngine;

        public TestRecorderTests()
        {
            _mockLogger = new Mock<ILogger>();
            _mockBrowserContext = new Mock<IBrowserContext>();
            _mockTestState = new Mock<ITestState>();
            _mockTestInfraFunctions = new Mock<ITestInfraFunctions>();
            _mockFileSystem = new Mock<IFileSystem>();
            _mockPage = new Mock<IPage>();
            _mockRequest = new Mock<IRequest>();
            _mockResponse = new Mock<IResponse>();
            _recalcEngine = new RecalcEngine();

            _mockTestInfraFunctions.SetupGet(m => m.Page).Returns(_mockPage.Object);
        }

        [Fact]
        public void CanCreate()
        {
            new TestRecorder(_mockLogger.Object, _mockBrowserContext.Object, _mockTestState.Object, _mockTestInfraFunctions.Object, _recalcEngine, _mockFileSystem.Object);
        }

        [Fact]
        public void Setup_SubscribesToEvents()
        {
            // Arrange
            var recorder = new TestRecorder(_mockLogger.Object, _mockBrowserContext.Object, _mockTestState.Object, _mockTestInfraFunctions.Object, _recalcEngine, _mockFileSystem.Object);

            // Act
            recorder.Setup();

            // Assert
            _mockBrowserContext.VerifyAdd(m => m.Response += It.IsAny<EventHandler<IResponse>>(), Times.Once);
        }

        [Fact]
        public void Generate_CreatesDirectoryAndWritesToFile()
        {
            // Arrange
            var path = "testPath";
            var recorder = new TestRecorder(_mockLogger.Object, _mockBrowserContext.Object, _mockTestState.Object, _mockTestInfraFunctions.Object, _recalcEngine, _mockFileSystem.Object);
            _mockFileSystem.Setup(fs => fs.Exists(path)).Returns(false);

            // Act
            recorder.Generate(path);

            // Assert
            _mockFileSystem.Verify(fs => fs.CreateDirectory(path), Times.Once);
            _mockFileSystem.Verify(fs => fs.WriteTextToFile($"{path}/testSteps.txt", It.IsAny<string>()), Times.Once);
        }

        // TODO: Add test case for datetime

        [Theory]
        [InlineData("https://www.example.com", 0, "", "")]
        // Empty table
        [InlineData("https://www.example.com/api/data/v9.2/accounts", 1, "{\"value\":[]}", "Experimental.SimulateDataverse({Action: \"Query\", Entity: \"accounts\", Then: Table()});")]
        // Single record from array
        [InlineData("https://www.example.com/api/data/v9.2/accounts", 1, "{\"value\":[{\"Name\":\"Test\"}]}", "Experimental.SimulateDataverse({Action: \"Query\", Entity: \"accounts\", Then: Table({Name: \"Test\"})});")]
        // Two records from array
        [InlineData("https://www.example.com/api/data/v9.2/accounts", 1, "{\"value\":[{\"Name\":\"Test\"},{\"Name\":\"Other\"}]}", "Experimental.SimulateDataverse({Action: \"Query\", Entity: \"accounts\", Then: Table({Name: \"Test\"}, {Name: \"Other\"})});")]
        // Record value
        [InlineData("https://www.example.com/api/data/v9.2/accounts", 1, "{\"value\":{\"Name\":\"Test\"}}", "Experimental.SimulateDataverse({Action: \"Query\", Entity: \"accounts\", Then: {Name: \"Test\"}});")]
        public void OnResponse_HandlesDataverseResponse(string url, int count, string json, string action)
        {
            // Arrange
            var recorder = new TestRecorder(_mockLogger.Object, _mockBrowserContext.Object, _mockTestState.Object, _mockTestInfraFunctions.Object, _recalcEngine, _mockFileSystem.Object);

            var args = new object[] { null, _mockResponse.Object };

            _mockResponse.Setup(m => m.Request).Returns(_mockRequest.Object);
            _mockRequest.SetupGet(m => m.Url).Returns(url);
            _mockRequest.SetupGet(m => m.Method).Returns("GET");

            if (count > 0)
            {
                _mockResponse.Setup(m => m.JsonAsync()).Returns(Task.FromResult(ParseJson(json)));
            }

            // Act
            recorder.Setup();
            _mockBrowserContext.Raise(m => m.Response += null, args);

            // Assert
            Assert.Equal(count, recorder.TestSteps.Count());

            if (count > 0)
            {
                Assert.Equal(action, recorder.TestSteps.First());
            }
        }

        [Theory]
        [InlineData("", "", "")]
        // Empty table
        [InlineData("/apim/test", "{}", "Experimental.SimulateConnector({Name: \"test\", Then: Blank()});")]
        // Record match
        [InlineData("/apim/test", "{\"Name\": \"Test\"}", "Experimental.SimulateConnector({Name: \"test\", Then: {Name: \"Test\"}});")]
        // Test for action after the connector id
        [InlineData("/apim/test/a1234567-1111-2222-3333-44445555666/v1.0/action", "{\"Name\": \"Test\"}", "Experimental.SimulateConnector({Name: \"test\", When: {Action: \"v1.0/action\"}, Then: {Name: \"Test\"}});")]
        // OData filter scenarios
        [InlineData("/apim/test?$filter=a eq 1", "{\"Name\": \"Test\"}", "Experimental.SimulateConnector({Name: \"test\", When: {Filter: \"a = 1\"}, Then: {Name: \"Test\"}});")]
        [InlineData("/apim/test?$filter=a ne 1", "{\"Name\": \"Test\"}", "Experimental.SimulateConnector({Name: \"test\", When: {Filter: \"a != 1\"}, Then: {Name: \"Test\"}});")]
        [InlineData("/apim/test?$filter=a ge 1", "{\"Name\": \"Test\"}", "Experimental.SimulateConnector({Name: \"test\", When: {Filter: \"a >= 1\"}, Then: {Name: \"Test\"}});")]
        [InlineData("/apim/test?$filter=a gt 1", "{\"Name\": \"Test\"}", "Experimental.SimulateConnector({Name: \"test\", When: {Filter: \"a > 1\"}, Then: {Name: \"Test\"}});")]
        [InlineData("/apim/test?$filter=a le 1", "{\"Name\": \"Test\"}", "Experimental.SimulateConnector({Name: \"test\", When: {Filter: \"a <= 1\"}, Then: {Name: \"Test\"}});")]
        [InlineData("/apim/test?$filter=a lt 1", "{\"Name\": \"Test\"}", "Experimental.SimulateConnector({Name: \"test\", When: {Filter: \"a < 1\"}, Then: {Name: \"Test\"}});")]
        // OData filter to function
        [InlineData("/apim/test?$filter=(a eq 1) and (b eq 2)", "{\"Name\": \"Test\"}", "Experimental.SimulateConnector({Name: \"test\", When: {Filter: \"AND(a = 1,b = 2)\"}, Then: {Name: \"Test\"}});")]
        [InlineData("/apim/test?$filter=(a eq 1) or (b eq 2)", "{\"Name\": \"Test\"}", "Experimental.SimulateConnector({Name: \"test\", When: {Filter: \"OR(a = 1,b = 2)\"}, Then: {Name: \"Test\"}});")]
        public void OnResponse_HandlesConnectorResponse(string url, string body, string action)
        {
            // Arrange
            var recorder = new TestRecorder(_mockLogger.Object, _mockBrowserContext.Object, _mockTestState.Object, _mockTestInfraFunctions.Object, _recalcEngine, _mockFileSystem.Object);

            var args = new object[] { null, _mockResponse.Object };

            _mockResponse.Setup(m => m.Request).Returns(_mockRequest.Object);
            if (!string.IsNullOrEmpty(url))
            {
                _mockResponse.Setup(m => m.JsonAsync()).ReturnsAsync(ParseJson(body));
            }
            _mockRequest.SetupGet(m => m.Url).Returns("https://example.com/invoke");
            _mockRequest.SetupGet(m => m.Method).Returns("POST");

            var headers = new Dictionary<string, string> { };

            if (!string.IsNullOrEmpty(url))
            {
                headers.Add("x-ms-request-url", url);
            }

            _mockRequest.SetupGet(m => m.Headers).Returns(headers);

            // Act
            recorder.Setup();
            _mockBrowserContext.Raise(m => m.Response += null, args);

            // Assert
            if (string.IsNullOrEmpty(action))
            {
                Assert.Empty(recorder.TestSteps);
            }
            else
            {
                Assert.Single(recorder.TestSteps);
                Assert.Equal(action, recorder.TestSteps.First());
            }
        }

        private JsonElement? ParseJson(string jsonString)
        {
            if (string.IsNullOrEmpty(jsonString))
            {
                return null;
            }
            JsonDocument jsonDocument = JsonDocument.Parse(jsonString);
            return jsonDocument.RootElement;
        }
    }
}
