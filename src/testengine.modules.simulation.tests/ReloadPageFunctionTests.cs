// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Providers;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Moq;
using testengine.module;
using Xunit;

namespace testengine.modules.simulation.tests
{
    public class ReloadPageFunctionTests
    {
        private Mock<ITestInfraFunctions> MockTestInfraFunctions;
        private Mock<ITestState> MockTestState;
        private Mock<ILogger> MockLogger;
        private Mock<IPage> MockPage;
        private Mock<ITestWebProvider> MockTestProvider;

        public ReloadPageFunctionTests()
        {
            MockTestInfraFunctions = new Mock<ITestInfraFunctions>(MockBehavior.Strict);
            MockTestState = new Mock<ITestState>(MockBehavior.Strict);
            MockLogger = new Mock<ILogger>();
            MockPage = new Mock<IPage>(MockBehavior.Strict);
            MockTestProvider = new Mock<ITestWebProvider>(MockBehavior.Strict);
        }

        [Fact]
        public void CanCreate()
        {
            new ReloadPageFunction(MockTestInfraFunctions.Object, MockTestState.Object, MockLogger.Object);
        }

        [Fact]
        public async Task CanExecute()
        {
            // Arrange
            var fxFunction = new ReloadPageFunction(MockTestInfraFunctions.Object, MockTestState.Object, MockLogger.Object);

            MockTestInfraFunctions.Setup(m => m.Page).Returns(MockPage.Object);

            MockPage.Setup(m => m.ReloadAsync(null)).Returns(Task.FromResult<IResponse>(null));

            MockTestState.Setup(m => m.TestProvider).Returns(MockTestProvider.Object);
            MockTestState.Setup(m => m.GetTimeout()).Returns(100);

            MockTestProvider.Setup(m => m.CheckProviderAsync()).Returns(Task.CompletedTask);
            MockTestProvider.Setup(m => m.TestEngineReady()).ReturnsAsync(true);
            MockTestProvider.Setup(m => m.CheckIsIdleAsync()).ReturnsAsync(true);

            // Act
            await fxFunction.ExecuteAsync();
        }
    }
}
