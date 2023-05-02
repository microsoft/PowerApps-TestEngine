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
        public string Method { get; set; } = "";
        /// <summary>
        /// Gets or sets the request's headers.
        /// </summary>
        public IDictionary<string, string> Headers { get; set; }
        /// <summary>
        /// Gets or sets the request's payload.
        /// </summary>
        public string RequestBodyFile { get; set; }

        /// <summary>
        /// Indicate if this request should be used for registed Network Module extensions
        /// </summary>
        /// <value></value>
        public bool IsExtension { get;set; } = false;

        /// <summary>
        /// Extended parameters to be used by extension
        /// </summary>
        /// <value></value>
        public Dictionary<string, string> ExtensionProperties { get; set; } = new Dictionary<string, string>();
    }
}
