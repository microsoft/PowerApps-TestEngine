// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Dynamic;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
using Newtonsoft.Json.Linq;

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

        private static readonly RecordType dataverseRequest = RecordType.Empty()
                .Add(new NamedFormulaType("Action", StringType.String))
                .Add(new NamedFormulaType("Entity", StringType.String))
                .Add(new NamedFormulaType("Then", TableType.Blank))
                .Add(new NamedFormulaType("When", TableType.Blank));

        public SimulateDataverseFunction(ITestInfraFunctions testInfraFunctions, ITestState testState, ILogger logger)
            : base(DPath.Root.Append(new DName("Experimental")), "SimulateDataverse", FormulaType.Blank, dataverseRequest)
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

            if (thenResult == null)
            {
                throw new InvalidDataException("Missing field then");
            }

            await _testInfraFunctions.Page.RouteAsync($"**/api/data/v*/{entityName.Value.ToLower()}*", async (IRoute route) => {
                var request = route.Request;
                if (request.Method == "GET")
                {
                    var data = new List<object>();
                    foreach ( var item in thenResult.Rows)
                    {
                        var row = new Dictionary<string, object?>(new ExpandoObject());

                        await foreach ( var field in item.Value.GetFieldsAsync(CancellationToken.None) )
                        {
                            if (field.Value.TryGetPrimitiveValue(out object val))
                            {
                                row.Add(field.Name, val);
                                continue;
                            }

                            // TODO: Handle complexity non primative types
                        }
                        
                        data.Add(row);
                    }
                    // Convert the TableValue to JSON
                    var responseBody = JObject.FromObject(new { value = data }).ToString();

                    // Generate a new response with HTTP body for OData call
                    await route.FulfillAsync(new RouteFulfillOptions
                    {
                        Status = 200,
                        ContentType = "application/json",
                        Body = responseBody
                    });
                }
                else
                {
                    await route.ContinueAsync();
                }
            });

            
        }
    }
}

