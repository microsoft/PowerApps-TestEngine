// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.PowerApps.TestEngine.Providers
{
    public class MCPRequest
    {
        public string Endpoint { get; set; } = string.Empty;
        public string Method { get; set; } = "GET";
        public string? Body { get; set; }
        public string? ContentType { get; set; }
    }
}
