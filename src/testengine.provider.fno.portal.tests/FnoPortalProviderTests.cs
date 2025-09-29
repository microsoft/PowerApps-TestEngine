// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Providers;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx.Types;
using Moq;
using Xunit;

namespace testengine.provider.fno.portal.tests
{
    public class FnoPortalProviderTests
    {
        private readonly Mock<ITestInfraFunctions> _mockInfraFunctions = new(MockBehavior.Strict);
        private readonly Mock<ISingleTestInstanceState> _mockSingleTestInstanceState = new(MockBehavior.Strict);
        private readonly Mock<ITestState> _mockTestState = new(MockBehavior.Strict);
        private readonly Mock<ILogger> _mockLogger = new(MockBehavior.Strict);

        private FnoPortalProvider CreateProvider()
        {
            var provider = new FnoPortalProvider
            {
                TestInfraFunctions = _mockInfraFunctions.Object,
                SingleTestInstanceState = _mockSingleTestInstanceState.Object,
                TestState = _mockTestState.Object
            };

            return provider;
        }

        private void SetupCommonExpectations()
        {
            _mockSingleTestInstanceState.Setup(s => s.GetLogger()).Returns(_mockLogger.Object);
            _mockTestState.Setup(s => s.GetTimeout()).Returns(1_000);
            _mockLogger
                .Setup(l => l.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, type) => true),
                    It.IsAny<System.Exception>(),
                    It.IsAny<Func<It.IsAnyType, System.Exception, string>>()))
                .Verifiable();
            _mockLogger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        }

        [Fact]
        public async Task CheckProviderAsync_InjectsHelperScript_WhenNotLoaded()
        {
            // Arrange
            SetupCommonExpectations();

            _mockInfraFunctions
                .SetupSequence(i => i.RunJavascriptAsync<bool>(It.IsAny<string>()))
                .ReturnsAsync(false) // Helper not yet registered
                .ReturnsAsync(true)  // After injection
                .ReturnsAsync(true); // Poll ensureReady

            _mockInfraFunctions
                .Setup(i => i.AddScriptContentAsync(It.Is<string>(script => script.Contains("FnoTestEngine"))))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var provider = CreateProvider();

            // Act
            await provider.CheckProviderAsync();

            // Assert
            _mockInfraFunctions.Verify(i => i.AddScriptContentAsync(It.IsAny<string>()), Times.Once);
            _mockInfraFunctions.Verify(i => i.RunJavascriptAsync<bool>(It.Is<string>(expr => expr.Contains("typeof window.FnoTestEngine"))), Times.AtLeast(2));
        }

        [Fact]
        public async Task LoadObjectModelAsync_ReturnsControlsFromObjectModel()
        {
            // Arrange
            SetupCommonExpectations();

            var objectModel = "{\"controls\":[{\"name\":\"PrimaryButton\",\"properties\":[{\"propertyName\":\"Text\",\"propertyType\":\"s\"},{\"propertyName\":\"Visible\",\"propertyType\":\"b\"}]}]}";

            _mockInfraFunctions
                .Setup(i => i.RunJavascriptAsync<bool>(It.Is<string>(expr => expr.Contains("typeof window.FnoTestEngine"))))
                .ReturnsAsync(true);
            _mockInfraFunctions.Setup(i => i.RunJavascriptAsync<string>(It.Is<string>(expr => expr.Contains("buildObjectModel")))).ReturnsAsync(objectModel);

            var provider = CreateProvider();

            // Act
            var controls = await provider.LoadObjectModelAsync();

            // Assert
            Assert.Contains("PrimaryButton", controls.Keys);
            var control = controls["PrimaryButton"];
            Assert.NotNull(control);
            Assert.Contains(control.Type.GetFieldTypes(), f => f.Name == "Text");
            _mockInfraFunctions.Verify(i => i.RunJavascriptAsync<string>(It.Is<string>(expr => expr.Contains("buildObjectModel"))), Times.AtLeastOnce);
        }

        [Fact]
        public async Task SetPropertyAsync_SendsSerializedPayload()
        {
            // Arrange
            SetupCommonExpectations();

            _mockInfraFunctions
                .Setup(i => i.RunJavascriptAsync<bool>(It.Is<string>(expr => expr.Contains("typeof window.FnoTestEngine"))))
                .ReturnsAsync(true);
            _mockInfraFunctions
                .Setup(i => i.RunJavascriptAsync<bool>(It.Is<string>(expr => expr.StartsWith("window.FnoTestEngine.setPropertyValue"))))
                .ReturnsAsync(true)
                .Verifiable();

            var provider = CreateProvider();
            var itemPath = new ItemPath { ControlName = "PrimaryInput", PropertyName = "Value" };
            var value = StringValue.New("alpha");

            // Act
            var result = await provider.SetPropertyAsync(itemPath, value);

            // Assert
            Assert.True(result);
            _mockInfraFunctions.Verify();
        }

        [Fact]
        public async Task SelectControlAsync_InvokesHelper()
        {
            // Arrange
            SetupCommonExpectations();
            _mockInfraFunctions
                .Setup(i => i.RunJavascriptAsync<bool>(It.Is<string>(expr => expr.Contains("typeof window.FnoTestEngine"))))
                .ReturnsAsync(true);
            _mockInfraFunctions
                .Setup(i => i.RunJavascriptAsync<bool>(It.Is<string>(expr => expr.StartsWith("window.FnoTestEngine.selectControl"))))
                .ReturnsAsync(true)
                .Verifiable();

            var provider = CreateProvider();
            var itemPath = new ItemPath { ControlName = "PrimaryButton" };

            // Act
            var result = await provider.SelectControlAsync(itemPath);

            // Assert
            Assert.True(result);
            _mockInfraFunctions.Verify();
        }

        [Fact]
        public void GenerateTestUrl_WithExplicitDomain_AppendsSourceAndSetsDomain()
        {
            // Arrange
            SetupCommonExpectations();

            _mockTestState.Setup(s => s.SetDomain(It.IsAny<string>())).Verifiable();

            var provider = CreateProvider();
            var domain = "https://contoso.operations.dynamics.com/main";
            var result = provider.GenerateTestUrl(domain, "cmp=USMF");

            // Assert
            Assert.Contains("source=testengine", result);
            Assert.Contains("cmp=USMF", result);
            _mockTestState.Verify(s => s.SetDomain("https://contoso.operations.dynamics.com"), Times.Once);
        }
    }
}
