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
        /// <returns>True if test steps were successfully run.</returns>
        public bool Execute(string testSteps);

        /// <summary>
        /// Updates variables in Power FX engine
        /// </summary>
        /// <param name="name">Name of variable</param>
        /// <param name="value">Variable object</param>
        public void UpdateVariable(string name, IUntypedObject value);
    }
}
