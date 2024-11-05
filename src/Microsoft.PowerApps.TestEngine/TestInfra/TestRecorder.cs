﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Web;
using Microsoft.Data.Edm.Library;
using Microsoft.Data.OData.Query;
using Microsoft.Data.OData.Query.SemanticAst;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;
using Newtonsoft.Json.Linq;
using static System.Net.WebRequestMethods;

namespace Microsoft.PowerApps.TestEngine.TestInfra
{
    ///<summary>
    /// The TestRecorder class is designed to generate and record test steps for the current test session.
    /// This includes network interaction for Dataverse and Connectors, as well as user interaction via Keyboard and Mouse.
    ///</summary>
    public class TestRecorder
    {
        private readonly ILogger _logger;
        private readonly IBrowserContext _browserContext;
        private readonly ITestState _testState;
        private readonly ITestInfraFunctions _infra;
        private readonly RecalcEngine _recalcEngine;
        private readonly IFileSystem _fileSystem;
        private readonly StringBuilder _textBuilder;

        public ConcurrentBag<string> TestSteps = new ConcurrentBag<string>();

        ///<summary>
        /// Initializes a new instance of the TestRecorder class.
        ///</summary>
        ///<param name="logger">The logger instance for logging information.</param>
        ///<param name="browserContext">The browser context for Playwright interactions.</param>
        ///<param name="testState">The current test state.</param>
        ///<param name="infra">The infrastructure functions providing access to the current page.</param>
        ///<param name="recalcEngine">The recalc engine representing the current test state of controls, properties, variables, and collections.</param>
        ///<param name="fileSystem">The file system interface for interacting with the file system.</param>
        public TestRecorder(ILogger logger, IBrowserContext browserContext, ITestState testState, ITestInfraFunctions infra, RecalcEngine recalcEngine, IFileSystem fileSystem)
        {
            _logger = logger;
            _browserContext = browserContext;
            _testState = testState;
            _infra = infra;
            _recalcEngine = recalcEngine;
            _fileSystem = fileSystem;
            _textBuilder = new StringBuilder();
        }

        ///<summary>
        /// Sets up the TestRecorder by subscribing to browser and page events.
        ///</summary>
        public void Setup()
        {
            _browserContext.Response += OnResponse;

            //TODO: Subscribe to key down events from the page


            //TODO: Subscribe to mouse click events from the page
        }

        private void OnResponse(object sender, IResponse e)
        {
            Task.Factory.StartNew(async () => await HandleResponse(e));
        }

        private async Task HandleResponse(IResponse response)
        {
            // Check of the request related to a Dataverse connection
            if (response.Request.Url.Contains("/api/data/v"))
            {
                switch (response.Request.Method)
                {
                    case "GET":
                        var entity = GetODataEntity(response.Request.Url);
                        var data = await ConvertODataToFormulaValue(response);
                        // TODO: Check for $filter and convert OData $filter to Filter record and Power Fx expression
                        TestSteps.Add(GenerateDataverseQuery(entity, data));
                        break;
                    case "POST":
                        // TODO Handle create
                        break;
                }
            }

            if (response.Request.Url.Contains("/invoke") && response.Request.Headers.ContainsKey("x-ms-request-url"))
            {
                switch (response.Request.Method)
                {
                    case "POST":
                        var action = GetActionName(response.Request.Headers["x-ms-request-url"]);
                        var when = GetWhenConnectorValue(response.Request.Headers["x-ms-request-url"]);
                        var then = await ConvertJsonResultToFormulaValue(response);
                        TestSteps.Add(GenerateConnector(action, when, then));
                        break;
                }
            }
        }

        private string GenerateConnector(string name, FormulaValue when, FormulaValue then)
        {
            StringBuilder connectorBuilder = new StringBuilder();

            connectorBuilder.Append($"Experimental.SimulateConnector({{Name: \"{name}\"");


            if (when is RecordValue whenRecord)
            {
                connectorBuilder.Append(", When: ");
                connectorBuilder.Append("{");
                foreach (var field in whenRecord.Fields)
                {
                    connectorBuilder.Append($"{field.Name}: {FormatValue(field.Value)}, ");
                }
                connectorBuilder.Length -= 2; // Remove the trailing comma and space
                connectorBuilder.Append("}, ");
            }
            else
            {
                connectorBuilder.Append(", ");
            }

            if (then is BlankValue blankThenTable)
            {
                connectorBuilder.Append("Then: Blank()");
            }

            if (then is TableValue thenTable)
            {
                connectorBuilder.Append("Then: ");
                connectorBuilder.Append("Table(");

                var rowAdded = false;

                foreach (var record in thenTable.Rows)
                {
                    var recordValue = record.Value as RecordValue;

                    if (recordValue != null)
                    {
                        rowAdded = true;
                        connectorBuilder.Append("{");
                        foreach (var field in recordValue.Fields)
                        {
                            connectorBuilder.Append($"{field.Name}: {FormatValue(field.Value)}, ");
                        }
                        connectorBuilder.Length -= 2; // Remove the trailing comma and space
                        connectorBuilder.Append("}, ");
                    }
                }

                if (rowAdded)
                {
                    connectorBuilder.Length -= 2; // Remove the trailing comma and space
                }

                connectorBuilder.Append(")"); // Close the table
            }

            if (then is RecordValue thenRecord)
            {
                connectorBuilder.Append("Then: ");
                if (thenRecord.Fields.Count() == 0)
                {
                    connectorBuilder.Append("Blank()");
                }
                else
                {
                    connectorBuilder.Append("{");
                    foreach (var field in thenRecord.Fields)
                    {
                        connectorBuilder.Append($"{field.Name}: {FormatValue(field.Value)}, ");
                    }
                    connectorBuilder.Length -= 2; // Remove the trailing comma and space
                    connectorBuilder.Append("}");
                }
            }

            connectorBuilder.Append("});"); // Close record argument and the SimulateConnector function

            return connectorBuilder.ToString();
        }

        private object GetConnectorThenResult(IResponse response)
        {
            throw new NotImplementedException();
        }

        private string GetActionName(string url)
        {
            var requestUrl = new Uri(new Uri("https://example.com"), new Uri(url, UriKind.Relative));

            var segments = requestUrl.AbsolutePath.Split('/');

            // Assuming the entity name is the last segment in the URL and using format /api/data/v9.X/entityname
            // The first segment will be empty as has leading /
            if (segments.Length >= 3 && segments[1].Equals("apim", StringComparison.OrdinalIgnoreCase))
            {
                // TODO: Handle case where requesting connector name vs list using /apim/name/**
                return segments[2];
            }

            throw new ArgumentException("Invalid request url");
        }

        private FormulaValue GetWhenConnectorValue(string url)
        {
            var requestUrl = new Uri(new Uri("https://example.com"), new Uri(url, UriKind.Relative));

            var segments = requestUrl.AbsolutePath.Split('/');

            List<NamedValue> fields = new List<NamedValue>();


            var action = String.Empty;

            // Assuming the entity name is the last segment in the URL and using format /api/data/v9.X/entityname
            // The first segment will be empty as has leading /
            if (segments.Length > 4)
            {
                // TODO: Handle case where requesting connector name vs list using /apim/name/**
                var parts = new List<string>(segments);
                parts.RemoveAt(0); // Remove empty item
                parts.RemoveAt(0); // Remove apim
                parts.RemoveAt(0); // Remove connector name
                parts.RemoveAt(0); // Remove connector id

                // Assume the reminaing item is the action
                fields.Add(new NamedValue("Action", FormulaValue.New(string.Join("/", parts))));
            }

            if (!string.IsNullOrEmpty(requestUrl.Query))
            {
                var items = HttpUtility.ParseQueryString(HttpUtility.UrlDecode(requestUrl.Query));
                foreach (var key in items.AllKeys)
                {
                    switch (key.ToLower())
                    {
                        case "$filter":
                            string powerFxExpression = ConvertODataToPowerFx(items[key]);
                            fields.Add(new NamedValue("Filter", FormulaValue.New(powerFxExpression)));
                            break;
                    }
                }
            }

            if (fields.Count() == 0)
            {
                return RecordValue.NewBlank();
            }

            return RecordValue.NewRecordFromFields(fields);
        }

        string ConvertODataToPowerFx(string odataFilter)
        {
            // Parse the OData filter without a known EDM model
            var filterClause = ParseFilter(odataFilter);

            // Convert the filter clause to Power Fx expression
            return ConvertFilterClauseToPowerFx(filterClause.Expression);
        }

        private FilterClause ParseFilter(string odataFilter)
        {
            EdmModel edmModel = new EdmModel();
            // Define the entity type and set up the EDM model
            EdmEntityType entityType = new EdmEntityType("Namespace", "EntityName", null, false, true);
            edmModel.AddElement(entityType);

            // Parse the filter
            return ODataUriParser.ParseFilter(odataFilter, edmModel, entityType);
        }

        string ConvertFilterClauseToPowerFx(SingleValueNode expression)
        {
            if (expression is BinaryOperatorNode binaryOperatorNode)
            {
                string left = ConvertFilterClauseToPowerFx(binaryOperatorNode.Left);
                string right = ConvertFilterClauseToPowerFx(binaryOperatorNode.Right);
                string operatorString = binaryOperatorNode.OperatorKind switch
                {
                    BinaryOperatorKind.Equal => "=",
                    BinaryOperatorKind.GreaterThan => ">",
                    BinaryOperatorKind.GreaterThanOrEqual => ">=",
                    BinaryOperatorKind.LessThan => "<",
                    BinaryOperatorKind.LessThanOrEqual => "<=",
                    BinaryOperatorKind.NotEqual => "!=",
                    BinaryOperatorKind.Multiply => "*",
                    BinaryOperatorKind.Divide => "/",
                    BinaryOperatorKind.Modulo => "MOD(",
                    BinaryOperatorKind.And => "AND(",
                    BinaryOperatorKind.Or => "OR(",
                    _ => throw new NotSupportedException($"Operator {binaryOperatorNode.OperatorKind} is not supported")
                };
                if (operatorString.Contains("("))
                {
                    // It is a function
                    return $"{operatorString}{left},{right})";
                }
                else
                {
                    return $"{left} {operatorString} {right}";
                }

            }
            else if (expression is UnaryOperatorNode unaryOperatorNode)
            {
                string operand = ConvertFilterClauseToPowerFx(unaryOperatorNode.Operand);
                return $"NOT({operand})";
            }
            else if (expression is SingleValuePropertyAccessNode propertyAccessNode)
            {
                return propertyAccessNode.Property.Name;
            }
            else if (expression is SingleValueOpenPropertyAccessNode openPropertyAccessNode)
            {
                return openPropertyAccessNode.Name;
            }
            else if (expression is ConstantNode constantNode)
            {
                return constantNode.Value.ToString();
            }
            if (expression is ConvertNode convertNode)
            {
                return ConvertFilterClauseToPowerFx(convertNode.Source);
            }

            throw new NotSupportedException($"Expression type {expression.GetType().Name} is not supported");
        }

        private string GenerateDataverseQuery(string entity, FormulaValue data)
        {
            StringBuilder queryBuilder = new StringBuilder();

            queryBuilder.Append($"Experimental.SimulateDataverse({{Action: \"Query\", Entity: \"{entity}\", Then: ");

            if (data is TableValue tableValue)
            {
                queryBuilder.Append($"Table(");

                var rowAdded = false;

                foreach (var record in tableValue.Rows)
                {
                    var recordValue = record.Value as RecordValue;


                    if (recordValue != null)
                    {
                        rowAdded = true;
                        queryBuilder.Append("{");
                        foreach (var field in recordValue.Fields)
                        {
                            queryBuilder.Append($"{field.Name}: {FormatValue(field.Value)}, ");
                        }
                        queryBuilder.Length -= 2; // Remove the trailing comma and space
                        queryBuilder.Append("}, ");
                    }
                }

                if (rowAdded)
                {
                    queryBuilder.Length -= 2; // Remove the trailing comma and space
                }

                queryBuilder.Append(")"); // Close the table
            }
            else if (data is RecordValue recordValue)
            {
                queryBuilder.Append("{");
                foreach (var field in recordValue.Fields)
                {
                    queryBuilder.Append($"{field.Name}: {FormatValue(field.Value)}, ");
                }
                queryBuilder.Length -= 2; // Remove the trailing comma and space
                queryBuilder.Append("}");
            }
            else
            {
                queryBuilder.Append(FormatValue(data));
            }

            queryBuilder.Append("});"); // Close record argument and the SimulateDataverse dataverse function

            return queryBuilder.ToString();
        }

        private string FormatValue(FormulaValue value)
        {
            //TODO: Handle special case of DateTime As unix time to DateTime
            return value switch
            {
                StringValue stringValue => $"\"{stringValue.Value}\"",
                NumberValue numberValue => numberValue.Value.ToString(),
                BooleanValue booleanValue => booleanValue.Value.ToString().ToLower(),
                // Assume all dates should be in UTC
                DateValue dateValue => dateValue.GetConvertedValue(TimeZoneInfo.Utc).ToString("o"), // ISO 8601 format
                DateTimeValue dateTimeValue => dateTimeValue.GetConvertedValue(TimeZoneInfo.Utc).ToString("o"), // ISO 8601 format
                _ => throw new ArgumentException("Unsupported FormulaValue type")
            };
        }

        private async Task<FormulaValue> ConvertODataToFormulaValue(IResponse response)
        {
            // Read the JSON content from the response
            var jsonString = await response.JsonAsync();
            var json = jsonString.ToString();
            var jsonObject = JObject.Parse(json);

            if (jsonObject.ContainsKey("value"))
            {
                return await ConvertJsonToFormulaValue(jsonObject["value"]);
            }
            return await ConvertJsonToFormulaValue(jsonObject);
        }

        private async Task<FormulaValue> ConvertJsonResultToFormulaValue(IResponse response)
        {
            // Read the JSON content from the response
            var jsonString = await response.JsonAsync();
            var jsonObject = JObject.Parse(jsonString.ToString());

            return await ConvertJsonToFormulaValue(jsonObject.Root);
        }

        private async Task<FormulaValue> ConvertJsonToFormulaValue(JToken jsonObject)
        {
            // Check if the value parameter is an array
            if (jsonObject is JArray jsonArray)
            {
                // Create a list of RecordValue to hold the attributes of each object
                var records = new List<RecordValue>();

                // Use empty type as each record might have different values
                RecordType recordType = RecordType.Empty();

                foreach (var item in jsonArray)
                {
                    var fields = new List<NamedValue>();

                    foreach (var property in item.Children<JProperty>())
                    {
                        fields.Add(new NamedValue(property.Name, FormulaValue.New(property.Value.ToString())));
                        recordType = recordType.Add(new NamedFormulaType(property.Name, FormulaType.String));
                    }

                    records.Add(RecordValue.NewRecordFromFields(fields));
                }

                // Convert the list of RecordValue to a TableValue with the generated recordType
                return TableValue.NewTable(recordType, records);
            }
            // Check if the value parameter is an object
            else if (jsonObject is JObject jsonObjectValue)
            {
                var fields = new List<NamedValue>();
                RecordType recordType = RecordType.Empty();

                foreach (var property in jsonObjectValue.Children<JProperty>())
                {
                    fields.Add(new NamedValue(property.Name, FormulaValue.New(property.Value.ToString())));
                    recordType = recordType.Add(new NamedFormulaType(property.Name, FormulaType.String));
                }

                // Convert the object to a RecordValue with the generated recordType
                return RecordValue.NewRecordFromFields(recordType, fields);
            }
            // Check if the value parameter is a scalar value
            else if (jsonObject != null)
            {
                // Convert the scalar value to a FormulaValue
                return FormulaValue.New(jsonObject.ToString());
            }

            throw new ArgumentException("The value parameter is not a valid JSON type");
        }

        private string GetODataEntity(string url)
        {
            var requestUrl = new Uri(url);
            var segments = requestUrl.AbsolutePath.Split('/');

            // Assuming the entity name is the last segment in the URL and using format /api/data/v9.X/entityname
            // The first segment will be empty as has leading /
            if (segments.Length >= 5 && segments[1].Equals("api", StringComparison.OrdinalIgnoreCase) &&
                segments[2].Equals("data", StringComparison.OrdinalIgnoreCase) && segments[3].StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                // TODO: Handle case where requesting entity vs list using /api/data/v9.X/entityname(id) syntax
                return segments[4];
            }

            throw new ArgumentException("Invalid OData URL format");
        }

        ///<summary>
        /// Generates test steps and data, and saves them to the specified path.
        ///</summary>
        ///<param name="path">The path where the test steps and data will be saved.</param>
        public void Generate(string path)
        {
            //TODO: Check if the directory exists, if not, create it
            if (!_fileSystem.Exists(path))
            {
                _fileSystem.CreateDirectory(path);
            }

            //TODO: Define the file path for saving test steps
            string filePath = $"{path}/testSteps.txt";

            //TODO: Map captured test steps to _testBuilder
            foreach (var step in TestSteps)
            {
                _textBuilder.Append($"{step}\r\n");
            }

            //TODO: Write the recorded test steps to the file
            _fileSystem.WriteTextToFile(filePath, _textBuilder.ToString());
        }
    }
}
