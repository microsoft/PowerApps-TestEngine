// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx.Types;
using Moq;

namespace testengine.module.powerapps.portal.tests
{
    public class CheckConnectionExistsFunctionTests
    {
        private Mock<ITestInfraFunctions> MockTestInfraFunctions;
        private Mock<ILogger> MockLogger;
        private Mock<ITestState> MockTestState;
        private Mock<ConnectionHelper> MockConnectionHelper;
        private Mock<IBrowserContext> MockBrowserContext;

        public CheckConnectionExistsFunctionTests()
        {
            MockTestInfraFunctions = new Mock<ITestInfraFunctions>(MockBehavior.Strict);
            MockTestState = new Mock<ITestState>(MockBehavior.Strict);
            MockLogger = new Mock<ILogger>();
            MockConnectionHelper = new Mock<ConnectionHelper>(MockBehavior.Strict);
            MockBrowserContext = new Mock<IBrowserContext>(MockBehavior.Strict);
        }

        [Fact]
        public async Task ConnectionExists()
        {
            // Arrange
            MockTestState.Setup(x => x.GetDomain()).Returns("https://make.powerapps.com");
            MockTestInfraFunctions.Setup(x => x.GetContext()).Returns(MockBrowserContext.Object);
            MockConnectionHelper.Setup(x => x.Exists(MockBrowserContext.Object, "https://make.powerapps.com", "Test")).Returns(Task.FromResult(true));

            var function = new CheckConnectionExistsFunction(MockTestInfraFunctions.Object, MockTestState.Object, MockLogger.Object);
            function.GetConnectionHelper = () => { return MockConnectionHelper.Object; };

            // Act
            var result = function.Execute(StringValue.New("Test"));

            // Assert
            Assert.True(result.Value);
        }

        [Fact]
        public async Task ConnectionNotExists()
        {
            // Arrange
            MockTestState.Setup(x => x.GetDomain()).Returns("https://make.powerapps.com");
            MockTestInfraFunctions.Setup(x => x.GetContext()).Returns(MockBrowserContext.Object);
            MockConnectionHelper.Setup(x => x.Exists(MockBrowserContext.Object, "https://make.powerapps.com", "Test")).Returns(Task.FromResult(false));

            var function = new CheckConnectionExistsFunction(MockTestInfraFunctions.Object, MockTestState.Object, MockLogger.Object);
            function.GetConnectionHelper = () => { return MockConnectionHelper.Object; };

            // Act
            var result = function.Execute(StringValue.New("Test"));

            // Assert
            Assert.False(result.Value);
        }
    }
}
