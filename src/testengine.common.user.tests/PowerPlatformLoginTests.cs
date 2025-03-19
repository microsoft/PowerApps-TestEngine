// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerApps.TestEngine.Tests.Helpers;
using Microsoft.PowerApps.TestEngine.Users;
using Moq;
using testengine.common.user;
using Xunit;

namespace testengine.user.storagestate.tests
{
    public class PowerPlatformLoginTests
    {
        private Mock<IConfigurableUserManager> MockUserManager;
        private Dictionary<string, object> MockSettings;
        private Mock<IPage> MockPage;
        private Mock<ILocator> MockLocator;

        public PowerPlatformLoginTests()
        {
            MockUserManager = new Mock<IConfigurableUserManager>(MockBehavior.Strict);
            MockSettings = new Dictionary<string, object>();
            MockPage = new Mock<IPage>(MockBehavior.Strict);
            MockLocator = new Mock<ILocator>(MockBehavior.Strict);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("", null)]
        [InlineData("new", null)]
        [InlineData("new", "old")]
        [InlineData(null, "old")]
        public async Task DialogErrors(string? title, string? existing)
        {
            // Arrange
            var find = !String.IsNullOrEmpty(title);

            var login = new PowerPlatformLogin();
            var state = new LoginState()
            {
                Module = MockUserManager.Object,
                DesiredUrl = "http://example.com",
                Page = MockPage.Object
            };

            MockLocator.Setup(m => m.IsEditableAsync(null)).ReturnsAsync(false);

            MockPage.SetupGet(m => m.Url).Returns("https://someother.com");
            MockPage.Setup(m => m.Locator(PowerPlatformLogin.EmailSelector, null))
                .Returns(MockLocator.Object);

            MockUserManager.SetupGet(m => m.Settings).Returns(MockSettings);

            if (!string.IsNullOrEmpty(existing))
            {
                MockSettings.Add(PowerPlatformLogin.ERROR_DIALOG_KEY, existing);
            }

            if (find)
            {
                var engine = new Jint.Engine();
                // Simulate querySelector returning value
                engine.Evaluate($"var document = {{ querySelector: name => {{ return {{ textContent: '{title}' }}}} }}");
                MockPage
                    .Setup(m => m.EvaluateAsync<string>(PowerPlatformLogin.DIAGLOG_CHECK_JAVASCRIPT, null))
                    .Returns((string expression, object? args) =>
                    {
                        var value = engine.Evaluate(expression).ToString();
                        return Task.FromResult(value);
                    });
            }
            else
            {
                MockPage.Setup(m => m.EvaluateAsync<string>(PowerPlatformLogin.DIAGLOG_CHECK_JAVASCRIPT, null)).Returns(Task.FromResult(String.Empty));
            }

            // Act
            await login.HandleCommonLoginState(state);

            // Assert
            Assert.Equal(find, state.IsError);

            if (find)
            {
                Assert.Equal(title, MockSettings[PowerPlatformLogin.ERROR_DIALOG_KEY]);
            }

            if (!find && !string.IsNullOrEmpty(existing))
            {
                Assert.Equal(existing, MockSettings[PowerPlatformLogin.ERROR_DIALOG_KEY]);
            }

            if (!find && string.IsNullOrEmpty(existing))
            {
                Assert.Empty(MockSettings);
            }
        }

        [Theory]
        [InlineData("http://example.com", "http://example.com", "example.com")]
        [InlineData("http://example.com.mcas.ms", "http://example.com", "example.com.mcas.ms")]
        [InlineData("http://example.com/Home", "http://example.com", "example.com")]
        public async Task FindMatch(string url, string desiredUrl, string host)
        {
            // Arrange
            var login = new PowerPlatformLogin();
            var state = new LoginState()
            {
                Module = MockUserManager.Object,
                DesiredUrl = desiredUrl,
                Page = MockPage.Object
            };

            MockPage.SetupGet(m => m.Url).Returns(url);
            MockPage.Setup(m => m.EvaluateAsync<string>(PowerPlatformLogin.DEFAULT_OFFICE_365_CHECK, null))
                .Returns(Task.FromResult("Idle"));

            // Act
            await login.HandleCommonLoginState(state);

            // Assert
            Assert.True(state.FoundMatch);
            Assert.Equal(host, state.MatchHost);
        }
    }
}
