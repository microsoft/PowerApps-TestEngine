// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace testengine.module.powerapps.portal
{
    /// <summary>
    /// Common functions to get connection information from the logged in Power App portal
    /// </summary>
    public class ConnectionHelper
    {
        public static string QUERY_CONNECTION_REFERENCES = "fetch('{0}api/data/v9.2/connectionreferences?$filter=connectionid%20eq%20null&$select=connectorid').then(response => response.json())";
        public static Func<string, string, string, string> GetConnectionUpdateJavaScript = (instanceUrl, connectionreferenceid, id) =>
        {
            if (!instanceUrl.EndsWith("/"))
            {
                instanceUrl += "/";
            }
            return $"fetch('{instanceUrl}api/data/v9.2/connectionreferences({connectionreferenceid})', {{ method:'PATCH',body: JSON.stringify({{connectionid:'{id}', statuscode: 0 }}), headers: {{ 'Content-type': 'application/json; charset=UTF-8' }} }})";
        };

        /// <summary>
        /// Get list of all connections with status
        /// </summary>
        /// <param name="context">The authenticated browser session</param>
        /// <param name="domain">The base Power Apps portal domain to query for connections</param>
        /// <returns>Matching connections</returns>
        public virtual async Task<List<Connection>?> GetConnections(IBrowserContext context, string domain, Func<IPage, Task> lookup = null)
        {
            var page = await context.NewPageAsync();

            var url = new Uri(new Uri(domain), "/connections?source=testengine").ToString();

            await page.GotoAsync(url);

            await page.AddScriptTagAsync(new PageAddScriptTagOptions { Content = LoadResource("PowerAppsPortalConnections.js") });

            await page.Locator(".connections-list-container").WaitForAsync();

            var connectionsJson = await page.EvaluateAsync<string>("PowerAppsPortalConnections.getConnections()");

            if ( lookup != null )
            {
                await lookup(page);
            }

            await page.CloseAsync();

            return JsonSerializer.Deserialize<List<Connection>>(connectionsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        /// <summary>
        /// Updates all connection referebces with Connected connections if they exist
        /// </summary>
        /// <param name="context">The authenticated browser session</param>
        /// <param name="domain">The base Power Apps portal domain to query for connections</param>
        public virtual async Task UpdateConnectionReferences(IBrowserContext context, string domain, ILogger logger)
        {
            var instanceUrl = "";

            var connections = await GetConnections(context, domain, async (connectionPage) =>
            {
                logger.LogInformation("Waiting for settings");
                await connectionPage.WaitForSelectorAsync("#O365_MainLink_Settings");

                await connectionPage.ClickAsync("#O365_MainLink_Settings");

                await connectionPage.ClickAsync("#sessionDetails-help-menu-item");

                await connectionPage.AddScriptTagAsync(new PageAddScriptTagOptions { Content = LoadResource("PowerAppsPortalConnections.js") });

                instanceUrl = await connectionPage.EvaluateAsync<string>("PowerAppsPortalConnections.getInstanceUrl()");

                await connectionPage.GetByText("Close").ClickAsync();
            } );

            var page = await context.NewPageAsync();          
            if ( instanceUrl.Length > 0 && !instanceUrl.EndsWith("/") )
            {
                instanceUrl += "/";
            }

            var url = $"{instanceUrl}main.aspx";

            await page.GotoAsync(url);
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            logger.LogInformation("Query connection references");
            var connectionReferences = await page.EvaluateHandleAsync(string.Format(QUERY_CONNECTION_REFERENCES, instanceUrl));

            dynamic connectionReferencesData = await connectionReferences.JsonValueAsync<object>();

            foreach ( var reference in connectionReferencesData.value ) {
                var parts = reference.connectorid.Split(new[] { '/' });
                var connectorName = parts[parts.Length - 1];
                var match = connections.Where(c => c.Name == connectorName && c.Status == "Connected").FirstOrDefault();
                if (match != null)
                {
                    logger.LogInformation($"Updating connection for {connectorName}");
                    var script = GetConnectionUpdateJavaScript(instanceUrl, reference.connectionreferenceid, match.Id);
                    await page.EvaluateHandleAsync(script);
                }
            }

            connectionReferences = await page.EvaluateHandleAsync(string.Format(QUERY_CONNECTION_REFERENCES, instanceUrl));
            connectionReferencesData = await connectionReferences.JsonValueAsync<object>();

            var values = new List<dynamic>(connectionReferencesData.value);

            logger.LogInformation($"{values.Count()} connection references with missing connection remaining");
        }

        /// <summary>
        /// Check if a connected connection exists in the Power Apps Portal for the authenicated system user
        /// </summary>
        /// <param name="context">The authenticated browser session</param>
        /// <param name="domain">The base Power Apps portal domain to query for connections</param>
        /// <param name="name">The name of the connection to search for</param>
        /// <returns><c>True</c> if a Connected connection found, <c>False</c> if not</returns>

        public virtual async Task<bool> Exists(IBrowserContext context, string domain, string name)
        {
            var connections = await GetConnections(context, domain);

            //TODO: Localize the connected status
            return connections.Any(x => x.Name == name && x.Status == "Connected");
        }

        private string LoadResource(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"testengine.module.powerapps.portal.{name}";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
