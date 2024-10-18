// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Dynamic;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Moq;
using Newtonsoft.Json;

namespace testengine.module.powerapps.portal.tests
{
    public class ConnectionHelperTests
    {
        private Mock<IPage> MockPage;
        private Mock<IBrowserContext> MockBrowserContext;
        private Mock<ILogger> MockLogger;

        private static string POWER_APPS_PORTAL = "https://make.powerapps.com";

        public ConnectionHelperTests()
        {
            MockPage = new Mock<IPage>(MockBehavior.Strict);
            MockBrowserContext = new Mock<IBrowserContext>(MockBehavior.Strict);
            MockLogger = new Mock<ILogger>();
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
            MockPage.Setup(x => x.Locator(".connections-list", null)).Returns(mockLocator.Object);
            MockPage.Setup(x => x.IsVisibleAsync(".ba-DetailsList-empty", null)).Returns(Task.FromResult((bool)false));
            MockPage.Setup(x => x.IsVisibleAsync(".connections-list-container", null)).Returns(Task.FromResult((bool)true));
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
        public async Task Exists(string name, string status, bool exists)
        {
            // Arrange
            MockBrowserContext.Setup(x => x.NewPageAsync()).Returns(Task.FromResult(MockPage.Object));

            // Goto and return json
            MockPage.Setup(x => x.GotoAsync(new Uri(new Uri("http://make.powerapps.com"), "connections?source=testengine").ToString(), It.IsAny<PageGotoOptions>())).Returns(Task.FromResult(new Mock<IResponse>().Object));
            MockPage.Setup(x => x.AddScriptTagAsync(It.IsAny<PageAddScriptTagOptions>())).Returns(Task.FromResult(new Mock<IElementHandle>().Object));
            var connection = new List<Connection>();
            if (!string.IsNullOrEmpty(name))
            {
                connection.Add(new Connection { Name = name, Status = status });
            }
            MockPage.Setup(x => x.EvaluateAsync<string>(It.IsAny<string>(), null)).Returns(Task.FromResult(System.Text.Json.JsonSerializer.Serialize(connection)));
            MockPage.Setup(x => x.CloseAsync(null)).Returns(Task.CompletedTask);

            // Wait until the container exists
            var mockLocator = new Mock<ILocator>();
            MockPage.Setup(x => x.Locator(".connections-list", null)).Returns(mockLocator.Object);
            MockPage.Setup(x => x.IsVisibleAsync(".ba-DetailsList-empty", null)).Returns(Task.FromResult((bool)false));
            MockPage.Setup(x => x.IsVisibleAsync(".connections-list-container", null)).Returns(Task.FromResult((bool)true));
            mockLocator.Setup(x => x.WaitForAsync(null)).Returns(Task.FromResult(Task.CompletedTask));

            var helper = new ConnectionHelper();

            // Act
            var result = await helper.Exists(MockBrowserContext.Object, "http://make.powerapps.com", name);

            // Assert
            Assert.Equal(exists, result);
        }

        [Theory]
        [MemberData(nameof(UpdateConnectionReferencesData))]
        public async Task UpdateConnectionReferences(string connectionsJson, string instanceUrl, string connectionReferenceId, string connectionId, string connectionReferencesBeforeJson, string connectionReferenceAfterJson)
        {
            // Arrange
            var mockPortalPage = MockPowerAppsPortalGetConnections(connectionsJson, instanceUrl);
            var mockDataversePage = MockDataverseQueryAndUpdateConnectionReferences(instanceUrl, connectionReferenceId, connectionId, connectionReferencesBeforeJson, connectionReferenceAfterJson);

            mockPortalPage.Setup(x => x.IsVisibleAsync(".ba-DetailsList-empty", null)).Returns(Task.FromResult((bool)false));
            mockPortalPage.Setup(x => x.IsVisibleAsync(".connections-list-container", null)).Returns(Task.FromResult((bool)true));

            MockBrowserContext.SetupSequence(x => x.NewPageAsync())
                .Returns(Task.FromResult(mockPortalPage.Object))
                .Returns(Task.FromResult(mockDataversePage.Object));

            var helper = new ConnectionHelper();

            // Act
            await helper.UpdateConnectionReferences(MockBrowserContext.Object, POWER_APPS_PORTAL, MockLogger.Object);

            // Assert
        }

        private Mock<IPage> MockPowerAppsPortalGetConnections(string connectionsJson, string instanceUrl)
        {
            var page = new Mock<IPage>(MockBehavior.Strict);

            // Assume starts a new page and goes to the power apps portal page to request connections
            page.Setup(x => x.GotoAsync(POWER_APPS_PORTAL + "/connections?source=testengine", null)).Returns(Task.FromResult(new Mock<IResponse>().Object));

            // Assume will inject script into page to be able to query connections using PowerAppsPortalConnections.getConnections(
            page.Setup(x => x.AddScriptTagAsync(It.IsAny<PageAddScriptTagOptions>())).Returns(Task.FromResult(new Mock<IElementHandle>().Object));

            // Assume will wait for list of connections to be visable
            var mockLocator = new Mock<ILocator>();
            page.Setup(x => x.Locator(".connections-list", null)).Returns(mockLocator.Object);
            MockPage.Setup(x => x.IsVisibleAsync(".ba-DetailsList-empty", null)).Returns(Task.FromResult((bool)false));
            MockPage.Setup(x => x.IsVisibleAsync(".connections-list-container", null)).Returns(Task.FromResult((bool)true));
            mockLocator.Setup(x => x.WaitForAsync(null)).Returns(Task.CompletedTask);

            // Interaction pattern to open settings dialog and get instace url
            SetupMockSettingDialog(page, instanceUrl);

            // Return the expected json when try get list of connections from the Power Apps Portal Page
            page.Setup(x => x.EvaluateAsync<string>("PowerAppsPortalConnections.getConnections()", null)).Returns(Task.FromResult(connectionsJson.Replace("'", "\"")));

            // Portal page will be closed
            page.Setup(x => x.CloseAsync(null)).Returns(Task.FromResult(Task.CompletedTask));

            return page;
        }

        private Mock<IPage> MockDataverseQueryAndUpdateConnectionReferences(string instanceUrl, string connectionReferenceId, string connectionId, string connectionReferencesBeforeJson, string connectionReferenceAfterJson)
        {
            var page = new Mock<IPage>();

            if (!instanceUrl.EndsWith("/"))
            {
                instanceUrl += "/";
            }

            MockBrowserContext.Setup(x => x.NewPageAsync()).Returns(Task.FromResult(page.Object));

            // Expect will Goto the dataverse instance to query connection references 
            page.Setup(x => x.GotoAsync(new Uri(new Uri(instanceUrl), "main.aspx").ToString(), null)).Returns(Task.FromResult(new Mock<IResponse>().Object));

            // Expect waits for document to be loaded
            page.Setup(x => x.WaitForLoadStateAsync(LoadState.DOMContentLoaded, null)).Returns(Task.CompletedTask);

            // Will make first call to get connection references
            var callBefore = new Mock<IJSHandle>();
            callBefore.Setup(x => x.JsonValueAsync<object>())
                .Returns(Task.FromResult<object>(JsonConvert.DeserializeObject<ExpandoObject>(connectionReferencesBeforeJson.Replace("'", "\""))));

            // Will make second call to check connection references
            var callAfter = new Mock<IJSHandle>();
            callAfter.Setup(x => x.JsonValueAsync<object>())
                .Returns(Task.FromResult<object>(JsonConvert.DeserializeObject<ExpandoObject>(connectionReferenceAfterJson.Replace("'", "\""))));

            page.Setup(x => x.EvaluateHandleAsync(string.Format(ConnectionHelper.QUERY_CONNECTION_REFERENCES, instanceUrl), null))
                .Returns(Task.FromResult(callBefore.Object));

            // Will update connectionreference with connection id
            var javaScript = ConnectionHelper.GetConnectionUpdateJavaScript(instanceUrl, connectionReferenceId, connectionId);
            page.Setup(x => x.EvaluateHandleAsync(javaScript, null))
                .Returns(Task.FromResult(new Mock<IJSHandle>().Object));

            page.Setup(x => x.EvaluateHandleAsync(string.Format(ConnectionHelper.QUERY_CONNECTION_REFERENCES, instanceUrl), null))
                .Returns(Task.FromResult(callAfter.Object));

            return page;
        }

        /// <summary>
        /// Setup interaction pattern to open Settings Dialog and extract the instance url
        /// </summary>
        /// <param name="mockPage">The page to add mock expectatio non</param>
        /// <param name="instanceUrl">The instance url to return</param>
        private void SetupMockSettingDialog(Mock<IPage> mockPage, string instanceUrl)
        {
            mockPage.Setup(x => x.WaitForSelectorAsync("#O365_MainLink_Settings", null)).Returns(Task.FromResult(new Mock<IElementHandle>().Object));
            mockPage.Setup(x => x.ClickAsync("#O365_MainLink_Settings", null)).Returns(Task.FromResult(MockPage.Object));
            mockPage.Setup(x => x.ClickAsync("#sessionDetails-help-menu-item", null)).Returns(Task.FromResult(MockPage.Object));
            mockPage.Setup(x => x.AddScriptTagAsync(It.IsAny<PageAddScriptTagOptions>())).Returns(Task.FromResult(new Mock<IElementHandle>().Object));
            mockPage.Setup(x => x.EvaluateAsync<string>("PowerAppsPortalConnections.getInstanceUrl()", null)).Returns(Task.FromResult(instanceUrl));
            var closeLocator = new Mock<ILocator>();
            closeLocator.Setup(x => x.ClickAsync(null)).Returns(Task.FromResult(Task.CompletedTask));
            mockPage.Setup(x => x.GetByText("Close", null)).Returns(closeLocator.Object);
        }

        public static IEnumerable<object[]> UpdateConnectionReferencesData()
        {
            yield return UpdateConnectionReferencesTestCase(
                    "[{'Id':'A', 'Name':'shared_test', 'Status':'Connected'}]",
                    "https://contoso.crm.dynamics.com",
                    "123",
                    "A",
                    "{'value':[{ 'connectorid': '/api/shared_test', 'connectionreferenceid': '123' }]}",
                    "{'value':[{ 'connectorid': '/api/shared_test', 'connectionreferenceid': '123', connectionid: 'A' }]}"
            );
        }
        private static object[] UpdateConnectionReferencesTestCase(string connectionsJson, string instanceUrl, string connectionReferenceId, string connectionId, string connectionReferencesBeforeJson, string connectionReferenceAfterJson)
        {
            return new object[] {
                connectionsJson,
                instanceUrl,
                connectionReferenceId,
                connectionId,
                connectionReferencesBeforeJson,
                connectionReferenceAfterJson };
        }
    }
}
