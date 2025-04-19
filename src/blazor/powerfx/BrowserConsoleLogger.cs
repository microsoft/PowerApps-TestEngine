// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

public class BrowserConsoleLogger : ILogger
{
    private readonly string _name;

    public BrowserConsoleLogger(string name)
    {
        _name = name;
    }

    
    
    IDisposable ILogger.BeginScope<TState>(TState state)
    {
        return NullScope.Instance;
    }


     public bool IsEnabled(LogLevel logLevel) => true;

     public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
     {
        var message = formatter(state, exception);
        ConsoleLog(logLevel, $"{_name} - {message}");
     }

     private void ConsoleLog(LogLevel logLevel, string message)
     {
        var logLevelString = logLevel.ToString().ToUpper();
        var logMessage = $"{logLevelString}: {message}";

        Console.WriteLine(logMessage);
     }

     
    public class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new NullScope();
        public void Dispose() { }
    }
}
