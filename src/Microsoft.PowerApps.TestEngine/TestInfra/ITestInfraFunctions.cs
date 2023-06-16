// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;

namespace Microsoft.PowerApps.TestEngine.TestInfra
{
    /// <summary>
    /// Interface that abstracts away all test infrastructure functionality
    /// </summary>
    public interface ITestInfraFunctions
    {
        /// <summary>
        /// Setup the test infrastructure
        /// </summary>
        /// <returns>Task</returns>
        public Task SetupAsync();

        /// <summary>
        /// Setup the network request mocking
        /// </summary>
        /// <returns>Task</returns>
        public Task SetupNetworkRequestMockAsync();

        /// <summary>
        /// Navigates to url specified
        /// </summary>
        /// <param name="url">Url to go to</param>
        /// <returns>Task</returns>
        public Task GoToUrlAsync(string url);

        /// <summary>
        /// Ends the test run
        /// </summary>
        /// <returns>Task</returns>
        public Task EndTestRunAsync();

        /// <summary>
        /// Dispose the instances
        /// </summary>
        /// <returns>Task</returns>
        public Task DisposeAsync();

        /// <summary>
        /// Takes a screenshot
        /// </summary>
        /// <param name="screenshotFilePath">Path for screenshot file</param>
        /// <returns>Task</returns>
        public Task ScreenshotAsync(string screenshotFilePath);

        /// <summary>
        /// Fills in input element
        /// </summary>
        /// <param name="selector">Selector to find element</param>
        /// <param name="value">Value to fill in</param>
        /// <returns>Task</returns>
        public Task FillAsync(string selector, string value);

        /// <summary>
        /// Clicks an element
        /// </summary>
        /// <param name="selector">Selector to find element</param>
        /// <returns>Task</returns>
        public Task ClickAsync(string selector);

        /// <summary>
        /// Adds a script tag to page
        /// </summary>
        /// <param name="scriptTag">Path to script</param>
        /// <param name="frameName">Frame name to add the script to. If null, it will be added to the main page.</param>
        /// <returns>Task</returns>
        public Task AddScriptTagAsync(string scriptTag, string frameName);

        /// <summary>
        /// Runs javascript on the page
        /// </summary>
        /// <typeparam name="T">Expected return type</typeparam>
        /// <param name="jsExpression">Javascript expression to run</param>
        /// <returns>Return value of javascript</returns>
        public Task<T> RunJavascriptAsync<T>(string jsExpression);

        /// <summary>
        /// Fills in user email 
        /// </summary>
        /// <param name="selector">Selector to find element</param>
        /// <param name="value">Value to fill in</param>
        /// <returns>Task</returns>
        public Task HandleUserEmailScreen(string selector, string value);

        /// <summary>
        /// Fills in user password
        /// </summary>
        /// <param name="selector">Selector to find element</param>
        /// <param name="value">Value to fill in</param>
        /// <returns>Task</returns>
        public Task HandleUserPasswordScreen(string selector, string value, string desiredUrl);
    }
}
