// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
using Newtonsoft.Json.Linq;

namespace Microsoft.PowerApps.TestEngine.Providers.Functions
{
    internal class SendTextFunction : ReflectionFunction
    {
        private readonly ITestInfraFunctions _testInfraFunctions;
        private readonly ITestState _testState;
        private readonly ILogger _logger;

        public SendTextFunction(ITestInfraFunctions testInfraFunctions, ITestState testState, ILogger logger)
            : base(DPath.Root.Append(new DName("Experimental")), "SendText", FormulaType.Blank, FormulaType.String)
        {
            _testInfraFunctions = testInfraFunctions;
            _testState = testState;
            _logger = logger;
        }

        public BlankValue Execute(StringValue text)
        {
            ExecuteAsync(text).Wait();

            return FormulaValue.NewBlank();
        }

        public async Task ExecuteAsync(StringValue text)
        {
            await _testInfraFunctions.FillAsync("[data-testid=\"send box text area\"]", text.Value);
            _logger.LogDebug($"Sent {text.Value}");
            await _testInfraFunctions.Page.PressAsync("[data-testid=\"send box text area\"]", "Enter");
        }
    }
}
