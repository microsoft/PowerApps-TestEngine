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
                    var loadedAllModules = false;
                    if (settings.DenyModule?.Count == 0 && (settings.AllowModule?.Count() == 1 && settings.AllowModule.Contains("*") || settings.AllowModule?.Count() == 0))
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
                                        _logger.LogInformation($"Skipping {file}");
                                        continue;
                                    }
                                    _logger.LogInformation(Path.GetFileName(file));
                                    if (Checker.Verify(settings, file))
                                    {
                                        match.Add(LoadAssembly(file));
                                    }
                                }
                            }
                        }
                        loadedAllModules = true;
                    }

                    var possibleUserManager = DirectoryGetFiles(location, "testengine.user.*.dll");
#if RELEASE
                    //temporarily limiting to a fixed set of providers, move to allow deny list later #410
                    var allowedUserManager = new string[] { Path.Combine(location, "testengine.user.storagestate.dll") };
                    possibleUserManager = possibleUserManager.Where(file => allowedUserManager.Contains(file)).ToArray();
#endif
                    foreach (var possibleModule in possibleUserManager)
                    {
                        if (!Checker.ValidateProvider(settings, possibleModule))
                        {
                            _logger.LogInformation($"Skipping provider {possibleModule}");
                            continue;
                        }

                        if (Checker.Verify(settings, possibleModule))
                        {
                            match.Add(LoadAssembly(possibleModule));
                        }
                    }

                    var possibleWebProviderModule = DirectoryGetFiles(location, "testengine.provider.*.dll");
#if RELEASE
                    //temporarily limiting to a fixed set of providers, move to allow deny list later #410
                    var allowedProviderManager = new string[] { Path.Combine(location, "testengine.provider.canvas.dll"), Path.Combine(location, "testengine.provider.mda.dll"), Path.Combine(location, "testengine.provider.powerapps.portal.dll") };
                    possibleWebProviderModule = possibleWebProviderModule.Where(file => allowedProviderManager.Contains(file)).ToArray();
#endif
                    foreach (var possibleModule in possibleWebProviderModule)
                    {
                        if (!Checker.ValidateProvider(settings, possibleModule))
                        {
                            _logger.LogInformation($"Skipping provider {possibleModule}");
                            continue;
                        }

                        if (Checker.Verify(settings, possibleModule))
                        {
                            match.Add(LoadAssembly(possibleModule));
                        }
                    }

                    var possibleAuthTypeProviderModule = DirectoryGetFiles(location, "testengine.auth.*.dll");
#if RELEASE
                    //temporarily limiting to a fixed set of providers for milestone 1, move to allow deny list later #410. Environment Certificate used for multi machine auth
                    var allowedAuthTypeManager = new string[] { Path.Combine(location, "testengine.auth.environment.certificate.dll"), Path.Combine(location, "testengine.auth.certificatestore.dll") };
                    possibleAuthTypeProviderModule = possibleAuthTypeProviderModule.Where(file => allowedAuthTypeManager.Contains(file)).ToArray();
#endif
                    foreach (var possibleModule in possibleAuthTypeProviderModule)
                    {
                        if (!Checker.ValidateProvider(settings, possibleModule))
                        {
                            _logger.LogInformation($"Skipping provider {possibleModule}");
                            continue;
                        }
                        if (Checker.Verify(settings, possibleModule))
                        {
                            match.Add(LoadAssembly(possibleModule));
                        }
                    }

                    // Check if need to deny a module or a specific list of modules are allowed
                    if (!loadedAllModules)
                    {
                        _logger.LogInformation("Load modules from " + location);
                        var possibleModules = DirectoryGetFiles(location, "testengine.module.*.dll");
                        foreach (var possibleModule in possibleModules)
                        {
                            if (!string.IsNullOrEmpty(possibleModule))
                            {
                                // Convert from testegine.module.name.dll format to name for search comparision
                                var moduleName = Path.GetFileNameWithoutExtension(possibleModule).Replace("testengine.module.", "").ToLower();
                                var allow = settings.AllowModule != null ? settings.AllowModule.Any(a => Regex.IsMatch(moduleName, WildCardToRegular(a.ToLower()))) : false;
                                var deny = settings.DenyModule != null ? settings.DenyModule.Any(d => Regex.IsMatch(moduleName, WildCardToRegular(d.ToLower()))) : true;
                                var allowLongest = settings.AllowModule?.Max(a => Regex.IsMatch(moduleName, WildCardToRegular(a.ToLower())) ? a : "");
                                var denyLongest = settings.DenyModule?.Max(d => Regex.IsMatch(moduleName, WildCardToRegular(d.ToLower())) ? d : "");

                                // Two cases: 
                                //  1. Found deny but also found allow. Assume that the allow has higher proirity if a longer match
                                //      allow | deny | add
                                //      *     | name | No
                                //      name  | *    | Yes
                                //      n*    | name | No
                                //  2. No deny match found, allow is found
                                if (deny && allow && allowLongest?.Length > denyLongest?.Length || allow && !deny)
                                {
                                    if (settings.CheckAssemblies)
                                    {
                                        if (!Checker.Validate(settings, possibleModule))
                                        {
                                            _logger.LogInformation($"Skipping {possibleModule}");
                                            continue;
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
