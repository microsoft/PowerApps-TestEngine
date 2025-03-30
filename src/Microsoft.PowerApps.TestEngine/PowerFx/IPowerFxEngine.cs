// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Globalization;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Providers;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerApps.TestEngine.PowerFx
{
    /// <summary>
    /// Wrapper for Power FX interpreter
    /// </summary>
    public interface IPowerFxEngine
    {
        /// <summary>
        /// Set up the Power FX engine
        /// </summary>       
        public void Setup(TestSettings settings);

        /// <summary>
        /// Executes testSteps with retry
        /// </summary>
        /// <param name="testSteps">Test steps</param>
        /// <param name="culture">The locale to be used when excecuting tests. This is typically provided by the test plan file</param>
        /// <returns>A task</returns>
        public Task<FormulaValue> ExecuteWithRetryAsync(string testSteps, CultureInfo culture);

        /// <summary>
        /// Executes a list of Power FX functions
        /// </summary>
        /// <param name="testSteps">Test steps</param>
        /// <param name="culture">The locale to be when excecuting tests. This is typically provided by the test plan file</param>
        /// <returns>Result of the Power FX.</returns>
        public Task<FormulaValue> ExecuteAsync(string testSteps, CultureInfo culture);

        /// <summary>
        /// Update the Power FX object model
        /// </summary>
        /// <returns>A task</returns>
        public Task UpdatePowerFxModelAsync();

        /// <summary>
        /// Run requirements checks for the engine
        /// </summary>
        /// <returns>A task</returns>
        public Task RunRequirementsCheckAsync();

        /// <summary>
        /// Get Web Provider instance
        /// </summary>
        public ITestWebProvider GetWebProvider();

        /// <summary>
        /// Disables checking Power Apps state checks
        /// </summary>
        public bool PowerAppIntegrationEnabled { get; set; }

        /// <summary>
        /// The setup engine instance
        /// </summary>
        public RecalcEngine Engine { get; }
    }
}
