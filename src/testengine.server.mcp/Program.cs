// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using ModelContextProtocol.Server;

// The Test Engein MCP Server is in preview and tools are likely to change.

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(consoleLogOptions =>
{
    // Configure all logs to go to stderr
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

// Add MCP server with tools from the current assembly
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

IHost host = builder.Build();

host.Services.GetService<ILogger>();

await host.RunAsync();

/// <summary>
/// Tools for the Test Engine MCP server.
/// </summary>
[McpServerToolType]
public static class TestEngineTools
{
    private static readonly HttpClient HttpClient = new HttpClient();

    /// <summary>
    /// Validates a Power Fx expression.
    /// </summary>
    /// <param name="powerFx">The Power Fx expression to validate.</param>
    /// <returns>A JSON string indicating whether the expression is valid.</returns>
    [McpServerTool, Description("Validates a Power Fx expression.")]
    public static async Task<string> ValidatePowerFx(string powerFx)
    {
        if (string.IsNullOrWhiteSpace(powerFx))
        {
            return JsonSerializer.Serialize(new { valid = false, errors = new[] { "Power Fx string is empty." } });
        }

        var validationResult = await MakeRequest("validate", HttpMethod.Post, false, powerFx);
        return JsonSerializer.Serialize(validationResult);
    }

    /// Gets the list of Plan Designer plans.
    /// </summary>
    /// <returns>A JSON string containing the list of plans.</returns>
    [McpServerTool, Description("Gets the list of Plan Designer plans.")]
    public static async Task<string> GetPlanList()
    {
        var plan = await MakeRequest("plans", HttpMethod.Get, true);
        return JsonSerializer.Serialize(plan);
    }

    /// <summary>
    /// Gets details for a specific plan.
    /// </summary>
    /// <param name="planId">The ID of the plan.</param>
    /// <returns>A JSON string containing the plan details.</returns>
    [McpServerTool, Description("Gets details for a specific plan and scans the current workspace and provides facts and recommendations to help generate automated tests")]
    public static async Task<string> GetPlanDetails(string planId, string workspacePath)
    {
        var planDetails = await MakeRequest($"plans/{planId}", HttpMethod.Post, data: workspacePath);
        return JsonSerializer.Serialize(planDetails);
    }

    /// <summary>
    /// Gets details for a specific plan.
    /// </summary>
    /// <param name="planId">The ID of the plan.</param>
    /// <returns>A JSON string containing the plan details.</returns>
    [McpServerTool, Description("Gets details for available scan types.")]
    public static async Task<string> GetScanTypes()
    {
        var availableScans = await MakeRequest($"scans", HttpMethod.Get);
        return JsonSerializer.Serialize(availableScans);
    }

    /// <summary>
    /// Gets details for a specific plan.
    /// </summary>
    /// <param name="workspacePath">The open workspace to scan</param>
    /// <param name="scans">Optional list of scans to apply</param>
    /// <param name="scans">Optional post processing Power Fx statements to apply</param>
    /// <returns>A JSON string containing the plan details.</returns>
    [McpServerTool, Description("Gets details for workspace with optional scans and post processing Power Fx steps")]
    public static async Task<string> Scan(string workspacePath, string[] scans, string powerFx)
    {
        var scanResults = await MakeRequest($"workspace", HttpMethod.Post, data: JsonSerializer.Serialize(new WorkspaceRequest
        {
            Location = workspacePath,
            Scans = scans,
            PowerFx = powerFx
        }));
        return JsonSerializer.Serialize(scanResults);
    }

    /// <summary>
    /// Makes an HTTP request to the .NET server.
    /// </summary>
    /// <param name="endpoint">The endpoint to call.</param>
    /// <param name="method">The HTTP method to use.</param>
    /// <param name="data">The data to send (optional).</param>
    /// <returns>The response as an object.</returns>
    private static async Task<object?> MakeRequest(string endpoint, HttpMethod method, bool token = false, string data = null)
    {
        try
        {
            var request = new MCPRequest();
            request.Method = method.ToString();
            request.Endpoint = endpoint;

            if (!string.IsNullOrEmpty(data))
            {
                request.Body = data;
            }

            // Create and configure the necessary dependencies
            var logger = NullLogger.Instance;

            var fileSystem = new FileSystem(); // Replace with actual implementation of IFileSystem

            var parser = new YamlTestConfigParser(fileSystem);

            var testState = new TestState(parser); // Replace with actual implementation of ITestState

            var args = Environment.GetCommandLineArgs();
            if (args.Length > 1 && File.Exists(args[1]))
            {
                testState.ParseAndSetTestState(args[1], logger);
            }

            string tokenValue = String.Empty;
            string target = String.Empty;

            if (args.Length > 2 && Uri.TryCreate(args[2], UriKind.Absolute, out Uri? result) && result.Scheme == "https" && result.Host.EndsWith("dynamics.com"))
            {
                request.Target = args[2];
            }

            var singleTestInstanceState = new SingleTestInstanceState(); // Replace with actual implementation of ISingleTestInstanceState
            var testInfraFunctions = new PlaywrightTestInfraFunctions(testState, singleTestInstanceState, fileSystem); // Replace with actual implementation of ITestInfraFunctions

            // Configure the SingleTestInstanceState
            singleTestInstanceState.SetLogger(logger);

            // Create the MCPProvider instance
            var provider = new MCPProvider
            {
                TestSuite = testState.GetTestSuiteDefinition(),
                MCPTestSettings = testState.GetTestSettings(),
                FileSystem = fileSystem,
                Logger = logger
            };

            var response = await provider.HandleRequest(request);

            return response.Body;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error communicating with .NET server at {endpoint}: {ex.Message}");
            return new { error = $"Failed to communicate with the .NET server at {endpoint}." };
        }
    }
}
