// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Reflection.Metadata.Ecma335;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.PowerApps;
using Microsoft.PowerApps.TestEngine.PowerFx;
using Microsoft.PowerApps.TestEngine.Reporting;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerApps.TestEngine.TestStudioConverter;
using Microsoft.PowerApps.TestEngine.Users;
using PowerAppsTestEngine;



var switchMappings = new Dictionary<string, string>()
{
    { "-i", "TestPlanFile" },
    { "-e", "EnvironmentId" },
    { "-t", "TenantId" },
    { "-o", "OutputDirectory" }
};

if (args.Length > 1)
{
    if (args[0].Equals("convert"))
    {
        string InputDir = args[1];

        ILoggerFactory loggerFactory = LoggerFactory.Create(builder => { builder.ClearProviders(); builder.AddConsole(); });
        ILogger<CreateYAMLTestPlan> logger = loggerFactory.CreateLogger<CreateYAMLTestPlan>();
        CreateYAMLTestPlan converter = new CreateYAMLTestPlan(logger, InputDir);
        converter.exportYAML();
        return;
    }
}

var inputOptions = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("config.json", true)
    .AddJsonFile("config.dev.json", true)
    .AddCommandLine(args, switchMappings)
    .Build()
    .Get<InputOptions>();

if (inputOptions == null)
{
    Console.Out.WriteLine("Input options are null");
    return;
}
else
{
    // Get EngineLoggingLevel from testSettings
    TestState tempState = new TestState(new YamlTestConfigParser(new FileSystem()));
    TestLoggerProvider tempLoggerProvider = new TestLoggerProvider(new FileSystem());
    tempLoggerProvider.SetEngineLoggingLevel(LogLevel.Error);
    ILoggerFactory tempLoggerFactory = LoggerFactory.Create(tempLoggingBuilder =>
    {
        tempLoggingBuilder
        .ClearProviders()
        .AddProvider(tempLoggerProvider);
    });
    ILogger tempLogger = tempLoggerFactory.CreateLogger("Suite");

    if (inputOptions.TestPlanFile == null)
    {
        throw new ArgumentNullException("TestPlanFile cannot be null");
    }

    tempState.ParseAndSetTestState(inputOptions.TestPlanFile);
    LogLevel engineLoggingLevel = tempState.GetEngineLoggingLevel();

    // Set provider & logger
    TestState state = new TestState(new YamlTestConfigParser(new FileSystem()));
    TestLoggerProvider loggerProvider = new TestLoggerProvider(new FileSystem());
    loggerProvider.SetEngineLoggingLevel(engineLoggingLevel);
    ILoggerFactory loggerFactory = LoggerFactory.Create(loggingBuilder =>
    {
        loggingBuilder
        .ClearProviders()
        .AddProvider(loggerProvider);
    });
    ILogger logger = loggerFactory.CreateLogger("Suite");
    state.ParseAndSetTestState(inputOptions.TestPlanFile);

    // Set serviceProvider & Logger
    var serviceProvider = new ServiceCollection()
    .AddLogging(loggingBuilder =>
    {
        loggingBuilder
        .ClearProviders()
        .AddProvider(loggerProvider);
    })
    .AddScoped<ITestInfraFunctions, PlaywrightTestInfraFunctions>()
    .AddSingleton<ITestConfigParser, YamlTestConfigParser>()
    .AddScoped<IPowerFxEngine, PowerFxEngine>()
    .AddScoped<IUserManager, UserManager>()
    .AddSingleton<ITestState, TestState>()
    .AddScoped<IUrlMapper, PowerAppsUrlMapper>()
    .AddScoped<IPowerAppFunctions, PowerAppFunctions>()
    .AddSingleton<ITestReporter, TestReporter>()
    .AddScoped<ISingleTestInstanceState, SingleTestInstanceState>()
    .AddScoped<ISingleTestRunner, SingleTestRunner>()
    .AddSingleton<IFileSystem, FileSystem>()
    .AddSingleton<IEnvironmentVariable, EnvironmentVariable>()
    .AddSingleton<TestEngine>()
    .BuildServiceProvider();


    TestEngine testEngine = serviceProvider.GetRequiredService<TestEngine>();


    if (inputOptions.EnvironmentId == null)
    {
        throw new ArgumentNullException("EnvironmentID cannot be null");
    }
    else if (inputOptions.TenantId == null)
    {
        throw new ArgumentNullException("TenantID cannot be null");
    }
    else if (inputOptions.OutputDirectory == null)
    {
        throw new ArgumentNullException("OutputDirectory cannot be null");
    }

    var testResult = await testEngine.RunTestAsync(inputOptions.TestPlanFile, inputOptions.EnvironmentId, inputOptions.TenantId, inputOptions.OutputDirectory);

    Console.Out.WriteLine($"Test results can be found here: {testResult}");
}
