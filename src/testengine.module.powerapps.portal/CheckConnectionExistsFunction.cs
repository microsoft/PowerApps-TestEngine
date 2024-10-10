// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
using testengine.module.powerapps.portal;

namespace testengine.module
{
    /// <summary>
    /// This provide the ability to check if a connection exists using power apps portal
    /// </summary>
    public class CheckConnectionExistsFunction : ReflectionFunction
    {
        private readonly ITestInfraFunctions _testInfraFunctions;
        private readonly ITestState _testState;
        private readonly ILogger _logger;

        public Func<ConnectionHelper> GetConnectionHelper = () => new ConnectionHelper();

        // NOTE: Order of calling base is name, return type then argument types
        public CheckConnectionExistsFunction(ITestInfraFunctions testInfraFunctions, ITestState testState, ILogger logger)
            : base(DPath.Root.Append(new DName("TestEngine")), "CheckConnectionExists", FormulaType.Boolean)
        {
            _testInfraFunctions = testInfraFunctions;
            _testState = testState;
            _logger = logger;
        }

        public BooleanValue Execute(StringValue name)
        {
            _logger.LogInformation("------------------------------\n\n" +
                "Executing TestEngine.CheckConnectionExists function.");

            return BooleanValue.New(ExecuteAsync(name.Value).Result);
        }

        /// <summary>
        /// Check if an connection exists
        /// </summary>
        /// <param name="name">The name of the connection to check if exists</param>
        private async Task<bool> ExecuteAsync(string name)
        {
            var url = _testState.GetDomain();

            return await GetConnectionHelper().Exists(_testInfraFunctions.GetContext(), url, name);
        }
    }
}

