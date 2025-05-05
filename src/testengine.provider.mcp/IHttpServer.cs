// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Net;

public interface IHttpServer
{
    void Start();

    void Stop();

    event Func<HttpListenerContext, Task>? OnRequestReceived;
}
