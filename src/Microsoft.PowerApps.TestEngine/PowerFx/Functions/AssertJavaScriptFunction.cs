// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Jint;
using Jint.Native;
using Jint.Runtime;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Microsoft.PowerApps.TestEngine.PowerFx.Functions
{
    /// <summary>
    /// Execute JavaScript assertions for testing web resources and custom JavaScript
    /// </summary>
    public class AssertJavaScriptFunction : ReflectionFunction
    {
        private readonly ILogger _logger;
        private readonly IOrganizationService _client;
        private readonly IFileSystem _fileSystem;
        private readonly ITestState _testState;

        // Define record type for results
        private static readonly RecordType _result = RecordType.Empty()
            .Add(new NamedFormulaType("Success", BooleanType.Boolean))
            .Add(new NamedFormulaType("Message", StringType.String))
            .Add(new NamedFormulaType("Details", StringType.String));

        // Define record type for parameters
        private static readonly RecordType _parameters = RecordType.Empty()
            .Add(new NamedFormulaType("WebResource", StringType.String))
            .Add(new NamedFormulaType("Location", StringType.String))
            .Add(new NamedFormulaType("Setup", StringType.String))
            .Add(new NamedFormulaType("Run", StringType.String))
            .Add(new NamedFormulaType("Expected", StringType.String)); public AssertJavaScriptFunction(ILogger logger, IOrganizationService client, IFileSystem fileSystem = null, ITestState testState = null) : base(DPath.Root.Append(new DName("Preview")), "AssertJavaScript", _result, _parameters)
        {
            _logger = logger;
            _client = client;
            _fileSystem = fileSystem ?? new FileSystem();
            _testState = testState;
        }

        /// <summary>
        /// Executes JavaScript code and assertions to validate test conditions
        /// </summary>
        /// <param name="record">A record containing test parameters</param>
        /// <returns>A record with test results</returns>
        public RecordValue Execute(RecordValue record)
        {
            return ExecuteAsync(record).Result;
        }

        /// <summary>
        /// Retrieves the content of a web resource from Dataverse
        /// </summary>
        /// <param name="webResourceName">Name of the web resource</param>
        /// <returns>The content of the web resource as string</returns>
        private async Task<string> RetrieveWebResourceContentAsync(string webResourceName)
        {
            try
            {
                _logger.LogInformation($"Retrieving web resource '{webResourceName}'");

                if (_client == null)
                {
                    _logger.LogWarning("Organization service is not available, cannot retrieve web resource");
                    return string.Empty;
                }

                // Query for the web resource
                QueryExpression query = new QueryExpression("webresource")
                {
                    ColumnSet = new ColumnSet("content", "name"),
                    Criteria = new FilterExpression
                    {
                        Conditions =
                        {
                            new ConditionExpression("name", ConditionOperator.Equal, webResourceName)
                        }
                    }
                };

                EntityCollection results = await Task.Run(() => _client.RetrieveMultiple(query));

                if (results.Entities.Count == 0)
                {
                    _logger.LogWarning($"Web resource '{webResourceName}' not found");
                    return string.Empty;
                }

                Entity webResource = results.Entities[0];
                string content = webResource.Contains("content") ?
                    webResource["content"].ToString() : string.Empty;

                // Web resource content is stored as base64
                if (!string.IsNullOrEmpty(content))
                {
                    byte[] bytes = Convert.FromBase64String(content);
                    return Encoding.UTF8.GetString(bytes);
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving web resource '{webResourceName}'");
                return string.Empty;
            }
        }

        /// <summary>
        /// Reads JavaScript content from a local file
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <returns>The file content as string</returns>
        private async Task<string> ReadJavaScriptFileAsync(string filePath)
        {
            try
            {
                // Resolve the file path relative to the test config file if needed
                filePath = ResolveFilePath(filePath);

                _logger.LogInformation($"Reading JavaScript file from '{filePath}'");

                if (string.IsNullOrEmpty(filePath))
                {
                    return string.Empty;
                }

                if (!_fileSystem.FileExists(filePath))
                {
                    _logger.LogWarning($"File not found: '{filePath}'");
                    _logger.LogDebug($"Current directory: '{Directory.GetCurrentDirectory()}'");
                    return string.Empty;
                }

                return await Task.Run(() => _fileSystem.ReadAllText(filePath));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error reading file: '{filePath}'");
                return string.Empty;
            }
        }

        /// <summary>
        /// Resolves a file path relative to the test configuration file if it's not already an absolute path
        /// </summary>
        /// <param name="filePath">The file path to resolve</param>
        /// <returns>The resolved absolute file path</returns>
        private string ResolveFilePath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || Path.IsPathRooted(filePath) || _testState == null)
            {
                return filePath;
            }

            try
            {
                var testConfigFile = _testState.GetTestConfigFile();
                if (testConfigFile != null)
                {
                    var testResultDirectory = Path.GetDirectoryName(testConfigFile.FullName);
                    return Path.Combine(testResultDirectory, filePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Unable to resolve file path relative to test config: '{filePath}'");
            }

            return filePath;
        }

        public async Task<RecordValue> ExecuteAsync(RecordValue record)
        {
            _logger.LogInformation("Starting JavaScript assertion execution");

            // Extract parameters from the record
            string webResource = string.Empty;
            string location = string.Empty;
            string setupCode = string.Empty;
            string runCode = string.Empty;
            string expectedResult = string.Empty;

            // Extract values from record fields
            foreach (var field in record.Fields)
            {
                if (field.Name == "WebResource" && field.Value is StringValue webResourceVal)
                {
                    webResource = webResourceVal.Value;
                }
                else if (field.Name == "Location" && field.Value is StringValue locationVal)
                {
                    location = locationVal.Value;
                }
                else if (field.Name == "Setup" && field.Value is StringValue setupVal)
                {
                    setupCode = setupVal.Value;
                }
                else if (field.Name == "Run" && field.Value is StringValue runVal)
                {
                    runCode = runVal.Value;
                }
                else if (field.Name == "Expected" && field.Value is StringValue expectedVal)
                {
                    expectedResult = expectedVal.Value;
                }
            }

            // Validate required parameters
            if (string.IsNullOrEmpty(runCode))
            {
                return CreateErrorResult(false, "Missing required parameter", "The 'Run' parameter is required.");
            }

            if (string.IsNullOrEmpty(expectedResult))
            {
                return CreateErrorResult(false, "Missing required parameter", "The 'Expected' parameter is required.");
            }

            // Retrieve web resource content from Dataverse if specified
            string webResourceContent = string.Empty;
            if (!string.IsNullOrEmpty(webResource))
            {
                webResourceContent = await RetrieveWebResourceContentAsync(webResource);
                if (string.IsNullOrEmpty(webResourceContent))
                {
                    return CreateErrorResult(false, "Web resource error", $"Could not retrieve web resource '{webResource}'");
                }
            }

            // Read JavaScript from local file if location is specified
            string locationContent = string.Empty;
            if (!string.IsNullOrEmpty(location))
            {
                locationContent = await ReadJavaScriptFileAsync(location);
                if (string.IsNullOrEmpty(locationContent))
                {
                    return CreateErrorResult(false, "File error", $"Could not read JavaScript file from '{location}'");
                }
            }

            // Create a new engine instance for each test to ensure isolation
            Jint.Engine jsEngine = new Jint.Engine(options =>
            {
                options.Strict();  // Use strict mode
                options.TimeoutInterval(TimeSpan.FromSeconds(10));  // Prevent infinite loops
                options.MaxStatements(10000);  // Limit complexity
                // No CLR integration as per requirements
            });

            try
            {
                // Execute web resource content if it was retrieved
                if (!string.IsNullOrEmpty(webResourceContent))
                {
                    _logger.LogInformation("Executing web resource script");
                    try
                    {
                        await Task.Run(() => jsEngine.Execute(webResourceContent));
                    }
                    catch (JavaScriptException jex)
                    {
                        _logger.LogError($"Error in web resource script: {jex.Message}");
                        return CreateErrorResult(false, "Web resource execution failed",
                            $"Error: {jex.Error}");
                    }
                }

                // Execute local file content if it was read
                if (!string.IsNullOrEmpty(locationContent))
                {
                    _logger.LogInformation("Executing JavaScript file");
                    try
                    {
                        await Task.Run(() => jsEngine.Execute(locationContent));
                    }
                    catch (JavaScriptException jex)
                    {
                        _logger.LogError($"Error in JavaScript file: {jex.Message}");
                        return CreateErrorResult(false, "JavaScript file execution failed",
                            $"Error: {jex.Error}");
                    }
                }
                // Execute setup code if provided
                if (!string.IsNullOrEmpty(setupCode))
                {
                    _logger.LogInformation("Executing setup code");
                    try
                    {
                        // Check if setupCode is a file path
                        if (setupCode.EndsWith(".js", StringComparison.OrdinalIgnoreCase) && !setupCode.Contains("\n") && !setupCode.Contains(";"))
                        {
                            var setupFileContent = await ReadJavaScriptFileAsync(setupCode);
                            if (!string.IsNullOrEmpty(setupFileContent))
                            {
                                setupCode = setupFileContent;
                            }
                        }

                        await Task.Run(() => jsEngine.Execute(setupCode));
                    }
                    catch (JavaScriptException jex)
                    {
                        _logger.LogError($"Error in setup code: {jex.Message}");
                        return CreateErrorResult(false, "Setup code execution failed",
                            $"Error: {jex.Error}");
                    }
                }

                // Execute run code
                _logger.LogInformation("Executing test code");
                JsValue result;

                try
                {
                    result = await Task.Run(() => jsEngine.Evaluate(runCode));
                }
                catch (JavaScriptException jex)
                {
                    _logger.LogError($"Error in test code: {jex.Message}");
                    return CreateErrorResult(false, "Test code execution failed",
                        $"Error: {jex.Error}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Unexpected error: {ex.Message}");
                    return CreateErrorResult(false, "Unexpected error during execution", ex.ToString());
                }

                // Compare the result with expected value
                string actualResult = result.ToString();
                bool testPassed = string.Equals(actualResult, expectedResult);

                if (testPassed)
                {
                    _logger.LogInformation("Assertion passed");
                    return CreateSuccessResult();
                }
                else
                {
                    _logger.LogWarning($"Assertion failed: Expected '{expectedResult}', got '{actualResult}'");
                    return CreateErrorResult(false, "Assertion failed",
                        $"Expected '{expectedResult}', got '{actualResult}'");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during JavaScript assertion execution");
                return CreateErrorResult(false, "Execution error", ex.ToString());
            }
        }

        /// <summary>
        /// Creates a success result record
        /// </summary>
        private RecordValue CreateSuccessResult()
        {
            var success = new NamedValue("Success", FormulaValue.New(true));
            var message = new NamedValue("Message", FormulaValue.New("Assertion passed"));
            var details = new NamedValue("Details", FormulaValue.New(string.Empty));

            return RecordValue.NewRecordFromFields(_result, new[] { success, message, details });
        }

        /// <summary>
        /// Creates an error result record
        /// </summary>
        private RecordValue CreateErrorResult(bool success, string message, string details)
        {
            var successValue = new NamedValue("Success", FormulaValue.New(success));
            var messageValue = new NamedValue("Message", FormulaValue.New(message));
            var detailsValue = new NamedValue("Details", FormulaValue.New(details));

            return RecordValue.NewRecordFromFields(_result, new[] { successValue, messageValue, detailsValue });
        }
    }
}
