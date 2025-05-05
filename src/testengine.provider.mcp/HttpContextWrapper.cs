// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Net;

public class HttpContextWrapper : IHttpContext
{
    private readonly HttpListenerContext _context;

    public HttpContextWrapper(HttpListenerContext context)
    {
        _context = context;
    }

    public IHttpRequest Request => new HttpRequestWrapper(_context.Request);
    public IHttpResponse Response => new HttpResponseWrapper(_context.Response);
}
