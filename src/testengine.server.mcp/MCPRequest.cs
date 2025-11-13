// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

public class MCPRequest
{
    public string Target { get; set; } = string.Empty;

    public string Endpoint { get; set; } = string.Empty;
    public string Method { get; set; } = "GET";
    public string? Body { get; set; }
    public string? ContentType { get; set; }
}
