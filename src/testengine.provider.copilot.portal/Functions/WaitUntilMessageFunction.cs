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

namespace Microsoft.PowerApps.TestEngine.Providers.Functions
    {
        public class WaitUntilMessageFunction : ReflectionFunction
        {
            private readonly ITestInfraFunctions _testInfraFunctions;
            private readonly ITestState _testState;
            private readonly ILogger _logger;
            private readonly IMessageProvider _provider;

            public WaitUntilMessageFunction(ITestInfraFunctions testInfraFunctions, ITestState testState, ILogger logger, IMessageProvider provider)
                : base(DPath.Root.Append(new DName("Experimental")), "WaitUntilMessage", FormulaType.Blank, FormulaType.String)
            {
                _testInfraFunctions = testInfraFunctions;
                _testState = testState;
                _logger = logger;
                _provider = provider;
            }

            public BlankValue Execute(StringValue text)
            {
                ExecuteAsync(text).Wait();

                return FormulaValue.NewBlank();
            }

            public async Task ExecuteAsync(StringValue text)
            {
                var timeout = _testState.GetTimeout();

                _logger.LogInformation("Start Wait");

                var startTime = DateTime.Now;
                while (DateTime.Now.Subtract(startTime).TotalSeconds < timeout)
                {
                    await _provider.GetNewMessages();
                    var jsonPathQuery = $"$..[?(@.text =~ /.*{Sanitize(text.Value)}.*/i)]";
                    if (_provider.Messages.Where(json => JToken.Parse(json).SelectTokens(jsonPathQuery).Any()).Any())
                    {
                        _logger.LogInformation("Match found");
                        return;
                    }
                    Thread.Sleep(500);
                }
            }

            public static string Sanitize(string value)
            {
                // Escape special characters for regex
                return Regex.Escape(value);
            }
        }
    }

