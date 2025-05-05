// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Net;
using System.Text;

public class HttpRequestWrapper : IHttpRequest
{
    private readonly HttpListenerRequest _request;

    public HttpRequestWrapper(HttpListenerRequest request)
    {
        _request = request;
    }

    public string HttpMethod => _request.HttpMethod;
    public Uri Url => _request.Url;
    public Stream InputStream => _request.InputStream;
    public Encoding ContentEncoding => _request.ContentEncoding;
    public string ContentType => _request.ContentType;
}
