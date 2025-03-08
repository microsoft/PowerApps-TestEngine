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
using testengine.provider.copilot.portal.services;
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

                var complete = await _provider.MessageWorker.WaitUntilCompleteAsync(async (object? state) =>
                {
                    TimerState timerState = (TimerState)state;
                    _logger.LogInformation("Start Wait");

                    var startTime = DateTime.Now;
                    while (DateTime.Now.Subtract(startTime).TotalSeconds < timeout)
                    {
                        await _provider.GetNewMessages();
                        var jsonPathQuery = $"$..[?(@.text =~ /.*{Sanitize(text.Value)}.*/i)]";
                        if (_provider.Messages.Where(json => JToken.Parse(json).SelectTokens(jsonPathQuery).Any()).Any())
                        {
                            _logger.LogInformation("Match found");
                            timerState.Tcs.TrySetResult(true);
                        }
                        Thread.Sleep(500);
                    }

                    timerState.Tcs.SetResult(false);
                }, timeout);

                if (!complete)
                {
                    throw new TimeoutException($"No match found");
                }
            }

            public static string Sanitize(string value)
            {
                // Escape special characters for regex
                return Regex.Escape(value);
            }
        }
    }

