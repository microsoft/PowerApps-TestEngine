// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using CommandLine;
using SimpleExec;
using static Bullseye.Targets;
using static SimpleExec.Command;

namespace targets
{
    class Options
    {
        [Value(0, MetaName = "target", HelpText = "build target to run; see available with: '--list", Default = "rebuild")]
        public string Target { get; set; }

        [Option('c', "configuration", Required = false, Default = "Debug")]
        public string Configuration { get; set; }

        [Option('o', "outputDirectory", Required = false, Default = "")]
        public string OutputDirectory { get; set; }
    }

    class Program
    {
        static Options options;

        static void Main(string[] args)
        {
            
            string RootDir = "", gitHash = "";
            bool gitExists = true;
            try 
            {
                RootDir = Read("git", "rev-parse --show-toplevel", noEcho: true).Trim();
                gitHash = Read("git", "rev-parse HEAD", noEcho: true).Trim();               
            }
            catch
            {
                RootDir = Directory.GetCurrentDirectory();
                Console.WriteLine("Unable to find root directory using git, assuming this script is being run from root = " + RootDir);
                gitExists = false;
            }

            string ObjDir = Path.Combine(RootDir, "obj");
            string SrcDir = Path.Combine(RootDir, "src");

            List<string> cleanDirs = new(){
                Path.Combine(RootDir, "bin"),
                Path.Combine(RootDir, "obj"),
                Path.Combine(RootDir, "pkg"),
                Path.Combine(SrcDir, "Microsoft.PowerApps.TestEngine/bin"),
                Path.Combine(SrcDir, "PowerAppsTestEngine/bin"),
                Path.Combine(SrcDir, "Microsoft.PowerApps.TestEngine.Tests/bin"),
                Path.Combine(SrcDir, "Microsoft.PowerApps.TestEngine/obj"),
                Path.Combine(SrcDir, "PowerAppsTestEngine/obj"),
                Path.Combine(SrcDir, "Microsoft.PowerApps.TestEngine.Tests/obj"),
                Path.Combine(SrcDir, "Microsoft.PowerApps.TestEngine/pkg"),
                Path.Combine(SrcDir, "PowerAppsTestEngine/pkg"),
                Path.Combine(SrcDir, "Microsoft.PowerApps.TestEngine.Tests/pkg")
            };            

            string LogDir = Path.Combine(ObjDir, "logs");
            string TestLogDir = Path.Combine(ObjDir, "testResults");

            string PATestEngineDir = Path.Combine(SrcDir, "Microsoft.PowerApps.TestEngine");
            var solution = Path.Combine(SrcDir, "PowerAppsTestEngine.sln");
            
            var project = Path.Combine(PATestEngineDir, "Microsoft.PowerApps.TestEngine.csproj");

            Target("squeaky-clean",
                () =>
                {
                    CleanDirectories(cleanDirs);
                });

            Target("clean",
                () => RunDotnet("clean", $"{EscapePath(solution)} --configuration {options.Configuration}", gitExists, LogDir));

            Target("restore",
                DependsOn("clean"),
                () => RunDotnet("restore", $"{EscapePath(solution)}", gitExists, LogDir));

            Target("build",
                () => {
                    if (gitExists)
                        CreateBuildHashFile(ObjDir, gitHash);
                    RunDotnet("build", $"{EscapePath(solution)} --configuration {options.Configuration} --no-restore", gitExists, LogDir);
                });

            Target("test",
                () => RunDotnet("test", $"{EscapePath(solution)} --configuration {options.Configuration} --no-build --logger trx --results-directory {EscapePath(TestLogDir)}", gitExists, LogDir));

            Target("rebuild",
                DependsOn("restore", "build"));

            Target("pack",
                () => RunDotnet("pack", $"{EscapePath(project)} --configuration {options.Configuration} --no-build -o {options.OutputDirectory} -p:Packing=true", gitExists, LogDir));

            Target("ci",
                DependsOn("squeaky-clean", "rebuild", "test"));

            Parser.Default.ParseArguments<Options>(args)
            .WithParsed<Options>(o =>
                {
                    options = o;
                    RunTargetsAndExit(new[] {options.Target},
                        logPrefix: options.Target,
                        messageOnly: ex => ex is NonZeroExitCodeException);
                })
            .WithNotParsed(errs =>
            {
                RunTargetsAndExit(args);
            });
        }

        static void RunDotnet(string verb, string verbArgs, bool gitExists, string LogDir)
        {
            var gitDef = "";
            if (gitExists) 
                gitDef = "-p:GitExists=true";
            var optionsLogPath = Path.Combine(LogDir, $"{verb}-{options.Configuration}");
            var logSettings = $"/clp:verbosity=minimal /flp:Verbosity=normal;LogFile={EscapePath(optionsLogPath + ".log")} /flp3:PerformanceSummary;Verbosity=diag;LogFile={EscapePath(optionsLogPath + ".diagnostics.log")}";
            Run("dotnet", $"{verb} {verbArgs} {logSettings} {gitDef} /nologo");
        }

        static void CreateBuildHashFile(string objDir, string gitHash)
        {
            var filePath = Path.Combine(objDir, "buildver.json");
            var file = new System.IO.FileInfo(filePath);
            file.Directory.Create();

            var jsonContents = new {
                CommitHash = gitHash,
#if !ADOBuild
                IsLocalBuild = true
#endif
            };
            var text = JsonSerializer.Serialize(jsonContents);
            File.WriteAllText(filePath, text);
        }

        static void CleanDirectory(string directoryPath)
        {
            directoryPath = Path.GetFullPath(directoryPath);            
            try {
                if (Directory.Exists(directoryPath))
                {
                    Console.WriteLine($"Cleaning directory: {directoryPath}");
                    Directory.Delete(directoryPath, recursive: true);
                }
            }
            catch (AccessViolationException) { /* swallow */ }
        }

        static void CleanDirectories(List<string> listDirectoryPath)
        {
            foreach (var directoryPath in listDirectoryPath)
            {
                CleanDirectory(directoryPath);
            }
        }

        static string EscapePath(string path)
        {
            return $"\"{path}\"";
        }
    }
}
