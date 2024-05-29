using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
using Moq;

namespace testengine.user.local.tests
{
    public class LocalUserManagerModuleTests
    {
        [Fact]
        public void NameIsLocal()
        {
            // Arrange


            // Act
            var module = new LocalUserManagerModule();

            // Assert
            Assert.Equal("local", module.Name);
        }

        [Fact]
        public void EmptyLocation()
        {
            // Arrange


            // Act
            var module = new LocalUserManagerModule();

            // Assert
            Assert.Empty(module.Location);
        }

        [Fact]
        public void DoesNotUseStaticContext()
        {
            // Arrange


            // Act
            var module = new LocalUserManagerModule();

            // Assert
            Assert.False(module.UseStaticContext);
        }

        [Fact]
        public async Task LoginAsUserNoAction()
        {
            // Arrange
            var module = new LocalUserManagerModule();
            var mockBrowser = new Mock<IBrowserContext>(MockBehavior.Strict);
            var mockState = new Mock<ITestState>(MockBehavior.Strict);
            var mockInstaceState = new Mock<ISingleTestInstanceState>(MockBehavior.Strict);
            var mockEnvironment = new Mock<IEnvironmentVariable>(MockBehavior.Strict);

            // Act
            await module.LoginAsUserAsync("https://www.microsoft.com", mockBrowser.Object, mockState.Object, mockInstaceState.Object, mockEnvironment.Object);

            // Assert

        }
    }
}
