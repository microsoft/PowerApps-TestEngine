// Copyright(c) Microsoft Corporation.
// Licensed under the MIT license.

using System.ComponentModel.Composition;
using System.Net.Mail;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.Users;
using testengine.common.user;

namespace testengine.user.storagestate
{
    /// <summary>
    /// Implement single sign on to a test resource from a login.microsoftonline.com resource assuming storage state
    /// </summary>
    /// <remarks>
    ///Requires cookies 
    /// </remarks>
    [Export(typeof(IUserManager))]
    public class StorageStateUserManagerModule : IConfigurableUserManager
    {
        /// <summary>
        /// The namespace of namespaces that this provider relates to
        /// </summary>
        public string[] Namespaces { get; private set; } = new string[] { "TestEngine" };

        public Dictionary<string, object> Settings { get; private set; }

        public Action<IFileSystem, string> Protect = static (IFileSystem filesystem, string fileName) =>
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new InvalidOperationException("DPAPI protection only available on Windows");
            }

            bool fileSaved = false;
            try
            {
                var content = filesystem.ReadAllText(fileName);

                byte[] dataBytes = Encoding.UTF8.GetBytes(content);
                byte[] encryptedData = ProtectedData.Protect(dataBytes, null, DataProtectionScope.CurrentUser);
                filesystem.WriteTextToFile(fileName, Convert.ToBase64String(encryptedData), true);
                fileSaved = true;
            }
            catch
            {
                throw;
            }
            finally
            {
                if (!fileSaved)
                {
                    filesystem.Delete(fileName);
                }
            }
        };

        public Func<IFileSystem, string, string> Unprotect = (IFileSystem filesystem, string fileName) =>
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new InvalidOperationException("DPAPI protection only available on Windows");
            }
            var base64Text = filesystem.ReadAllText(fileName);
            byte[] dataBytes = Convert.FromBase64String(base64Text);
            byte[] decryptedData = ProtectedData.Unprotect(dataBytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decryptedData);
        };

        public StorageStateUserManagerModule()
        {
            Settings = ConfigureSettings();
        }

        private Dictionary<string, object> ConfigureSettings()
        {
            var result = new Dictionary<string, object>
            {
                { "LoadState", LoadStateIfExists() }
            };

            return result;
        }

        private Func<IEnvironmentVariable, ISingleTestInstanceState, ITestState, IFileSystem, string> LoadStateIfExists()
        {
            return (IEnvironmentVariable environmentVariable, ISingleTestInstanceState singleTestInstanceState, ITestState testState, IFileSystem fileSystem) =>
            {
                var testSuiteDefinition = singleTestInstanceState.GetTestSuiteDefinition();
                var userConfig = testState.GetUserConfiguration(testSuiteDefinition.Persona);

                if (string.IsNullOrEmpty(userConfig.EmailKey))
                {
                    return String.Empty;
                }

                var user = environmentVariable.GetVariable(userConfig.EmailKey);
                var userName = GetUserNameFromEmail(user);
                Location = !Location.EndsWith($"-{userName}") ? Location += $"-{userName}" : Location;
                Location = !Path.IsPathRooted(Location) ? Path.Combine(fileSystem.GetDefaultRootTestEngine(), Location) : Location;
                if (!IsValidEmail(user))
                {
                    return String.Empty;
                }

                if (!Path.IsPathRooted(Location))
                {
                    Location = Path.Combine(fileSystem.GetDefaultRootTestEngine(), Location);
                }

                if (!fileSystem.Exists(Location))
                {
                    return String.Empty;
                }

                var stateFile = Path.Combine(Location, "state.json");
                if (fileSystem.FileExists(stateFile))
                {
                    return Unprotect(fileSystem, stateFile);
                }

                return String.Empty;
            };
        }

        public virtual string Name => "storagestate";

        public int Priority => 300;

        public bool UseStaticContext { get; set; } = false;

        public string Location { get; set; } = ".storage-state";

        public string ContextLocation { get; } = ".TemporaryContext";

        public static string EmailSelector = "input[type=\"email\"]";

        /// <summary>
        /// Assume that navigation to the resoure url has started.
        /// </summary>
        /// <param name="desiredUrl">The page that has been requested to be opened</param>
        /// <param name="context">The created browser context</param>
        /// <param name="testState">Current test settings to execute</param>
        /// <param name="singleTestInstanceState">The test instance that will be executed to obtain a result</param>
        /// <param name="environmentVariable">Access to environments</param>
        /// <param name="userManagerLogin">User manager that may be required as part of login process</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="UserInputException"></exception>
        public async Task LoginAsUserAsync(
            string desiredUrl,
            IBrowserContext context,
            ITestState testState,
            ISingleTestInstanceState singleTestInstanceState,
            IEnvironmentVariable environmentVariable,
            IUserManagerLogin userManagerLogin)
        {
            var testSuiteDefinition = singleTestInstanceState.GetTestSuiteDefinition();
            var logger = singleTestInstanceState.GetLogger();

            var userConfig = testState.GetUserConfiguration(testSuiteDefinition.Persona);

            if (userConfig == null)
            {
                logger.LogError("Cannot find user config for persona");
                throw new InvalidOperationException();
            }

            var user = environmentVariable.GetVariable(userConfig.EmailKey);

            if (string.IsNullOrEmpty(user))
            {
                logger.LogError("Email key for persona cannot be empty");
                throw new InvalidOperationException();
            }

            if (!IsValidEmail(user))
            {
                logger.LogError($"Invalid email {userConfig.EmailKey} for {testSuiteDefinition.Persona}");
                throw new UserInputException();
            }

            if (!Settings.ContainsKey("FileSystem") || Settings.ContainsKey("FileSystem") && !(Settings["FileSystem"] is IFileSystem))
            {
                throw new UserInputException("File system not provided");
            }

            var fileSystem = Settings["FileSystem"] as IFileSystem;

            var userName = GetUserNameFromEmail(user);
            Location = !Location.EndsWith($"-{userName}") ? Location += $"-{userName}" : Location;
            Location = !Path.IsPathRooted(Location) ? Path.Combine(fileSystem.GetDefaultRootTestEngine(), Location) : Location;

            if (!Path.IsPathRooted(Location))
            {
                Location = Path.Combine(fileSystem.GetDefaultRootTestEngine(), Location);
            }

            if (!fileSystem.Exists(Location))
            {
                fileSystem.CreateDirectory(Location);
            }

            var started = DateTime.Now;

            // Wait a minimum of a five minutes
            var timeout = Math.Max(5 * 60000, testState.GetTimeout());

            var state = new LoginState()
            {
                DesiredUrl = desiredUrl,
                UserEmail = user,
                CallbackErrorFound = async () =>
                {
                    var stateFile = Path.Combine(Location, "state.json");
                    try
                    {
                        await context.StorageStateAsync(new BrowserContextStorageStateOptions { Path = stateFile });
                        Protect(fileSystem, stateFile);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error saving storage state after error. Cleaning up partial state file.");
                        // Delete the state file if it was created but encryption failed
                        if (fileSystem.FileExists(stateFile))
                        {
                            try
                            {
                                fileSystem.Delete(stateFile);
                                logger.LogInformation("Deleted partial storage state file due to error");
                            }
                            catch (Exception deleteEx)
                            {
                                logger.LogWarning(deleteEx, $"Could not delete partial storage state file: {stateFile}");
                            }
                        }
                        throw;
                    }
                },
                CallbackDesiredUrlFound = async (match) =>
                {
                    var stateFile = Path.Combine(Location, "state.json");
                    try
                    {
                        await context.StorageStateAsync(new BrowserContextStorageStateOptions { Path = stateFile });
                        Protect(fileSystem, stateFile);
                        logger.LogInformation("Successfully saved and encrypted storage state");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error saving storage state. Cleaning up partial state file.");
                        // Delete the state file if it was created but encryption failed
                        if (fileSystem.FileExists(stateFile))
                        {
                            try
                            {
                                fileSystem.Delete(stateFile);
                                logger.LogInformation("Deleted partial storage state file due to error");
                            }
                            catch (Exception deleteEx)
                            {
                                logger.LogWarning(deleteEx, $"Could not delete partial storage state file: {stateFile}");
                            }
                        }
                        throw;
                    }
                },
                CallbackRedirectRequiredFound = !UseStaticContext ? null : async (matchPage) =>
                {
                    try
                    {
                        var present = context.Pages.First(p => p == matchPage);
                        if (present != null)
                        {
                            logger.LogDebug($"Redirecting to desired URL: {desiredUrl}");
                            var resp = await present.GotoAsync(desiredUrl);
                            if (resp?.Ok == true)
                            {
                                logger.LogDebug($"Successfully navigated to desired URL, waiting for NetworkIdle");
                                await present.WaitForLoadStateAsync(LoadState.NetworkIdle);
                                logger.LogDebug($"NetworkIdle state reached");
                            }
                            else
                            {
                                logger.LogWarning($"Navigation to {desiredUrl} returned status: {resp?.Status}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"Error during redirect to desired URL: {desiredUrl}");
                    }
                },
                Module = this
            };

            // Create a custom PowerPlatformLogin with FnO-compatible idle check
            var loginHelper = new PowerPlatformLogin();
            
            // Override the LoginIsComplete check for FnO portals
            // FnO doesn't have O365_MainLink_NavMenu, so check for document ready state and common FnO elements
            loginHelper.LoginIsComplete = async (page) =>
            {
                try
                {
                    // Check if page is fully loaded
                    var readyState = await page.EvaluateAsync<string>("document.readyState");
                    logger.LogTrace($"Document ready state: {readyState}");
                    
                    if (readyState != "complete")
                    {
                        return false;
                    }
                    
                    // For FnO portals, check if we're on the actual portal (not login page)
                    var url = page.Url.ToLowerInvariant();
                    
                    // If we're still on Microsoft login pages, not ready
                    if (url.Contains("login.microsoftonline.com") || 
                        url.Contains("login.microsoft.com") ||
                        url.Contains("login.live.com"))
                    {
                        logger.LogTrace($"Still on login page: {url}");
                        return false;
                    }
                    
                    // Check for common FnO portal elements or lack of login elements
                    var hasFnOElements = await page.EvaluateAsync<bool>(@"
                        () => {
                            // Check if we have FnO specific elements or at least not login elements
                            var hasLoginEmail = document.querySelector('input[type=""email""]') !== null;
                            var hasLoginPassword = document.querySelector('input[type=""password""]') !== null;
                            var hasSubmitButton = document.querySelector('input[type=""submit""]') !== null;
                            
                            // If we have login elements, we're not ready
                            if (hasLoginEmail || hasLoginPassword || hasSubmitButton) {
                                return false;
                            }
                            
                            // Otherwise, assume we're on the portal
                            return true;
                        }
                    ");
                    
                    logger.LogTrace($"FnO elements check passed: {hasFnOElements}");
                    return hasFnOElements;
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "Error during idle check");
                    return false;
                }
            };

            logger.LogDebug($"Waiting for {timeout} milliseconds for desired url: {desiredUrl}");
            
            // Parse the desired URL to get base URL for flexible matching
            Uri desiredUri;
            string desiredBaseUrl = desiredUrl;
            string desiredHost = "";
            try
            {
                desiredUri = new Uri(desiredUrl);
                desiredHost = desiredUri.Host;
                desiredBaseUrl = $"{desiredUri.Scheme}://{desiredUri.Host}{desiredUri.AbsolutePath}".TrimEnd('/');
                logger.LogDebug($"Parsed desired URL - Host: {desiredHost}, Base URL: {desiredBaseUrl}");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, $"Could not parse desired URL: {desiredUrl}");
            }
            
            while (DateTime.Now.Subtract(started).TotalMilliseconds < timeout && !state.FoundMatch && !state.IsError)
            {
                try
                {
                    foreach (var page in context.Pages)
                    {
                        var currentUrl = page.Url;
                        
                        // Log current page URL to help diagnose matching issues
                        logger.LogDebug($"Checking page: {currentUrl}");
                        
                        // Try flexible URL matching: compare hosts and paths without query parameters
                        try
                        {
                            var currentUri = new Uri(currentUrl.Replace(".mcas.ms", ""));
                            var currentHost = currentUri.Host;
                            var currentBaseUrl = $"{currentUri.Scheme}://{currentUri.Host}{currentUri.AbsolutePath}".TrimEnd('/');
                            
                            logger.LogTrace($"Current URL - Host: {currentHost}, Base URL: {currentBaseUrl}");
                            
                            // Check if we're on the right domain and path (ignore query parameters)
                            if (currentHost.Equals(desiredHost, StringComparison.OrdinalIgnoreCase) &&
                                await loginHelper.LoginIsComplete(page))
                            {
                                logger.LogInformation($"Found matching host and page is idle");
                                
                                if (!state.FoundMatch)
                                {
                                    if (state.CallbackDesiredUrlFound != null && !state.CallbackDesired)
                                    {
                                        await state.CallbackDesiredUrlFound(currentUrl);
                                        state.CallbackDesired = true;
                                    }
                                    
                                    state.FoundMatch = true;
                                    state.MatchHost = currentHost;
                                    logger.LogInformation($"Successfully matched desired URL on host: {state.MatchHost}");
                                }
                            }
                        }
                        catch (Exception urlEx)
                        {
                            logger.LogTrace(urlEx, $"Could not parse URL for flexible matching: {currentUrl}");
                        }
                        
                        // Fall back to original HandleCommonLoginState logic
                        if (!state.FoundMatch)
                        {
                            state.Page = page;
                            await loginHelper.HandleCommonLoginState(state);
                        }
                        
                        // Log state after handling
                        logger.LogDebug($"After processing page - FoundMatch: {state.FoundMatch}, IsError: {state.IsError}, MatchHost: {state.MatchHost}");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error waiting for login");
                }

                if (!state.FoundMatch && !state.IsError)
                {
                    logger.LogDebug($"Desired page not found, elapsed time: {DateTime.Now.Subtract(started).TotalSeconds:F1} seconds");
                    System.Threading.Thread.Sleep(1000);
                }
                else
                {
                    logger.LogInformation($"Test page found after {DateTime.Now.Subtract(started).TotalSeconds:F1} seconds");
                    break;
                }
            }
        }

        public bool IsValidEmail(string emailAddress)
        {
            try
            {
                if (string.IsNullOrEmpty(emailAddress))
                {
                    return false;
                }
                var email = new MailAddress(emailAddress);
                return email.Address == emailAddress.Trim();
            }
            catch
            {
                return false;
            }
        }

        public string GetUserNameFromEmail(string emailAddress)
        {
            if (string.IsNullOrEmpty(emailAddress))
            {
                return String.Empty;
            }

            // Extract the username part of the email address
            var userName = emailAddress.Split('@');

            // Remove invalid characters for file paths
            string invalidChars = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            string invalidCharsPattern = string.Format("[{0}]", Regex.Escape(invalidChars));
            string validUserName = Regex.Replace(userName[0], invalidCharsPattern, "_");

            return validUserName;
        }
    }
}
