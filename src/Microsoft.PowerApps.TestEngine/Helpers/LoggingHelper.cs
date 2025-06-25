// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Dynamic;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Providers;
using Microsoft.PowerApps.TestEngine.System;

namespace Microsoft.PowerApps.TestEngine.Helpers
{
    public class LoggingHelper
    {
        private readonly ITestWebProvider _testWebProvider;
        private readonly ISingleTestInstanceState _singleTestInstanceState;
        private readonly ITestEngineEvents _eventHandler;
        private ILogger Logger { get { return _singleTestInstanceState.GetLogger(); } }

        public LoggingHelper(ITestWebProvider testWebProvider,
                             ISingleTestInstanceState singleTestInstanceState, ITestEngineEvents eventHandler)
        {
            _testWebProvider = testWebProvider;
            _singleTestInstanceState = singleTestInstanceState;
            _eventHandler = eventHandler;
        }


        public static ExpandoObject ToExpando(Dictionary<string, object> dict)
        {
            var expando = new ExpandoObject();
            var expandoDict = (IDictionary<string, object>)expando;
            foreach (var kvp in dict)
            {
                expandoDict.Add(kvp.Key, kvp.Value);
            }
            return expando;
        }


        public async void DebugInfo()
        {
            try
            {
                ExpandoObject debugInfo = null;
                var results = await _testWebProvider.GetDebugInfo();
                if (results is Dictionary<string, object> dictionaryData)
                {
                    debugInfo = ToExpando(dictionaryData);
                }

                if (debugInfo != null && debugInfo.ToString() != "undefined")
                {
                    Logger.LogInformation($"------------------------------\n Debug Info \n------------------------------");
                    foreach (var info in debugInfo)
                    {
                        Logger.LogInformation($"{info.Key}:\t{info.Value}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogDebug("Issue getting DebugInfo. This can be a result of not being properly logged in.");
                Logger.LogDebug(ex.ToString());
                _eventHandler.EncounteredException(new UserAppException());
            }
        }
    }
}
