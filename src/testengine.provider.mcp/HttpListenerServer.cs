// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Net;

public class HttpListenerServer : IHttpServer
{
    private readonly HttpListener _listener;

    public event Func<HttpListenerContext, Task>? OnRequestReceived;

    public HttpListenerServer(string prefix)
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add(prefix);
    }

    public void Start()
    {
        _listener.Start();
        Task.Run(async () =>
        {
            while (_listener.IsListening)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    if (OnRequestReceived != null)
                    {
                        await OnRequestReceived(context);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in HTTP server: {ex}");
                }
            }
        });
    }

    public void Stop()
    {
        _listener.Stop();
    }
}
