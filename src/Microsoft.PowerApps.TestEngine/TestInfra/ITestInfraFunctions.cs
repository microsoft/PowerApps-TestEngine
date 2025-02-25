﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Users;

namespace Microsoft.PowerApps.TestEngine.TestInfra
{
    /// <summary>
    /// Interface that abstracts away all test infrastructure functionality
    /// </summary>
    public interface ITestInfraFunctions
    {
        /// <summary>
        /// The current page to execute actions
        /// </summary>
        public IPage Page { get; set; }

        /// <summary>
        /// Return the current browser context
        /// </summary>
        /// <returns>The current browser context</returns>
        public IBrowserContext GetContext();

        /// <summary>
        /// Setup the test infrastructure
        /// </summary>
        /// <returns>Task</returns>
        public Task SetupAsync(IUserManager userManager);

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
        public Task EndTestRunAsync(IUserManager userManager);

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
        /// Adds a script content to page
        /// </summary>
        /// <param name="content">The script to add</param>
        /// <returns>Task</returns>
        public Task AddScriptContentAsync(string content);

        /// <summary>
        /// Runs javascript on the page
        /// </summary>
        /// <typeparam name="T">Expected return type</typeparam>
        /// <param name="jsExpression">Javascript expression to run</param>
        /// <returns>Return value of javascript</returns>
        public Task<T> RunJavascriptAsync<T>(string jsExpression);
    }
}
