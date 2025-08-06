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

        // Preview namespace property for this function
        private static readonly string PREVIEW_NAMESPACE = "Preview";

        // Define record type for parameters
        private static readonly RecordType _parameters = RecordType.Empty()
            .Add(new NamedFormulaType("WebResource", StringType.String))
            .Add(new NamedFormulaType("Location", StringType.String))
            .Add(new NamedFormulaType("Setup", StringType.String))
            .Add(new NamedFormulaType("Run", StringType.String))
            .Add(new NamedFormulaType("Expected", StringType.String));
        public AssertJavaScriptFunction(ILogger logger, IOrganizationService client, IFileSystem fileSystem = null, ITestState testState = null)
            : base("AssertJavaScript", FormulaType.Blank, _parameters)
        {
            _logger = logger;
            _client = client;
            _fileSystem = fileSystem ?? new FileSystem();
            _testState = testState;
        }

        public BlankValue Execute(RecordValue record)
        {
            try
            {
                return ExecuteAsync(record).Result;
            }
            catch (AggregateException aex) when (aex.InnerException is AssertionFailureException afe)
            {
                // Re-throw the AssertionFailureException from async method
                throw afe;
            }
            catch (AssertionFailureException)
            {
                // Re-throw AssertionFailureException as-is
                throw;
            }
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

        /// <summary>
        /// Checks if the required namespace is enabled in testSettings
        /// </summary>
        /// <returns>True if the required namespace is enabled, false otherwise</returns>
        private bool IsPreviewEnabled()
        {
            var testSettings = _testState?.GetTestSettings();

            // Check if Preview namespace is allowed in allowPowerFxNamespaces
            if (testSettings?.ExtensionModules?.AllowPowerFxNamespaces != null &&
                testSettings.ExtensionModules.AllowPowerFxNamespaces.Contains(PREVIEW_NAMESPACE))
            {
                return true;
            }

            return false;
        }

        public async Task<BlankValue> ExecuteAsync(RecordValue record)
        {
            // Check if Preview namespace is enabled - this function requires it
            if (!IsPreviewEnabled())
            {
                var errorMessage = $"AssertJavaScript function requires '{PREVIEW_NAMESPACE}' namespace to be enabled. Please add '{PREVIEW_NAMESPACE}' to 'allowPowerFxNamespaces' in testSettings.yaml extensionModules.";
                _logger.LogError(errorMessage);
                throw new AssertionFailureException(errorMessage);
            }

            _logger.LogInformation($"Starting JavaScript assertion execution with '{PREVIEW_NAMESPACE}' namespace enabled");

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
                _logger.LogError("Missing required parameter: Run");
                throw new AssertionFailureException("Missing required parameter: Run");
            }

            if (string.IsNullOrEmpty(expectedResult))
            {
                _logger.LogError("Missing required parameter: Expected");
                throw new AssertionFailureException("Missing required parameter: Expected");
            }

            // Retrieve web resource content from Dataverse if specified
            string webResourceContent = string.Empty;
            if (!string.IsNullOrEmpty(webResource))
            {
                webResourceContent = await RetrieveWebResourceContentAsync(webResource);
                if (string.IsNullOrEmpty(webResourceContent))
                {
                    _logger.LogError($"Could not retrieve web resource '{webResource}'");
                    throw new AssertionFailureException($"Could not retrieve web resource '{webResource}'");
                }
            }

            // Read JavaScript from local file if location is specified
            string locationContent = string.Empty;
            if (!string.IsNullOrEmpty(location))
            {
                locationContent = await ReadJavaScriptFileAsync(location);
                if (string.IsNullOrEmpty(locationContent))
                {
                    _logger.LogError($"Could not read JavaScript file from '{location}'");
                    throw new AssertionFailureException($"Could not read JavaScript file from '{location}'");
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
                // Mock the `window` object
                jsEngine.SetValue("window", new
                {
                    showDialogResponse = true,
                    showModalDialog = new Func<string, object, object, bool>((url, args, options) =>
                    {
                        _logger.LogInformation($"[JS WINDOW] showModalDialog called with URL: {url}, Args: {args}, Options: {options}");
                        return true; // Simulate a successful dialog response
                    })
                });

                // Mock the `console` object
                jsEngine.SetValue("console", new
                {
                    log = new Action<object>(message => _logger.LogInformation($"[JS LOG] {message}")),
                    error = new Action<object>(message => _logger.LogError($"[JS ERROR] {message}")),
                    warn = new Action<object>(message => _logger.LogWarning($"[JS WARN] {message}")),
                    debug = new Action<object>(message => _logger.LogDebug($"[JS DEBUG] {message}"))
                });

                // Execute setup code if provided (should run first to initialize environment)
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
                        throw new AssertionFailureException($"Setup code execution failed: {jex.Error}");
                    }
                }

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
                        throw new AssertionFailureException($"Web resource script execution failed: {jex.Error}");
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
                        throw new AssertionFailureException($"JavaScript file execution failed: {jex.Error}");
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
                    throw new AssertionFailureException($"JavaScript test code execution failed: {jex.Error}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Unexpected error: {ex.Message}");
                    throw new AssertionFailureException($"Unexpected error during JavaScript execution: {ex.Message}");
                }

                // Compare the result with expected value
                string actualResult = result.ToString();
                bool testPassed = string.Equals(actualResult, expectedResult);

                if (testPassed)
                {
                    _logger.LogInformation("JavaScript assertion passed");
                    return FormulaValue.NewBlank();
                }
                else
                {
                    _logger.LogError($"JavaScript assertion failed: Expected '{expectedResult}', got '{actualResult}'");
                    throw new AssertionFailureException($"JavaScript assertion failed: Expected '{expectedResult}', got '{actualResult}'");
                }
            }
            catch (AssertionFailureException)
            {
                // Re-throw AssertionFailureException as-is
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during JavaScript assertion execution");
                throw new AssertionFailureException($"JavaScript execution error: {ex.Message}");
            }
        }
    }
}
