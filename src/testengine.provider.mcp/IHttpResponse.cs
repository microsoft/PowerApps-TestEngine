// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

public interface IHttpResponse
{
    int StatusCode { get; set; }
    string ContentType { get; set; }
    Stream OutputStream { get; }
}
