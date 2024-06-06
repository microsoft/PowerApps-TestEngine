// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using NuGet.Configuration;

namespace Microsoft.PowerApps.TestEngine.Modules
{
    /// <summary>
    /// Load matching Test Engine Managed Extensibility Framework modules
    /// </summary>
    public class TestEngineModuleMEFLoader
    {
        public Func<string, bool> DirectoryExists { get; set; } = (string location) => Directory.Exists(location);

        public Func<string, string, string[]> DirectoryGetFiles { get; set; } = (string location, string searchPattern) => Directory.GetFiles(location, searchPattern);

        public Func<string, AssemblyCatalog> LoadAssembly { get; set; } = (string file) => new AssemblyCatalog(file);

        public TestEngineExtensionChecker Checker { get; set; }

        ILogger _logger;

        public TestEngineModuleMEFLoader(ILogger logger)
        {
            _logger = logger;
            Checker = new TestEngineExtensionChecker(logger);
        }

        /// <summary>
        /// Load matching modules using test settings from provided file system location
        /// </summary>
        /// <param name="settings">The settings to use determine if extensions are enaabled and allow / deny settings</param>
        /// <param name="location">The file system location to read the modules from</param>
        /// <returns>Catalog of located modules</returns>
        public AggregateCatalog LoadModules(TestSettingExtensions settings)
        {
            List<ComposablePartCatalog> match = new List<ComposablePartCatalog>() { };

            if (settings.Enable)
            {
                _logger.LogInformation("Extensions enabled");

                // Load MEF exports from this assembly 
                match.Add(new AssemblyCatalog(typeof(TestEngine).Assembly));

                foreach (var sourceLocation in settings.Source.InstallSource)
                {
                    string location = sourceLocation;
                    if (settings.Source.EnableNuGet)
                    {
                        var nuGetSettings = Settings.LoadDefaultSettings(null);
                        location = Path.Combine(SettingsUtility.GetGlobalPackagesFolder(nuGetSettings), location, "lib", "netstandard2.0");
                    }

                    if (!DirectoryExists(location))
                    {
                        _logger.LogDebug("Skipping " + location);
                        continue;
                    }

                    // Check if want all modules in the location
                    if (settings.DenyModule.Count == 0 && settings.AllowModule.Count() == 1 && settings.AllowModule[0].Equals("*") || settings.AllowModule.Count() == 0)
                    {
                        _logger.LogInformation("Load all modules from " + location);

                        var files = DirectoryGetFiles(location, "testengine.module.*.dll");
                        foreach (var file in files)
                        {
                            if (!string.IsNullOrEmpty(file))
                            {
                                _logger.LogInformation(Path.GetFileName(file));
                                if (settings.CheckAssemblies)
                                {
                                    if (!Checker.Validate(settings, file))
                                    {
                                        continue;
                                    }
                                }
                                match.Add(LoadAssembly(file));
                            }
                        }
                    }

                    var possibleUserManager = DirectoryGetFiles(location, "testengine.user.*.dll");
                    foreach (var possibleModule in possibleUserManager)
                    {
                        if (Checker.Verify(settings, possibleModule))
                        {
                            match.Add(LoadAssembly(possibleModule));
                        }
                    }

                    var possibleWebProviderModule = DirectoryGetFiles(location, "testengine.provider.*.dll");
                    foreach (var possibleModule in possibleWebProviderModule)
                    {
                        if (Checker.Verify(settings, possibleModule))
                        {
                            match.Add(LoadAssembly(possibleModule));
                        }
                    }

                    // Check if need to deny a module or a specific list of modules are allowed
                    if (settings.DenyModule.Count > 0 || (settings.AllowModule.Count() > 1))
                    {
                        _logger.LogInformation("Load modules from " + location);
                        var possibleModules = DirectoryGetFiles(location, "testengine.module.*.dll");
                        foreach (var possibleModule in possibleModules)
                        {
                            if (!string.IsNullOrEmpty(possibleModule))
                            {
                                // Convert from testegine.module.name.dll format to name for search comparision
                                var moduleName = Path.GetFileNameWithoutExtension(possibleModule).Replace("testengine.module.", "").ToLower();
                                var allow = settings.AllowModule.Any(a => Regex.IsMatch(moduleName, WildCardToRegular(a.ToLower())));
                                var deny = settings.DenyModule.Any(d => Regex.IsMatch(moduleName, WildCardToRegular(d.ToLower())));
                                var allowLongest = settings.AllowModule.Max(a => Regex.IsMatch(moduleName, WildCardToRegular(a.ToLower())) ? a : "");
                                var denyLongest = settings.DenyModule.Max(d => Regex.IsMatch(moduleName, WildCardToRegular(d.ToLower())) ? d : "");

                                // Two cases: 
                                //  1. Found deny but also found allow. Assume that the allow has higher proirity if a longer match
                                //      allow | deny | add
                                //      *     | name | No
                                //      name  | *    | Yes
                                //      n*    | name | No
                                //  2. No deny match found, allow is found
                                if (deny && allow && allowLongest.Length > denyLongest.Length || allow && !deny)
                                {
                                    if (settings.CheckAssemblies)
                                    {
                                        if (!Checker.Validate(settings, possibleModule))
                                        {
                                            continue;
                                        }
                                    }
                                    _logger.LogInformation(Path.GetFileName(possibleModule));
                                    if (Checker.Verify(settings, possibleModule))
                                    {
                                        match.Add(LoadAssembly(possibleModule));
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                _logger.LogInformation("Extensions not enabled");
            }

            AggregateCatalog results = new AggregateCatalog(match);
            return results;
        }

        private static String WildCardToRegular(String value)
        {
            return "^" + Regex.Escape(value).Replace("\\*", ".*") + "$";
        }
    }
}
