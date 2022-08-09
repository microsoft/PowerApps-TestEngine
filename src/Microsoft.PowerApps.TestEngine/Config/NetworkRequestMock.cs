// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerApps.TestEngine.Config
{
    /// <summary>
    /// Defines the network request to be mocked
    /// </summary>
    public class NetworkRequestMock
    {
        /// <summary>
        /// Gets or sets the request URL.
        /// </summary>
        public string RequestURL { get; set; } = "";
        /// <summary>
        /// Gets or sets the data sent back in response.
        /// </summary>
        public string ResponseDataFile { get; set; } = "";
        /// <summary>
        /// Gets or sets the request's method (GET, POST, etc.).
        /// </summary>
        public string? Method { get; set; } = "";
        /// <summary>
        /// Gets or sets the request's headers.
        /// </summary>
        public IDictionary<string, string>? Headers { get; set; }
        /// <summary>
        /// Gets or sets the request's payload.
        /// </summary>
        public string? RequestBodyFile { get; set; }
    }
}
