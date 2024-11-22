// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
using testengine.module.powerapps.portal;

namespace testengine.module
{
    /// <summary>
    /// This provide the ability to export connection to a Json file. Compatible with powerApps.portal provider
    /// </summary>
    public class ExportConnectionsFunction : ReflectionFunction
    {
        private readonly ITestInfraFunctions _testInfraFunctions;
        private readonly ITestState _testState;
        private readonly ILogger _logger;
        private IPage? Page { get; set; }

        public Func<ConnectionHelper> GetConnectionHelper = () => new ConnectionHelper();

        public Action<string, string> WriteAllText = (file, json) => File.WriteAllText(file, json);

        public ExportConnectionsFunction(ITestInfraFunctions testInfraFunctions, ITestState testState, ILogger logger)
            : base(DPath.Root.Append(new DName("Experimental")), "ExportConnections", FormulaType.Blank, StringType.String)
        {
            _testInfraFunctions = testInfraFunctions;
            _testState = testState;
            _logger = logger;
        }

        public BlankValue Execute(StringValue fileName)
        {
            _logger.LogInformation("------------------------------\n\n" +
                "Executing TestEngine.ExportConnections function.");

            ExecuteAsync(fileName).Wait();

            return BlankValue.NewBlank();
        }

        private async Task ExecuteAsync(StringValue fileName)
        {
            var connections = await GetConnectionHelper().GetConnections(_testInfraFunctions.GetContext(), _testState.GetDomain());

            WriteAllText(fileName.Value, JsonSerializer.Serialize(connections));
        }
    }
}

