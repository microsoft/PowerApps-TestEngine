using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.PowerApps;

namespace Microsoft.PowerApps.TestEngine.Helpers
{
    public static class ExceptionHandlingHelper
    {
        // Error code suggesting published app without JSSDK, error code sent from the serverside JSSDK code
        public static string PublishedAppWithoutJSSDKErrorCode = "1";
        public static string PublishedAppWithoutJSSDKMessage = "Please republish the app and try again!";

        public static void CheckIfOutDatedPublishedApp(Exception ex, ILogger logger)
        {
            if (ex.Message?.ToString() == PublishedAppWithoutJSSDKErrorCode || ex.InnerException?.Message?.ToString() == PublishedAppWithoutJSSDKErrorCode)
            {
                logger.LogError(PublishedAppWithoutJSSDKMessage);
            }
        }
    }
}
