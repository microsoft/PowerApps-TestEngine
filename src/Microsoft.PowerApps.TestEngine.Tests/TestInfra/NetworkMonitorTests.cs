// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Moq;
using Xunit;


namespace Microsoft.PowerApps.TestEngine.Tests.TestInfra
{
    public class NetworkMonitorTests
    {
        Mock<ILogger> MockLogger;
        Mock<IBrowserContext> MockBrowserContext;
        Mock<IRoute> MockRoute;
        Mock<IRequest> MockRequest;
        Mock<IResponse> MockResponse;
        Mock<ITestState> MockTestState;

        public NetworkMonitorTests()
        {
            MockBrowserContext = new Mock<IBrowserContext>(MockBehavior.Strict);
            MockLogger = new Mock<ILogger>();
            MockRoute = new Mock<IRoute>(MockBehavior.Strict);
            MockRequest = new Mock<IRequest>(MockBehavior.Strict);
            MockResponse = new Mock<IResponse>(MockBehavior.Strict);
            MockTestState = new Mock<ITestState>(MockBehavior.Strict);
        }

        static List<string> urls = new List<string> {
            "login.microsoftonline.com",
            "login.microsoftonline.us",
            "login.chinacloudapi.cn",
            "login.microsoftonline.de"
        };

        public static IEnumerable<object[]> LoginUrls()
        {
            return urls.Select(val => new object[] { val });
        }

        [Theory]
        [MemberData(nameof(LoginUrls))]
        public async Task WillTrackRequest(string url)
        {
            // Arrange 
            var monitor = new NetworkMonitor(MockLogger.Object, MockBrowserContext.Object, MockTestState.Object);
            Func<IRoute, Task> callback = null;
            List<string> routeUrl = new List<string>();

            MockBrowserContext.Setup(m => m.RouteAsync(It.IsAny<string>(), It.IsAny<Func<IRoute, Task>>(), It.IsAny<BrowserContextRouteOptions>()))
                .Callback((string callbackUrl, Func<IRoute, Task> a, BrowserContextRouteOptions options) =>
                {
                    routeUrl.Add(callbackUrl);
                    callback = a;
                })
                .Returns(Task.CompletedTask);

            MockRoute.Setup(m => m.ContinueAsync(null)).Returns(Task.CompletedTask);
            MockRoute.Setup(m => m.Request).Returns(MockRequest.Object);

            MockRequest.Setup(m => m.Method).Returns("GET");
            MockRequest.Setup(m => m.Url).Returns($"https://{url}?query=value");

            // Act 
            await monitor.MonitorEntraLoginAsync($"https://app.powerapps.com");
            await callback(MockRoute.Object);

            // Assert
            Assert.Equal(urls.Count() + 1, routeUrl.Count());
        }

        public static IEnumerable<object[]> InvalidUrls()
        {
            yield return new object[] { "https://example.com" };
        }

        [Theory]
        [MemberData(nameof(InvalidUrls))]
        public async Task WillNotTrackRequest(string url)
        {
            // Arrange 
            var monitor = new NetworkMonitor(MockLogger.Object, MockBrowserContext.Object, MockTestState.Object);
            Func<IRoute, Task> callback = null;
            List<string> routeUrl = new List<string>();

            MockBrowserContext.Setup(m => m.RouteAsync(It.IsAny<string>(), It.IsAny<Func<IRoute, Task>>(), It.IsAny<BrowserContextRouteOptions>()))
                .Callback((string callbackUrl, Func<IRoute, Task> a, BrowserContextRouteOptions options) =>
                {
                    routeUrl.Add(callbackUrl);
                    callback = a;
                })
                .Returns(Task.CompletedTask);

            MockRoute.Setup(m => m.ContinueAsync(null)).Returns(Task.CompletedTask);
            MockRoute.Setup(m => m.Request).Returns(MockRequest.Object);

            MockLogger = new Mock<ILogger>(MockBehavior.Strict);

            MockRequest.Setup(m => m.Method).Returns("GET");
            MockRequest.Setup(m => m.Url).Returns(url);

            // Act 
            await monitor.MonitorEntraLoginAsync($"https://app.powerapps.com");
            await callback(MockRoute.Object);

            // Assert
            Assert.Equal(urls.Count() + 1, routeUrl.Count());
        }

        [Theory]
        [MemberData(nameof(LoginUrls))]
        public async Task WillTrackResponse(string url)
        {
            // Arrange
            var monitor = new NetworkMonitor(MockLogger.Object, MockBrowserContext.Object, MockTestState.Object);

            MockBrowserContext.Setup(m => m.RouteAsync(It.IsAny<string>(), It.IsAny<Func<IRoute, Task>>(), It.IsAny<BrowserContextRouteOptions>()))
               .Returns(Task.CompletedTask);

            MockRoute.Setup(m => m.ContinueAsync(null)).Returns(Task.CompletedTask);
            MockRoute.Setup(m => m.Request).Returns(MockRequest.Object);

            MockLogger = new Mock<ILogger>(MockBehavior.Strict);

            MockRequest.Setup(m => m.Method).Returns("GET");
            MockRequest.Setup(m => m.Url).Returns($"https://{url}/query=data");

            MockRequest.Setup(m => m.RedirectedFrom).Returns((IRequest)null);
            MockRequest.Setup(m => m.RedirectedTo).Returns((IRequest)null);
            MockRequest.Setup(m => m.ResponseAsync()).ReturnsAsync(MockResponse.Object);

            MockResponse.Setup(m => m.Status).Returns(200);

            // Act
            await monitor.MonitorEntraLoginAsync($"https://app.powerapps.com");
            MockBrowserContext.Raise(context => context.RequestFinished += null, args: new object[] { null, MockRequest.Object });


            // Assert
        }

        [Theory]
        [MemberData(nameof(InvalidUrls))]
        public async Task WillNotTrackResponse(string url)
        {
            // Arrange
            var monitor = new NetworkMonitor(MockLogger.Object, MockBrowserContext.Object, MockTestState.Object);

            MockBrowserContext.Setup(m => m.RouteAsync(It.IsAny<string>(), It.IsAny<Func<IRoute, Task>>(), It.IsAny<BrowserContextRouteOptions>()))
               .Returns(Task.CompletedTask);

            MockRoute.Setup(m => m.ContinueAsync(null)).Returns(Task.CompletedTask);
            MockRoute.Setup(m => m.Request).Returns(MockRequest.Object);

            MockLogger = new Mock<ILogger>(MockBehavior.Strict);

            MockRequest.Setup(m => m.Method).Returns("GET");
            MockRequest.Setup(m => m.Url).Returns($"https://{url}/query=data");

            // Act
            await monitor.MonitorEntraLoginAsync($"https://app.powerapps.com");
            MockBrowserContext.Raise(context => context.RequestFinished += null, args: new object[] { null, MockRequest.Object });


            // Assert
        }

        [Theory]
        [InlineData("https://example.com")]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("/page")]
        public async Task NoCookies(string? url)
        {
            // Arrange
            var monitor = new NetworkMonitor(MockLogger.Object, MockBrowserContext.Object, MockTestState.Object);

            MockTestState.Setup(m => m.GetDomain()).Returns(url);
            MockBrowserContext.Setup(m => m.CookiesAsync(It.IsAny<IEnumerable<string>>())).Returns(Task.FromResult((IReadOnlyList<BrowserContextCookiesResult>)null));

            // Act & Assert
            await monitor.LogCookies("");
        }

        internal class RequestEventArgs : EventArgs
        {
            public IRequest? Request { get; set; }
        }
    }
}
