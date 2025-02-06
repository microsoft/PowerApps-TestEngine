// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.PowerFx;
using Microsoft.PowerApps.TestEngine.Providers;
using Microsoft.PowerApps.TestEngine.Reporting;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerApps.TestEngine.Users;
using System;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Linq;
using System.IO;

namespace testengine.plugin.copilot
{
    public class EvaluateTestSteps
    {
        public string EnvironmentId { get; set; }

        public async Task<string> ExecuteAsync(string identifer, string steps, string token)
        {
            string result = "Unknown";
            try
            {
                // Create the YAML file dynamically
                var yamlContent = CreateYamlFromSteps(identifer, steps);

                // Pass the YAML file to the Test Engine
                return await ExecuteTestEngineAsync(yamlContent, token);
            } 
            catch (Exception ex)
            {
                result = ex.Message;
            }

            // Set the output parameters
            return result;
        }

        public string CreateYamlFromSteps(string identifer, string steps)
        {
            // Logic to create YAML content from steps
            return string.Format(@"
# yaml-embedded-languages: powerfx
testSuite:
  testSuiteName: Template
  testSuiteDescription: Verifies that the calculator app works. The calculator is a component.
  persona: User1
  appId: {0}

  testCases:
    - testCaseName: Test Case
      testSteps: |
        = {1}

testSettings:
  locale: ""en-US""
  recordVideo: true
  browserConfigurations:
    - browser: Chromium

environmentVariables:
  users:
    - personaName: User1
      emailKey: user1Email", identifer, steps.Replace("\r", "").Replace("\n", ""));
        }

        public async Task<string> ExecuteTestEngineAsync(string yamlContent, string token)
        {
            var fileSystem = new InMemoryFileSystem();

            fileSystem.Files.Add("test.yaml", yamlContent);

            var state = new TestState(new YamlTestConfigParser(fileSystem));
            state.SetEnvironment(this.EnvironmentId);

            using var loggerFactory = LoggerFactory.Create(loggingBuilder => loggingBuilder
                        .ClearProviders()
                        .SetMinimumLevel(LogLevel.Debug)
                        .AddProvider(new TestLoggerProvider(fileSystem)));

            var logger = loggerFactory.CreateLogger<EvaluateTestSteps>();

            var environment = new InMemoryEnvironment();
            var provider = new CopilotAPIProvider() { Environment = environment };

            environment.Values.Add("AgentToken", token);
            
            var serviceProvider = new ServiceCollection()
                .AddSingleton<ITestState>(state)
                .AddSingleton<ILoggerFactory>(loggerFactory)
                .AddSingleton<ITestEngineEvents, TestEngineEventHandler>()
                .AddSingleton<ITestConfigParser, YamlTestConfigParser>()
                .AddSingleton<ITestWebProvider>(sp => provider)
                .AddScoped<IPowerFxEngine, PowerFxEngine>()
                .AddSingleton<ITestReporter, TestReporter>()
                .AddScoped<ISingleTestInstanceState, SingleTestInstanceState>()
                .AddScoped<ISingleTestRunner, SingleTestRunner>()
                .AddSingleton<ILogger>((sp) => logger)
                .AddSingleton<IFileSystem>(fileSystem)
                .AddScoped<ITestInfraFunctions, PlaywrightTestInfraFunctions>()
                .AddSingleton<IEnvironmentVariable>(environment)
                .AddSingleton<IUserManagerLogin, UserManagerLogin>()
                // START Not needed for this provider
                .AddTransient<IUserManager>(sp => null)
                .AddTransient<IUserCertificateProvider>(sp => null)
                // END Not Needed
                .AddSingleton<TestEngine>()
                .BuildServiceProvider();

            var testEngine = serviceProvider.GetService<TestEngine>();

            var report = await testEngine.RunTestAsync(new System.IO.FileInfo("test.yaml"), EnvironmentId, Guid.Empty, new System.IO.DirectoryInfo("TestEngineOutput"), string.Empty, string.Empty);

            var json = XmlJsonConvertor.ConvertXmlToJson(fileSystem.ReadAllText(report));

            JsonNode jsonNode = JsonNode.Parse(json);

            // Access the number of passed test cases directly
            int passedTestCases = GetValue<int>(jsonNode, "ResultSummary", "Counters", "passed");
            int failedTestCases = GetValue<int>(jsonNode, "ResultSummary", "Counters", "failed");
            string duration = GetValue<string>(jsonNode, "Results", "UnitTestResult", "duration");
            string outcome = GetValue<string>(jsonNode, "Results", "UnitTestResult", "outcome");

            var logFile = fileSystem.Files
                .Where(f => Path.GetFileName(f.Key) == "logs.txt")
                .Select(f => f.Key)
                .OrderByDescending(f => f.Length)
                .FirstOrDefault<string>();

            var debugLogFile = fileSystem.Files
                .Where(f => Path.GetFileName(f.Key) == "debugLogs.txt")
                .Select(f => f.Key)
                .OrderByDescending(f => f.Length)
                .FirstOrDefault<string>();

            var id = "UNKNOWN";
            try
            {
                id = provider.GetPropertyValueFromControl<string>(new ItemPath { PropertyName = "ConversationId" });
            } 
            catch
            {

            }

            var result = new {
                ConversationId = id,
                Duration = duration,
                Outcome = outcome,
                Pass = passedTestCases,
                Failed = failedTestCases,
                TestResults = json,
                Logs = !string.IsNullOrEmpty(logFile) ? fileSystem.ReadAllText(logFile) : string.Empty,
                DebugLogs = !string.IsNullOrEmpty(debugLogFile) ? fileSystem.ReadAllText(debugLogFile) : string.Empty
            };

            return JsonSerializer.Serialize(result);
        }

        private static T GetValue<T>(JsonNode jsonNode, params string[] keys)
        {
            JsonNode currentNode = jsonNode;
            foreach (var key in keys)
            {
                currentNode = currentNode[key];
                if (currentNode == null)
                {
                    throw new InvalidOperationException($"Key '{key}' not found in JSON.");
                }
            }

            // Handle text-to-int conversion
            if (typeof(T) == typeof(int) && currentNode is JsonValue jsonValue && jsonValue.TryGetValue(out string stringValue))
            {
                if (int.TryParse(stringValue, out int intValue))
                {
                    return (T)(object)intValue;
                }
                throw new InvalidOperationException($"Value '{stringValue}' cannot be converted to int.");
            }

            return currentNode.GetValue<T>();
        }
    }
}
