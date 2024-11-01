// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Dynamic;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
using Newtonsoft.Json.Linq;

namespace testengine.module
{
    /// <summary>
    /// This will execute Simulate connector request to Power Platform
    /// </summary>
    public class SimulateConnectorFunction : ReflectionFunction
    {
        private readonly ITestInfraFunctions _testInfraFunctions;
        private readonly ITestState _testState;
        private readonly ILogger _logger;

        private static readonly RecordType dataverseRequest = RecordType.Empty();

        public SimulateConnectorFunction(ITestInfraFunctions testInfraFunctions, ITestState testState, ILogger logger)
            : base(DPath.Root.Append(new DName("Experimental")), "SimulateConnector", FormulaType.Blank, dataverseRequest)
        {
            _testInfraFunctions = testInfraFunctions;
            _testState = testState;
            _logger = logger;
        }

        public BlankValue Execute(RecordValue intercept)
        {
            ExecuteAsync(intercept).Wait();
            return FormulaValue.NewBlank();
        }

        private async Task ExecuteAsync(RecordValue intercept)
        {
            _logger.LogInformation("------------------------------\n\n" +
               "Executing SimulateConnector function.");

            List<NamedValue> fields = new List<NamedValue>();

            await foreach (var field in intercept.GetFieldsAsync(CancellationToken.None))
            {
                fields.Add(field);
            }

            StringValue connectorName = FormulaValue.New("");

            var connectorNameField = fields.FirstOrDefault(f => f.Name.ToLower() == "name");
            if (connectorNameField != null)
            {
                connectorName = intercept.GetField(connectorNameField.Name) as StringValue;
            }

            if (String.IsNullOrEmpty(connectorName.Value))
            {
                throw new InvalidDataException("Missing field name");
            }

            RecordValue parametersRecord = RecordValue.Empty();

            var parametersField = fields.FirstOrDefault(f => f.Name.ToLower() == "parameters");
            if (parametersField != null)
            {
                parametersRecord = intercept.GetField(parametersField.Name) as RecordValue;
            }

            TableValue thenResult = null;

            var thenField = fields.FirstOrDefault(f => f.Name.ToLower() == "then");
            if (thenField != null)
            {
                thenResult = intercept.GetField(thenField.Name) as TableValue;
            }

            // TODO handle case Then field is a record rather than a Table

            if (thenResult == null)
            {
                throw new InvalidDataException("Missing field then");
            }

            await _testInfraFunctions.Page.RouteAsync($"**/invoke", async (IRoute route) =>
            {
                var request = route.Request;
                if (request.Method == "POST")
                {
                    await HandleConnectorRequest(route, connectorName.Value, parametersRecord, thenResult);
                }
                else
                {
                    await route.ContinueAsync();
                }
            });
        }

        private async Task<string> ConvertTableToJson(TableValue value)
        {
            // Convert the TableValue to JSON
            var data = await ConvertToObject(value);
            var responseData = new Dictionary<string, object?>();
            responseData["value"] = data;

            var jObject = JObject.FromObject(responseData);
            string jsonString = jObject.ToString(Newtonsoft.Json.Formatting.None);

            return jsonString;
        }

        private async Task HandleConnectorRequest(IRoute route, string connector, RecordValue parameters, TableValue thenResult)
        {
            var request = route.Request.PostData;

            // Find out what connector the request belongs to
            KeyValuePair<string, string>? requestUrl = route.Request.Headers.Where(h => h.Key == "x-ms-request-url").FirstOrDefault();

            if (requestUrl == null || string.IsNullOrEmpty(requestUrl?.Value))
            {
                // This request is not for a Power Platform connector
                await route.ContinueAsync();
                return;
            }

            string connectorUrl = requestUrl?.Value;

            var parts = GetQueryParameters(new Uri(connectorUrl, UriKind.Relative));

            if (!connectorUrl.ToLower().StartsWith($"/apim/{connector.ToLower()}"))
            {
                // This connector request is not one we are looking for ... continue on
                await route.ContinueAsync();
                return;
            }

            // TODO: Handle parameter match

            await route.FulfillAsync(new RouteFulfillOptions
            {
                Status = 200,
                ContentType = "application/json",
                Body = await ConvertTableToJson(thenResult)
            });
        }


        public Dictionary<string, string> GetQueryParameters(Uri uri)
        {
            // Convert the relative URI to an absolute URI so can get Query
            Uri absoluteUri = new Uri(new Uri("https://example"), uri);

            var queryParameters = new Dictionary<string, string>();
            string query = absoluteUri.Query.TrimStart('?');
            string[] pairs = query.Split('&');

            foreach (string pair in pairs)
            {
                string[] keyValue = pair.Split('=');
                if (keyValue.Length == 2)
                {
                    string key = Uri.UnescapeDataString(keyValue[0]);
                    string value = Uri.UnescapeDataString(keyValue[1]);
                    queryParameters[key] = value;
                }
            }

            return queryParameters;
        }

        private async Task<List<object>> ConvertToObject(TableValue value)
        {
            var data = new List<object>();
            foreach (var item in value.Rows)
            {
                var row = new Dictionary<string, object?>(new ExpandoObject());

                await foreach (var field in item.Value.GetFieldsAsync(CancellationToken.None))
                {
                    if (field.Value.TryGetPrimitiveValue(out object val))
                    {
                        row.Add(field.Name, val);
                        continue;
                    }

                    // TODO: Handle complex non primative types
                }

                data.Add(row);
            }
            return data;
        }
    }
}

