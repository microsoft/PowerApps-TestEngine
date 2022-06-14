// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Types;

namespace Microsoft.PowerApps.TestEngine.PowerFx
{
    /// <summary>
    /// Wrapper for Power FX interpreter
    /// </summary>
    public interface IPowerFxEngine
    {
        /// <summary>
        /// Executes a list of Power FX functions
        /// </summary>
        /// <param name="testSteps">Test steps</param>
        /// <returns>Result of the Power FX.</returns>
        public FormulaValue Execute(string testSteps);

        /// <summary>
        /// Sets up the Power FX engine
        /// </summary>
        /// <returns>A task</returns>
        public Task SetupAsync();
    }
}
