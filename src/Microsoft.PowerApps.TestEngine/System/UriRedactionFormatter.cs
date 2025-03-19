// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Microsoft.PowerApps.TestEngine.System
{
    public class UriRedactionFormatter
    {
        ILogger _logger;

        public UriRedactionFormatter(ILogger logger)
        {
            _logger = logger;
        }
        public string ToString(Uri uri)
        {
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                return uri.ToString();
            }

            return "[URI REDACTED]";
        }
    }
}
