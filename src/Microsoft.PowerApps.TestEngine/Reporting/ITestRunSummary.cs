// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerApps.TestEngine.Reporting
{
    /// <summary>
    /// Interface for generating test run summary reports
    /// </summary>
    public interface ITestRunSummary
    {
        /// <summary>
        /// Generates a summary report from test result files
        /// </summary>
        /// <param name="resultsDirectory">Directory containing the .trx files</param>
        /// <param name="outputPath">Path where the summary report will be saved</param>
        /// <param name="runName">Optional name to filter test runs by. If specified, only includes test runs with matching names</param>
        /// <returns>Path to the generated summary report</returns>
        string GenerateSummaryReport(string resultsDirectory, string outputPath, string runName = null);
    }
}
