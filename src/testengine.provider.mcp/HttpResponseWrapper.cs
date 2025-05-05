// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Net;

public class HttpResponseWrapper : IHttpResponse
{
    private readonly HttpListenerResponse _response;

    public HttpResponseWrapper(HttpListenerResponse response)
    {
        _response = response;
    }

    public int StatusCode
    {
        get => _response.StatusCode;
        set => _response.StatusCode = value;
    }

    public string ContentType
    {
        get => _response.ContentType;
        set => _response.ContentType = value;
    }

    public Stream OutputStream => _response.OutputStream;
}
