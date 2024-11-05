﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.PowerFx;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Moq;
using Xunit;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
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
        private Mock<IPowerFxEngine> _mockEngine;

        public TestRecorderTests()
        {
            _mockLogger = new Mock<ILogger>();
            _mockBrowserContext = new Mock<IBrowserContext>(MockBehavior.Strict);
            _mockTestState = new Mock<ITestState>(MockBehavior.Strict);
            _mockTestInfraFunctions = new Mock<ITestInfraFunctions>(MockBehavior.Strict);
            _mockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            _mockPage = new Mock<IPage>(MockBehavior.Strict);
            _mockRequest = new Mock<IRequest>(MockBehavior.Strict);
            _mockResponse = new Mock<IResponse>(MockBehavior.Strict);
            _mockEngine = new Mock<IPowerFxEngine>(MockBehavior.Strict);

            _mockTestInfraFunctions.SetupGet(m => m.Page).Returns(_mockPage.Object);
        }

        [Fact]
        public void CanCreate()
        {
            new TestRecorder(_mockLogger.Object, _mockBrowserContext.Object, _mockTestState.Object, _mockTestInfraFunctions.Object, _mockEngine.Object, _mockFileSystem.Object);
        }

        [Fact]
        public void Setup_SubscribesToEvents()
        {
            // Arrange
            var recorder = new TestRecorder(_mockLogger.Object, _mockBrowserContext.Object, _mockTestState.Object, _mockTestInfraFunctions.Object, _mockEngine.Object, _mockFileSystem.Object);

            // Act
            recorder.Setup();

            // Assert
            _mockBrowserContext.VerifyAdd(m => m.Response += It.IsAny<EventHandler<IResponse>>(), Times.Once);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("// Test")]
        public void Generate_CreatesDirectoryAndWritesToFile(string? steps)
        {
            // Arrange
            var path = "testPath";
            var fileContents = string.Empty;
            var recorder = new TestRecorder(_mockLogger.Object, _mockBrowserContext.Object, _mockTestState.Object, _mockTestInfraFunctions.Object, _mockEngine.Object, _mockFileSystem.Object);
            _mockFileSystem.Setup(fs => fs.Exists(path)).Returns(false);
            _mockFileSystem.Setup(fs => fs.CreateDirectory(path));
            _mockFileSystem.Setup(fs => fs.WriteTextToFile($"{path}/recorded.te.yaml", It.IsAny<string>()))
                .Callback((string name, string contents) =>
                {
                    fileContents = contents;
                });
            if (!string.IsNullOrEmpty(steps))
            {
                recorder.TestSteps.Add(steps);
            }

            // Act
            recorder.Generate(path);

            // Assert
            Assert.True(ValidateYamlFile(fileContents));
        }

        private bool ValidateYamlFile(string content)
        {
            try
            {
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();


                var yamlObject = deserializer.Deserialize(content);

                return true;
            }
            catch (YamlException ex)
            {
                Console.WriteLine($"YAML validation error: {ex.Message}");
                return false;
            }
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
        public async Task OnResponse_HandlesDataverseResponse(string url, int count, string json, string action)
        {
            // Arrange
            var recorder = new TestRecorder(_mockLogger.Object, _mockBrowserContext.Object, _mockTestState.Object, _mockTestInfraFunctions.Object, _mockEngine.Object, _mockFileSystem.Object);
            var tasks = new List<Task>();

            var args = new object[] { tasks, _mockResponse.Object };

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
            if (tasks.Count > 0)
            {
                await tasks[0];
            }

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
        // Complex object
        [InlineData("/apim/test", "{\"Name\": {\"Child\":\"Test\"}}", "Experimental.SimulateConnector({Name: \"test\", Then: {Name: {Child: \"Test\"}}});")]
        [InlineData("/apim/test", "{\"List\": [{\"Child\":\"Test\"}]}", "Experimental.SimulateConnector({Name: \"test\", Then: {List: Table({Child: \"Test\"})}});")]
        [InlineData("/apim/test", @"[{""Name"": {""Child"":""Test""}}]", "Experimental.SimulateConnector({Name: \"test\", Then: Table({Name: {Child: \"Test\"}})});")]
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
        [InlineData("/apim/test?$filter=(a eq 'value')", "{\"Name\": \"Test\"}", "Experimental.SimulateConnector({Name: \"test\", When: {Filter: \"a = \"\"value\"\"\"}, Then: {Name: \"Test\"}});")]
        public async Task OnResponse_HandlesConnectorResponse(string url, string body, string action)
        {
            // Arrange
            var recorder = new TestRecorder(_mockLogger.Object, _mockBrowserContext.Object, _mockTestState.Object, _mockTestInfraFunctions.Object, _mockEngine.Object, _mockFileSystem.Object);
            var tasks = new List<Task>();

            var args = new object[] { tasks, _mockResponse.Object };

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

            if (tasks.Count > 0)
            {
                await tasks[0];
            }

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
