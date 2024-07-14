// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Reflection;
using System.Text.Json;
using Microsoft.Playwright;

namespace testengine.module.powerapps.portal
{
    /// <summary>
    /// Common functions to get connection information from the logged in Power App portal
    /// </summary>
    public class ConnectionHelper
    {
        /// <summary>
        /// Get list of all connections with status
        /// </summary>
        /// <param name="context">The authenticated browser session</param>
        /// <param name="domain">The base Power Aps portal domain to query for connections</param>
        /// <returns>Matching connections</returns>
        public virtual async Task<List<Connection>?> GetConnections(IBrowserContext context, string domain)
        {
            var page = await context.NewPageAsync();

            var url = new Uri(new Uri(domain), "/connections?source=testengine").ToString();

            await page.GotoAsync(url);

            await page.AddScriptTagAsync(new PageAddScriptTagOptions { Content = LoadResource("PowerAppsPortalConnections.js") });

            await page.Locator(".connections-list-container").WaitForAsync();

            var connectionsJson = await page.EvaluateAsync<string>("PowerAppsPortalConnections.getConnections()");

            await page.CloseAsync();

            return JsonSerializer.Deserialize<List<Connection>>(connectionsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        /// <summary>
        /// Check if a connected connection exists in the Power Apps Portal for the authenicated system user
        /// </summary>
        /// <param name="context">The authenticated browser session</param>
        /// <param name="domain">The base Power Aps portal domain to query for connections</param>
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
