// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Text.Json;
using Microsoft.Playwright;
using Moq;

namespace testengine.module.powerapps.portal.tests
{
    public class ConnectionHelperTests
    {
        private Mock<IPage> MockPage;
        private Mock<IBrowserContext> MockBrowserContext;

        public ConnectionHelperTests()
        {
            MockPage = new Mock<IPage>(MockBehavior.Strict);
            MockBrowserContext = new Mock<IBrowserContext>(MockBehavior.Strict);
        }

        [Theory]
        [InlineData("[]", "http://make.powerapps.com", 0)]
        [InlineData("[]", "http://make.powerapps.com/", 0)]
        [InlineData("[{\"Name\":\"Test\",\"Id\":\"\",\"Status\":\"\"}]", "http://make.powerapps.com/", 1)]
        public async Task ExecuteGetConnections(string json, string baseDomain, int expectedCount)
        {
            // Arrange
            MockBrowserContext.Setup(x => x.NewPageAsync()).Returns(Task.FromResult(MockPage.Object));

            // Goto and return json
            MockPage.Setup(x => x.GotoAsync(new Uri(new Uri(baseDomain), "connections?source=testengine").ToString(), It.IsAny<PageGotoOptions>())).Returns(Task.FromResult(new Mock<IResponse>().Object));
            MockPage.Setup(x => x.AddScriptTagAsync(It.IsAny<PageAddScriptTagOptions>())).Returns(Task.FromResult(new Mock<IElementHandle>().Object));
            MockPage.Setup(x => x.EvaluateAsync<string>(It.IsAny<string>(), null)).Returns(Task.FromResult(json));
            MockPage.Setup(x => x.CloseAsync(null)).Returns(Task.CompletedTask);

            // Wait until the container exists
            var mockLocator = new Mock<ILocator>();
            MockPage.Setup(x => x.Locator(".connections-list-container", null)).Returns(mockLocator.Object);
            mockLocator.Setup(x => x.WaitForAsync(null)).Returns(Task.FromResult(Task.CompletedTask));

            var helper = new ConnectionHelper();

            // Act
            var result = await helper.GetConnections(MockBrowserContext.Object, baseDomain);

            // Assert
            Assert.Equal(expectedCount, result.Count());
        }

        [Theory]
        [InlineData("", "", false)]
        [InlineData("Test", "", false)]
        [InlineData("Test", "Connected", true)]
        public async Task Exists(string name, string status,  bool exists)
        {
            // Arrange
            MockBrowserContext.Setup(x => x.NewPageAsync()).Returns(Task.FromResult(MockPage.Object));

            // Goto and return json
            MockPage.Setup(x => x.GotoAsync(new Uri(new Uri("http://make.powerapps.com"), "connections?source=testengine").ToString(), It.IsAny<PageGotoOptions>())).Returns(Task.FromResult(new Mock<IResponse>().Object));
            MockPage.Setup(x => x.AddScriptTagAsync(It.IsAny<PageAddScriptTagOptions>())).Returns(Task.FromResult(new Mock<IElementHandle>().Object));
            var connection = new List<Connection>();
            if (!string.IsNullOrEmpty(name)) {
                connection.Add(new Connection { Name = name, Status = status });
            }
            MockPage.Setup(x => x.EvaluateAsync<string>(It.IsAny<string>(), null)).Returns(Task.FromResult(JsonSerializer.Serialize(connection)));
            MockPage.Setup(x => x.CloseAsync(null)).Returns(Task.CompletedTask);

            // Wait until the container exists
            var mockLocator = new Mock<ILocator>();
            MockPage.Setup(x => x.Locator(".connections-list-container", null)).Returns(mockLocator.Object);
            mockLocator.Setup(x => x.WaitForAsync(null)).Returns(Task.FromResult(Task.CompletedTask));

            var helper = new ConnectionHelper();

            // Act
            var result = await helper.Exists(MockBrowserContext.Object, "http://make.powerapps.com", name);

            // Assert
            Assert.Equal(exists, result);
        }
    }
}
