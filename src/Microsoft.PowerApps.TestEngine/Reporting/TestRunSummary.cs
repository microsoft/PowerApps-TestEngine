// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.PowerApps.TestEngine.Reporting.Format;
using Microsoft.PowerApps.TestEngine.System;
using Newtonsoft.Json;
using Path = System.IO.Path;

namespace Microsoft.PowerApps.TestEngine.Reporting
{
    /// <summary>
    /// Generates a summary report from test run results
    /// </summary>
    public class TestRunSummary : ITestRunSummary
    {
        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestRunSummary"/> class.
        /// </summary>
        /// <param name="fileSystem">The file system implementation</param>
        public TestRunSummary(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        /// <summary>
        /// Generates a summary report from test result files
        /// </summary>
        /// <param name="resultsDirectory">Directory containing the .trx files</param>
        /// <param name="outputPath">Path where the summary report will be saved</param>
        /// <returns>Path to the generated summary report</returns>
        public string GenerateSummaryReport(string resultsDirectory, string outputPath)
        {            if (string.IsNullOrEmpty(resultsDirectory))
            {
                throw new ArgumentException("Results directory cannot be null or empty", nameof(resultsDirectory));
            }
            
            if (!_fileSystem.Exists(resultsDirectory))
            {
                throw new ArgumentException($"Results directory does not exist: {resultsDirectory}", nameof(resultsDirectory));
            }

            if (string.IsNullOrEmpty(outputPath))
            {
                throw new ArgumentException("Output path cannot be null or empty", nameof(outputPath));
            }

            // Find all .trx files in the directory
            var trxFiles = _fileSystem.GetFiles(resultsDirectory, "*.trx");
            if (trxFiles.Length == 0)
            {
                throw new InvalidOperationException($"No .trx files found in directory: {resultsDirectory}");
            }

            var testRuns = new List<TestRun>();
            foreach (var trxFile in trxFiles)
            {
                var testRun = LoadTestRunFromFile(trxFile);
                if (testRun != null)
                {
                    testRuns.Add(testRun);
                }
            }

            // Generate the HTML report
            var reportHtml = GenerateHtmlReport(testRuns);
              // Make sure output directory exists
            var outputDirectory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDirectory) && !_fileSystem.Exists(outputDirectory))
            {
                _fileSystem.CreateDirectory(outputDirectory);
            }

            // Write the HTML report to the output file
            _fileSystem.WriteTextToFile(outputPath, reportHtml);

            return outputPath;
        }

        /// <summary>
        /// Loads a TestRun object from a .trx file
        /// </summary>
        /// <param name="trxFilePath">Path to the .trx file</param>
        /// <returns>A TestRun object representing the test run</returns>
        private TestRun LoadTestRunFromFile(string trxFilePath)
        {            try
            {
                var fileContent = _fileSystem.ReadAllText(trxFilePath);                if (string.IsNullOrWhiteSpace(fileContent))
                {
                    // Log warning about empty file
                    // Using System.Diagnostics.Debug instead of Console to avoid namespace conflicts                    Debug.WriteLine($"Warning: File {trxFilePath} is empty.");
                    return null;
                }
                
                var serializer = new XmlSerializer(typeof(TestRun));
                
                using var stringReader = new StringReader(fileContent);
                using var reader = XmlReader.Create(stringReader);
                
                var testRun = (TestRun)serializer.Deserialize(reader);                // Validate the deserialized object                
                if (testRun.Results == null || testRun.Results.UnitTestResults == null)
                {
                    // Log warning about invalid format
                    // Using Debug.WriteLine instead of Console to avoid namespace conflicts
                    Debug.WriteLine($"Warning: File {trxFilePath} has invalid format - missing test results.");
                    return null;
                }
                
                return testRun;
            }            catch (XmlException xmlEx)
            {
                // Using Debug.WriteLine instead of Console to avoid namespace conflicts
                Debug.WriteLine($"XML parsing error in file {trxFilePath}: {xmlEx.Message}");
                return null;
            }            catch (InvalidOperationException invOpEx)
            {
                // Using Debug.WriteLine instead of Console to avoid namespace conflicts
                Debug.WriteLine($"Serialization error in file {trxFilePath}: {invOpEx.Message}");
                return null;
            }
            catch (Exception ex)
            {
                // Log or handle the exception as needed
                // Using Debug.WriteLine instead of Console to avoid namespace conflicts
                Debug.WriteLine($"Error loading test run from file {trxFilePath}: {ex.Message}");
                return null;
            }
        }        /// <summary>
        /// Generates an HTML report from test runs
        /// </summary>
        /// <param name="testRuns">List of test runs</param>
        /// <returns>HTML content for the report</returns>
        private string GenerateHtmlReport(List<TestRun> testRuns)
        {
            // Get the HTML template from embedded resources
            var htmlTemplate = GetEmbeddedHtmlTemplate();            // Prepare summary data
            int totalTests = 0;
            int passedTests = 0;
            int failedTests = 0;
            int otherTests = 0; // Not passed or failed (e.g., skipped, inconclusive)
            DateTime? minStartTime = null;
            DateTime? maxEndTime = null;
            
            // Build the test results rows for both table format and card format
            var testResultsRows = new StringBuilder();
            var testResultsCards = new StringBuilder();// Group tests by test run
            var groupedTests = GroupTestsByRun(testRuns);
            
            // Process all test results for statistics
            foreach (var testRun in testRuns)
            {
                foreach (var testResult in testRun.Results.UnitTestResults)
                {
                    totalTests++;
                    
                    if (testResult.Outcome == TestReporter.PassedResultOutcome)
                    {
                        passedTests++;
                    }
                    else if (testResult.Outcome == TestReporter.FailedResultOutcome)
                    {
                        failedTests++;
                    }
                    else
                    {
                        otherTests++; // Count other outcomes (not passed or failed)
                    }
                    
                    // Track earliest start time and latest end time
                    if (testResult.StartTime != default)
                    {
                        if (minStartTime == null || testResult.StartTime < minStartTime)
                        {
                            minStartTime = testResult.StartTime;
                        }
                    }
                    
                    if (testResult.EndTime != default)
                    {
                        if (maxEndTime == null || testResult.EndTime > maxEndTime)
                        {
                            maxEndTime = testResult.EndTime;
                        }
                    }
                }
            }
              // Create JSON data for the Tabulator tables and charts
            var testsData = new List<Dictionary<string, object>>();
            var entityGroups = new Dictionary<string, (string entityType, int passes, int failures)>();
            
            // Process grouped test results to build the HTML
            foreach (var group in groupedTests)
            {
                // Add a group header
                testResultsRows.AppendLine($@"
                <tr class=""group-header"">
                    <td colspan=""6""><strong>{group.Key}</strong> ({group.Value.Count} tests)</td>
                </tr>");
                
                // Create a card for this group
                var groupCard = new StringBuilder();
                groupCard.AppendLine($@"
                <div class=""card test-details-card"" data-entity=""{group.Key}"">
                    <div class=""card-header"">{group.Key}</div>
                    <div class=""card-body"">
                        <div class=""table-responsive"">
                            <table class=""table table-hover"">
                                <thead>
                                    <tr>
                                        <th>Test Name</th>
                                        <th>Status</th>
                                        <th>Duration</th>
                                        <th>Start Time</th>
                                        <th>App URL</th>
                                        <th>Error</th>
                                    </tr>
                                </thead>
                                <tbody>");
                
                // Add test results for this group
                foreach (var (testResult, testRun) in group.Value)
                {
                    // Extract app URL and results path if available
                    string appUrl = "";
                    string resultsPath = "";
                    string videoPath = "";
                    string entityType = "Unknown"; // Default type
                    
                    if (testRun.ResultSummary?.Output?.StdOut != null)
                    {
                        try
                        {
                            // Try to parse the JSON content from StdOut
                            var stdOut = testRun.ResultSummary.Output.StdOut;
                            if (stdOut.Contains("AppURL") && stdOut.Contains("TestResults"))
                            {
                                // Simple parsing - could be improved with proper JSON parsing
                                appUrl = ExtractValueBetween(stdOut, "AppURL\": \"", "\"");
                                resultsPath = ExtractValueBetween(stdOut, "TestResults\": \"", "\"");
                                videoPath = ExtractValueBetween(stdOut, "VideoPath\": \"", "\"");
                                entityType = ExtractValueBetween(stdOut, "EntityType\": \"", "\"");
                            }
                        }
                        catch
                        {
                            // Ignore parsing errors
                        }
                    }
                    
                    // Track entity group statistics for coverage reporting
                    string entityName = group.Key;
                    if (!entityGroups.ContainsKey(entityName))
                    {
                        entityGroups[entityName] = (entityType, 0, 0);
                    }
                    var stats = entityGroups[entityName];
                    if (testResult.Outcome == TestReporter.PassedResultOutcome)
                    {
                        stats.passes++;
                    }
                    else if (testResult.Outcome == TestReporter.FailedResultOutcome)
                    {
                        stats.failures++;
                    }
                    entityGroups[entityName] = stats;                    // Format the duration
                    string duration = "N/A";
                    if (!string.IsNullOrEmpty(testResult.Duration))
                    {
                        TimeSpan durationTs;
                        if (TimeSpan.TryParse(testResult.Duration, out durationTs))
                        {
                            duration = $"{durationTs.TotalSeconds:F2}s";
                        }
                        else
                        {
                            duration = testResult.Duration;
                        }
                    }

                    // Get error message if test failed
                    string errorMessage = testResult.Output?.ErrorInfo?.Message ?? "";
                    bool hasError = !string.IsNullOrEmpty(errorMessage);

                    // Format the start time
                    string startTime = testResult.StartTime != default ? testResult.StartTime.ToString("yyyy-MM-dd HH:mm:ss") : "N/A";
                    
                    // Format end time
                    string endTime = testResult.EndTime != default ? testResult.EndTime.ToString("yyyy-MM-dd HH:mm:ss") : "N/A";
                    
                    // Convert app URL to a hyperlink if available
                    string appUrlLink = string.IsNullOrEmpty(appUrl) ? 
                        "" : 
                        $@"<a href=""{appUrl}"" target=""_blank"" class=""app-link"">{appUrl}</a>";
                        
                    // Format error message with a details toggle if it's too long
                    string errorDisplay = hasError && errorMessage.Length > 100 ? 
                        $@"<div class=""error-container"">
                            <div class=""error-message-short"">{errorMessage.Substring(0, 100)}...</div>
                            <button class=""details-toggle"" onclick=""toggleErrorDetails(this)"">Show More</button>
                            <div class=""error-details"" style=""display:none;""><pre>{errorMessage}</pre></div>
                        </div>" : 
                        errorMessage;
                        
                    // Build the row with appropriate styling based on outcome
                    string rowClass;
                    string badgeClass;
                    string statusIcon;
                    
                    if (testResult.Outcome == TestReporter.PassedResultOutcome)
                    {
                        rowClass = "success";
                        badgeClass = "badge-passed";
                        statusIcon = "<i class=\"fas fa-check-circle status-icon passed\"></i>";
                    }
                    else if (testResult.Outcome == TestReporter.FailedResultOutcome)
                    {
                        rowClass = "danger";
                        badgeClass = "badge-failed";
                        statusIcon = "<i class=\"fas fa-times-circle status-icon failed\"></i>";
                    }
                    else
                    {
                        rowClass = "warning";
                        badgeClass = "badge-result";
                        statusIcon = "<i class=\"fas fa-exclamation-triangle\"></i>";
                    }
                    
                    // Add to legacy table format
                    testResultsRows.AppendLine($@"
                    <tr class=""{rowClass}"">
                        <td>{testResult.TestName}</td>
                        <td><span class=""badge badge-{rowClass}"">{testResult.Outcome}</span></td>
                        <td>{startTime}</td>
                        <td>{duration}</td>
                        <td>{appUrlLink}</td>
                        <td>{errorDisplay}</td>
                    </tr>");
                      // Add to modern card format
                    // Use the videoPath variable that is already set above
                    string resultsDirectory = string.IsNullOrEmpty(resultsPath) ? 
                        string.Empty : 
                        Path.GetDirectoryName(resultsPath)?.Replace("\\", "/") ?? string.Empty;
                        
                    string videoSection = !string.IsNullOrEmpty(videoPath) ? 
                        $@"<div class=""video-actions mb-2"">
                            <button class=""btn btn-sm btn-primary toggle-video"" data-video-id=""video-{testResult.TestId}"" data-video-src=""{videoPath}"" data-timecode=""00:00:00"">
                                <i class=""fas fa-play-circle""></i> Play Video
                            </button>
                            <a href=""file:///{videoPath.Replace("\\", "/")}"" class=""btn btn-sm btn-outline-secondary"" target=""_blank"">
                                <i class=""fas fa-external-link-alt""></i> Open
                            </a>
                            {(!string.IsNullOrEmpty(resultsDirectory) ? 
                                $@"<button class=""btn btn-sm btn-outline-info show-log-files"" data-test-dir=""{resultsDirectory}"" data-test-name=""{testResult.TestName}"">
                                    <i class=""fas fa-file-alt""></i> Log Files
                                </button>" : "")}
                        </div>
                        <div class=""video-container"" id=""video-{testResult.TestId}"" style=""display: none;"">
                            <video width=""100%"" controls>
                                <source src=""file:///{videoPath.Replace("\\", "/")}"" type=""video/webm"">
                                Your browser does not support the video tag.
                            </video>
                        </div>" : "";
                        
                    groupCard.AppendLine($@"
                    <tr data-status=""{testResult.Outcome}"">
                        <td>{testResult.TestName}</td>
                        <td>
                            <span class=""badge-result {badgeClass}"">{statusIcon} {testResult.Outcome}</span>
                        </td>
                        <td>{duration}</td>
                        <td>{startTime}</td>
                        <td>{appUrlLink}</td>
                        <td>
                            {errorDisplay}
                            {videoSection}
                        </td>
                    </tr>");
                      // Add to tabulator data for the new report
                    testsData.Add(new Dictionary<string, object> {
                        { "testName", testResult.TestName },
                        { "outcome", testResult.Outcome },
                        { "duration", duration },
                        { "startTime", startTime },
                        { "endTime", endTime },
                        { "entityName", group.Key },
                        { "pageType", entityType },
                        { "errorMessage", errorMessage },
                        { "appUrl", appUrl },
                        { "resultsPath", resultsPath },
                        { "videoPath", videoPath }
                    });
                }
                
                // Finalize and add the card
                groupCard.AppendLine($@"
                                </tbody>
                            </table>
                        </div>
                    </div>
                </div>");
                
                testResultsCards.Append(groupCard);
            }            // Calculate pass percentage and total duration
            double passPercentage = totalTests > 0 ? (double)passedTests / totalTests * 100 : 0;
            string totalDuration = "N/A";
            if (minStartTime.HasValue && maxEndTime.HasValue)
            {
                TimeSpan duration = maxEndTime.Value - minStartTime.Value;
                totalDuration = $"{duration.TotalMinutes:F2} minutes";
            }
            
            // Calculate health score - currently using pass percentage
            int healthScore = (int)Math.Round(passPercentage);
              // Prepare coverage data
            var coverageData = new List<Dictionary<string, object>>();
            var coverageLabels = new List<string>();
            var coveragePassData = new List<int>();
            var coverageFailData = new List<int>();
            
            // Add default values in case there are no entity groups
            if (entityGroups.Count == 0)
            {
                coverageLabels.Add("No Data");
                coveragePassData.Add(0);
                coverageFailData.Add(0);
            }
            
            foreach (var entity in entityGroups)
            {
                var name = entity.Key;
                var type = entity.Value.entityType;
                var passes = entity.Value.passes;
                var failures = entity.Value.failures;
                var total = passes + failures;
                var status = (passes > 0 && failures == 0) ? "Healthy" : 
                             (passes == 0 && failures > 0) ? "Failed" :
                             "Mixed Results";
                             
                coverageData.Add(new Dictionary<string, object> {
                    { "entityName", name },
                    { "entityType", type },
                    { "status", status },
                    { "passes", passes },
                    { "failures", failures }
                });
                
                coverageLabels.Add(name);
                coveragePassData.Add(passes);
                coverageFailData.Add(failures);
            }
              // Ensure test data isn't empty
            if (testsData.Count == 0)
            {
                testsData.Add(new Dictionary<string, object> {
                    { "testName", "No tests found" },
                    { "outcome", "N/A" },
                    { "duration", "N/A" },
                    { "startTime", "N/A" },
                    { "endTime", "N/A" },
                    { "entityName", "N/A" },
                    { "pageType", "N/A" },
                    { "errorMessage", "" },
                    { "appUrl", "" },
                    { "resultsPath", "" },
                    { "videoPath", "" }
                });
            }
              // Serialize the JSON data for Tabulator and charts
            Func<object, string> serializeObject = (object obj) => {
                try {
                    // Use Newtonsoft.Json instead of System.Text.Json
                    return Newtonsoft.Json.JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.Indented);
                }
                catch (Exception) {
                    // Fallback for serialization errors
                    return "[]";
                }
            };

            // Create the legacy summary statistics
            var summaryTable = $@"
            <div class=""summary-stats row"">
                <div class=""col-md-3"">
                    <div class=""stat-box total"">
                        <div class=""stat-value"">{totalTests}</div>
                        <div class=""stat-label"">Total Tests</div>
                    </div>
                </div>
                <div class=""col-md-3"">
                    <div class=""stat-box success"">
                        <div class=""stat-value"">{passedTests}</div>
                        <div class=""stat-label"">Passed</div>
                    </div>
                </div>
                <div class=""col-md-3"">
                    <div class=""stat-box danger"">
                        <div class=""stat-value"">{failedTests}</div>
                        <div class=""stat-label"">Failed</div>
                    </div>
                </div>
                <div class=""col-md-3"">
                    <div class=""stat-box info"">
                        <div class=""stat-value"">{passPercentage:F2}%</div>
                        <div class=""stat-label"">Pass Rate</div>
                    </div>
                </div>
            </div>
            
            <div class=""summary-details"">
                <table class=""table table-bordered"">
                    <tbody>
                        <tr>
                            <th>Start Time</th>
                            <td>{minStartTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A"}</td>
                        </tr>
                        <tr>
                            <th>End Time</th>
                            <td>{maxEndTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A"}</td>
                        </tr>
                        <tr>
                            <th>Total Duration</th>
                            <td>{totalDuration}</td>
                        </tr>
                        <tr>
                            <th>Test Files</th>
                            <td>{testRuns.Count}</td>
                        </tr>
                    </tbody>
                </table>
            </div>";
            
            // Create health score calculation explanation
            var healthCalculation = $@"
            <div class=""health-calculation"">
                <h3>Health Score Calculation</h3>
                <div class=""calculation-step"">
                    <p>Total Tests: {totalTests}</p>
                    <p>Passed Tests: {passedTests}</p>
                    <p>Failed Tests: {failedTests}</p>
                </div>
                <div class=""calculation-step"">
                    <p>Health Score = (Passed Tests / Total Tests) * 100</p>
                    <p>Health Score = ({passedTests} / {totalTests}) * 100</p>
                    <p>Health Score = {passPercentage:F2}%</p>
                </div>
                <div class=""calculation-result"">
                    Final Health Score: {healthScore}%
                </div>
            </div>";

            // Create environment information
            string machineName = Environment.MachineName;
            string osVersion = Environment.OSVersion.ToString();
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string environment = $@"
            <table class=""table table-bordered"">
                <tbody>
                    <tr>
                        <th>Machine Name</th>
                        <td>{machineName}</td>
                    </tr>
                    <tr>
                        <th>Operating System</th>
                        <td>{osVersion}</td>
                    </tr>
                    <tr>
                        <th>Report Generated</th>
                        <td>{timestamp}</td>
                    </tr>
                    <tr>
                        <th>PowerApps Test Engine</th>
                        <td>{GetAssemblyVersion()}</td>
                    </tr>
                    <tr>
                        <th>Start Time</th>
                        <td>{minStartTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A"}</td>
                    </tr>
                    <tr>
                        <th>End Time</th>
                        <td>{maxEndTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A"}</td>
                    </tr>
                    <tr>
                        <th>Total Duration</th>
                        <td>{totalDuration}</td>
                    </tr>
                    <tr>
                        <th>Test Files</th>
                        <td>{testRuns.Count}</td>
                    </tr>
                </tbody>
            </table>";

            // Replace placeholders in the new template
            var report = htmlTemplate
                // Legacy placeholders
                .Replace("{{SUMMARY_TABLE}}", summaryTable)
                .Replace("{{ENVIRONMENT_INFO}}", environment)
                .Replace("{{TEST_RESULTS}}", testResultsRows.ToString())
                .Replace("{{REPORT_DATE}}", timestamp)
                .Replace("{{TITLE}}", $"PowerApps Test Engine Results - {healthScore}% Health Score")
                
                // New template placeholders
                .Replace("{{PASS_COUNT}}", passedTests.ToString())
                .Replace("{{FAIL_COUNT}}", failedTests.ToString())
                .Replace("{{TOTAL_COUNT}}", totalTests.ToString())
                .Replace("{{HEALTH_PERCENT}}", healthScore.ToString())
                .Replace("{{TEST_RESULTS_CARDS}}", testResultsCards.ToString())
                .Replace("{{HEALTH_CALCULATION}}", healthCalculation)
                .Replace("{{TESTS_DATA}}", serializeObject(testsData))
                .Replace("{{COVERAGE_DATA}}", serializeObject(coverageData))
                .Replace("{{COVERAGE_CHART_LABELS}}", serializeObject(coverageLabels))
                .Replace("{{COVERAGE_CHART_PASS_DATA}}", serializeObject(coveragePassData))
                .Replace("{{COVERAGE_CHART_FAIL_DATA}}", serializeObject(coverageFailData));

            return report;
        }        /// <summary>
        /// Gets the HTML template from embedded resources
        /// </summary>
        /// <returns>The HTML template string</returns>
        private string GetEmbeddedHtmlTemplate()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Microsoft.PowerApps.TestEngine.Reporting.Templates.TestRunSummaryTemplate.html";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");
            }

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
        
        /// <summary>
        /// Gets all TRX files from a directory
        /// </summary>
        /// <param name="directoryPath">Directory to search</param>
        /// <returns>Array of file paths</returns>
        public string[] GetTrxFiles(string directoryPath)
        {
            if (string.IsNullOrEmpty(directoryPath))
            {
                throw new ArgumentException("Directory path cannot be null or empty", nameof(directoryPath));
            }
            
            if (!_fileSystem.Exists(directoryPath))
            {
                throw new ArgumentException($"Directory does not exist: {directoryPath}", nameof(directoryPath));
            }
            
            return _fileSystem.GetFiles(directoryPath, "*.trx");
        }

        /// <summary>
        /// Groups test results by test run for better organization in the report
        /// </summary>
        /// <param name="testRuns">List of test runs</param>
        /// <returns>Dictionary mapping test run names to their results</returns>
        private Dictionary<string, List<(UnitTestResult Result, TestRun Run)>> GroupTestsByRun(List<TestRun> testRuns)
        {
            var grouped = new Dictionary<string, List<(UnitTestResult, TestRun)>>();
            
            foreach (var testRun in testRuns)
            {
                string runName = !string.IsNullOrEmpty(testRun.Name) ? testRun.Name : $"Run ID: {testRun.Id}";
                
                if (!grouped.ContainsKey(runName))
                {
                    grouped[runName] = new List<(UnitTestResult, TestRun)>();
                }
                
                foreach (var result in testRun.Results.UnitTestResults)
                {
                    grouped[runName].Add((result, testRun));
                }
            }
            
            return grouped;
        }

        /// <summary>
        /// Gets the current assembly version
        /// </summary>
        /// <returns>The assembly version string</returns>
        private string GetAssemblyVersion()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                return version?.ToString() ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        /// <summary>
        /// Extracts a value between two strings from a source string
        /// </summary>
        /// <param name="source">The source string</param>
        /// <param name="start">The start delimiter</param>
        /// <param name="end">The end delimiter</param>
        /// <returns>The extracted value or empty string if not found</returns>
        private string ExtractValueBetween(string source, string start, string end)
        {
            int startIndex = source.IndexOf(start);
            if (startIndex < 0)
                return string.Empty;

            startIndex += start.Length;
            int endIndex = source.IndexOf(end, startIndex);
            
            if (endIndex < 0)
                return string.Empty;

            return source.Substring(startIndex, endIndex - startIndex);
        }
    }
}
