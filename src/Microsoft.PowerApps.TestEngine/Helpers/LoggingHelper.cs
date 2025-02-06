using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Providers;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;

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

        public async void DebugInfo()
        {
            try
            {
                var infoObject = await _testWebProvider.GetDebugInfo();
                if (infoObject is ExpandoObject debugInfo)
                {
                    if (debugInfo != null && debugInfo.ToString() != "undefined")
                    {
                        Logger.LogInformation($"------------------------------\n Debug Info \n------------------------------");
                        foreach (var info in debugInfo)
                        {
                            Logger.LogInformation($"{info.Key}:\t{info.Value}");
                        }
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
