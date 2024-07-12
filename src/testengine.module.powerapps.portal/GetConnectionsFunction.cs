// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace testengine.module
{
    /// <summary>
    /// This provide the ability to query connections. Compatible with powerApps.portal provider
    /// </summary>
    public class GetConnectionsFunction : ReflectionFunction
    {
        private readonly ITestInfraFunctions _testInfraFunctions;
        private readonly ITestState _testState;
        private readonly ILogger _logger;
        private IPage? Page { get; set; }

        private static RecordType recordType = RecordType.Empty()
               .Add(new NamedFormulaType("Name", FormulaType.String, displayName: "Name"))
                .Add(new NamedFormulaType("Id", FormulaType.String, displayName: "Id"))
                .Add(new NamedFormulaType("Status", FormulaType.String, displayName: "Status"));


        private static TableType Results = TableType.Empty()
                .Add(new NamedFormulaType("Name", FormulaType.String, displayName: "Name"))
                .Add(new NamedFormulaType("Id", FormulaType.String, displayName: "Id"))
                .Add(new NamedFormulaType("Status", FormulaType.String, displayName: "Status"));

        public GetConnectionsFunction(ITestInfraFunctions testInfraFunctions, ITestState testState, ILogger logger)
            : base(DPath.Root.Append(new DName("TestEngine")), "GetConnections", Results)
        {
            _testInfraFunctions = testInfraFunctions;
            _testState = testState;
            _logger = logger;
        }

        public TableValue Execute()
        {
            _logger.LogInformation("------------------------------\n\n" +
                "Executing TestEngine.GetConnections function.");

            return ExecuteAsync().Result;
        }

        private async Task<TableValue> ExecuteAsync()
        {
            Page = await _testInfraFunctions.GetContext().NewPageAsync();

            var url = new Uri(new Uri(_testState.GetDomain()), "/connections?source=testengine").ToString();

            await Page.GotoAsync(url);

            await Page.AddScriptTagAsync(new PageAddScriptTagOptions { Content = LoadResource("PowerAppsPortalConnections.js") });

            await Page.Locator(".connections-list-container").WaitForAsync();

            var connectionsJson = await Page.EvaluateAsync<string>("PowerAppsPortalConnections.getConnections()");

            await Page.CloseAsync();

            var connections = JsonSerializer.Deserialize<List<Connection>>(connectionsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var result = TableValue.NewTable(recordType);

            foreach (Connection connection in connections)
            {
                await result.AppendAsync(RecordValue.NewRecordFromFields(
                    new NamedValue("Name", FormulaValue.New(connection.Name)),
                    new NamedValue("Id", FormulaValue.New(connection.Id)),
                    new NamedValue("Status", FormulaValue.New(connection.Status))), CancellationToken.None);
            }

            return result;
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

