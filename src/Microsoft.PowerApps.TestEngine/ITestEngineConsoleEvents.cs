﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;

namespace Microsoft.PowerApps.TestEngine
{
    /// <summary>
    /// Interface that handles all console related events
    /// </summary>
    public interface ITestEngineConsoleEvents
    {
        /// <summary>
        /// To be executed when an assertion fails
        /// </summary>
        public void AssertionFailed(string message);

        /// <summary>
        /// Exception encountered when logger not available
        /// </summary>
        public void EncounteredCriticalException(Exception ex);

        /// <summary>
        /// Issue encountered when logger not available
        /// </summary>
        public void EncounteredCriticalIssue(string message);

        /// <summary>
        /// To be executed when an exception is encountered
        /// </summary>
        public void EncounteredException(Exception ex);

        /// <summary>
        /// Handles result of a test case
        /// </summary>
        public void TestCasePassed(boolean result);

        /// <summary>
        /// To be executed when starting the suite
        /// </summary>
        public void SuiteBegin(string message);

        /// <summary>
        /// To be executed when ending the suite
        /// </summary>
        public void SuiteEnd(string message);

        /// <summary>
        /// Received test case name
        /// </summary>
        public void TestCaseName(string name);

        /// <summary>
        /// Contains test report path
        /// </summary>
        public void TestReportPath(string path);
    }
}
