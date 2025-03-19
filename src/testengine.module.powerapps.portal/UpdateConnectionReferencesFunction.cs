// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
using testengine.module.powerapps.portal;

namespace testengine.module
{
    /// <summary>
    /// This provided the ability to update power platform connections references. Compatible with powerApps.portal provider
    /// 
    /// Notes:
    /// - This approach should be considered a backup. Ideally connections should be created by service principal and shared with user account as needed
    /// - This approach assumes known login credentials using browser auth or certificate based authentication
    /// </summary>
    public class UpdateConnectionReferencesFunction : ReflectionFunction
    {
        private readonly ITestInfraFunctions _testInfraFunctions;
        private readonly ITestState _testState;
        private readonly ILogger _logger;
        public IPage? Page { get; set; }

        public Func<ConnectionHelper> GetConnectionHelper = () => new ConnectionHelper();

        // NOTE: Order of calling base is name, return type then argument types
        public UpdateConnectionReferencesFunction(ITestInfraFunctions testInfraFunctions, ITestState testState, ILogger logger)
            : base(DPath.Root.Append(new DName("Preview")), "UpdateConnectionReferences", FormulaType.Blank)
        {
            _testInfraFunctions = testInfraFunctions;
            _testState = testState;
            _logger = logger;
        }

        public BlankValue Execute()
        {
            _logger.LogInformation("------------------------------\n\n" +
                "Executing TestEngine.CreateConnection function.");

            ExecuteAsync().Wait();

            return BlankValue.NewBlank();
        }

        /// <summary>
        /// Attempt to update cconnection references with connected connection id
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task ExecuteAsync()
        {
            var baseUrl = _testState.GetDomain();
            var url = baseUrl;

            await GetConnectionHelper().UpdateConnectionReferences(_testInfraFunctions.GetContext(), baseUrl, _logger);
        }
    }
}

