// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
using Newtonsoft.Json.Linq;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Microsoft.PowerApps.TestEngine.Providers
{
    internal class WaitUntilMessageFunction : ReflectionFunction
    {
        private readonly ITestInfraFunctions _testInfraFunctions;
        private readonly ITestState _testState;
        private readonly ILogger _logger;
        private readonly CopilotPortalProvider _provider;

        public WaitUntilMessageFunction(ITestInfraFunctions testInfraFunctions, ITestState testState, ILogger logger, CopilotPortalProvider provider)
            : base(DPath.Root.Append(new DName("Experimental")), "WaitUntilMessage", FormulaType.Blank, StringType.String)
        {
            _testInfraFunctions = testInfraFunctions;
            _testState = testState;
            _logger = logger;
            _provider = provider;
        }

        public BlankValue Execute(StringValue text)
        {
            var timeout = _testState.GetTimeout();

            _logger.LogInformation("Start Wait");

            var startTime = DateTime.Now;
            while (DateTime.Now.Subtract(startTime).TotalSeconds < timeout)
            {
                var jsonPathQuery = $"$..[?(@.text =~ /.*{Sanitize(text.Value)}.*/i)]";
                if (_provider.Messages.Where(json => JToken.Parse(json).SelectTokens(jsonPathQuery).Any()).Any())
                {
                    _logger.LogInformation("Match found");
                    return BlankValue.NewBlank();
                }
                Thread.Sleep(500);
            }

            return BlankValue.NewBlank();
        }

        public static string Sanitize(string value)
        {
            // Escape special characters for regex
            return Regex.Escape(value);
        }
    }
}
