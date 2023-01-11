using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.PowerApps;

namespace Microsoft.PowerApps.TestEngine.Helpers
{
    internal static class ExceptionHandlingHelper
    {
        public static void CheckIfOutDatedPublishedApp(Exception ex, ILogger logger)
        {
            if (ex.Message?.ToString() == PowerAppFunctions.PublishedAppWithoutJSSDKErrorCode || ex.InnerException?.Message?.ToString() == PowerAppFunctions.PublishedAppWithoutJSSDKErrorCode)
            {
                logger.LogError(PowerAppFunctions.PublishedAppWithoutJSSDKMessage);
            }
        }
    }
}
