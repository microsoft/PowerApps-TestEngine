// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.PowerApps.TestEngine.Reporting.Format;
using Microsoft.PowerApps.TestEngine.System;
using Path = System.IO.Path;

namespace Microsoft.PowerApps.TestEngine.Reporting
{
    /// <summary>
    /// Represents environment information for the test run report
    /// </summary>
    public class EnvironmentInfo
    {
        /// <summary>
        /// Machine name where the tests were run
        /// </summary>
        public string MachineName { get; set; }

        /// <summary>
        /// Operating system information
        /// </summary>
        public string OperatingSystem { get; set; }

        /// <summary>
        /// When the report was generated
        /// </summary>
        public string ReportTimestamp { get; set; }

        /// <summary>
        /// Version of the PowerApps Test Engine
        /// </summary>
        public string TestEngineVersion { get; set; }

        /// <summary>
        /// Test run start time
        /// </summary>
        public string StartTime { get; set; }

        /// <summary>
        /// Test run end time
        /// </summary>
        public string EndTime { get; set; }

        /// <summary>
        /// Total duration of the test run
        /// </summary>
        public string TotalDuration { get; set; }

        /// <summary>
        /// Number of test files processed
        /// </summary>
        public int TestFileCount { get; set; }
    }

    /// <summary>
    /// Class to encapsulate all data needed for the HTML report template
    /// </summary>
    public class TemplateData
    {
        // Summary data
        public int TotalTests { get; set; }
        public int PassedTests { get; set; }
        public int FailedTests { get; set; }
        public double PassPercentage { get; set; }
        public int HealthScore { get; set; }
        public string TotalDuration { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public int TestFileCount { get; set; }        // Environment information
        public EnvironmentInfo Environment { get; set; }

        // Test data
        public List<Dictionary<string, object>> TestsData { get; set; }
        public string TestResultsRows { get; set; }
        public string TestResultsCards { get; set; }

        // Structured test results data - will eventually replace TestResultsRows and TestResultsCards
        public Dictionary<string, List<Dictionary<string, object>>> GroupedTests { get; set; }

        // Coverage data
        public List<Dictionary<string, object>> CoverageData { get; set; }
        public List<string> CoverageLabels { get; set; }
        public List<int> CoveragePassData { get; set; }
        public List<int> CoverageFailData { get; set; }

        // Application metadata
        public Dictionary<string, Dictionary<string, int>> AppTypes { get; set; }
        public Dictionary<string, Dictionary<string, int>> EntityTypes { get; set; }

        // Video information
        public List<Dictionary<string, object>> Videos { get; set; }
        public TemplateData()
        {
            TestsData = new List<Dictionary<string, object>>();
            GroupedTests = new Dictionary<string, List<Dictionary<string, object>>>();
            CoverageData = new List<Dictionary<string, object>>();
            CoverageLabels = new List<string>();
            CoveragePassData = new List<int>();
            CoverageFailData = new List<int>();
            AppTypes = new Dictionary<string, Dictionary<string, int>>();
            EntityTypes = new Dictionary<string, Dictionary<string, int>>();
            Videos = new List<Dictionary<string, object>>();
            TestResultsRows = string.Empty;
            TestResultsCards = string.Empty;
            TotalDuration = "N/A";
            StartTime = "N/A";
            EndTime = "N/A";
            TestFileCount = 0;
            HealthScore = 0;
            Environment = new EnvironmentInfo
            {
                MachineName = global::System.Environment.MachineName,
                OperatingSystem = global::System.Environment.OSVersion.ToString(),
                ReportTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                TotalDuration = "N/A",
                StartTime = "N/A",
                EndTime = "N/A",
                TestFileCount = 0
            };
        }
    }

    /// <summary>
    /// Generates a summary report from test run results
    /// </summary>
    public class TestRunSummary : ITestRunSummary
    {
        private readonly IFileSystem _fileSystem;

        public Func<string, string[]> GetTrxFiles = path => Directory.GetFiles(path, "*.trx", SearchOption.AllDirectories);

        // Function to find video files in a directory and its subdirectories
        public Func<string, string[]> GetVideoFiles = path => Directory.GetFiles(path, "*.webm", SearchOption.AllDirectories);

        /// <summary>
        /// Initializes a new instance of the <see cref="TestRunSummary"/> class.
        /// </summary>
        /// <param name="fileSystem">The file system implementation</param>
        public TestRunSummary(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;

            // Ensure template files exist
            EnsureTemplatesExist();
        }

        /// <summary>
        /// Generates a summary report from test result files
        /// </summary>
        /// <param name="resultsDirectory">Directory containing the .trx files</param>
        /// <param name="outputPath">Path where the summary report will be saved</param>
        /// <param name="runName">Optional name to filter test runs by. If specified, only includes test runs with matching names</param>
        /// <returns>Path to the generated summary report</returns>
        public string GenerateSummaryReport(string resultsDirectory, string outputPath, string runName = null)
        {
            if (string.IsNullOrEmpty(resultsDirectory))
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
            var trxFiles = GetTrxFiles(resultsDirectory);
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
                    // Filter by runName if specified
                    if (string.IsNullOrEmpty(runName) ||
                        (testRun.Name != null &&
                         testRun.Name.IndexOf(runName, StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        testRuns.Add(testRun);
                    }
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
            _fileSystem.WriteTextToFile(outputPath, reportHtml, overwrite: true);

            return outputPath;
        }

        /// <summary>
        /// Loads a TestRun object from a .trx file and enhances it with video information
        /// </summary>
        /// <param name="trxFilePath">Path to the .trx file</param>
        /// <returns>A TestRun object representing the test run</returns>
        public TestRun LoadTestRunFromFile(string trxFilePath)
        {
            try
            {
                var fileContent = _fileSystem.ReadAllText(trxFilePath);

                if (string.IsNullOrWhiteSpace(fileContent))
                {
                    // Log warning about empty file
                    Debug.WriteLine($"Warning: File {trxFilePath} is empty.");
                    return null;
                }

                var serializer = new XmlSerializer(typeof(TestRun));

                using var stringReader = new StringReader(fileContent);
                using var reader = XmlReader.Create(stringReader);

                var testRun = (TestRun)serializer.Deserialize(reader);
                // Validate the deserialized object                
                if (testRun.Results == null || testRun.Results.UnitTestResults == null)
                {
                    // Log warning about invalid format
                    Debug.WriteLine($"Warning: File {trxFilePath} has invalid format - missing test results.");
                    return null;
                }

                // Check if the test run has videos associated with it
                // Look for video files in the same directory as the trx file and its parent directory
                var trxDirectory = Path.GetDirectoryName(trxFilePath);
                if (!string.IsNullOrEmpty(trxDirectory))
                {
                    try
                    {
                        // Search for video files recursively in the directory and parent directory
                        var videoFiles = GetVideoFiles(trxDirectory);

                        // If there are video files, add them to the test run output
                        if (videoFiles.Length > 0)
                        {
                            // Find the valid video files (filter out incomplete recordings)
                            long minValidSize = 10 * 1024; // 10KB minimum to consider a video valid
                            var validVideos = videoFiles
                                .Where(v => _fileSystem.GetFileSize(v) >= minValidSize)
                                .ToArray();

                            if (validVideos.Length > 0)
                            {
                                // In PowerApps TestEngine, videos are associated with the entire test session,
                                // not with individual tests. Store video paths in the ResultSummary.
                                if (testRun.ResultSummary == null)
                                {
                                    testRun.ResultSummary = new TestResultSummary();
                                }

                                if (testRun.ResultSummary.Output == null)
                                {
                                    testRun.ResultSummary.Output = new TestOutput();
                                }

                                // Initialize or format StdOut
                                if (testRun.ResultSummary.Output.StdOut == null)
                                {
                                    testRun.ResultSummary.Output.StdOut = "{ ";
                                }
                                else if (!testRun.ResultSummary.Output.StdOut.Contains("{"))
                                {
                                    testRun.ResultSummary.Output.StdOut = "{ " + testRun.ResultSummary.Output.StdOut;
                                }

                                if (testRun.ResultSummary.Output.StdOut.EndsWith("}"))
                                {
                                    testRun.ResultSummary.Output.StdOut = testRun.ResultSummary.Output.StdOut.Substring(0, testRun.ResultSummary.Output.StdOut.Length - 1);
                                }

                                // Add video paths as JSON - only if they don't already exist
                                if (!testRun.ResultSummary.Output.StdOut.Contains("\"VideoPath\"") &&
                                    !testRun.ResultSummary.Output.StdOut.Contains("\"Videos\""))
                                {
                                    // Always sort videos by size (larger files first - more likely to be complete)
                                    validVideos = validVideos
                                        .OrderByDescending(v => _fileSystem.GetFileSize(v))
                                        .ToArray();

                                    if (testRun.ResultSummary.Output.StdOut.Length > 0)
                                    {
                                        testRun.ResultSummary.Output.StdOut += ", ";
                                    }

                                    // For backward compatibility, include VideoPath for the first video
                                    if (validVideos.Length >= 1)
                                    {
                                        testRun.ResultSummary.Output.StdOut += $"\"VideoPath\": \"{validVideos[0].Replace("\\", "\\\\")}\", ";
                                    }

                                    // Always add Videos array regardless of the number of videos
                                    // This ensures the test has consistent access to the Videos property
                                    testRun.ResultSummary.Output.StdOut += "\"Videos\": [";
                                    for (int i = 0; i < validVideos.Length; i++)
                                    {
                                        if (i > 0)
                                            testRun.ResultSummary.Output.StdOut += ", ";
                                        testRun.ResultSummary.Output.StdOut += $"\"{validVideos[i].Replace("\\", "\\\\")}\"";
                                    }
                                    testRun.ResultSummary.Output.StdOut += "], ";
                                }

                                testRun.ResultSummary.Output.StdOut = testRun.ResultSummary.Output.StdOut.Trim();
                                if (testRun.ResultSummary.Output.StdOut.EndsWith(","))
                                {
                                    testRun.ResultSummary.Output.StdOut = testRun.ResultSummary.Output.StdOut.Substring(0, testRun.ResultSummary.Output.StdOut.Length - 1) + "}";
                                }

                                // Calculate estimated timecodes for each test based on its position
                                // and duration in the test run sequence
                                TimeSpan cumulativeTime = TimeSpan.Zero;
                                foreach (var testResult in testRun.Results.UnitTestResults)
                                {
                                    string timecodeStart = cumulativeTime.ToString(@"hh\:mm\:ss");

                                    // Parse duration and add to cumulative time
                                    if (!string.IsNullOrEmpty(testResult.Duration))
                                    {
                                        TimeSpan durationTimeSpan;
                                        if (TimeSpan.TryParse(testResult.Duration, out durationTimeSpan))
                                        {
                                            // Add the timecode to the test result for use in the report
                                            if (testResult.Output == null)
                                            {
                                                testResult.Output = new TestOutput();
                                            }

                                            if (testResult.Output.StdOut == null)
                                            {
                                                testResult.Output.StdOut = "";
                                            }

                                            // Add the timecode only if it doesn't already exist
                                            if (!testResult.Output.StdOut.Contains("TimecodeStart"))
                                            {
                                                if (testResult.Output.StdOut.Length > 0)
                                                {
                                                    testResult.Output.StdOut += ", ";
                                                }
                                                testResult.Output.StdOut += $" TimecodeStart: \"{timecodeStart}\"";
                                            }

                                            cumulativeTime = cumulativeTime.Add(durationTimeSpan);

                                            // Add the timecode end as well for completeness
                                            string timecodeEnd = cumulativeTime.ToString(@"hh\:mm\:ss");
                                            if (!testResult.Output.StdOut.Contains("TimecodeEnd"))
                                            {
                                                if (testResult.Output.StdOut.Length > 0)
                                                {
                                                    testResult.Output.StdOut += ", ";
                                                }
                                                testResult.Output.StdOut += $" TimecodeEnd: \"{timecodeEnd}\"";
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log any errors with finding video files
                        Debug.WriteLine($"Error finding video files for {trxFilePath}: {ex.Message}");
                    }
                }

                return testRun;
            }
            catch (XmlException xmlEx)
            {
                // Using Debug.WriteLine instead of Console to avoid namespace conflicts
                Debug.WriteLine($"XML parsing error in file {trxFilePath}: {xmlEx.Message}");
                return null;
            }
            catch (InvalidOperationException invOpEx)
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
        }

        /// <summary>
        /// Generates template context required by an HTML report from test runs
        /// </summary>
        /// <param name="testRuns">List of test runs</param>
        /// <returns>Template data from the runs</returns>
        public TemplateData GetTemplateContext(List<TestRun> testRuns)
        {
            // Create template data container
            var templateData = new TemplateData();            // Prepare summary data
            int totalTests = 0;
            int passedTests = 0;
            int failedTests = 0;
            int otherTests = 0; // Not passed or failed (e.g., skipped, inconclusive)
            DateTime? minStartTime = null;
            DateTime? maxEndTime = null;
            
            // Track overall test run times from TestRun.Times properties
            DateTime? runStartTime = null;
            DateTime? runEndTime = null;

            // Build the test results rows for both table format and card format
            var testResultsRows = new StringBuilder();
            var testResultsCards = new StringBuilder();

            // Group tests by test run
            var groupedTests = GroupTestsByRun(testRuns);            // Process all test results for statistics
            foreach (var testRun in testRuns)
            {
                // Track overall test run times from TestRun.Times
                if (testRun.Times != null)
                {
                    if (testRun.Times.Start != default)
                    {
                        if (runStartTime == null || testRun.Times.Start < runStartTime)
                        {
                            runStartTime = testRun.Times.Start;
                        }
                    }

                    if (testRun.Times.Finish != default)
                    {
                        if (runEndTime == null || testRun.Times.Finish > runEndTime)
                        {
                            runEndTime = testRun.Times.Finish;
                        }
                    }
                }
                
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

                    // Track earliest start time and latest end time for individual tests
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

            // Collect coverage data grouped by page type
            var entityGroups = CollectCoverageData(testRuns);

            // Process grouped test results to build the HTML
            foreach (var group in groupedTests)
            {
                // Create a list to hold this group's test data
                var groupTestData = new List<Dictionary<string, object>>();
                templateData.GroupedTests[group.Key] = groupTestData;

                // Add a group header
                testResultsRows.AppendLine($@"
                <tr class=""group-header"">
                    <td colspan=""8""><strong>{group.Key}</strong> ({group.Value.Count} tests)</td>
                </tr>");

                // We'll build a list of test data for this group
                // The actual card HTML generation will happen at the end of this group's processing
                // Add test results for this group
                foreach (var (testResult, testRun) in group.Value)
                {
                    // Use the helper method to create test data with all the necessary information
                    var testData = CreateTestResultData(testResult, testRun, group.Key);
                    // Add the test data to this group's list
                    groupTestData.Add(testData);

                    // Extract error message for UI formatting
                    string errorMessage = (string)testData["errorMessage"];
                    bool hasError = (bool)testData["hasError"];

                    // Convert app URL to a hyperlink if available
                    string appUrl = (string)testData["appUrl"];
                    string appUrlLink = string.IsNullOrEmpty(appUrl) ?
                        "" :
                        $@"<a href=""{appUrl}"" target=""_blank"" class=""app-link"">Open App</a>";

                    // Format error message with a details toggle if it's too long
                    string errorDisplay = hasError && errorMessage.Length > 100 ?
                        $@"<div class=""error-container"">
                            <div class=""error-message-short"">{errorMessage.Substring(0, 100)}...</div>
                            <button class=""details-toggle"" onclick=""toggleErrorDetails(this)"">Show More</button>
                            <div class=""error-details"" style=""display:none;""><pre>{errorMessage}</pre></div>
                        </div>" :
                        errorMessage;

                    // Build the row with appropriate styling based on outcome - now handled by GenerateTestResultRowHtml

                    // Use helper methods to generate HTML for test row and video section
                    testResultsRows.AppendLine(GenerateTestResultRowHtml(testData));
                    string videoSection = GenerateVideoSectionHtml(testData);

                    // Get video information for template
                    Dictionary<string, string> videoInfo = null;
                    if (testData["videoInfo"] != null)
                    {
                        videoInfo = testData["videoInfo"] as Dictionary<string, string>;
                    }
                    // We no longer need to manually add card rows here since we're using the
                    // GenerateTestCardHtml method at the end of group processing

                    // Add the test data to template data list
                    templateData.TestsData.Add(testData);

                    // Add video information to the videos collection if available
                    if (testData["hasVideo"] as bool? == true && videoInfo != null)
                    {
                        templateData.Videos.Add(new Dictionary<string, object> {
                            { "id", videoInfo["id"] },
                            { "path", videoInfo["path"] },
                            { "timecodeStart", videoInfo["timecodeStart"] },
                            { "timecode30s", videoInfo["timecode30s"] },
                            { "timecode60s", videoInfo["timecode60s"] },
                            { "testId", testData["id"] },
                            { "testName", testData["testName"] }
                        });
                    }

                    // Track app types statistics
                    string appType = (string)testData["appType"];
                    if (!templateData.AppTypes.ContainsKey(appType))
                    {
                        templateData.AppTypes[appType] = new Dictionary<string, int> {
                            { "passed", 0 },
                            { "failed", 0 },
                            { "total", 0 }
                        };
                    }

                    templateData.AppTypes[appType]["total"]++;
                    string outcome = (string)testData["outcome"];
                    if (outcome == TestReporter.PassedResultOutcome)
                    {
                        templateData.AppTypes[appType]["passed"]++;
                    }
                    else if (outcome == TestReporter.FailedResultOutcome)
                    {
                        templateData.AppTypes[appType]["failed"]++;
                    }
                }
                // Generate and add the card using the template method
                string cardHtml = GenerateTestCardHtml(group.Key, templateData.GroupedTests[group.Key]);
                testResultsCards.Append(cardHtml);
            }            // Calculate pass percentage and total duration
            templateData.PassPercentage = totalTests > 0 ? (double)passedTests / totalTests * 100 : 0;
            templateData.TotalDuration = "N/A";
            
            // Prefer TestRun.Times data for start/end times if available
            if (runStartTime.HasValue && runEndTime.HasValue)
            {
                TimeSpan runDuration = runEndTime.Value - runStartTime.Value;
                templateData.TotalDuration = $"{runDuration.TotalMinutes:F2} minutes";
                templateData.StartTime = runStartTime.Value.ToString("g");
                templateData.EndTime = runEndTime.Value.ToString("g");
            }
            // Fall back to individual test times if run times are not available
            else if (minStartTime.HasValue && maxEndTime.HasValue)
            {   
                TimeSpan duration = maxEndTime.Value - minStartTime.Value;
                templateData.TotalDuration = $"{duration.TotalMinutes:F2} minutes";
                templateData.StartTime = minStartTime.Value.ToString("g");
                templateData.EndTime = maxEndTime.Value.ToString("g");
            }

            // Calculate health score - currently using pass percentage
            templateData.HealthScore = (int)Math.Round(templateData.PassPercentage);

            // Set summary data
            templateData.TotalTests = totalTests;
            templateData.PassedTests = passedTests;
            templateData.FailedTests = failedTests;
            templateData.TestFileCount = testRuns.Count;

            // Add default values in case there are no entity groups
            if (entityGroups.Count == 0)
            {
                templateData.CoverageLabels.Add("No Data");
                templateData.CoveragePassData.Add(0);
                templateData.CoverageFailData.Add(0);
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
                templateData.CoverageData.Add(new Dictionary<string, object> {
                    { "entityName", type != "Unknown" ? type : name },  // Use entityType if known, otherwise use page type
                    { "entityType", type },       // Keep entityType for consistency
                    { "pageType", name },         // Page type from the key used in the grouping
                    { "status", status },
                    { "passes", passes },
                    { "failures", failures },
                    { "totalTests", total }       // Add total tests count for convenience
                });
                // Use the key (page type) for labels to match the coverage chart
                templateData.CoverageLabels.Add(name);
                templateData.CoveragePassData.Add(passes);
                templateData.CoverageFailData.Add(failures);
            }
            
            // Set final template data
            templateData.TestResultsRows = testResultsRows.ToString();
            templateData.TestResultsCards = testResultsCards.ToString();

            // Ensure test data isn't empty
            if (templateData.TestsData.Count == 0)
            {
                templateData.TestsData.Add(new Dictionary<string, object> {
                    { "id", Guid.NewGuid().ToString() },
                    { "testName", "No tests found" },
                    { "outcome", "N/A" },
                    { "duration", "N/A" },
                    { "startTime", "N/A" },
                    { "endTime", "N/A" },
                    { "entityName", "N/A" },
                    { "pageType", "N/A" },
                    { "entityType", "N/A" },
                    { "appType", "N/A" },
                    { "errorMessage", "" },
                    { "appUrl", "" },
                    { "resultsPath", "" },
                    { "videoPath", "" },
                    { "timecode", "00:00:00" },
                    { "folderPath", "" },
                    { "logFiles", new List<Dictionary<string, string>>() }
                });
            }            // Set environment information in TemplateData
            templateData.Environment.TestEngineVersion = GetAssemblyVersion();
            templateData.Environment.StartTime = templateData.StartTime;
            templateData.Environment.EndTime = templateData.EndTime;
            templateData.Environment.TotalDuration = templateData.TotalDuration;
            templateData.Environment.TestFileCount = templateData.TestFileCount;

            return templateData;
        }

        /// <summary>
        /// Generates an HTML report from test runs
        /// </summary>
        /// <param name="testRuns">List of test runs</param>
        /// <returns>HTML content for the report</returns>
        public string GenerateHtmlReport(List<TestRun> testRuns)
        {
            // Get the HTML template from embedded resources
            var htmlTemplate = GetEmbeddedHtmlTemplate();

            var templateData = GetTemplateContext(testRuns);

            // Serialize the JSON data for Tabulator and charts
            Func<object, string> serializeObject = (object obj) =>
            {
                try
                {
                    // Use Newtonsoft.Json instead of System.Text.Json
                    // Escape quotes to ensure it's valid within HTML data attributes
                    // This prevents JavaScript errors with direct JSON injection
                    string serialized = Newtonsoft.Json.JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.None);
                    return serialized.Replace("\"", "&quot;");
                }
                catch (Exception)
                {
                    // Fallback for serialization errors
                    return "[]";
                }
            };

            // Replace placeholders in the new template
            var report = htmlTemplate
                .Replace("{{TITLE}}", $"PowerApps Test Engine Results - {templateData.HealthScore}% Health Score")
                .Replace("{{REPORT_DATE}}", templateData.Environment.ReportTimestamp)
                .Replace("{{PASS_COUNT}}", templateData.PassedTests.ToString())
                .Replace("{{FAIL_COUNT}}", templateData.FailedTests.ToString())
                .Replace("{{TOTAL_COUNT}}", templateData.TotalTests.ToString())
                .Replace("{{HEALTH_PERCENT}}", templateData.HealthScore.ToString())
                .Replace("{{TEST_RESULTS_CARDS}}", templateData.TestResultsCards)
                // Environment information - generate from template data using helper method
                .Replace("{{ENVIRONMENT_INFO}}", GenerateEnvironmentInfoHtml(templateData.Environment))
                // Health calculation - generate from template data using helper method
                .Replace("{{HEALTH_CALCULATION}}", GenerateHealthCalculationHtml(templateData))

                // Videos section - generate from structured data in template
                .Replace("{{VIDEOS_DATA}}", serializeObject(templateData.Videos))
                .Replace("{{SUMMARY_TABLE}}", GenerateSummaryTableHtml(templateData))
                // JSON data 
                .Replace("{{TESTS_DATA}}", serializeObject(templateData.TestsData))
                .Replace("{{GROUPED_TESTS_DATA}}", serializeObject(templateData.GroupedTests))
                .Replace("{{COVERAGE_DATA}}", serializeObject(templateData.CoverageData))
                .Replace("{{COVERAGE_CHART_LABELS}}", serializeObject(templateData.CoverageLabels))
                .Replace("{{COVERAGE_CHART_PASS_DATA}}", serializeObject(templateData.CoveragePassData))
                .Replace("{{COVERAGE_CHART_FAIL_DATA}}", serializeObject(templateData.CoverageFailData))
                .Replace("{{APP_TYPES_DATA}}", serializeObject(templateData.AppTypes));

            return report;
        }

        /// <summary>
        /// Renders a template with placeholder values
        /// </summary>
        /// <param name="template">Template string with placeholders</param>
        /// <param name="values">Dictionary of placeholder keys and their replacement values</param>
        /// <returns>Template with placeholders replaced with values</returns>
        private string RenderTemplate(string template, Dictionary<string, string> values)
        {
            if (string.IsNullOrEmpty(template) || values == null)
            {
                return template;
            }

            string result = template;
            foreach (var kvp in values)
            {
                // Replace placeholders in format {{KEY}} with values
                result = result.Replace("{{" + kvp.Key + "}}", kvp.Value);
            }

            return result;
        }

        /// <summary>
        /// Gets a template from the embedded resources or a file
        /// </summary>
        /// <param name="templateName">Name of the template to load</param>
        /// <returns>Template content as string</returns>
        private string GetTemplate(string templateName)
        {
            try
            {
                // First try to load from embedded resources
                string resourcePath = $"Microsoft.PowerApps.TestEngine.Reporting.Templates.{templateName}.html";

                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourcePath))
                {
                    if (stream != null)
                    {
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            return reader.ReadToEnd();
                        }
                    }
                }

                // If not found in embedded resources, try to load from file
                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", $"{templateName}.html");
                if (_fileSystem.Exists(filePath))
                {
                    return _fileSystem.ReadAllText(filePath);
                }

                // If neither found, return empty string
                Debug.WriteLine($"Template not found: {templateName}");
                return string.Empty;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading template {templateName}: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets the HTML template from embedded resources
        /// </summary>
        /// <returns>The HTML template string</returns>
        private string GetEmbeddedHtmlTemplate()
        {
            return GetTemplate("TestRunSummaryTemplate");
        }        /// <summary>
                 /// Groups test results by entity name and type for better organization in the report.
                 /// This method parses the AppURL from test results to extract entity information and uses
                 /// that for grouping instead of just using test run names. This makes test results more meaningful
                 /// to users by organizing tests by the entities they act upon rather than arbitrary test run names.        /// </summary>
                 /// <param name="testRuns">List of test runs</param>
                 /// <param name="groupByPageType">When true, group by page type; when false, group by entity name</param>
                 /// <returns>Dictionary mapping entity names or page types with their results</returns>
        public Dictionary<string, List<(UnitTestResult Result, TestRun Run)>> GroupTestsByRun(List<TestRun> testRuns, bool groupByPageType = false)
        {
            var grouped = new Dictionary<string, List<(UnitTestResult, TestRun)>>();

            foreach (var testRun in testRuns)
            {
                foreach (var result in testRun.Results.UnitTestResults)
                {                    // Extract the AppURL from the test result stdout if it exists
                    string appUrl = string.Empty;
                    string entityName = "Unknown";
                    string pageType = "Unknown";

                    if (!string.IsNullOrEmpty(testRun.ResultSummary?.Output?.StdOut))
                    {
                        // Try to parse as JSON first using the existing ExtractValueBetween method
                        appUrl = ExtractValueBetween(testRun.ResultSummary.Output.StdOut, "AppURL\": \"", "\"");

                        // If still empty and StdOut looks like JSON, try using JSON parsing
                        if (string.IsNullOrEmpty(appUrl) &&
                            testRun.ResultSummary.Output.StdOut.TrimStart().StartsWith("{") &&
                            testRun.ResultSummary.Output.StdOut.TrimEnd().EndsWith("}"))
                        {
                            try
                            {
                                using (JsonDocument doc = JsonDocument.Parse(testRun.ResultSummary.Output.StdOut))
                                {
                                    if (doc.RootElement.TryGetProperty("AppURL", out JsonElement appUrlElement) &&
                                        appUrlElement.ValueKind == JsonValueKind.String)
                                    {
                                        appUrl = appUrlElement.GetString() ?? string.Empty;
                                    }
                                }
                            }
                            catch
                            {
                                // Ignore JSON parsing errors - we'll use default values
                            }
                        }
                    }

                    // Use GetAppTypeAndEntityFromUrl to extract entity information from AppURL
                    if (!string.IsNullOrEmpty(appUrl))
                    {
                        var appInfo = GetAppTypeAndEntityFromUrl(appUrl);
                        entityName = appInfo.entityName;
                        pageType = appInfo.pageType;
                        // If entityName is Unknown but pageType is not, use pageType for grouping
                        if (entityName == "Unknown" && pageType != "Unknown")
                        {
                            entityName = pageType;
                        }
                    }

                    // Select grouping key based on strategy
                    string groupKey;
                    if (groupByPageType && pageType != "Unknown")
                    {
                        // Group by page type when requested and page type is known
                        groupKey = pageType;
                    }
                    else
                    {
                        // Otherwise group by entity name
                        groupKey = entityName;
                    }

                    // If we couldn't determine either page type or entity name, use the test run name as fallback
                    if (groupKey == "Unknown")
                    {
                        groupKey = !string.IsNullOrEmpty(testRun.Name) ? testRun.Name : $"Run ID: {testRun.Id}";
                    }

                    // Create the group if it doesn't exist
                    if (!grouped.ContainsKey(groupKey))
                    {
                        grouped[groupKey] = new List<(UnitTestResult, TestRun)>();
                    }

                    // Add the result to the group
                    grouped[groupKey].Add((result, testRun));
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
        /// Extracts a value between start and end markers in a source string.
        /// Enhanced to handle complex nested delimiters often found in JSON-like strings.
        /// </summary>
        /// <param name="source">Source string</param>
        /// <param name="start">Start marker</param>
        /// <param name="end">End marker</param>
        /// <returns>String between markers or empty string if not found</returns>
        private string ExtractValueBetween(string source, string start, string end)
        {
            if (string.IsNullOrEmpty(source))
                return string.Empty;

            // Find the start marker position
            int startIndex = source.IndexOf(start, StringComparison.OrdinalIgnoreCase);
            if (startIndex < 0)
                return string.Empty;

            startIndex += start.Length;

            // Special handling for file paths and JSON strings
            if (end == "\"" && start.EndsWith("\""))
            {
                // We're extracting a quoted string value, likely from JSON
                // Need to handle escaped quotes within the string
                int endIndex = startIndex;
                bool foundEnd = false;

                while (endIndex < source.Length && !foundEnd)
                {
                    endIndex = source.IndexOf(end, endIndex);

                    if (endIndex < 0)
                    {
                        // End quote not found
                        return source.Substring(startIndex);
                    }

                    // Check if this quote is escaped
                    if (endIndex > 0 && source[endIndex - 1] == '\\')
                    {
                        // Count how many backslashes before the quote
                        int backslashCount = 0;
                        int position = endIndex - 1;

                        while (position >= 0 && source[position] == '\\')
                        {
                            backslashCount++;
                            position--;
                        }

                        // If there's an odd number of backslashes, the quote is escaped
                        if (backslashCount % 2 == 1)
                        {
                            // This quote is escaped, continue searching
                            endIndex++;
                            continue;
                        }
                    }

                    // We found a genuine end quote
                    foundEnd = true;
                }

                if (!foundEnd)
                    return source.Substring(startIndex); // Return the rest if no unescaped end quote

                return source.Substring(startIndex, endIndex - startIndex);
            }
            else
            {
                // Standard extraction for non-path and non-JSON strings
                int endIndex = source.IndexOf(end, startIndex);

                if (endIndex < 0)
                    return source.Substring(startIndex); // Return the rest if end marker not found

                return source.Substring(startIndex, endIndex - startIndex);
            }
        }

        /// <summary>
        /// Creates a structured test result object from a test result
        /// </summary>
        /// <param name="testResult">The test result</param>
        /// <param name="testRun">The test run</param>
        /// <param name="groupName">The group name</param>
        /// <returns>A dictionary containing the test result data</returns>
        private Dictionary<string, object> CreateTestResultData(UnitTestResult testResult, TestRun testRun, string groupName)
        {
            // Extract app URL and results path if available
            string appUrl = "";
            string resultsPath = "";
            string videoPath = "";
            string entityType = "Unknown"; // Default type
            string appType = "Unknown";
            string pageType = "Unknown";
            string errorMessage = testResult.Output?.ErrorInfo?.Message ?? "";
            bool hasError = !string.IsNullOrEmpty(errorMessage);
            List<string> videos = new List<string>();

            // Get app info from test run
            if (testRun.ResultSummary?.Output?.StdOut != null)
            {
                try
                {
                    // Try to parse the JSON content from StdOut
                    var stdOut = testRun.ResultSummary.Output.StdOut;
                    // Try to extract videos if they exist in the data
                    if (stdOut.Contains("\"Videos\""))
                    {
                        // First try to parse using regex - more reliable for malformed JSON
                        var videosMatch = Regex.Match(stdOut, "\"Videos\"\\s*:\\s*\\[(.*?)\\]", RegexOptions.Singleline);
                        if (videosMatch.Success && videosMatch.Groups.Count > 1)
                        {
                            var videosContent = videosMatch.Groups[1].Value;
                            var videoItems = Regex.Matches(videosContent, "\"([^\"]*)\"");
                            foreach (Match match in videoItems)
                            {
                                if (match.Groups.Count > 1)
                                {
                                    videos.Add(match.Groups[1].Value.Replace(@"\\", @"\"));
                                }
                            }
                        }

                        // If regex didn't find any videos, try JSON parsing
                        if (videos.Count == 0 && stdOut.StartsWith("{") && stdOut.EndsWith("}"))
                        {
                            try
                            {
                                using (JsonDocument doc = JsonDocument.Parse(stdOut))
                                {
                                    if (doc.RootElement.TryGetProperty("Videos", out JsonElement videosElement) &&
                                        videosElement.ValueKind == JsonValueKind.Array)
                                    {
                                        foreach (JsonElement videoItem in videosElement.EnumerateArray())
                                        {
                                            if (videoItem.ValueKind == JsonValueKind.String)
                                            {
                                                var videoPathItem = videoItem.GetString();
                                                if (!string.IsNullOrWhiteSpace(videoPathItem))
                                                {
                                                    videos.Add(videoPathItem);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            catch
                            {
                                // Ignore JSON parsing errors - we'll use other methods
                            }
                        }
                    }

                    // Try parsing with JsonDocument for more reliable extraction
                    try
                    {
                        if (stdOut.StartsWith("{") && stdOut.EndsWith("}"))
                        {
                            using (JsonDocument doc = JsonDocument.Parse(stdOut))
                            {
                                if (doc.RootElement.TryGetProperty("AppURL", out JsonElement appUrlElement) &&
                                    appUrlElement.ValueKind == JsonValueKind.String)
                                {
                                    appUrl = appUrlElement.GetString() ?? "";
                                }

                                if (doc.RootElement.TryGetProperty("TestResults", out JsonElement resultsElement) &&
                                    resultsElement.ValueKind == JsonValueKind.String)
                                {
                                    resultsPath = resultsElement.GetString() ?? "";
                                }

                                if (doc.RootElement.TryGetProperty("VideoPath", out JsonElement videoElement) &&
                                    videoElement.ValueKind == JsonValueKind.String)
                                {
                                    videoPath = videoElement.GetString() ?? "";
                                }

                                if (doc.RootElement.TryGetProperty("EntityType", out JsonElement entityElement) &&
                                    entityElement.ValueKind == JsonValueKind.String)
                                {
                                    entityType = entityElement.GetString() ?? "Unknown";
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Fallback to simple parsing if JSON parsing fails
                        appUrl = ExtractValueBetween(stdOut, "AppURL\": \"", "\"");
                        resultsPath = ExtractValueBetween(stdOut, "TestResults\": \"", "\"");
                        videoPath = ExtractValueBetween(stdOut, "VideoPath\": \"", "\"");
                        entityType = ExtractValueBetween(stdOut, "EntityType\": \"", "\"");
                    }

                    // Parse app URL to determine app type, page type, and entity name
                    if (!string.IsNullOrEmpty(appUrl))
                    {
                        var appInfo = GetAppTypeAndEntityFromUrl(appUrl);
                        appType = appInfo.appType;

                        // Only override page type and entity name if they're not already set
                        // or if they're set to "Unknown"
                        if (pageType == "Unknown")
                        {
                            pageType = appInfo.pageType;
                        }

                        // Update entityType based on the URL analysis if needed
                        if ((entityType == "Unknown" || string.IsNullOrEmpty(entityType)) && appInfo.entityName != "Unknown")
                        {
                            entityType = appInfo.entityName;
                        }
                    }
                }
                catch
                {
                    // Ignore parsing errors
                }
            }

            // Format the duration
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
            // Format the start time - Use test run start time if available, otherwise use individual test start time
            string startTime;
            if (testRun.Times != null && testRun.Times.Start != default)
            {
                startTime = testRun.Times.Start.ToString("g");
            }
            else
            {
                startTime = testResult.StartTime != default ? testResult.StartTime.ToString("g") : "N/A";
            }

            // Format end time - Use test run finish time if available, otherwise use individual test end time
            string endTime;
            if (testRun.Times != null && testRun.Times.Finish != default)
            {
                endTime = testRun.Times.Finish.ToString("g");
            }
            else
            {
                endTime = testResult.EndTime != default ? testResult.EndTime.ToString("g") : "N/A";
            }

            // Get results directory
            string resultsDirectory = string.IsNullOrEmpty(resultsPath) ?
                string.Empty :
                resultsPath?.Replace("\\", "/") ?? string.Empty;

            // Extract any video path mentioned in the test output
            if (string.IsNullOrEmpty(videoPath))
            {
                // First try to extract from test result StdOut if available
                if (testResult.Output?.StdOut != null)
                {
                    videoPath = ExtractValueBetween(testResult.Output.StdOut, "VideoPath: \"", "\"");
                }

                // If not found, try test run StdOut
                if (string.IsNullOrEmpty(videoPath) && testRun.ResultSummary?.Output?.StdOut != null)
                {
                    videoPath = ExtractValueBetween(testRun.ResultSummary.Output.StdOut, "VideoPath\": \"", "\"");
                }
            }

            // Extract any timecodes from test results
            string timecodeStart = "00:00:00";
            if (testResult.Output?.StdOut != null)
            {
                var extractedTimecode = ExtractValueBetween(testResult.Output.StdOut, "TimecodeStart: \"", "\"");
                if (!string.IsNullOrEmpty(extractedTimecode))
                {
                    timecodeStart = extractedTimecode;
                }
            }

            // Store video information in a structured way
            var videoInfo = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(videoPath))
            {
                videoInfo["path"] = videoPath.Replace("\\", "/");
                videoInfo["id"] = $"video-{testResult.TestId}";
                videoInfo["timecodeStart"] = timecodeStart;
                videoInfo["timecode30s"] = AddTimeToTimecode(timecodeStart, 30);
                videoInfo["timecode60s"] = AddTimeToTimecode(timecodeStart, 60);
            }

            // Find log files in the results directory for this test
            var testFolder = resultsDirectory;
            if (testResult.ResultFiles?.ResultFile.Count() > 0)
            {
                testFolder = Path.GetDirectoryName(testResult.ResultFiles.ResultFile.FirstOrDefault()?.Path);
            }

            var hasVideo = !string.IsNullOrEmpty(videoPath) || videos.Count > 0;

            // Create the test result data object
            var results = new Dictionary<string, object> {
                { "id", testResult.TestId?.ToString() ?? Guid.NewGuid().ToString() },
                { "testName", testResult.TestName },
                { "outcome", testResult.Outcome },
                { "duration", duration },
                { "startTime", startTime },
                { "endTime", endTime },
                { "entityName", groupName },
                { "pageType", pageType },
                { "entityType", entityType },
                { "appType", appType },
                { "errorMessage", errorMessage },
                { "hasError", hasError },
                { "appUrl", appUrl },
                { "resultsPath", resultsPath },
                { "hasVideo",  hasVideo },
                { "timecode", timecodeStart },
                { "folderPath", !string.IsNullOrEmpty(testFolder) ? testFolder : null },
                { "logFiles", testResult.ResultFiles?.ResultFile.Select(r => r.Path).ToList() },
                { "videoInfo", videoInfo.Count > 0 ? videoInfo : null }
            };

            // Add single video path for backward compatibility
            if (!string.IsNullOrEmpty(videoPath))
            {
                results.Add("videoPath", videoPath);
            }

            // Make sure videos list includes the single videoPath if it exists
            if (!string.IsNullOrEmpty(videoPath) && !videos.Contains(videoPath))
            {
                videos.Add(videoPath);
            }

            // Always add the videos array if we have any videos
            if (videos.Count > 0)
            {
                results.Add("videos", videos.ToArray());
            }

            return results;
        }

        /// <summary>
        /// Helper method to generate the HTML for a test result row
        /// </summary>
        /// <param name="testData">Dictionary containing test data</param>
        /// <returns>HTML string for the test result row</returns>
        private string GenerateTestResultRowHtml(Dictionary<string, object> testData)
        {
            string outcome = (string)testData["outcome"];
            string appUrl = (string)testData["appUrl"];
            string errorMessage = (string)testData["errorMessage"];
            bool hasError = (bool)testData["hasError"];

            // Convert app URL to a hyperlink if available
            string appUrlLink = string.IsNullOrEmpty(appUrl) ?
                "" :
                $@"<a href=""{appUrl}"" target=""_blank"" class=""app-link"">Open App</a>";

            // Format error message using template
            string errorDisplay;
            if (hasError && errorMessage.Length > 100)
            {
                string errorTemplate = GetTemplate("ErrorDisplay");
                if (string.IsNullOrEmpty(errorTemplate))
                {
                    // Fall back to inline HTML if template not available
                    errorDisplay = $@"<div class=""error-container"">
                        <div class=""error-message-short"">{errorMessage.Substring(0, 100)}...</div>
                        <button class=""details-toggle"" onclick=""toggleErrorDetails(this)"">Show More</button>
                        <div class=""error-details"" style=""display:none;""><pre>{errorMessage}</pre></div>
                    </div>";
                }
                else
                {
                    // Use template
                    errorDisplay = RenderTemplate(errorTemplate, new Dictionary<string, string> {
                        { "ERROR_SHORT", errorMessage.Substring(0, 100) + "..." },
                        { "ERROR_FULL", errorMessage }
                    });
                }
            }
            else
            {
                errorDisplay = errorMessage;
            }

            // Build the row with appropriate styling based on outcome
            string rowClass = outcome == TestReporter.PassedResultOutcome ? "success" :
                             outcome == TestReporter.FailedResultOutcome ? "danger" :
                             "warning";

            // Try to use template
            string template = GetTemplate("TestResultRow");
            if (string.IsNullOrEmpty(template))
            {
                // Fall back to inline HTML if template not available
                return $@"
                <tr class=""{rowClass}"">
                    <td>{testData["testName"]}</td>
                    <td><span class=""badge badge-{rowClass}"">{outcome}</span></td>
                    <td>{testData["startTime"]}</td>
                    <td>{testData["duration"]}</td>
                    <td>{testData["appType"]}</td>
                    <td>{testData["pageType"]}</td>
                    <td>{appUrlLink}</td>
                    <td>{errorDisplay}</td>
                </tr>";
            }
            else
            {
                // Use template with placeholders
                return RenderTemplate(template, new Dictionary<string, string> {
                    { "ROW_CLASS", rowClass },
                    { "TEST_NAME", (string)testData["testName"] },
                    { "OUTCOME", outcome },
                    { "START_TIME", (string)testData["startTime"] },
                    { "DURATION", (string)testData["duration"] },
                    { "APP_TYPE", (string)testData["appType"] },
                    { "PAGE_TYPE", (string)testData["pageType"] },
                    { "APP_URL_LINK", appUrlLink },
                    { "ERROR_DISPLAY", errorDisplay }
                });
            }
        }

        /// <summary>
        /// Helper method to generate the HTML for video section
        /// </summary>
        /// <param name="testData">Dictionary containing test data</param>
        /// <returns>HTML string for the video section or empty string if no video</returns>
        private string GenerateVideoSectionHtml(Dictionary<string, object> testData)
        {
            if (testData["hasVideo"] as bool? != true || testData["videoInfo"] == null)
            {
                return string.Empty;
            }

            var videoInfo = testData["videoInfo"] as Dictionary<string, string>;
            if (videoInfo == null || !videoInfo.ContainsKey("path"))
            {
                return string.Empty;
            }

            // Try to use template
            string template = GetTemplate("VideoSection");
            if (!string.IsNullOrEmpty(template))
            {
                // Use template with placeholders
                return RenderTemplate(template, new Dictionary<string, string> {
                    { "VIDEO_ID", videoInfo["id"] },
                    { "VIDEO_PATH", videoInfo["path"] },
                    { "TIMECODE_START", videoInfo["timecodeStart"] },
                    { "TIMECODE_30S", videoInfo["timecode30s"] },
                    { "TIMECODE_60S", videoInfo["timecode60s"] }
                });
            }

            // Fall back to inline HTML if template not available
            return $@"<div class=""video-actions mb-2"">
                <div class=""btn-group mb-2"">
                    <button class=""btn btn-sm btn-primary toggle-video"" 
                            data-video-id=""{videoInfo["id"]}"" 
                            data-video-src=""{videoInfo["path"]}"" 
                            data-timecode=""{videoInfo["timecodeStart"]}"">
                        <i class=""fas fa-play-circle""></i> Play Video
                    </button>
                    <button class=""btn btn-sm btn-outline-primary video-timestamp"" 
                            data-video-id=""{videoInfo["id"]}"" 
                            data-timecode=""{videoInfo["timecodeStart"]}"">
                        <i class=""fas fa-step-backward""></i> Start
                    </button>
                    <button class=""btn btn-sm btn-outline-primary video-timestamp"" 
                            data-video-id=""{videoInfo["id"]}"" 
                            data-timecode=""{videoInfo["timecode30s"]}"">
                        +30s
                    </button>
                    <button class=""btn btn-sm btn-outline-primary video-timestamp"" 
                            data-video-id=""{videoInfo["id"]}"" 
                            data-timecode=""{videoInfo["timecode60s"]}"">
                        +1m
                    </button>
                </div>
                <div class=""video-container"" id=""{videoInfo["id"]}"" style=""display: none;"">
                <video width=""100%"" controls class=""test-video"">
                    <source src=""file:///{videoInfo["path"]}"" type=""video/webm"">
                    Your browser does not support the video tag.
                </video>
                <div class=""video-controls mt-2"">
                    <span class=""video-time"">Time: <span class=""current-time"">00:00:00</span></span>
                    <button class=""btn btn-sm btn-outline-secondary copy-timecode"" 
                            data-video-id=""{videoInfo["id"]}"">
                        <i class=""fas fa-copy""></i> Copy Timecode
                    </button>
                </div>
                <div class=""keyboard-shortcuts"">
                    Keyboard shortcuts: <span>Space</span> Play/Pause 
                    <span>←</span> -5s <span>→</span> +5s 
                    <span>Home</span> Start <span>End</span> End
                </div>
            </div>";
        }

        /// <summary>
        /// Adds a specified number of seconds to a timecode and returns the new timecode
        /// </summary>
        /// <param name="timecode">Starting timecode in format HH:MM:SS</param>
        /// <param name="secondsToAdd">Number of seconds to add</param>
        /// <returns>New timecode string in format HH:MM:SS</returns>
        private string AddTimeToTimecode(string timecode, int secondsToAdd)
        {
            // Default value if parsing fails
            if (string.IsNullOrEmpty(timecode))
                return "00:00:00";

            try
            {
                // Parse the timecode as TimeSpan
                if (TimeSpan.TryParse(timecode, out TimeSpan currentTime))
                {
                    // Add the seconds and return formatted string
                    TimeSpan newTime = currentTime.Add(TimeSpan.FromSeconds(secondsToAdd));
                    return newTime.ToString(@"hh\:mm\:ss");
                }

                return timecode; // Return original if parsing fails
            }
            catch
            {
                return timecode; // Return original on any error
            }
        }

        /// <summary>
        /// Parses an app URL to extract app type, page type, and entity information
        /// </summary>
        /// <param name="url">URL to parse</param>
        /// <returns>Tuple containing app type, page type, and entity name</returns>
        public (string appType, string pageType, string entityName) GetAppTypeAndEntityFromUrl(string url)
        {
            // Default values for invalid or unknown URLs
            string appType = "Unknown";
            string pageType = "Unknown";
            string entityName = "Unknown";

            if (string.IsNullOrEmpty(url))
            {
                return (appType, pageType, entityName);
            }

            try
            {
                // Try to parse the URL
                Uri uri;
                if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
                {
                    return (appType, pageType, entityName);
                }
                // Determine app type based on hostname
                if (uri.Host.Contains("make.powerapps.com") || uri.Host.Contains("make.preview.powerapps.com"))
                {
                    appType = "Power Apps Portal";

                    // Check for specific query parameters to determine entity type 
                    if (uri.Segments.Count() >= 4)
                    {
                        pageType = uri.Segments[1].Replace("/", "");
                        entityName = uri.Segments[3].Replace("/", "");
                    }
                }
                else
                // Determine app type based on hostname
                if (uri.Host.Contains("apps.powerapps.com") || uri.Host.Contains("play.apps.appsplatform.us") || uri.Host.Contains("apps.high.powerapps.us") || uri.Host.Contains("play.apps.appsplatform.us") || uri.Host.Contains("apps.powerapps.cn"))
                {
                    appType = "Canvas App";
                }
                else if (uri.Host.Contains("dynamics.com"))
                {
                    appType = "Model-driven App";

                    var query = HttpUtility.ParseQueryString(uri.Query);

                    // Check for specific query parameters to determine entity type
                    if (query["etn"] != null)
                    {
                        entityName = query["etn"];
                    }

                    if (query["pageType"] != null)
                    {
                        switch (query["pageType"])
                        {
                            case "custom":
                                pageType = "Custom Page";
                                entityName = query["name"] ?? "Unknown";
                                break;
                            default:
                                pageType = query["pageType"];
                                break;
                        }

                    }
                }
            }
            catch
            {
                // Silently return default values on any parsing error
            }

            return (appType, pageType, entityName);
        }

        /// <summary>
        /// Generate HTML for the environment information section
        /// </summary>
        /// <param name="environment">Environment information object</param>
        /// <returns>HTML for the environment information section</returns>
        private string GenerateEnvironmentInfoHtml(EnvironmentInfo environment)
        {
            if (environment == null)
            {
                return string.Empty;
            }

            return $@"
            <table class=""table table-bordered"">
                <tbody>
                    <tr>
                        <th>Machine Name</th>
                        <td>{environment.MachineName}</td>
                    </tr>
                    <tr>
                        <th>Operating System</th>
                        <td>{environment.OperatingSystem}</td>
                    </tr>
                    <tr>
                        <th>Report Generated</th>
                        <td>{environment.ReportTimestamp}</td>
                    </tr>
                    <tr>
                        <th>PowerApps Test Engine</th>
                        <td>{environment.TestEngineVersion}</td>
                    </tr>
                    <tr>
                        <th>Start Time</th>
                        <td>{environment.StartTime}</td>
                    </tr>
                    <tr>
                        <th>End Time</th>
                        <td>{environment.EndTime}</td>
                    </tr>
                    <tr>
                        <th>Total Duration</th>
                        <td>{environment.TotalDuration}</td>
                    </tr>
                    <tr>
                        <th>Test Files</th>
                        <td>{environment.TestFileCount}</td>
                    </tr>
                </tbody>
            </table>";
        }

        /// <summary>
        /// Generate HTML for the summary statistics section
        /// </summary>
        /// <param name="templateData">Template data with test statistics</param>
        /// <returns>HTML for the summary statistics section</returns>
        private string GenerateSummaryTableHtml(TemplateData templateData)
        {
            if (templateData == null)
            {
                return string.Empty;
            }

            return $@"
            <div class=""summary-stats row"">
                <div class=""col-md-3"">
                    <div class=""stat-box total"">
                        <div class=""stat-value"">{templateData.TotalTests}</div>
                        <div class=""stat-label"">Total Tests</div>
                    </div>
                </div>
                <div class=""col-md-3"">
                    <div class=""stat-box success"">
                        <div class=""stat-value"">{templateData.PassedTests}</div>
                        <div class=""stat-label"">Passed</div>
                    </div>
                </div>
                <div class=""col-md-3"">
                    <div class=""stat-box danger"">
                        <div class=""stat-value"">{templateData.FailedTests}</div>
                        <div class=""stat-label"">Failed</div>
                    </div>
                </div>
                <div class=""col-md-3"">
                    <div class=""stat-box info"">
                        <div class=""stat-value"">{templateData.PassPercentage:F2}%</div>
                        <div class=""stat-label"">Pass Rate</div>
                    </div>
                </div>
            </div>
            
            <div class=""summary-details"">
                <table class=""table table-bordered"">
                    <tbody>
                        <tr>
                            <th>Start Time</th>
                            <td>{templateData.StartTime}</td>
                        </tr>
                        <tr>
                            <th>End Time</th>
                            <td>{templateData.EndTime}</td>
                        </tr>
                        <tr>
                            <th>Total Duration</th>
                            <td>{templateData.TotalDuration}</td>
                        </tr>
                        <tr>
                            <th>Test Files</th>
                            <td>{templateData.TestFileCount}</td>
                        </tr>
                    </tbody>
                </table>
            </div>";
        }

        /// <summary>
        /// Generate HTML for the health score calculation section
        /// </summary>
        /// <param name="templateData">Template data with test statistics</param>
        /// <returns>HTML for the health calculation section</returns>
        private string GenerateHealthCalculationHtml(TemplateData templateData)
        {
            if (templateData == null)
            {
                return string.Empty;
            }

            return $@"
            <div class=""health-calculation"">
                <h3>Health Score Calculation</h3>
                <div class=""calculation-step"">
                    <p>Total Tests: {templateData.TotalTests}</p>
                    <p>Passed Tests: {templateData.PassedTests}</p>
                    <p>Failed Tests: {templateData.FailedTests}</p>
                </div>
                <div class=""calculation-step"">
                    <p>Health Score = (Passed Tests / Total Tests) * 100</p>
                    <p>Health Score = ({templateData.PassedTests} / {templateData.TotalTests}) * 100</p>
                    <p>Health Score = {templateData.PassPercentage:F2}%</p>
                </div>
                <div class=""calculation-result"">
                    Final Health Score: {templateData.HealthScore}%
                </div>
            </div>";
        }

        /// <summary>
        /// Creates a template file for test result row
        /// </summary>
        /// <param name="templateName">Name of the template to create</param>
        /// <param name="content">Content of the template</param>
        /// <returns>True if template was created successfully, false otherwise</returns>
        private bool CreateTemplateFile(string templateName, string content)
        {
            try
            {
                string templateDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates");
                if (!_fileSystem.Exists(templateDirectory))
                {
                    _fileSystem.CreateDirectory(templateDirectory);
                }

                string filePath = Path.Combine(templateDirectory, $"{templateName}.html");
                _fileSystem.WriteTextToFile(filePath, content, overwrite: true);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating template file {templateName}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Creates templates for different sections of the report if they don't exist
        /// </summary>
        private void EnsureTemplatesExist()
        {
            // Template for test result row
            string testResultRow = @"<tr class=""{{ROW_CLASS}}"">
    <td>{{TEST_NAME}}</td>
    <td><span class=""badge badge-{{ROW_CLASS}}"">{{OUTCOME}}</span></td>
    <td>{{START_TIME}}</td>
    <td>{{DURATION}}</td>
    <td>{{APP_TYPE}}</td>
    <td>{{PAGE_TYPE}}</td>
    <td>{{APP_URL_LINK}}</td>
    <td>{{ERROR_DISPLAY}}</td>
</tr>";
            CreateTemplateFile("TestResultRow", testResultRow);

            // Template for video section
            string videoSection = @"<div class=""video-actions mb-2"">
    <div class=""btn-group mb-2"">
        <button class=""btn btn-sm btn-primary toggle-video"" 
                data-video-id=""{{VIDEO_ID}}"" 
                data-video-src=""{{VIDEO_PATH}}"" 
                data-timecode=""{{TIMECODE_START}}"">
            <i class=""fas fa-play-circle""></i> Play Video
        </button>
        <button class=""btn btn-sm btn-outline-primary video-timestamp"" 
                data-video-id=""{{VIDEO_ID}}"" 
                data-timecode=""{{TIMECODE_START}}"">
            <i class=""fas fa-step-backward""></i> Start
        </button>
        <button class=""btn btn-sm btn-outline-primary video-timestamp"" 
                data-video-id=""{{VIDEO_ID}}"" 
                data-timecode=""{{TIMECODE_30S}}"">
            +30s
        </button>
        <button class=""btn btn-sm btn-outline-primary video-timestamp"" 
                data-video-id=""{{VIDEO_ID}}"" 
                data-timecode=""{{TIMECODE_60S}}"">
            +1m
        </button>
    </div>
    <div class=""video-player-container"" id=""video-container-{{VIDEO_ID}}"" style=""display:none;"">
        <video id=""video-player-{{VIDEO_ID}}"" controls class=""video-player"">
            <source src=""{{VIDEO_PATH}}"" type=""video/webm"">
            Your browser does not support the video tag.
        </video>
    </div>
</div>";
            CreateTemplateFile("VideoSection", videoSection);

            // Template for error display
            string errorDisplay = @"<div class=""error-container"">
    <div class=""error-message-short"">{{ERROR_SHORT}}</div>
    <button class=""details-toggle"" onclick=""toggleErrorDetails(this)"">Show More</button>
    <div class=""error-details"" style=""display:none;""><pre>{{ERROR_FULL}}</pre></div>
</div>";
            CreateTemplateFile("ErrorDisplay", errorDisplay);

            // Template for test card
            string testCard = @"<div class=""card test-details-card"" data-entity=""{{GROUP_NAME}}"">
    <div class=""card-header"">{{GROUP_NAME}}</div>
    <div class=""card-body"">
        <div class=""table-responsive"">
            <table class=""table table-hover"">
                <thead>
                    <tr>
                        <th>Test Name</th>
                        <th>Status</th>
                        <th>Duration</th>
                        <th>Start Time</th>
                        <th>App Type</th>
                        <th>Page Type</th>
                        <th>App URL</th>
                        <th>Logs</th>
                    </tr>
                </thead>
                <tbody>
                    {{TEST_ROWS}}
                </tbody>
            </table>
        </div>
    </div>
</div>";
            CreateTemplateFile("TestCard", testCard);

            // Template for test card row
            string testCardRow = @"<tr data-status=""{{OUTCOME}}"">
    <td>{{TEST_NAME}}</td>
    <td>
        <span class=""badge-result {{BADGE_CLASS}}"">{{STATUS_ICON}} {{OUTCOME}}</span>
    </td>
    <td>{{DURATION}}</td>
    <td>{{START_TIME}}</td>
    <td>{{APP_TYPE}}</td>
    <td>{{PAGE_TYPE}}</td>
    <td>{{APP_URL_LINK}}</td>
    <td>
        {{ERROR_DISPLAY}}
        {{VIDEO_SECTION}}
    </td>
</tr>";
            CreateTemplateFile("TestCardRow", testCardRow);
        }

        /// <summary>
        /// Generates HTML for a test card with all test results for a group
        /// </summary>
        /// <param name="groupName">Name of the test group</param>
        /// <param name="testResults">List of test results in this group</param>
        /// <returns>HTML for the test card</returns>
        private string GenerateTestCardHtml(string groupName, List<Dictionary<string, object>> testResults)
        {
            if (string.IsNullOrEmpty(groupName) || testResults == null || testResults.Count == 0)
            {
                return string.Empty;
            }

            // Generate rows for all tests in this group
            var testRows = new StringBuilder();
            foreach (var testData in testResults)
            {
                string outcome = (string)testData["outcome"];
                string appUrl = (string)testData["appUrl"];
                string errorMessage = (string)testData["errorMessage"];
                bool hasError = (bool)testData["hasError"];

                // Convert app URL to a hyperlink if available
                string appUrlLink = string.IsNullOrEmpty(appUrl) ?
                    "" :
                    $@"<a href=""{appUrl}"" target=""_blank"" class=""app-link"">Open App</a>";

                // Format error display
                string errorDisplay = "";
                if (hasError)
                {
                    // Use previously created error display method or template
                    if (errorMessage.Length > 100)
                    {
                        string errorTemplate = GetTemplate("ErrorDisplay");
                        if (!string.IsNullOrEmpty(errorTemplate))
                        {
                            errorDisplay = RenderTemplate(errorTemplate, new Dictionary<string, string> {
                                { "ERROR_SHORT", errorMessage.Substring(0, 100) + "..." },
                                { "ERROR_FULL", errorMessage }
                            });
                        }
                        else
                        {
                            errorDisplay = $@"<div class=""error-container"">
                                <div class=""error-message-short"">{errorMessage.Substring(0, 100)}...</div>
                                <button class=""details-toggle"" onclick=""toggleErrorDetails(this)"">Show More</button>
                                <div class=""error-details"" style=""display:none;""><pre>{errorMessage}</pre></div>
                            </div>";
                        }
                    }
                    else
                    {
                        errorDisplay = errorMessage;
                    }
                }

                // Generate video section if test has video
                string videoSection = GenerateVideoSectionHtml(testData);

                // Determine styling based on outcome
                string rowClass = outcome == TestReporter.PassedResultOutcome ? "success" :
                                 outcome == TestReporter.FailedResultOutcome ? "danger" :
                                 "warning";
                string badgeClass = outcome == TestReporter.PassedResultOutcome ? "badge-passed" :
                                   outcome == TestReporter.FailedResultOutcome ? "badge-failed" :
                                   "badge-result";
                string statusIcon = outcome == TestReporter.PassedResultOutcome ?
                                   "<i class=\"fas fa-check-circle status-icon passed\"></i>" :
                                   outcome == TestReporter.FailedResultOutcome ?
                                   "<i class=\"fas fa-times-circle status-icon failed\"></i>" :
                                   "<i class=\"fas fa-exclamation-triangle\"></i>";

                // Add row using template
                string rowTemplate = GetTemplate("TestCardRow");
                if (!string.IsNullOrEmpty(rowTemplate))
                {
                    string renderedRow = RenderTemplate(rowTemplate, new Dictionary<string, string>
                    {
                        { "OUTCOME", outcome },
                        { "TEST_NAME", (string)testData["testName"] },
                        { "BADGE_CLASS", badgeClass },
                        { "STATUS_ICON", statusIcon },
                        { "DURATION", (string)testData["duration"] },
                        { "START_TIME", (string)testData["startTime"] },
                        { "APP_TYPE", (string)testData["appType"] },
                        { "PAGE_TYPE", (string)testData["pageType"] },
                        { "APP_URL_LINK", appUrlLink },
                        { "ERROR_DISPLAY", errorDisplay },
                        { "VIDEO_SECTION", videoSection }
                    });
                    testRows.AppendLine(renderedRow);
                }
                else
                {
                    // Fallback to inline HTML
                    testRows.AppendLine($@"
                    <tr data-status=""{outcome}"">
                        <td>{testData["testName"]}</td>
                        <td>
                            <span class=""badge-result {badgeClass}"">{statusIcon} {outcome}</span>
                        </td>
                        <td>{testData["duration"]}</td>
                        <td>{testData["startTime"]}</td>
                        <td>{testData["appType"]}</td>
                        <td>{testData["pageType"]}</td>
                        <td>{appUrlLink}</td>
                        <td>
                            {errorDisplay}
                            {videoSection}
                        </td>
                    </tr>");
                }
            }

            // Generate the card using template
            string cardTemplate = GetTemplate("TestCard");
            if (!string.IsNullOrEmpty(cardTemplate))
            {
                return RenderTemplate(cardTemplate, new Dictionary<string, string>
                {
                    { "GROUP_NAME", groupName },
                    { "TEST_ROWS", testRows.ToString() }
                });
            }

            // Fallback to inline HTML
            return $@"
            <div class=""card test-details-card"" data-entity=""{groupName}"">
                <div class=""card-header"">{groupName}</div>
                <div class=""card-body"">
                    <div class=""table-responsive"">
                        <table class=""table table-hover"">
                            <thead>
                                <tr>
                                    <th>Test Name</th>
                                    <th>Status</th>
                                    <th>Duration</th>
                                    <th>Start Time</th>
                                    <th>App Type</th>
                                    <th>Page Type</th>
                                    <th>App URL</th>
                                    <th>Logs</th>
                                </tr>
                            </thead>
                            <tbody>
                                {testRows}
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>";
        }

        /// <summary>
        /// Collects coverage information grouped by page type
        /// </summary>
        /// <param name="testRuns">The list of test runs</param>
        /// <returns>Dictionary mapping page types to their coverage statistics</returns>
        private Dictionary<string, (string entityType, int passes, int failures)> CollectCoverageData(List<TestRun> testRuns)
        {
            // Group the tests by page type
            var groupedTests = GroupTestsByRun(testRuns, true);
            var pageTypeGroups = new Dictionary<string, (string entityType, int passes, int failures)>();

            // Process each grouped test to collect coverage statistics
            foreach (var group in groupedTests)
            {
                foreach (var (testResult, testRun) in group.Value)
                {
                    // Create test data to extract necessary information
                    var testData = CreateTestResultData(testResult, testRun, group.Key);

                    string pageType = (string)testData["pageType"];
                    string entityType = (string)testData["entityType"];
                    string outcome = (string)testData["outcome"];

                    // If entityType is Unknown but pageType is known, use pageType as entityType
                    if (entityType == "Unknown" && pageType != "Unknown")
                    {
                        entityType = pageType;
                    }

                    // Use page type as the key, or entity name as fallback
                    string groupKey = pageType != "Unknown" ? pageType : (string)testData["entityName"];

                    if (!pageTypeGroups.ContainsKey(groupKey))
                    {
                        pageTypeGroups[groupKey] = (entityType, 0, 0);
                    }

                    var stats = pageTypeGroups[groupKey];
                    if (outcome == TestReporter.PassedResultOutcome)
                    {
                        stats.passes++;
                    }
                    else if (outcome == TestReporter.FailedResultOutcome)
                    {
                        stats.failures++;
                    }
                    pageTypeGroups[groupKey] = stats;
                }
            }

            return pageTypeGroups;
        }
    }
}
