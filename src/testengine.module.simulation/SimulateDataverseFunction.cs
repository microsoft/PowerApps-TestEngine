// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Dynamic;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static System.Net.Mime.MediaTypeNames;
using static System.Net.WebRequestMethods;

namespace testengine.module
{
    /// <summary>
    /// This will execute Simulate request to Dataverse
    /// </summary>
    public class SimulateDataverseFunction : ReflectionFunction
    {
        private readonly ITestInfraFunctions _testInfraFunctions;
        private readonly ITestState _testState;
        private readonly ILogger _logger;

        private static readonly RecordType dataverseRequest = RecordType.Empty();

        public SimulateDataverseFunction(ITestInfraFunctions testInfraFunctions, ITestState testState, ILogger logger)
            : base(DPath.Root.Append(new DName("Preview")), "SimulateDataverse", FormulaType.Blank, dataverseRequest)
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
               "Executing SimulateDataverse function.");

            List<NamedValue> fields = new List<NamedValue>();

            await foreach (var field in intercept.GetFieldsAsync(CancellationToken.None))
            {
                fields.Add(field);
            }

            StringValue actionName = FormulaValue.New("");

            var actionField = fields.FirstOrDefault(f => f.Name.ToLower() == "action");
            if (actionField != null)
            {
                actionName = intercept.GetField(actionField.Name) as StringValue;
            }

            if (String.IsNullOrEmpty(actionName.Value))
            {
                throw new InvalidDataException("Missing field action");
            }

            switch (actionName.Value.ToLower())
            {
                case "query":
                    break;
                default:
                    throw new InvalidDataException($"Unsupported action {actionName.Value}");
            }

            StringValue entityName = FormulaValue.New("");

            var entityField = fields.FirstOrDefault(f => f.Name.ToLower() == "entity");
            if (entityField != null)
            {
                entityName = intercept.GetField(entityField.Name) as StringValue;
            }

            if (String.IsNullOrEmpty(entityName.Value))
            {
                throw new InvalidDataException("Missing field Entity");
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

            await _testInfraFunctions.Page.RouteAsync($"**/api/data/v*/$batch", async (IRoute route) =>
            {
                var request = route.Request;
                if (request.Method == "POST")
                {
                    await HandleBatchRequest(route, entityName.Value, thenResult);
                }
                else
                {
                    await route.ContinueAsync();
                }
            });

            // TODO: Handle When field
            // 1. Convert When field to fetchXML or $filter clause

            // TODO:
            // 1. Handle request for entity vs list
            // 2. Make the route async conditional on query string for different When clause 
            await _testInfraFunctions.Page.RouteAsync($"**/api/data/v*/{entityName.Value.ToLower()}*", async (IRoute route) =>
            {
                var request = route.Request;

                // TODO: Handle POST and PATCH commands
                // 1. POST to add new Record. Add POST record to collection so caller can Assert
                // 2. PATCH to update Record. Add Patched record to collection so caller can Assert
                if (request.Method == "GET")
                {
                    _logger.LogDebug($"Simulated Dataverse GET request {route.Request.Url}");
                    // TODO: 
                    // 1. Check for Query conditions and if this request if a match
                    // 2. Handle multiple GET requests based on query string - fetchXML (From MDA)
                    // 3. Handle multiple GET requests based on query string - $filter clause (From connector)

                    // Generate a new response with HTTP body for OData call
                    await route.FulfillAsync(new RouteFulfillOptions
                    {
                        Status = 200,
                        ContentType = "application/json",
                        Body = await ConvertTableToOData(thenResult)
                    });

                    // TODO: Handle count of simulation to conditionally UnRoute the request
                }
                else
                {
                    await route.ContinueAsync();
                }
            });
        }

        private async Task<string> ConvertTableToOData(TableValue value)
        {
            // Convert the TableValue to JSON
            var data = await ConvertToObject(value);
            var responseData = new Dictionary<string, object?>();
            responseData["@odata.count"] = data.Count();
            responseData["@Microsoft.Dynamics.CRM.totalrecordcount"] = data.Count();
            responseData["@Microsoft.Dynamics.CRM.totalrecordcountlimitexceeded"] = false;
            responseData["value"] = data;

            var jObject = JObject.FromObject(responseData);
            string jsonString = jObject.ToString(Newtonsoft.Json.Formatting.None);

            return jsonString;
        }

        private async Task HandleBatchRequest(IRoute route, string entity, TableValue value)
        {
            var request = route.Request.PostData;

            var id = GetRequestId(request);
            var uri = GetRequestUri(request);

            var parts = GetQueryParameters(uri);

            if (parts.ContainsKey("apply"))
            {
                /* Example request
             
                --batch_a3faec5d-befd-4ca0-973e-8dcbfc0981ca
                Content-Type: application/http
                Content-Transfer-Encoding: binary

                GET accounts?%24apply=aggregate%28%24count+as+result%29 HTTP/1.1
                Accept: application/json

                --batch_a3faec5d-befd-4ca0-973e-8dcbfc0981ca--
                */
                await HandleBatchCount(route, id, entity, value);
            }
            else
            {
                /* Example Request
                --batch_31b8538b-fdf5-4d97-b78f-83517f4ef163
                Content-Type: application/http
                Content-Transfer-Encoding: binary

                GET accounts?%24select=accountid%2Caccountnumber%2Centityimage%2Cemailaddress1%2Centityimage_timestamp&%24count=true HTTP/1.1
                Accept: application/json
                Prefer: odata.maxpagesize=100,odata.include-annotations=*

                --batch_31b8538b-fdf5-4d97-b78f-83517f4ef163--
                */
                await HandleBatchResults(route, id, entity, value);
            }
        }

        private async Task HandleBatchResults(IRoute route, string id, string entity, TableValue value)
        {
            /*
            --batchresponse_31b8538b-fdf5-4d97-b78f-83517f4ef163
            Content-Type: application/http
            Content-Transfer-Encoding: binary

            HTTP/1.1 200 OK
            Content-Type: application/json; odata.metadata=minimal
            OData-Version: 4.0
            Preference-Applied: odata.include-annotations="*",odata.maxpagesize=100

            {"@odata.context":"https://contoso.crm.dynamics.com/api/data/v9.0/$metadata#accounts(accountid,emailaddress1,entityimage,name,entityimage_timestamp)","@odata.count":2,"@Microsoft.Dynamics.CRM.totalrecordcount":2,"@Microsoft.Dynamics.CRM.totalrecordcountlimitexceeded":false,"@Microsoft.Dynamics.CRM.globalmetadataversion":"3073697","value":[{"@odata.etag":"W/\"3073589\"","accountid":"751d2108-5896-ef11-8a6a-6045bd02adee","emailaddress1":null,"entityimage":null,"name":"New Org","entityimage_timestamp":null},{"@odata.etag":"W/\"2446917\"","accountid":"8a2a5404-9753-ef11-a316-6045bd05082d","emailaddress1":null,"entityimage":null,"name":"Microsoft","entityimage_timestamp":null}]}
            --batchresponse_31b8538b-fdf5-4d97-b78f-83517f4ef163--
            */

            var multilineText = @"--batchresponse_{id}
Content-Type: application/http
Content-Transfer-Encoding: binary

HTTP/1.1 200 OK
Content-Type: application/json; odata.metadata=minimal
OData-Version: 4.0
Preference-Applied: odata.include-annotations=""*"",odata.maxpagesize=100

{json}
--batchresponse_{id}--";

            StringBuilder sb = new StringBuilder(multilineText);
            sb.Replace("{id}", id);
            sb.Replace("{entity}", entity.ToLower());
            sb.Replace("{json}", await ConvertTableToOData(value));

            await route.FulfillAsync(new RouteFulfillOptions
            {
                Status = 200,
                ContentType = $"multipart /mixed; boundary=batchresponse_{id}",
                Body = sb.ToString()
            });
        }

        private async Task HandleBatchCount(IRoute route, string id, string entity, TableValue value)
        {
            /*
           --batchresponse_a3faec5d-befd-4ca0-973e-8dcbfc0981ca
           Content-Type: application/http
           Content-Transfer-Encoding: binary

           HTTP/1.1 200 OK
           Content-Type: application/json; odata.metadata=minimal
           OData-Version: 4.0

           {"@odata.context":"https://contoso.crm.dynamics.com/api/data/v9.0/$metadata#accounts","value":[{"result":2}]}
           --batchresponse_a3faec5d-befd-4ca0-973e-8dcbfc0981ca--
            */

            var multilineText = @"--batchresponse_{id}
Content-Type: application/http
Content-Transfer-Encoding: binary

HTTP/1.1 200 OK
Content-Type: application/json; odata.metadata=minimal
OData-Version: 4.0

{""@odata.context"":""https://{host}/api/data/v9.0/$metadata#{entity}"",""value"":[{""result"":{count}}]}
--batchresponse_{id}--";

            StringBuilder sb = new StringBuilder(multilineText);
            sb.Replace("{id}", id);
            sb.Replace("{entity}", entity.ToLower());
            sb.Replace("{count}", value.Rows.Count().ToString());

            await route.FulfillAsync(new RouteFulfillOptions
            {
                Status = 200,
                ContentType = "application/json",
                Body = sb.ToString()
            });
        }

        private string GetRequestId(string request)
        {
            using (var reader = new StringReader(request))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("--batch_"))
                    {
                        var id = line.Replace("--batch_", "");
                        return id;
                    }
                }
            }
            return String.Empty;

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

        private Uri? GetRequestUri(string request)
        {
            using (var reader = new StringReader(request))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("GET "))
                    {
                        var text = line.Replace("GET ", "");
                        text = text.Replace(" HTTPS/1.1", "");
                        return new Uri(text, UriKind.Relative);
                    }
                }
            }
            return null;
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

                row.Add("@odata.etag", GenerateETag(row));

                data.Add(row);
            }
            return data;
        }

        private string GenerateETag(object jsonObject)
        {
            // Serialize the JSON object to a string
            string jsonString = JsonConvert.SerializeObject(jsonObject);

            // Compute the hash of the JSON string
            using (var sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(jsonString));
                return Convert.ToBase64String(hash);
            }
        }
    }
}

