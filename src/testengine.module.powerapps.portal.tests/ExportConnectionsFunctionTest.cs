// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Providers;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;
using Moq;
using testengine.module.powerapps.portal;

namespace testengine.module.powerappsportal.tests
{
    public class ExportConnectionsFunctionTest
    {
        private Mock<ITestInfraFunctions> MockTestInfraFunctions;
        private Mock<ITestState> MockTestState;
        private Mock<ITestWebProvider> MockTestWebProvider;
        private Mock<ISingleTestInstanceState> MockSingleTestInstanceState;
        private Mock<IFileSystem> MockFileSystem;
        private Mock<IPage> MockPage;
        private PowerFxConfig TestConfig;
        private NetworkRequestMock TestNetworkRequestMock;
        private Mock<ILogger> MockLogger;
        private Mock<IBrowserContext> MockBrowserContext;

        public ExportConnectionsFunctionTest()
        {
            MockTestInfraFunctions = new Mock<ITestInfraFunctions>(MockBehavior.Strict);
            MockTestState = new Mock<ITestState>(MockBehavior.Strict);
            MockTestWebProvider = new Mock<ITestWebProvider>();
            MockSingleTestInstanceState = new Mock<ISingleTestInstanceState>(MockBehavior.Strict);
            MockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            MockPage = new Mock<IPage>(MockBehavior.Strict);
            TestConfig = new PowerFxConfig();
            TestNetworkRequestMock = new NetworkRequestMock();
            MockLogger = new Mock<ILogger>();
            MockBrowserContext = new Mock<IBrowserContext>(MockBehavior.Strict);
        }

        [Fact]
        public void ExecuteExportConnections()
        {
            // Arrange
            var file = StringValue.New("test.json");
            MockTestInfraFunctions.Setup(x => x.GetContext()).Returns(MockBrowserContext.Object);
            MockBrowserContext.Setup(x => x.NewPageAsync()).Returns(Task.FromResult(MockPage.Object));
            MockTestState.Setup(x => x.GetDomain()).Returns("https://make.powerapps.com");

            // Goto and return json
            var mockConnectionHelper = new Mock<ConnectionHelper>();
            var connections = new List<Connection>();
            connections.Add(new Connection { Name = "Test", Id = "1", Status = "Connected" });
            mockConnectionHelper.Setup(x => x.GetConnections(MockBrowserContext.Object, "https://make.powerapps.com", null)).Returns(Task.FromResult(connections));

            var function = new ExportConnectionsFunction(MockTestInfraFunctions.Object, MockTestState.Object, MockLogger.Object);

            function.GetConnectionHelper = () => mockConnectionHelper.Object;
            string results = String.Empty;
            string fileName = String.Empty;
            function.WriteAllText = (file, json) =>
            {
                fileName = file;
                results = json;
            };

            // Act
            function.Execute(file);

            // Assert
            var data = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(results);
            Assert.Single(data);
            Assert.Equal("test.json", fileName);

            Assert.Equal("Test", data[0]["Name"]);
            Assert.Equal("1", data[0]["Id"]);
            Assert.Equal("Connected", data[0]["Status"]);
        }

    }
}
