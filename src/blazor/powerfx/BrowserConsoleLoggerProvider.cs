// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;

public class BrowserConsoleLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, BrowserConsoleLogger> _loggers = new();

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new BrowserConsoleLogger(name));
    }

    public void Dispose()
    {
        _loggers.Clear();
    }
}
