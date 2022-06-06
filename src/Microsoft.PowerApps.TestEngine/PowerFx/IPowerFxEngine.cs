// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Public.Values;

namespace Microsoft.PowerApps.TestEngine.PowerFx
{
    /// <summary>
    /// Wrapper for Power FX interpreter
    /// </summary>
    public interface IPowerFxEngine
    {
        /// <summary>
        /// Sets up the Power FX engine
        /// </summary>
        public void Setup();

        /// <summary>
        /// Executes a list of Power FX functions
        /// </summary>
        /// <param name="testSteps">Test steps</param>
        /// <returns>Result of the Power FX.</returns>
        public FormulaValue Execute(string testSteps);

        /// <summary>
        /// Updates the Power FX object model
        /// </summary>
        /// <returns>A task</returns>
        public Task UpdatePowerFXModelAsync();
    }
}
