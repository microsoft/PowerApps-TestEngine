// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Providers;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Moq;

namespace Microsoft.PowerApps.TestEngine.Tests.CopilotPortal.Tests
{
    public class CopilotDirectLineProviderTests
    {
        private readonly CopilotAPIProvider _provider;
        private readonly Mock<ITestInfraFunctions> _mockTestInfraFunctions;
        private readonly Mock<ISingleTestInstanceState> _mockSingleTestInstanceState;
        private readonly Mock<ITestState> _mockTestState;
        private readonly Mock<IEnvironmentVariable> _mockEnvironment;

        public CopilotDirectLineProviderTests()
        {
            _mockTestInfraFunctions = new Mock<ITestInfraFunctions>();
            _mockSingleTestInstanceState = new Mock<ISingleTestInstanceState>();
            _mockTestState = new Mock<ITestState>();
            _mockEnvironment = new Mock<IEnvironmentVariable>();

            _provider = new CopilotAPIProvider(_mockTestInfraFunctions.Object, _mockSingleTestInstanceState.Object, _mockTestState.Object, _mockEnvironment.Object);
        }

        [Fact]
        public void GenerateTestUrl_ShouldReturnAboutBlank()
        {
            // Act
            var result = _provider.GenerateTestUrl("", "");

            // Assert
            Assert.Equal("about:blank", result);
        }

        [Fact]
        public async Task SetupContext_ShouldSetAgentKeyAndBotFrameworkUrl()
        {
            // Arrange
            var testSettings = new TestSettings
            {
                ExtensionModules = new TestSettingExtensions
                {
                    Parameters = new Dictionary<string, string>
                    {
                        { "AgentKey", "test_agent_key" },
                        { "BotFrameworkUrl", "http://test.botframework.url" }
                    }
                }
            };

            _mockTestState.Setup(ts => ts.GetTestSettings()).Returns(testSettings);
            _mockEnvironment.Setup(env => env.GetVariable("test_agent_key")).Returns("test_secret");

            _provider.TestInfraFunctions = _mockTestInfraFunctions.Object;
            _provider.TestState = _mockTestState.Object;
            _provider.Environment = _mockEnvironment.Object;

            // Act
            await _provider.SetupContext();
        }
    }
}
