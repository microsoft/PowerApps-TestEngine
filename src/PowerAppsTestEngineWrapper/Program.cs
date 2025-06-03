// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Modules;
using Microsoft.PowerApps.TestEngine.PowerFx;
using Microsoft.PowerApps.TestEngine.Providers;
using Microsoft.PowerApps.TestEngine.Reporting;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerApps.TestEngine.Users;

namespace PowerAppsTestEngineWrapper
{
    public class Program
    {
        public static async Task Main(string[] args)
        {

            var switchMappings = new Dictionary<string, string>()
            {
                { "-i", "TestPlanFile" },
                { "-e", "EnvironmentId" },
                { "-t", "TenantId" },
                { "-o", "OutputDirectory" },
                { "-l", "LogLevel" },
                { "-q", "QueryParams" },
                { "-d", "Domain" },
                { "-m", "Modules" },
                { "-u", "UserAuth" },
                { "-p", "Provider" },
                { "-a", "UserAuthType"},
                { "-w", "Wait" },
                { "-r", "Record" },
                { "-c", "UseStaticContext" },
                { "--run-name", "RunName" },
                { "--output-file", "OutputFile" }
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
                Console.WriteLine("[Critical Error]: Input options are null");
                return;
            }
            else
            {
                if (!string.IsNullOrEmpty(inputOptions.OutputFile) && !string.IsNullOrEmpty(inputOptions.RunName))
                {
                    var system = new FileSystem();
                    var summary = new TestRunSummary(system);
                    summary.GenerateSummaryReport(Path.Combine(system.GetDefaultRootTestEngine(), "TestOutput"), inputOptions.OutputFile, inputOptions.RunName);
                    return;
                }

                // If an empty field is put in via commandline, it won't register as empty
                // It will cannabalize the next flag, and then ruin the next flag's operation
                // Therefore, we have to abort the program in this instance

                if (!string.IsNullOrEmpty(inputOptions.TestPlanFile))
                {
                    if (inputOptions.TestPlanFile.Substring(0, 1) == "-")
                    {
                        Console.WriteLine("[Critical Error]: TestPlanFile field is blank.");
                        return;
                    }
                }

                if (!string.IsNullOrEmpty(inputOptions.EnvironmentId))
                {
                    if (inputOptions.EnvironmentId.Substring(0, 1) == "-")
                    {
                        Console.WriteLine("[Critical Error]: EnvironmentId field is blank.");
                        return;
                    }
                }

                if (!string.IsNullOrEmpty(inputOptions.TenantId))
                {
                    if (inputOptions.TenantId.Substring(0, 1) == "-")
                    {
                        Console.WriteLine("[Critical Error]: TenantId field is blank.");
                        return;
                    }
                }

                if (!string.IsNullOrEmpty(inputOptions.OutputDirectory))
                {
                    if (inputOptions.OutputDirectory.Substring(0, 1) == "-")
                    {
                        Console.WriteLine("[Critical Error]: OutputDirectory field is blank.");
                        return;
                    }
                }

                if (!string.IsNullOrEmpty(inputOptions.LogLevel))
                {
                    if (inputOptions.LogLevel.Substring(0, 1) == "-")
                    {
                        Console.WriteLine("[Critical Error]: LogLevel field is blank.");
                        return;
                    }
                }

                if (!string.IsNullOrEmpty(inputOptions.Domain))
                {
                    if (inputOptions.Domain.Substring(0, 1) == "-")
                    {
                        Console.WriteLine("[Critical Error]: Domain field is blank.");
                        return;
                    }
                }

                if (!string.IsNullOrEmpty(inputOptions.QueryParams))
                {
                    if (inputOptions.QueryParams.Substring(0, 1) == "-")
                    {
                        Console.WriteLine("[Critical Error]: QueryParams field is blank.");
                        return;
                    }
                }
                if (!string.IsNullOrEmpty(inputOptions.Wait))
                {
                    if (inputOptions.Wait.Substring(0, 1) == "-")
                    {
                        Console.WriteLine("[Critical Error]: Wait field is blank. Set value to True or False.");
                        return;
                    }

                    if (inputOptions.Wait.ToLower() == "true")
                    {
                        Console.WriteLine("Waiting, press enter to continue. You can now optionally attach debugger to dotnet PowerAppsTestEngine.dll process now");
                        Console.ReadLine();
                        if (Debugger.IsAttached)
                        {
                            // Welcome to the debugger experience for Power Apps Test Engine
                            //
                            // Key classes you may want to investigate and add breakpoint inside to understand key components or :
                            // - SingleTestRunner.RunTestAsync that will run a single test case
                            // - PlaywrightTestInfraFunctions.SetupAsync for setup of Playwright state
                            // - PowerFxEngine.ExecuteWithRetryAsync that execute Power Fx test steps
                            // - Implementations or ITestWebProvider for Test Engine providers that get the state of the resource to be tested
                            // - Implementations of ITestEngineModule for Power Fx extensions
                            Debugger.Break();
                        }
                    }
                }
                var UseStaticContextValue = false;
                if (!string.IsNullOrEmpty(inputOptions.UseStaticContext))
                {
                    if (inputOptions.UseStaticContext.Substring(0, 1) == "-")
                    {
                        Console.WriteLine("[Critical Error]: UseStaticContext field is blank. Set value to True or False.");
                        return;
                    }

                    if (inputOptions.UseStaticContext.ToLower() == "true")
                    {
                        UseStaticContextValue = true;
                    }
                }


                var logLevel = LogLevel.Information; // Default log level
                if (string.IsNullOrEmpty(inputOptions.LogLevel) || !Enum.TryParse(inputOptions.LogLevel, true, out logLevel))
                {
                    logLevel = LogLevel.Information;
                }

                var userAuth = "storagestate"; // Default to storage state
                if (!string.IsNullOrEmpty(inputOptions.UserAuth))
                {
                    userAuth = inputOptions.UserAuth;
                }

                var provider = "canvas";
                if (!string.IsNullOrEmpty(inputOptions.Provider))
                {
                    provider = inputOptions.Provider;
                }

                var auth = "default";
                if (!string.IsNullOrEmpty(inputOptions.UserAuthType))
                {
                    auth = inputOptions.UserAuthType;
                }

                try
                {
                    using var loggerFactory = LoggerFactory.Create(loggingBuilder => loggingBuilder
                        .ClearProviders()
                        .AddFilter(l => l >= logLevel)
                        .AddProvider(new TestLoggerProvider(new FileSystem())));

                    var logger = loggerFactory.CreateLogger<Program>();

                    var serviceProvider = new ServiceCollection()
                    .AddSingleton<ILoggerFactory>(loggerFactory)
                    .AddSingleton<ITestEngineEvents, TestEngineEventHandler>()
                    .AddSingleton<ITestConfigParser, YamlTestConfigParser>()
                    .AddScoped<IPowerFxEngine, PowerFxEngine>()
                    .AddScoped<IUserManager>(sp =>
                    {
                        var testState = sp.GetRequiredService<ITestState>();
                        var userManagers = testState.GetTestEngineUserManager();
                        if (userManagers.Count == 0)
                        {
                            testState.LoadExtensionModules(logger);
                            userManagers = testState.GetTestEngineUserManager();
                        }

                        var match = userManagers.Where(x => x.Name.Equals(userAuth)).FirstOrDefault();

                        if (match == null)
                        {
                            throw new InvalidDataException($"Unable to find user auth {userAuth}");
                        }
                        match.UseStaticContext = UseStaticContextValue;

                        return match;
                    })
                    .AddTransient<ITestWebProvider>(sp =>
                    {
                        var testState = sp.GetRequiredService<ITestState>();
                        var testWebProviders = testState.GetTestEngineWebProviders();
                        if (testWebProviders.Count == 0)
                        {
                            testState.LoadExtensionModules(logger);
                            testWebProviders = testState.GetTestEngineWebProviders();
                        }

                        var match = testWebProviders.Where(x => x.Name.Equals(provider)).FirstOrDefault();

                        if (match == null)
                        {
                            throw new InvalidDataException($"Unable to find provider {provider}");
                        }


                        return match;
                    })
                    .AddSingleton<IUserCertificateProvider>(sp =>
                    {
                        var testState = sp.GetRequiredService<ITestState>();
                        var testAuthProviders = testState.GetTestEngineAuthProviders();
                        if (testAuthProviders.Count == 0)
                        {
                            testState.LoadExtensionModules(logger);
                            testAuthProviders = testState.GetTestEngineAuthProviders();
                        }

                        var match = testAuthProviders.Where(x => x.Name.Equals(auth)).FirstOrDefault();

                        if (match == null)
                        {
                            match = new DefaultUserCertificateProvider();
                        }

                        return match;
                    })
                    .AddSingleton<ITestState, TestState>()
                    .AddSingleton<ITestReporter, TestReporter>()
                    .AddScoped<ISingleTestInstanceState, SingleTestInstanceState>()
                    .AddScoped<ISingleTestRunner, SingleTestRunner>()
                    .AddScoped<ILogger>((sp) => sp.GetRequiredService<ISingleTestInstanceState>().GetLogger())
                    .AddSingleton<IFileSystem, FileSystem>()
                    .AddScoped<ITestInfraFunctions, PlaywrightTestInfraFunctions>()
                    .AddSingleton<IEnvironmentVariable, EnvironmentVariable>()
                    .AddSingleton<IUserManagerLogin, UserManagerLogin>()
                    .AddSingleton<TestEngine>()
                    .BuildServiceProvider();

                    TestEngine testEngine = serviceProvider.GetRequiredService<TestEngine>();

                    // Default value for optional arguments is set before the class library is invoked.
                    // The class library expects actual types in its input arguments, so optional arguments
                    // to the Test Engine entry point function RunTestAsync must be checked for null values and their
                    // corresponding default values set beforehand.
                    var testPlanFile = new FileInfo(inputOptions.TestPlanFile);
                    var tenantId = Guid.Parse(inputOptions.TenantId);
                    var environmentId = inputOptions.EnvironmentId;
                    var domain = string.Empty;
                    var queryParams = "";

                    DirectoryInfo outputDirectory;
                    const string DefaultOutputDirectory = "TestOutput";
                    var _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
                    if (!string.IsNullOrEmpty(inputOptions.OutputDirectory))
                    {
                        if (Path.IsPathRooted(inputOptions.OutputDirectory.Trim()))
                        {
                            Console.WriteLine("[Critical Error]: Please provide a relative path for the output.");
                            return;
                        }
                        else
                        {
                            outputDirectory = new DirectoryInfo(Path.Combine(_fileSystem.GetDefaultRootTestEngine(), inputOptions.OutputDirectory.Trim()));
                        }
                    }
                    else
                    {
                        outputDirectory = new DirectoryInfo(Path.Combine(_fileSystem.GetDefaultRootTestEngine(), DefaultOutputDirectory.Trim()));
                    }

                    if (!string.IsNullOrEmpty(inputOptions.QueryParams))
                    {
                        queryParams = inputOptions.QueryParams;
                    }

                    if (!string.IsNullOrEmpty(inputOptions.Domain))
                    {
                        domain = inputOptions.Domain;
                    }

                    string modulePath = Path.GetDirectoryName(typeof(Program).Assembly.Location);
                    List<ITestEngineModule> modules = new List<ITestEngineModule>();
                    if (!string.IsNullOrEmpty(inputOptions.Modules) && Directory.Exists(inputOptions.Modules))
                    {
                        modulePath = inputOptions.Modules;
                    }

                    ITestState state = serviceProvider.GetService<ITestState>();
                    state.SetModulePath(modulePath);

                    if (!string.IsNullOrEmpty(inputOptions.Record))
                    {
                        state.SetRecordMode();
                    }

                    //setting defaults for optional parameters outside RunTestAsync
                    var testResult = await testEngine.RunTestAsync(testPlanFile, environmentId, tenantId, outputDirectory, domain, queryParams, inputOptions.RunName);
                    if (testResult != "InvalidOutputDirectory")
                    {
                        Console.WriteLine($"Test results can be found here: {testResult}");
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine("[Critical Error]: " + ex.Message);
                    Console.WriteLine(ex);
                }
            }
        }
    }
}
