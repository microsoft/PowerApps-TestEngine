// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Globalization;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerApps.TestEngine.Providers
{
    /// <summary>
    /// Functions for interacting with the a web based resource to test
    /// </summary>
    public interface IExtendedTestWebProvider : ITestWebProvider
    {
        /// <summary>
        /// Allows the web provider to update the Power Fx test state
        /// </summary>
        /// <param name="powerFxConfig"></param>
        public void ConfigurePowerFx(PowerFxConfig powerFxConfig);

        /// <summary>
        /// Allows additional Provider specific functions or variables to be registered
        /// </summary>
        /// <param name="engine"></param>
        public void ConfigurePowerFxEngine(RecalcEngine engine);

        /// <summary>
        /// Allows the provider to perform any pre-test suite run setup
        /// </summary>
        /// <returns></returns>
        public Task SetupContext();

        /// <summary>
        /// Determine if specical case where provider will execute the power fx 
        /// </summary>
        public bool ProviderExecute { get; }

        /// <summary>
        /// Execute Power Fx
        /// </summary>
        /// <param name="steps">The test steps to exec</param>
        /// <param name="culture">The culture</param>
        /// <returns></returns>
        public FormulaValue ExecutePowerFx(string steps, CultureInfo culture);
    }
}
