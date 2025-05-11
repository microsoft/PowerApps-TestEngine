// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Helpers;
using Microsoft.PowerApps.TestEngine.PowerFx;
using Microsoft.PowerApps.TestEngine.Providers.PowerFxModel;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using testengine.provider.mcp;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Microsoft.PowerApps.TestEngine.Providers
{
    /// <summary>
    /// The MCPProvider class provides integration between the Test Engine and the Model Context Protocol (MCP) server.
    /// It acts as a bridge between the Node.js MCP server and the .NET Test Engine, enabling interoperability.
    /// 
    /// Key Responsibilities:
    /// - Hosts an Static server to handle requests from MCP server.
    /// - Validates Power Fx expressions using the Test Engine.
    /// - Providea ability to query plan designer and solution and provide recommendations
    /// 
    /// Dependencies:
    /// - RecalcEngine: Used for Power Fx validation.
    /// - ILogger: Used for logging.
    /// - TestState and SingleTestInstanceState: Provide context for the test engine.
    /// </summary>
    /// <remarks>
    /// This class is designed to be used in a test environment where the MCP server is running locally. It is not intended for production use and should be modified as needed for specific test scenarios.
    /// </remarks>
    [Export(typeof(ITestWebProvider))]
    public class MCPProvider : ITestWebProvider, IExtendedPowerFxProvider
    {
        public static MCPProvider? Server { get; set; }

        public ITestInfraFunctions? TestInfraFunctions { get; set; }

        public ISingleTestInstanceState? SingleTestInstanceState { get; set; }

        public ITestState? TestState { get; set; }

        public RecalcEngine? Engine { get; set; }

        public ILogger? Logger { get; set; }

        public string? Token { get; set; }

        private readonly ISerializer _yamlSerializer;

        public IFileSystem FileSystem { get; set; } = new FileSystem();

        public Func<ILogger, MCPProxyInstaller> ProxyInstaller = (logger) => new MCPProxyInstaller(new ProcessRunner(), logger);

        public Func<SourceCodeService> SourceCodeServiceFactory => () =>
        {
            var config = new PowerFxConfig();
            config.EnableJsonFunctions();
            config.EnableSetFunction();

            var engine = new RecalcEngine(config);
            return new SourceCodeService(engine);
        };

        public Func<IOrganizationService?> GetOrganizationService = () => null;

        public MCPProvider()
        {
            // Initialize the YAML serializer
            _yamlSerializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
        }

        public MCPProvider(ITestInfraFunctions? testInfraFunctions, ISingleTestInstanceState? singleTestInstanceState, ITestState? testState)
        {
            this.TestInfraFunctions = testInfraFunctions;
            this.SingleTestInstanceState = singleTestInstanceState;
            this.TestState = testState;
            this.Logger = SingleTestInstanceState.GetLogger();

            // Initialize the YAML serializer
            _yamlSerializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
        }

        public string Name { get { return "mcp"; } }

        public string[] Namespaces => new string[] { "Preview" };

        public ITestProviderState? ProviderState { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public string CheckTestEngineObject => "";

        public bool ProviderExecute => throw new NotImplementedException();

        private async Task<T> GetPropertyValueFromControlAsync<T>(ItemPath itemPath)
        {
            throw new NotImplementedException();
        }

        public T GetPropertyValueFromControl<T>(ItemPath itemPath)
        {
            throw new NotImplementedException();
        }


        public async Task<bool> CheckIsIdleAsync()
        {
            return true;
        }

        private async Task<Dictionary<string, ControlRecordValue>> LoadObjectModelAsyncHelper(Dictionary<string, ControlRecordValue> controlDictionary)
        {
            try
            {
                return controlDictionary;
            }

            catch (Exception ex)
            {
                ExceptionHandlingHelper.CheckIfOutDatedPublishedApp(ex, SingleTestInstanceState.GetLogger());
                throw;
            }
        }

        private async Task<string> GetPowerAppsTestEngineObject()
        {
            var result = "true";

            try
            {
                return "{}";
            }
            catch (NullReferenceException) { }

            return result;
        }

        public async Task CheckProviderAsync()
        {
            try
            {
                // See if using legacy player
                try
                {
                    // TODO: Update as needed
                    //await PollingHelper.PollAsync<string>("undefined", (x) => x.ToLower() == "undefined", () => GetPowerAppsTestEngineObject(), TestState.GetTestSettings().Timeout, SingleTestInstanceState.GetLogger());
                }
                catch (TimeoutException)
                {
                    // TODO
                }
            }
            catch (Exception ex)
            {
                SingleTestInstanceState.GetLogger().LogDebug(ex.ToString());
            }
        }

        public async Task<Dictionary<string, ControlRecordValue>> LoadObjectModelAsync()
        {
            var controlDictionary = new Dictionary<string, ControlRecordValue>();

            return controlDictionary;
        }

        public async Task<bool> SelectControlAsync(ItemPath itemPath, string filePath = null)
        {
            // TODO
            return true;
        }

        public async Task<bool> SetPropertyAsync(ItemPath itemPath, FormulaValue value)
        {
            // TODO
            return true;
        }


        public int GetItemCount(ItemPath itemPath)
        {
            return 0;
        }

        public async Task<object> GetDebugInfo()
        {
            try
            {
                return new Dictionary<string, object>();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> TestEngineReady()
        {
            try
            {
                // To support webplayer version without ready function 
                // return true for this without interrupting the test run
                return Server != null;
            }
            catch (Exception ex)
            {

                // If the error returned is anything other than PublishedAppWithoutJSSDKErrorCode capture that and throw
                SingleTestInstanceState.GetLogger().LogDebug(ex.ToString());
                throw;
            }
        }

        public string GenerateTestUrl(string domain, string additionalQueryParams)
        {
            return "about:blank";
        }

        /// <summary>
        /// Configures the Power Fx engine for the Test Engine and starts the static server.
        /// </summary>
        public void ConfigurePowerFxEngine(RecalcEngine engine)
        {
            this.Engine = engine;

            Server = this;

            if (Logger == null)
            {
                Logger = SingleTestInstanceState.GetLogger();
            }

            Console.WriteLine("Register the Test Engine MCP provider using the following");

            var current = Path.GetDirectoryName(GetType().Assembly.Location);

            var matches = Directory.GetFiles(current, "testengine.server.mcp*.nupkg");

            if (matches.Length == 0)
            {
                Console.WriteLine("No Test Engine MCP Servers NuGet install packages found");

            } else {
                Console.WriteLine("Install these Test Engine MCP servers");
                foreach (var match in matches.OrderByDescending(m => m))
                {
                    var version = Path.GetFileNameWithoutExtension(match).Replace("testengine.server.mcp.", "");
                    Console.WriteLine($"dotnet install testengine-server-mcp --add-source {current} -version {version}");
                }
            }

            Console.WriteLine("Press enter to contiue");
            Console.ReadLine();
        }

        /// <summary>
        /// Handles incoming MCP requests to enable MCP server to communicate with the Test Engine.
        /// </summary>
        /// <param name="context">The MCPRequest representing the incoming request.</param>
        /// <remarks>
        /// - Supports GET and POST requests for various endpoints.
        /// - Uses PlanDesignerService for plan-related operations.
        /// - Returns a 404 response for unsupported endpoints.
        /// - Logs errors and returns a 500 response for unexpected exceptions.
        /// </remarks>
        public async Task<MCPResponse> HandleRequest(MCPRequest request)
        {
            var response = new MCPResponse();
            try
            {
                if ((request.Method == "GET" || request.Method == "POST") && request.Endpoint.StartsWith("solution/"))
                {
                    // Handle /solution/<id> endpoint
                    var solutionId = request.Endpoint.Split('/').Last();

                    string powerFx = GetPowerFxFromTestSettings();
                    if (request.Method == "POST")
                    {
                        powerFx = request.Body;
                    }

                    // Create a FileSystem instance and SourceCodeService
                    var sourceCodeService = SourceCodeServiceFactory();
                    sourceCodeService.LoadSolutionFromSourceControl(solutionId, powerFx);

                    // Convert to dictionary and serialize the response
                    var dictionaryResponse = sourceCodeService.ToDictionary();
                    response.StatusCode = 200;
                    response.ContentType = "application/x-yaml";
                    response.Body =_yamlSerializer.Serialize(dictionaryResponse);
                }
                else if (request.Method == "GET" && request.Endpoint == "plans")
                {
                    // Get a list of plans
                    var service = GetOrganizationService();
                    if (service == null)
                    {
                        var domain = new Uri(TestState.GetDomain());
                        var api = new Uri("https://" + domain.Host);

                        // Run the token retrieval in a separate thread
                        var token = await new AzureCliHelper().GetAccessTokenAsync(api);

                        service = new ServiceClient(api, (url) => Task.FromResult(token));
                    }

                    var planDesignerService = new PlanDesignerService(service, SourceCodeServiceFactory());
                    var plans = planDesignerService.GetPlans();
                    response.StatusCode = 200;
                    response.ContentType = "application/x-yaml";
                    response.Body =_yamlSerializer.Serialize(plans);
                }
                else if (request.Method == "GET" && request.Endpoint.StartsWith("plans/"))
                {
                    // Get a specific plan
                    var planId = request.Endpoint.Split('/').Last();
                    var service = GetOrganizationService();
                    if (service == null)
                    {
                        var domain = new Uri(TestState.GetDomain());
                        var api = new Uri("https://" + domain.Host);

                        // Run the token retrieval in a separate thread
                        var token = await new AzureCliHelper().GetAccessTokenAsync(api);

                        service = new ServiceClient(api, (url) => Task.FromResult(token));
                    }

                    var planDesignerService = new PlanDesignerService(service, SourceCodeServiceFactory());
                    var plan = planDesignerService.GetPlanDetails(new Guid(planId));
                    response.StatusCode = 200;
                    response.ContentType = "application/x-yaml";
                    response.Body =_yamlSerializer.Serialize(plan);
                }
                else if (request.Method == "POST" && request.Endpoint == "validate")
                {
                    // Validate Power Fx expression
                    var powerFx = request.Body;

                    switch (request.ContentType)
                    {
                        case "application/json":
                            powerFx = JsonConvert.DeserializeObject<string>(powerFx);
                            break;
                        case "application/x-yaml":
                            powerFx = new DeserializerBuilder()
                                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                                .Build()
                                .Deserialize<string>(powerFx);
                            break;
                    }

                    var result = ValidatePowerFx(powerFx);

                    response.StatusCode = 200;
                    response.ContentType = "application/x-yaml";
                    response.Body =_yamlSerializer.Serialize(result);
                }
                else
                {
                    // Return a 404 response for unsupported endpoints
                    response.StatusCode = 404;
                    response.ContentType = "application/x-yaml";
                    response.Body = _yamlSerializer.Serialize(new ValidationResult { IsValid = false, Errors = new List<string>() { "Endpoint not found" } });
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 200;
                response.ContentType = "application/x-yaml";
                response.Body = _yamlSerializer.Serialize(new ValidationResult { IsValid = false, Errors = new List<string>() { "Unable to process request, check if valid", ex.Message } });
            }

            return response;
        }

        private string GetPowerFxFromTestSettings()
        {
            StringBuilder stringBuilder = new StringBuilder();
            var testSuite = SingleTestInstanceState.GetTestSuiteDefinition();
            foreach (var testCase in testSuite.TestCases)
            {
                if (testCase.TestCaseName.ToLower().StartsWith("post-"))
                {
                    if (stringBuilder.Length > 0)
                    {
                        stringBuilder.Append(";");
                    }
                    stringBuilder.Append(testCase.TestSteps);
                }
            }
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Validates a Power Fx expression using the configured RecalcEngine.
        /// </summary>
        /// <param name="powerFx">The Power Fx expression to validate.</param>
        /// <returns>A YAML string representing the validation result, including whether the expression is valid and any errors.</returns>
        /// <remarks>
        /// - Uses the RecalcEngine to check the syntax and semantics of the Power Fx expression.
        /// - Returns an error if the engine is not configured.
        /// - Includes detailed error messages for invalid expressions.
        /// </remarks>
        public string ValidatePowerFx(string powerFx)
        {
            if (Engine == null)
            {
                Engine = new RecalcEngine(new PowerFxConfig());
                Engine.Config.EnableJsonFunctions();
                Engine.Config.EnableSetFunction();
            }

            var testSettings = TestState.GetTestSettings();

            if (testSettings == null)
            {
                testSettings = new TestSettings();
            }

            if (this.Logger == null)
            {
                this.Logger = SingleTestInstanceState.GetLogger();
            }

            var locale = PowerFxEngine.GetLocaleFromTestSettings(testSettings.Locale, this.Logger);

            var parserOptions = new ParserOptions { AllowsSideEffects = true, Culture = locale };

            CheckResult checkResult = null;
            if (testSettings.PowerFxTestTypes.Count > 0 || testSettings.TestFunctions.Count > 0)
            {
                var config = new PowerFxConfig();
                config.EnableJsonFunctions();
                config.EnableSetFunction();

                PowerFxEngine.ConditionallyRegisterTestTypes(testSettings, config);

                var engine = new RecalcEngine(config);

                PowerFxEngine.ConditionallyRegisterTestFunctions(testSettings, config, Logger, engine);

                checkResult = engine.Check(string.IsNullOrEmpty(powerFx) ? string.Empty : powerFx, options: parserOptions, engine.Config.SymbolTable);
            } 
            else
            {
                checkResult = Engine.Check(string.IsNullOrEmpty(powerFx) ? string.Empty : powerFx, options: parserOptions, Engine.Config.SymbolTable);
            }

            var validationResult = new ValidationResult
            {
                IsValid = checkResult.IsSuccess
            };

            if (!checkResult.IsSuccess)
            {
                foreach (var error in checkResult.Errors)
                {
                    validationResult.Errors.Add(error.Message);
                }
            }

            return _yamlSerializer.Serialize(validationResult);
        }

        public async Task SetupContext()
        {

        }

        public FormulaValue ExecutePowerFx(string steps, CultureInfo culture)
        {
            return FormulaValue.NewBlank();
        }

        /// <summary>
        /// Configures the MCPProvider with the state of the test engine and test infrastructure functions.
        /// </summary>
        /// <param name="powerFxConfig">The configuration for the Power Fx engine, including custom functions and symbols.</param>
        /// <param name="testInfraFunctions">Provides access to common test infrastructure needs, such as file operations or environment settings.</param>
        /// <param name="singleTestInstanceState">The state of the current test instance, including logging and runtime context.</param>
        /// <param name="testState">The overall state of the test engine, including test settings and execution context.</param>
        /// <param name="fileSystem">An abstraction for file system operations, allowing for mocking in tests.</param>
        /// <remarks>
        /// - `powerFxConfig`: Used to configure the Power Fx engine with custom symbols, functions, and settings.
        /// - `testInfraFunctions`: Provides utilities for interacting with the test environment, such as accessing test data or managing test dependencies.
        /// - `singleTestInstanceState`: Contains runtime information for the current test instance, such as logs and execution state.
        /// - `testState`: Represents the global state of the test engine, including configuration and execution details.
        /// - `fileSystem`: Allows interaction with the file system, enabling operations like reading and writing files in a testable manner.
        /// </remarks>
        public void Setup(PowerFxConfig powerFxConfig, ITestInfraFunctions testInfraFunctions, ISingleTestInstanceState singleTestInstanceState, ITestState testState, IFileSystem fileSystem)
        {
            var logger = singleTestInstanceState.GetLogger();

            this.TestState = testState;
            this.TestInfraFunctions = testInfraFunctions;
            this.SingleTestInstanceState = singleTestInstanceState;
        }

        public void ConfigurePowerFx(PowerFxConfig powerFxConfig)
        {

        }
    }
}
