// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Providers;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Moq;

namespace Microsoft.PowerApps.TestEngine.Tests.PowerApps
{
    public class PowerFxProviderTest
    {
        private Mock<ITestInfraFunctions> MockTestInfraFunctions;
        private Mock<ITestState> MockTestState;
        private Mock<ISingleTestInstanceState> MockSingleTestInstanceState;
        private Mock<ILogger> MockLogger;

        public PowerFxProviderTest()
        {
            MockTestInfraFunctions = new Mock<ITestInfraFunctions>(MockBehavior.Strict);
            MockTestState = new Mock<ITestState>(MockBehavior.Strict);
            MockSingleTestInstanceState = new Mock<ISingleTestInstanceState>(MockBehavior.Strict);
            MockLogger = new Mock<ILogger>(MockBehavior.Strict);
            
        }

        [Fact]
        public async Task CheckNamespace()
        {
            // Arrange
            var provider = new PowerFxProvider(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);

            // Act
            var result = provider.Namespaces;

            // Assert
            Assert.Single(result);
            Assert.Equal("Preview", result[0]);
        }

        [Fact]
        public async Task CheckProviderName()
        {
            // Arrange
            var provider = new PowerFxProvider(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);

            // Act
            var result = provider.Name;

            // Assert
            Assert.Equal("powerfx", result);
        }

        [Fact]
        public async Task CheckIsIdleAsync_ReturnsTrue()
        {
            // Arrange
            var provider = new PowerFxProvider(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);

            // Act
            var result = await provider.CheckIsIdleAsync();

            // Assert
            Assert.True(result);
        }
    }
}
