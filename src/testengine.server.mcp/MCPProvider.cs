// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Helpers;
using Microsoft.PowerApps.TestEngine.PowerFx;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerFx;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

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

public class MCPProvider
{

    public string? Token { get; set; }

    private readonly ISerializer _yamlSerializer;

    public IFileSystem FileSystem { get; set; } = new FileSystem();

    public TestSettings? MCPTestSettings { get; set; } = null;

    public TestSuiteDefinition? TestSuite { get; set; } = null;

    public ILogger Logger { get; set; } = NullLogger.Instance;

    public RecalcEngine? Engine { get; set; }

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
                response.Body = _yamlSerializer.Serialize(dictionaryResponse);
            }
            else if (request.Method == "GET" && request.Endpoint == "plans")
            {
                // Get a list of plans
                var service = GetOrganizationService();
                if (service == null)
                {
                    var domain = new Uri(request.Target);
                    var api = new Uri("https://" + domain.Host);

                    // Run the token retrieval in a separate thread
                    var token = await new AzureCliHelper().GetAccessTokenAsync(api);

                    service = new ServiceClient(api, (url) => Task.FromResult(token));
                }

                var planDesignerService = new PlanDesignerService(service, SourceCodeServiceFactory());
                var plans = planDesignerService.GetPlans();
                response.StatusCode = 200;
                response.ContentType = "application/x-yaml";
                response.Body = _yamlSerializer.Serialize(plans);
            }
            else if (request.Method == "POST" && request.Endpoint.StartsWith("plans/"))
            {
                // Get a specific plan
                var planId = request.Endpoint.Split('/').Last();
                var service = GetOrganizationService();
                if (service == null)
                {
                    var domain = new Uri(request.Target);
                    var api = new Uri("https://" + domain.Host);

                    // Run the token retrieval in a separate thread
                    var token = await new AzureCliHelper().GetAccessTokenAsync(api);

                    service = new ServiceClient(api, (url) => Task.FromResult(token));
                }

                var planDesignerService = new PlanDesignerService(service, SourceCodeServiceFactory());
                var plan = planDesignerService.GetPlanDetails(new Guid(planId), workspace: request.Body);
                response.StatusCode = 200;
                response.ContentType = "application/x-yaml";
                response.Body = _yamlSerializer.Serialize(plan);
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
                response.Body = _yamlSerializer.Serialize(result);
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

        foreach (var testCase in TestSuite?.TestCases)
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

        var testSettings = MCPTestSettings;

        if (testSettings == null)
        {
            testSettings = new TestSettings();
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
}
