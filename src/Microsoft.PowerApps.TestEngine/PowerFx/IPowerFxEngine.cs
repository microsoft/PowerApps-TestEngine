// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Globalization;
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
        /// <param name="locale">The locale to be used when setting up the Power FX engine. This is typically provided by the test plan file</param>
        public void Setup(CultureInfo locale);

        /// <summary>
        /// Executes testSteps with retry
        /// </summary>
        /// <param name="testSteps">Test steps</param>
        /// <returns>A task</returns>
        public Task ExecuteWithRetryAsync(string testSteps);

        /// <summary>
        /// Executes a list of Power FX functions
        /// </summary>
        /// <param name="testSteps">Test steps</param>
        /// <returns>Result of the Power FX.</returns>
        public FormulaValue Execute(string testSteps);

        /// <summary>
        /// Update the Power FX object model
        /// </summary>
        /// <returns>A task</returns>
        public Task UpdatePowerFxModelAsync();

        /// <summary>
        /// get PowerAppFunctions
        /// </summary>
        public IPowerAppFunctions GetPowerAppFunctions();
    }
}
