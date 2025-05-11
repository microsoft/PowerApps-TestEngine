// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.PowerApps.TestEngine.Providers
{
    /// <summary>
    /// Represents the response that can be used by a MCP server.
    /// </summary>
    public class MCPResponse
    {
        public int StatusCode { get; set; }
        public string? ContentType { get; set; }
        public string? Body { get; set; }
    }
}