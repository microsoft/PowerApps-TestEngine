// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.PowerApps.TestEngine.PowerApps;
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
        public void Setup();

        /// <summary>
        /// Executes testSteps with retry
        /// </summary>
        /// <param name="testSteps">Test steps</param>
        /// <param name="culture">The locale to be used when excecuting tests. This is typically provided by the test plan file</param>
        /// <returns>A task</returns>
        public Task ExecuteWithRetryAsync(string testSteps, CultureInfo culture);

        /// <summary>
        /// Executes a list of Power FX functions
        /// </summary>
        /// <param name="testSteps">Test steps</param>
        /// <param name="culture">The locale to be when excecuting tests. This is typically provided by the test plan file</param>
        /// <returns>Result of the Power FX.</returns>
        public FormulaValue Execute(string testSteps, CultureInfo culture);

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
        /// get PowerAppFunctions
        /// </summary>
        public IPowerAppFunctions GetPowerAppFunctions();
    }
}
