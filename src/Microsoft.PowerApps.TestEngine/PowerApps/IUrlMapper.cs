// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
using Microsoft.Extensions.Logging;

namespace Microsoft.PowerApps.TestEngine.PowerApps
{
    /// <summary>
    /// Map urls based on the cloud
    /// </summary>
    public interface IUrlMapper
    {
        /// <summary>
        /// Generates url for the test
        /// </summary>
        /// <returns>Test url</returns>
        public string GenerateTestUrl(string additionalQuery);
    }
}
