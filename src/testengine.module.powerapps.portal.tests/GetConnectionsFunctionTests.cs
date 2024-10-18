// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Providers;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Moq;
using testengine.module.powerapps.portal;

namespace testengine.module.powerappsportal.tests
{
    public class GetConnectionFunctionTests
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

        public GetConnectionFunctionTests()
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

        [Theory]
        [InlineData("", "", "", 0)]
        [InlineData("test", "1", "Connected", 1)]
        public void ExecuteGetConnections(string name, string id, string status, int expectedCount)
        {
            // Arrange
            MockTestInfraFunctions.Setup(x => x.GetContext()).Returns(MockBrowserContext.Object);
            MockBrowserContext.Setup(x => x.NewPageAsync()).Returns(Task.FromResult(MockPage.Object));
            MockTestState.Setup(x => x.GetDomain()).Returns("https://make.powerapps.com");

            // Goto and return json
            var mockConnectionHelper = new Mock<ConnectionHelper>();
            var connections = new List<Connection>();
            if (!string.IsNullOrEmpty(name))
            {
                connections.Add(new Connection { Name = name, Id = id, Status = status });
            }
            mockConnectionHelper.Setup(x => x.GetConnections(MockBrowserContext.Object, "https://make.powerapps.com", null)).Returns(Task.FromResult(connections));

            var function = new GetConnectionsFunction(MockTestInfraFunctions.Object, MockTestState.Object, MockLogger.Object);

            function.GetConnectionHelper = () => mockConnectionHelper.Object;

            // Act
            var result = function.Execute();

            // Assert
            Assert.Equal(expectedCount, result.Count());
        }

    }
}
