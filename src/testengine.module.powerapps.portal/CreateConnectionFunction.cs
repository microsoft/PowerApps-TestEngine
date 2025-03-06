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
    /// This provide the ability to create power platform connections. Compatible with powerApps.portal provider
    /// 
    /// Notes:
    /// - This approach should be considered a backup. Ideally connections should be created by service principal and shared with user account as needed
    /// - This approach assumes known login credentials using browser auth or certificate based authentication
    /// </summary>
    public class CreateConnectionFunction : ReflectionFunction
    {
        private readonly ITestInfraFunctions _testInfraFunctions;
        private readonly ITestState _testState;
        private readonly ILogger _logger;
        public IPage? Page { get; set; }

        public Func<ConnectionHelper> GetConnectionHelper = () => new ConnectionHelper();

        public static string CREATE_BUTTON_LOCATOR = ".btn.btn-primary.add:has-text(\"Create\")";

        public static string EXCEPTION_CREATE_NOT_ENABLED = "Create connection not enabled. Check parameters";

        private static RecordType recordType = RecordType.Empty()
               .Add(new NamedFormulaType("Name", FormulaType.String, displayName: "Name"))
               .Add(new NamedFormulaType("Interactive", FormulaType.Boolean, displayName: "Interactive"))
               .Add(new NamedFormulaType("Parameters", FormulaType.String, displayName: "Parameters"))
               .Add(new NamedFormulaType("WaitUntilCreated", FormulaType.Boolean, displayName: "WaitUntilCreated"));


        private static TableType ConnectionType = TableType.Empty()
               .Add(new NamedFormulaType("Name", FormulaType.String, displayName: "Name"))
               .Add(new NamedFormulaType("Interactive", FormulaType.Boolean, displayName: "Interactive"))
               .Add(new NamedFormulaType("Parameters", FormulaType.String, displayName: "Parameters"))
               .Add(new NamedFormulaType("WaitUntilCreated", FormulaType.Boolean, displayName: "WaitUntilCreated"));

        // NOTE: Order of calling base is name, return type then argument types
        public CreateConnectionFunction(ITestInfraFunctions testInfraFunctions, ITestState testState, ILogger logger)
            : base(DPath.Root.Append(new DName("Preview")), "CreateConnection", FormulaType.Blank, ConnectionType)
        {
            _testInfraFunctions = testInfraFunctions;
            _testState = testState;
            _logger = logger;
        }

        public BlankValue Execute(TableValue create)
        {
            _logger.LogInformation("------------------------------\n\n" +
                "Executing TestEngine.CreateConnection function.");

            ExecuteAsync(create).Wait();

            return BlankValue.NewBlank();
        }

        /// <summary>
        /// Attempt to create Power Platform connection by automating the creating using logged in Power Apps portal session
        /// 
        /// Notes:
        /// - Will skip creation of connection if it already exists
        /// </summary>
        /// <param name="create">The name of the connection to create</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task ExecuteAsync(TableValue create)
        {
            Page = await _testInfraFunctions.GetContext().NewPageAsync();

            var rowCount = create.Rows.Count();

            _logger.LogInformation($"{rowCount} connections to be created");

            var timeout = _testState.GetTimeout();

            var baseUrl = _testState.GetDomain();
            var connections = await GetConnectionHelper().GetConnections(_testInfraFunctions.GetContext(), baseUrl);

            foreach (var row in create.Rows)
            {

                if (!row.IsBlank)
                {
                    var recordValue = row.Value;

                    object name = string.Empty;

                    if (recordValue.GetField("Name").TryGetPrimitiveValue(out name))
                    {
                        var url = baseUrl;

                        if (connections.Any(c => c.Name == name as string))
                        {
                            _logger.LogInformation($"Skipping connection {name}, already exists");
                            continue;
                        }

                        _logger.LogInformation($"Creating connection {name}");

                        if (!url.EndsWith("/"))
                        {
                            url += "/";
                        }
                        url += $"connections/available?apiName={name}&source=testengine";

                        await Page.GotoAsync(url);

                        if (recordValue.Fields.Any(f => f.Name == "Parameters"))
                        {
                            object parameters = string.Empty;

                            if (recordValue.GetField("Parameters").TryGetPrimitiveValue(out parameters))
                            {
                                var paramValue = parameters as string;
                                if (!string.IsNullOrEmpty(paramValue))
                                {
                                    await AddParameters(paramValue);
                                }
                            }
                        }

                        await CreateConnection(timeout);


                        if (recordValue.Fields.Any(f => f.Name == "Interactive"))
                        {
                            var interactive = recordValue.GetField("Interactive").AsBoolean();

                            if (interactive)
                            {
                                await HandleInteractiveLogin(timeout);
                            }
                        }

                        DateTime started = DateTime.Now;
                        while (await Page.IsVisibleAsync(".pa-model"))
                        {
                            Thread.Sleep(1000);
                            if (DateTime.Now.Subtract(started).TotalMilliseconds > timeout)
                            {
                                throw new Exception($"Timout waiting for dialog to close {name}");
                            }
                        }

                        // Wait until connection is created
                        while (!await GetConnectionHelper().Exists(_testInfraFunctions.GetContext(), baseUrl, name as string))
                        {
                            Thread.Sleep(1000);
                            if (DateTime.Now.Subtract(started).TotalMilliseconds > timeout)
                            {
                                throw new Exception($"Timout waiting for connection {name}");
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Add parameters to the connection dialog
        /// </summary>
        /// <param name="json">The name value pair of parameters to be populated</param>
        /// <returns></returns>
        public async Task AddParameters(string json)
        {
            // Replace single quotes to double quotes to make valid JSON. Done as in yaml may include json inside string and single quote handling easier
            json = json.Replace("'", "\"");
            var values = JsonSerializer.Deserialize<IDictionary<string, object>>(json);

            _logger.LogInformation($"Adding {values.Count} parameters");

            foreach (var key in values.Keys)
            {
                object keyValue;
                if (values.TryGetValue(key, out keyValue))
                {
                    // Assume keys are the label
                    var locator = Page.Locator($"[aria-label=\"{key}\"]");
                    await locator.WaitForAsync();

                    // Assume that input text box

                    _logger.LogInformation($"Adding {key}");

                    // TODO: Handle other parameter types like options
                    string textValue = keyValue.ToString();
                    await locator.FillAsync(textValue);
                }
            }
        }

        /// <summary>
        /// Attempts to create the connection
        /// 
        /// Assumptions:
        /// - Create button is enabled
        /// - All required parameters have been provided
        /// </summary>
        /// <returns></returns>
        public async Task CreateConnection(int timeout)
        {
            var createButton = Page.Locator(CREATE_BUTTON_LOCATOR);

            DateTime started = DateTime.Now;
            while (!await createButton.IsEnabledAsync())
            {
                Thread.Sleep(1000);
                if (DateTime.Now.Subtract(started).TotalMilliseconds > timeout)
                {
                    throw new Exception(EXCEPTION_CREATE_NOT_ENABLED);
                }
            }

            await createButton.ClickAsync();

            _logger.LogInformation($"Created connection");
        }

        /// <summary>
        /// Handle interactive login for connector.
        /// 
        /// Assumptions:
        /// - Using browser authentication type with user has elected to stay signed in
        /// - Using certificate based authentication
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception">If unable to successfully complete the interactive login process</exception>
        public async Task HandleInteractiveLogin(int timeout)
        {
            _logger.LogInformation($"Searching for interactive login");

            DateTime started = DateTime.Now;
            IPage authPage = null;
            do
            {
                foreach (var page in _testInfraFunctions.GetContext().Pages)
                {
                    var pageUrl = page.Url;
                    if (pageUrl.Contains("/oauth"))
                    {
                        authPage = page;
                        break;
                    }
                }
                if (authPage == null)
                {
                    Thread.Sleep(1000);
                    if (DateTime.Now.Subtract(started).TotalMilliseconds > timeout)
                    {
                        throw new Exception("Timeout waiting for authentication page");
                    }
                }
            } while (authPage == null);

            _logger.LogInformation($"Found login page");

            if (authPage == null)
            {
                throw new Exception("Authentication page not found");
            }

            // TODO: Handle certificate based authentication

            // Assume that select first logged in account
            await authPage.GetByRole(AriaRole.Button).Locator(".table-cell.content").First.WaitForAsync();
            _logger.LogInformation($"Selecting first user account");

            await authPage.GetByRole(AriaRole.Button).Locator(".table-cell.content").First.ClickAsync();

            // Assume that after user selected that the auth dialog will close
            started = DateTime.Now;
            while (!authPage.IsClosed)
            {
                Thread.Sleep(1000);
                if (DateTime.Now.Subtract(started).TotalMilliseconds > timeout)
                {
                    throw new Exception("Unable to complete interactive log");
                }
            }
        }
    }
}

