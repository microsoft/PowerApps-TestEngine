// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.ComponentModel.Composition;
using System.IO;
using Microsoft.PowerApps.TestEngine.System;

namespace Microsoft.PowerApps.TestEngine.Reporting
{
    /// <summary>
    /// Command to generate a test run summary report
    /// </summary>
    [Export]
    public class TestRunSummaryCommand
    {
        private readonly ITestRunSummary _testRunSummary;
        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestRunSummaryCommand"/> class.
        /// </summary>
        /// <param name="testRunSummary">The test run summary service</param>
        /// <param name="fileSystem">The file system service</param>
        [ImportingConstructor]
        public TestRunSummaryCommand(ITestRunSummary testRunSummary, IFileSystem fileSystem)
        {
            _testRunSummary = testRunSummary;
            _fileSystem = fileSystem;
        }        /// <summary>
                 /// Generates a test run summary report from a directory containing .trx files
                 /// </summary>
                 /// <param name="resultsDirectory">Directory containing the .trx files</param>
                 /// <param name="outputPath">Optional path where the summary report will be saved. If not specified, the report will be saved in the results directory</param>
                 /// <param name="runName">Optional name to filter test runs by</param>
                 /// <returns>Path to the generated summary report</returns>
        public string GenerateSummaryReport(string resultsDirectory, string outputPath = null, string runName = null)
        {
            if (string.IsNullOrEmpty(resultsDirectory))
            {
                throw new ArgumentException("Results directory cannot be null or empty", nameof(resultsDirectory));
            }

            if (!_fileSystem.Exists(resultsDirectory))
            {
                throw new ArgumentException($"Results directory does not exist: {resultsDirectory}", nameof(resultsDirectory));
            }

            // If output path is not specified, generate one in the results directory
            if (string.IsNullOrEmpty(outputPath))
            {
                outputPath = Path.Combine(resultsDirectory, $"TestRunSummary_{DateTime.Now:yyyyMMdd_HHmmss}.html");
            }

            return _testRunSummary.GenerateSummaryReport(resultsDirectory, outputPath, runName);
        }
    }
}
