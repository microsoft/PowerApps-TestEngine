// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

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
using Microsoft.PowerApps.TestEngine.Users;
using PowerAppsTestEngine;



var switchMappings = new Dictionary<string, string>()
{
    { "-i", "TestPlanFile" },
    { "-e", "EnvironmentId" },
    { "-t", "TenantId" },
    { "-o", "OutputDirectory" },
    { "-l", "LogLevel" },
    { "-q", "QueryParams" },
    { "-d", "Domain" }
};

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

    // If an empty field is put in via commandline, it won't register as empty
    // It will cannabalize the next flag, and then ruin the next flag's operation
    // Therefore, we have to abort the program in this instance

    if (!string.IsNullOrEmpty(inputOptions.TestPlanFile))
    {
        if (inputOptions.TestPlanFile.Substring(0, 1) == "-")
        {
            Console.Out.WriteLine("[Error]: Test plan file field is blank.");
            return;
        }
    }

    if (!string.IsNullOrEmpty(inputOptions.EnvironmentId))
    {
        if (inputOptions.EnvironmentId.Substring(0, 1) == "-")
        {
            Console.Out.WriteLine("[Error]: Environment ID field is blank.");
            return;
        }
    }

    if (!string.IsNullOrEmpty(inputOptions.TenantId))
    {
        if (inputOptions.TenantId.Substring(0, 1) == "-")
        {
            Console.Out.WriteLine("[Error]: Tenant ID field is blank.");
            return;
        }
    }

    if (!string.IsNullOrEmpty(inputOptions.OutputDirectory))
    {
        if (inputOptions.OutputDirectory.Substring(0, 1) == "-")
        {
            Console.Out.WriteLine("[Error]: Output Directory field is blank.");
            return;
        }
    }

    if (!string.IsNullOrEmpty(inputOptions.LogLevel))
    {
        if (inputOptions.LogLevel.Substring(0, 1) == "-")
        {
            Console.Out.WriteLine("[Error]: Log Level field is blank.");
            return;
        }
    }

    if (!string.IsNullOrEmpty(inputOptions.QueryParams))
    {
        if (inputOptions.QueryParams.Substring(0, 1) == "-")
        {
            Console.Out.WriteLine("[Error]: Query Params field is blank.");
            return;
        }
    }

    if (!string.IsNullOrEmpty(inputOptions.Domain))
    {
        if (inputOptions.Domain.Substring(0, 1) == "-")
        {
            Console.Out.WriteLine("[Error]: Domain field is blank.");
            return;
        }
    }

    var logLevel = LogLevel.Information; // Default log level
    if (!string.IsNullOrEmpty(inputOptions.LogLevel) && !Enum.TryParse(inputOptions.LogLevel, true, out logLevel))
    {
        Console.Out.WriteLine($"Unable to parse log level: {inputOptions.LogLevel}, using default: Information");
    }

    var serviceProvider = new ServiceCollection()
    .AddLogging(loggingBuilder =>
    {
        loggingBuilder
        .ClearProviders()
        .AddFilter(l => l >= logLevel)
        .AddProvider(new TestLoggerProvider(new FileSystem()));
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
    .AddScoped<ILogger>((sp) => sp.GetRequiredService<ISingleTestInstanceState>().GetLogger())
    .AddSingleton<IFileSystem, FileSystem>()
    .AddSingleton<IEnvironmentVariable, EnvironmentVariable>()
    .AddSingleton<TestEngine>()
    .BuildServiceProvider();

    TestEngine testEngine = serviceProvider.GetRequiredService<TestEngine>();

    var queryParams = "";
    var domain = "apps.powerapps.com";

    if (!string.IsNullOrEmpty(inputOptions.QueryParams))
    {
        queryParams = inputOptions.QueryParams;
    }

    if (!string.IsNullOrEmpty(inputOptions.Domain))
    {
        domain = inputOptions.Domain;
    }

    var testResult = await testEngine.RunTestAsync(inputOptions.TestPlanFile, inputOptions.EnvironmentId, inputOptions.TenantId, inputOptions.OutputDirectory, domain, queryParams);

    Console.Out.WriteLine($"TRX file: {testResult}\n");
}
