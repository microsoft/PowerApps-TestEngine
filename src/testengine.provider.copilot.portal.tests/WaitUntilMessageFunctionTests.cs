using System.Net;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Providers.Services;
using Microsoft.PowerApps.TestEngine.Providers;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx.Types;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Providers.Functions;
using Microsoft.PowerApps.TestEngine.System;

namespace testengine.provider.copilot.portal.tests
{
    public class WaitUntilMessageFunctionTests
    {
        private readonly Mock<ITestInfraFunctions> _mockTestInfraFunctions;
        private readonly Mock<ISingleTestInstanceState> _mockSingleTestInstanceState;
        private readonly Mock<ITestState> _mockTestState;
        private readonly Mock<IEnvironmentVariable> _mockEnvironment;
        private readonly Mock<ILogger> _mockLogger;
        private readonly Mock<IMessageProvider> _mockProvider;
        private readonly WaitUntilMessageFunction _function;

        public WaitUntilMessageFunctionTests()
        {
            _mockTestInfraFunctions = new Mock<ITestInfraFunctions>();
            _mockSingleTestInstanceState = new Mock<ISingleTestInstanceState>();
            _mockTestState = new Mock<ITestState>();
            _mockLogger = new Mock<ILogger>();
            _mockEnvironment = new Mock<IEnvironmentVariable>();
            _mockProvider = new Mock<IMessageProvider>();

            _function = new WaitUntilMessageFunction(
                _mockTestInfraFunctions.Object,
                _mockTestState.Object,
                _mockLogger.Object,
                _mockProvider.Object);
        }

       
        [Fact]
        public void Sanitize_ShouldEscapeSpecialCharacters()
        {
            // Arrange
            var input = "test.*+?^${}()|[]\\";
            var expected = "test\\.\\*\\+\\?\\^\\$\\{}\\(\\)\\|\\[]\\\\";

            // Act
            var result = WaitUntilMessageFunction.Sanitize(input);

            // Assert
            Assert.Equal(expected, result);
        }
    }
}
