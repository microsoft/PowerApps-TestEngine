// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Dynamic;
using System.Text;
using System.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.Playwright.Core;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

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

            if (string.IsNullOrEmpty(connectorName.Value))
            {
                throw new InvalidDataException("Missing field name");
            }

            RecordValue parametersRecord = RecordValue.Empty();

            var parametersField = fields.FirstOrDefault(f => f.Name.ToLower() == "parameters");
            if (parametersField != null)
            {
                parametersRecord = intercept.GetField(parametersField.Name) as RecordValue;
            }

            FormulaValue thenFieldValue = null;

            var thenField = fields.FirstOrDefault(f => f.Name.ToLower() == "then");
            if (thenField != null)
            {
                thenFieldValue = intercept.GetField(thenField.Name);

            }

            // TODO handle case Then field is a record rather than a Table

            if (thenFieldValue == null)
            {
                throw new InvalidDataException("Missing field then");
            }

            var whenField = fields.FirstOrDefault(f => f.Name.ToLower() == "when");
            FormulaValue whenFieldValue = null;
            if (whenField != null)
            {
                whenFieldValue = intercept.GetField(whenField.Name);
            }

            await _testInfraFunctions.Page.RouteAsync($"**/invoke", async (IRoute route) =>
            {
                var request = route.Request;
                if (request.Method == "POST")
                {
                    await HandleConnectorRequest(route, connectorName.Value, parametersRecord, whenFieldValue, thenFieldValue);
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

        private async Task HandleConnectorRequest(IRoute route, string connector, RecordValue parameters, FormulaValue whenField, FormulaValue thenResult)
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

            var isMatch = true;
            if (whenField != null)
            {
                if (whenField is RecordValue)
                {
                    var recordValue = whenField as RecordValue;
                    if (!await RecordValueMatch(connectorUrl, recordValue))
                    {
                        isMatch = false;
                    }
                }

                if (whenField is TableValue)
                {
                    var tableValue = whenField as TableValue;
                    foreach (var row in tableValue.Rows)
                    {
                        if (!await RecordValueMatch(connectorUrl, row.Value))
                        {
                            isMatch = false;
                        }
                    }
                }

                if (whenField.TryGetPrimitiveValue(out var value))
                {
                    if (!connectorUrl.Contains($"{value}"))
                    {
                        isMatch = false;
                    }
                }
            }

            if (!isMatch)
            {
                // This connector request is not one we are looking for ... continue on
                await route.ContinueAsync();
                return;
            }

            // TODO: Handle parameter match
            // Convert Power Fx expression to OData filter

            await route.FulfillAsync(new RouteFulfillOptions
            {
                Status = 200,
                ContentType = "application/json",
                Body = await ConvertToJson(thenResult)
            });
        }

        private async Task<bool> RecordValueMatch(string connectorUrl, RecordValue recordValue)
        {
            await foreach (var field in recordValue.GetFieldsAsync(CancellationToken.None))
            {
                switch (field.Name.ToLower())
                {
                    case "action":
                        if (field.Value is BlankValue)
                        {
                            continue;
                        }
                        if (field.Value.TryGetPrimitiveValue(out object actionField))
                        {
                            var actionValue = actionField.ToString();
                            if (!connectorUrl.Contains($"{actionValue}"))
                            {
                                return false;
                            }
                        }
                        break;
                    case "filter":
                        if (field.Value is BlankValue)
                        {
                            continue;
                        }
                        if (field.Value.TryGetPrimitiveValue(out object filterField))
                        {
                            var filterValue = filterField.ToString();
                            var odataFilter = ConvertPowerFxToODataFilter(filterValue);
                            if (!connectorUrl.Contains($"{odataFilter}"))
                            {
                                return false;
                            }
                        }
                        break;
                }
            }
            return true;
        }

        private string ConvertPowerFxToODataFilter(string powerFxExpression)
        {
            StringBuilder result = new StringBuilder();
            result.Append("$filter=");

            var engine = new RecalcEngine();
            var parsed = engine.Parse(powerFxExpression);

            ConvertNodeToOData(parsed.Root, result);

            var encoded = HttpUtility.UrlEncode(result.ToString());
            return encoded.Replace("%3d", "=");
        }

        private void ConvertNodeToOData(TexlNode node, StringBuilder sb)
        {
            if (node == null) return;

            switch (node.Kind)
            {
                case NodeKind.BinaryOp:
                    var binaryNode = (BinaryOpNode)node;
                    sb.Append("(");
                    ConvertNodeToOData(binaryNode.Left, sb);
                    sb.Append($" {GetODataOperator(binaryNode.Op)} ");
                    ConvertNodeToOData(binaryNode.Right, sb);
                    sb.Append(")");
                    break;

                case NodeKind.FirstName:
                    var firstNode = (FirstNameNode)node;
                    sb.Append(firstNode.Ident.Name);
                    break;

                case NodeKind.StrLit:
                    var strLitNode = (StrLitNode)node;
                    sb.Append($"'{strLitNode.Value}'");
                    break;

                case NodeKind.DecLit:
                    var decLitNode = (DecLitNode)node;
                    sb.Append(decLitNode.ActualDecValue);
                    break;

                case NodeKind.Call:
                    var callNode = (CallNode)node;
                    switch (callNode.Head.Name)
                    {
                        case "AND":
                            ConvertNodeToOData(callNode.Args.ChildNodes[0], sb);
                            sb.Append(" and ");
                            ConvertNodeToOData(callNode.Args.ChildNodes[1], sb);
                            break;
                        case "OR":
                            ConvertNodeToOData(callNode.Args.ChildNodes[0], sb);
                            sb.Append(" or ");
                            ConvertNodeToOData(callNode.Args.ChildNodes[1], sb);
                            break;
                        case "NOT":
                            sb.Append("not ");
                            ConvertNodeToOData(callNode.Args.ChildNodes[0], sb);
                            break;
                        default:
                            throw new NotSupportedException($"Call {callNode.Head.Name} is not supported.");
                    }

                    break;

                //case NodeKind.UnaryOp:
                //    var unaryNode = (UnaryOpNode)node;
                //    sb.Append(GetODataOperator(unaryNode.));
                //    ConvertNodeToOData(unaryNode.Operand, sb);
                //    break;

                //case NodeKind.Constant:
                //    var constantNode = (ConstantNode)node;
                //    sb.Append(constantNode.Value);
                //    break;

                //case NodeKind.Field:
                //    var fieldNode = (FieldNode)node;
                //    sb.Append(fieldNode.Name);
                //    break;

                // Add more cases as needed for other node types

                default:
                    throw new NotSupportedException($"Node kind {node.Kind} is not supported.");
            }
        }

        private string GetODataOperator(BinaryOp op)
        {
            return op switch
            {
                BinaryOp.And => "and",
                BinaryOp.Or => "or",
                BinaryOp.Equal => "eq",
                BinaryOp.NotEqual => "ne",

                BinaryOp.Greater => "gt",
                BinaryOp.GreaterEqual => "ge",
                BinaryOp.Less => "lt",
                BinaryOp.LessEqual => "le",
                _ => throw new NotSupportedException($"Binary operator {op} is not supported.")
            };
        }

        private async Task<string> ConvertToJson(FormulaValue thenResult)
        {
            if (thenResult is TableValue thenValue)
            {
                return await ConvertTableToJson(thenValue);
            }

            var stack = new Stack<(string, FormulaValue, Dictionary<string, object?>)>();
            var root = new Dictionary<string, object?>(new ExpandoObject());
            stack.Push(("root", thenResult, root));

            while (stack.Count > 0)
            {
                var (key, value, parent) = stack.Pop();

                if (value is RecordValue record)
                {
                    var row = new Dictionary<string, object?>(new ExpandoObject());
                    await foreach (var field in record.GetFieldsAsync(CancellationToken.None))
                    {
                        if (field.Value.TryGetPrimitiveValue(out object val))
                        {
                            row.Add(field.Name, val);
                        }
                        else
                        {
                            stack.Push((field.Name, field.Value, row));
                        }
                    }
                    parent[key] = row;
                }
                else if (value is TableValue table)
                {
                    var tableList = new List<Dictionary<string, object?>>();
                    var rows = table.Rows;
                    foreach (DValue<RecordValue> row in rows)
                    {
                        var rowDict = new Dictionary<string, object?>(new ExpandoObject());

                        if (row.IsValue)
                        {
                            await foreach (var field in row.Value.GetFieldsAsync(CancellationToken.None))
                            {
                                if (field.Value.TryGetPrimitiveValue(out object val))
                                {
                                    rowDict.Add(field.Name, val);
                                }
                                else
                                {
                                    stack.Push((field.Name, field.Value, rowDict));
                                }
                            }
                            tableList.Add(rowDict);
                        }
                    }
                    parent[key] = tableList;
                }
                else if (value.TryGetPrimitiveValue(out object primitiveVal))
                {
                    parent[key] = primitiveVal;
                }
            }

            if (root.Keys.Count == 1)
            {
                return JsonConvert.SerializeObject(root["root"], Formatting.None);
            }

            if (root.ContainsKey("root"))
            {
                root.Remove("root");
            }

            return JsonConvert.SerializeObject(root, Formatting.None);
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

