// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Helpers;
using Microsoft.PowerApps.TestEngine.PowerFx;
using Microsoft.PowerApps.TestEngine.Providers.PowerFxModel;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;
using testengine.provider.mcp;
using System.Security.Cryptography;
using Microsoft.Xrm.Sdk;

namespace Microsoft.PowerApps.TestEngine.Providers
{
    /// <summary>
    /// The MCPProvider class provides integration between the Test Engine and the Model Context Protocol (MCP) server.
    /// It acts as a bridge between the Node.js MCP server and the .NET Test Engine, enabling interoperability.
    /// 
    /// Key Responsibilities:
    /// - Hosts an HTTP server to handle requests from the Node.js MCP server.
    /// - Validates Power Fx expressions using the Test Engine.
    /// - Provides utility functions for hashing and validating the Node.js app.
    /// 
    /// Dependencies:
    /// - RecalcEngine: Used for Power Fx validation.
    /// - ILogger: Used for logging.
    /// - TestState and SingleTestInstanceState: Provide context for the test engine.
    /// </summary>
    /// <remarks>
    /// This class is designed to be used in a test environment where the MCP server is running locally. It is not intended for production use and should be modified as needed for specific test scenarios.
    /// 
    /// NOTES:
    /// 2. The Node.js app path is hardcoded for a local Debug Build and should be updated based on the actual deployment location as non Deub Builds are considered.
    /// 3. The HTTP server runs on port 8080 and should be configured to avoid port conflicts.  
    /// 4. The Node.js app hash is validated to ensure the correct version is being used.
    /// 5. The [https://www.nuget.org/packages/ModelContextProtocol](https://github.com/modelcontextprotocol/csharp-sdk) has not been directly used as Test Engine already has a console interface that would impact stdio usage patterns.
    /// 6. In the future, consider using the [https://www.nuget.org/packages/ModelContextProtocol.AspNetCore](https://www.nuget.org/packages/ModelContextProtocol.AspNetCore) when pac cli is moved allow .Net 8.0 remove the need for .Net Standard 2.0 backward compatibility
    /// </remarks>
    [Export(typeof(ITestWebProvider))]
    public class MCPProvider : ITestWebProvider, IExtendedPowerFxProvider
    {
        public const string NODE_APPJS_HASH = "11CC99890FFE8972B05108DBF26CAA53E19207579852CFFBAAA74DD90F5E1E01";

        public ITestInfraFunctions? TestInfraFunctions { get; set; }

        public ISingleTestInstanceState? SingleTestInstanceState { get; set; }

        public ITestState? TestState { get; set; }

        public RecalcEngine? Engine { get; set; }

        public ILogger? Logger { get; set; }

        /// <summary>
        /// Validate that the calculate NodeJs hash has the expected value 
        /// </summary>
        public Func<string, bool> NodeJsHashValidator = (string actual) =>
        {
            return actual == NODE_APPJS_HASH;
        };

        public Func<int, IHttpServer> GetHttpServer = (int port) => new HttpListenerServer($"http://localhost:{port}/");

        public Func<IOrganizationService> GetOrganizationService = () => new StubOrganizationService();

        public MCPProvider()
        {

        }

        public MCPProvider(ITestInfraFunctions? testInfraFunctions, ISingleTestInstanceState? singleTestInstanceState, ITestState? testState)
        {
            this.TestInfraFunctions = testInfraFunctions;
            this.SingleTestInstanceState = singleTestInstanceState;
            this.TestState = testState;
            this.Logger = SingleTestInstanceState.GetLogger();
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
                return true;
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
        /// Configures the Power Fx engine for the Test Engine and starts the HTTP server.
        /// </summary>
        /// <param name="engine">The RecalcEngine instance used for Power Fx validation.</param>
        /// <remarks>
        /// - This method initializes the HTTP server to handle requests from the Node.js MCP server.
        /// - It validates the hash of the Node.js app to ensure its integrity.
        /// - Outputs configuration details for integrating the MCP server into Visual Studio settings.
        /// </remarks>
        public void ConfigurePowerFxEngine(RecalcEngine engine)
        {
            this.Engine = engine;

            // Start the HTTP server to handle requests from the Node.js MCP server.
            StartHttpServer();

            // Get the path to the Node.js app (app.js) relative to the current assembly location.
            var nodeApp = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(this.GetType().Assembly.Location), "..", "..", "..", "src", "testengine.mcp", "app.js"));

            // Compute the hash of the Node.js app to ensure it has not been tampered with.
            var hash = ComputeFileHash(nodeApp);

            // Validate the computed hash against the expected hash.
            Debug.Assert(NodeJsHashValidator(hash), "Node app hash does not match expected value.");

            // Output configuration details for integrating the MCP server into Visual Studio settings.
            Console.WriteLine("You can add the following to your Visual Studio settings to enable the MCP interface.");
            Console.WriteLine(@"
{{
    ""mcp"": {{
        ""inputs"": [],
        ""servers"": {{
            ""TestEngine"": {{
                ""command"": ""node"",
                ""args"": [
                    ""{0}"",
                    ""{1}""
                ]
            }}
        }}
    }},
    ""chat.mcp.discovery.enabled"": true
}}", nodeApp.Replace("\\","/"), "8080");

            Console.WriteLine("Test Engine MCP Interface Ready. Press Enter to exit");
            Console.ReadLine();
        }

        /// <summary>
        /// Computes the SHA-256 hash of a file.
        /// </summary>
        /// <param name="filePath">The path to the file to hash.</param>
        /// <returns>A string representing the SHA-256 hash of the file in uppercase hexadecimal format.</returns>
        /// <remarks>
        /// - Used to validate the integrity of the Node.js app.
        /// - Throws exceptions if the file cannot be read.
        /// </remarks>
        public static string ComputeFileHash(string filePath)
        {
            using (var sha256 = SHA256.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    byte[] hashBytes = sha256.ComputeHash(stream);
                    return BitConverter.ToString(hashBytes).Replace("-", "").ToUpperInvariant();
                }
            }
        }

        /// <summary>
        /// Starts an HTTP server on localhost to handle requests from the Node.js MCP server.
        /// </summary>
        /// <remarks>
        /// - The server listens on port 8080.
        /// - It handles POST requests to the `/validate` endpoint for Power Fx validation.
        /// - Runs in a background task to avoid blocking the main thread.
        /// </remarks>
        private void StartHttpServer()
        {
            // Run the HTTP server in a background task to avoid blocking the main thread.
            Task.Run(() =>
            {
                var listener = GetHttpServer(8080);
                listener.OnRequestReceived += async (context) =>
                {
                    // Handle the request in a separate task to avoid blocking the server.
                    await HandleRequest(context);
                };
#if RELEASE
                Console.WriteError("MCP integration not enabled in Release mode");
#else
                listener.Start();
#endif
            });
        }

        /// <summary>
        /// Handles incoming HTTP requests to the MCPProvider's HTTP server.
        /// </summary>
        /// <param name="context">The HttpListenerContext representing the incoming request.</param>
        /// <remarks>
        /// - Supports GET and POST requests for various endpoints.
        /// - Uses PlanDesignerService for plan-related operations.
        /// - Returns a 404 response for unsupported endpoints.
        /// - Logs errors and returns a 500 response for unexpected exceptions.
        /// </remarks>
        public async Task HandleRequest(HttpListenerContext context)
        {
            try
            {
                var request = context.Request;
                var response = context.Response;

                var planDesignerService = new PlanDesignerService(GetOrganizationService()); 

                if (request.HttpMethod == "GET" && request.Url.AbsolutePath == "/plans")
                {
                    // Get a list of plans
                    var plans = await planDesignerService.GetPlansAsync();
                    response.StatusCode = 200;
                    response.ContentType = "application/json";
                    using (var writer = new StreamWriter(response.OutputStream, Encoding.UTF8))
                    {
                        await writer.WriteAsync(JsonSerializer.Serialize(plans));
                    }
                }
                else if (request.HttpMethod == "GET" && request.Url.AbsolutePath.StartsWith("/plans/") && !request.Url.AbsolutePath.Contains("/artifacts") && !request.Url.AbsolutePath.Contains("/assets"))
                {
                    // Get details for a specific plan
                    var planId = Guid.Parse(request.Url.AbsolutePath.Split('/').Last());
                    var planDetails = await planDesignerService.GetPlanDetailsAsync(planId);
                    response.StatusCode = 200;
                    response.ContentType = "application/json";
                    using (var writer = new StreamWriter(response.OutputStream, Encoding.UTF8))
                    {
                        await writer.WriteAsync(JsonSerializer.Serialize(planDetails));
                    }
                }
                else if (request.HttpMethod == "GET" && request.Url.AbsolutePath.Contains("/artifacts"))
                {
                    // Get artifacts for a specific plan
                    var planId = Guid.Parse(request.Url.AbsolutePath.Split('/')[2]);
                    var artifacts = await planDesignerService.GetPlanArtifactsAsync(planId);
                    response.StatusCode = 200;
                    response.ContentType = "application/json";
                    using (var writer = new StreamWriter(response.OutputStream, Encoding.UTF8))
                    {
                        await writer.WriteAsync(JsonSerializer.Serialize(artifacts));
                    }
                }
                else if (request.HttpMethod == "GET" && request.Url.AbsolutePath.Contains("/assets"))
                {
                    // Get solution assets for a specific plan
                    var planId = Guid.Parse(request.Url.AbsolutePath.Split('/')[2]);
                    var assets = await planDesignerService.GetSolutionAssetsAsync(planId);
                    response.StatusCode = 200;
                    response.ContentType = "application/json";
                    using (var writer = new StreamWriter(response.OutputStream, Encoding.UTF8))
                    {
                        await writer.WriteAsync(JsonSerializer.Serialize(assets));
                    }
                }
                else if (request.HttpMethod == "POST" && request.Url.AbsolutePath == "/validate")
                {
                    // Validate Power Fx expression
                    using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                    {
                        var powerFx = await reader.ReadToEndAsync();
                        Console.WriteLine($"Received Power Fx: {powerFx}");

                        var result = ValidatePowerFx(powerFx);

                        response.StatusCode = 200;
                        response.ContentType = "application/json";
                        using (var writer = new StreamWriter(response.OutputStream, Encoding.UTF8))
                        {
                            await writer.WriteAsync(result);
                        }
                    }
                }
                else
                {
                    // Return a 404 response for unsupported endpoints
                    response.StatusCode = 404;
                    using (var writer = new StreamWriter(response.OutputStream, Encoding.UTF8))
                    {
                        await writer.WriteAsync("{\"error\": \"Endpoint not found\"}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling request: {ex}");
                context.Response.StatusCode = 500;
                using (var writer = new StreamWriter(context.Response.OutputStream, Encoding.UTF8))
                {
                    await writer.WriteAsync("{\"error\": \"Internal server error\"}");
                }
            }
        }

        /// <summary>
        /// Validates a Power Fx expression using the configured RecalcEngine.
        /// </summary>
        /// <param name="powerFx">The Power Fx expression to validate.</param>
        /// <returns>A JSON string representing the validation result, including whether the expression is valid and any errors.</returns>
        /// <remarks>
        /// - Uses the RecalcEngine to check the syntax and semantics of the Power Fx expression.
        /// - Returns an error if the engine is not configured.
        /// - Includes detailed error messages for invalid expressions.
        /// </remarks>
        public string ValidatePowerFx(string powerFx)
        {
            if (Engine == null)
            {
                return "{\"valid\": false, \"errors\": [\"Engine is not configured\"]}";
            }

            var testSettings = TestState.GetTestSettings();

            if (this.Logger == null)
            {
                this.Logger = SingleTestInstanceState.GetLogger();
            }

            var locale = PowerFxEngine.GetLocaleFromTestSettings(testSettings.Locale, this.Logger);

            var parserOptions = new ParserOptions { AllowsSideEffects = true, Culture = locale };
            var checkResult = Engine.Check(string.IsNullOrEmpty(powerFx) ? String.Empty : powerFx, options: parserOptions, Engine.Config.SymbolTable);

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

            return JsonSerializer.Serialize(validationResult);
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
