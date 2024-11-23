// Copyright(c) Microsoft Corporation.
// Licensed under the MIT license.

using System.ComponentModel.Composition;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.Users;

namespace testengine.user.environment
{
    [Export(typeof(IUserManager))]
    public class CertificateUserManagerModule : IUserManager
    {
        /// <summary>
        /// The namespace of namespaces that this provider relates to
        /// </summary>
        public string[] Namespaces { get; private set; } = new string[] { "TestEngine" };

        // defining these 2 for improved testability
        public static Func<HttpClientHandler> GetHttpClientHandler = () => new HttpClientHandler();
        public static Func<HttpClientHandler, HttpClient> GetHttpClient = handler => new HttpClient(handler);

        public string Name { get { return "certificate"; } }

        public int Priority { get { return 50; } }

        public bool UseStaticContext { get { return false; } }

        public string Location { get; set; } = string.Empty;

        public IPage? Page { get; set; }

        private IBrowserContext? Context { get; set; }

        public static string EmailSelector = "input[type=\"email\"]";
        public static string SubmitButtonSelector = "input[type=\"submit\"]";
        public static string StaySignedInSelector = "[id=\"KmsiCheckboxField\"]";
        public static string KeepMeSignedInNoSelector = "[id=\"idBtn_Back\"]";

        private TaskCompletionSource<bool> responseReceived = new TaskCompletionSource<bool>();

        public async Task LoginAsUserAsync(
            string desiredUrl,
            IBrowserContext context,
            ITestState testState,
            ISingleTestInstanceState singleTestInstanceState,
            IEnvironmentVariable environmentVariable,
            IUserManagerLogin userManagerLogin)
        {
            Context = context;

            var testSuiteDefinition = singleTestInstanceState.GetTestSuiteDefinition();
            var logger = singleTestInstanceState.GetLogger();
            logger.LogDebug("Beginning certificate login authentication.");
            if (testSuiteDefinition == null)
            {
                logger.LogError("Test definition cannot be null");
                throw new InvalidOperationException();
            }

            if (string.IsNullOrEmpty(testSuiteDefinition.Persona))
            {
                logger.LogError("Persona cannot be empty");
                throw new InvalidOperationException();
            }

            var userConfig = testState.GetUserConfiguration(testSuiteDefinition.Persona);

            if (userConfig == null)
            {
                logger.LogError("Cannot find user config for persona");
                throw new InvalidOperationException();
            }

            if (string.IsNullOrEmpty(userConfig.EmailKey))
            {
                logger.LogError("Email key for persona cannot be empty");
                throw new InvalidOperationException();
            }

            if (string.IsNullOrEmpty(userConfig.CertificateSubjectKey))
            {
                logger.LogError("Certificate subject name key for persona cannot be empty");
                throw new InvalidOperationException();
            }

            var user = environmentVariable.GetVariable(userConfig.EmailKey);
            var certName = environmentVariable.GetVariable(userConfig.CertificateSubjectKey);
            bool missingUserOrCert = false;
            if (string.IsNullOrEmpty(user))
            {
                logger.LogError("User email cannot be null. Please check if the environment variable is set properly.");
                missingUserOrCert = true;
            }
            if (string.IsNullOrEmpty(certName))
            {
                logger.LogError("User certificate subject name cannot be null. Please check if the environment variable is set properly.");
                missingUserOrCert = true;
            }
            if (missingUserOrCert)
            {
                throw new UserInputException(UserInputException.ErrorMapping.UserInputExceptionLoginCredential.ToString());
            }

            X509Certificate2 cert = null;
            var userCertificateProvider = userManagerLogin.UserCertificateProvider;
            if (userCertificateProvider != null)
            {
                cert = userCertificateProvider.RetrieveCertificateForUser(certName);
            }
            else
            {
                logger.LogError("Certificate provider cannot be null. Please ensure certificate provider for user.");
                throw new UserInputException(UserInputException.ErrorMapping.UserInputExceptionLoginCredential.ToString());
            }

            if (cert == null)
            {
                logger.LogError("Certificate cannot be null. Please ensure certificate for user.");
                missingUserOrCert = true;
            }

            if (missingUserOrCert)
            {
                throw new UserInputException(UserInputException.ErrorMapping.UserInputExceptionLoginCredential.ToString());
            }

            if (Page == null)
            {
                Page = context.Pages.First();
            }

            await HandleUserEmailScreen(EmailSelector, user);

            var endpoint = new Uri(Page.Url).Host;
            var ep = string.IsNullOrEmpty(endpoint) ? "login.microsoftonline.com" : endpoint;
            var interceptUri = await GetCertAuthGlob(ep);

            // start listener to intercept certificate auth call
            await InterceptRestApiCallsAsync(Page, interceptUri, cert, logger);

            await Page.ClickAsync(SubmitButtonSelector);


            // Handle pre-authentication dialogs
            var workOrSchoolAccount = Page.GetByRole(AriaRole.Button, new() { Name = "Work or school account" });
            var useCertificateAuth = Page.GetByRole(AriaRole.Link, new() { Name = "Use a certificate or smart card" }).Or(Page.GetByRole(AriaRole.Button, new() { Name = "certificate" }));
            await Task.WhenAny(workOrSchoolAccount.Or(useCertificateAuth).IsVisibleAsync(), responseReceived.Task);

            if (responseReceived.Task.IsCompletedSuccessfully)
            {
                await ClickStaySignedIn(desiredUrl, logger);
                return;
            }

            if (await workOrSchoolAccount.IsVisibleAsync())
            {
                await workOrSchoolAccount.ClickAsync();
            }
            await Task.WhenAny(useCertificateAuth.WaitForAsync(), responseReceived.Task);


            if (responseReceived.Task.IsCompletedSuccessfully)
            {
                await ClickStaySignedIn(desiredUrl, logger);
                return;
            }

            if (await useCertificateAuth.IsVisibleAsync())
            {
                await useCertificateAuth.ClickAsync();
            }

            // Wait for certificate authentication response
            await responseReceived.Task;
            await ClickStaySignedIn(desiredUrl, logger);
        }

        public async Task ClickStaySignedIn(string desiredUrl, ILogger logger)
        {
            PageWaitForSelectorOptions selectorOptions = new PageWaitForSelectorOptions();
            selectorOptions.Timeout = 8000;

            // For instances where there is a 'Stay signed in?' dialogue box
            try
            {
                logger.LogDebug("Checking if asked to stay signed in.");

                // Check if we received a 'Stay signed in?' box?
                await Page.WaitForSelectorAsync(StaySignedInSelector, selectorOptions);
                logger.LogDebug("Was asked to 'stay signed in'.");

                // Click to stay signed in
                await Page.ClickAsync(KeepMeSignedInNoSelector);
            }
            // If there is no 'Stay signed in?' box, an exception will throw; just catch and continue
            catch (Exception ssiException)
            {
                logger.LogDebug("Exception encountered: " + ssiException.ToString());

                // Keep record if certificateError was encountered
                bool hasCertficateError = false;

                try
                {
                    selectorOptions.Timeout = 2000;

                    // Check if we received a password error
                    await Page.WaitForSelectorAsync("[id=\"CertificateAuthErrorHeading\"]", selectorOptions);
                    hasCertficateError = true;
                }
                catch (Exception peException)
                {
                    logger.LogDebug("Exception encountered: " + peException.ToString());
                }

                // If encountered password error, exit program
                if (hasCertficateError)
                {
                    logger.LogError("Incorrect certificate entered. Make sure you are using the correct credentials.");
                    throw new UserInputException(UserInputException.ErrorMapping.UserInputExceptionLoginCredential.ToString());
                }
                // If not, continue
                else
                {
                    logger.LogDebug("Did not encounter an invalid certificate error.");
                }

                logger.LogDebug("Was not asked to 'stay signed in'.");
            }

            await Page.WaitForURLAsync(desiredUrl);
        }

        public async Task HandleUserEmailScreen(string selector, string value)
        {
            ValidatePage();
            await Page.Locator(selector).WaitForAsync();
            await Page.Locator(selector).PressSequentiallyAsync(value, new LocatorPressSequentiallyOptions { Delay = 50 });
            await Page.Keyboard.PressAsync("Tab", new KeyboardPressOptions { Delay = 20 });
        }

        public async Task<string> GetCertAuthGlob(string endpoint)
        {
            return $"https://*certauth.{endpoint}/**";
        }

        public async Task InterceptRestApiCallsAsync(IPage page, string endpoint, X509Certificate2 cert, ILogger logger)
        {
            // Define the route to intercept
            await page.RouteAsync(endpoint, async route =>
            {
                await HandleRequest(route, cert, logger);
            });
        }

        public async Task HandleRequest(IRoute route, X509Certificate2 cert, ILogger logger)
        {
            var request = route.Request;

            Console.WriteLine($"Intercepted request: {request.Method} {request.Url}");
            if (request.Method == "POST")
            {
                try
                {
                    var response = await DoCertAuthPostAsync(request, cert, logger);

                    // Convert HttpResponseMessage to Playwright response
                    var headers = new Dictionary<string, string>();
                    foreach (var header in response.Headers)
                    {
                        headers[header.Key] = string.Join(",", header.Value);
                    }

                    await route.FulfillAsync(new RouteFulfillOptions
                    {
                        ContentType = "text/html",
                        Status = (int)response.StatusCode,
                        Headers = headers,
                        Body = await response.Content.ReadAsStringAsync()
                    });
                    responseReceived.SetResult(true);
                }
                catch (Exception ex)
                {
                    logger.LogError($"Failed to handle request: {ex.Message}");
                    await route.AbortAsync("failed");
                }
            }
            else
            {
                await route.ContinueAsync();
            }
        }

        public async Task<HttpResponseMessage> DoCertAuthPostAsync(IRequest request, X509Certificate2 cert, ILogger logger)
        {
            using (var handler = GetHttpClientHandler())
            {
                handler.ClientCertificates.Add(cert);
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;

                using (var httpClient = GetHttpClient(handler))
                {
                    // Prepare the request
                    var httpRequest = new HttpRequestMessage(HttpMethod.Post, request.Url);
                    var content = new StringContent(request.PostData);
                    foreach (var header in request.Headers)
                    {
                        httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
                        content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                    httpRequest.Content = content;

                    try
                    {
                        // Send the request
                        var response = await httpClient.SendAsync(httpRequest);

                        if (!response.IsSuccessStatusCode)
                        {
                            throw new HttpRequestException($"Cert auth request failed: {response.StatusCode} {response.ReasonPhrase}");
                        }

                        return response;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"Failed to send cert auth request: {ex.Message}");
                        throw;
                    }
                }
            }
        }

        private void ValidatePage()
        {
            if (Page == null)
            {
                Page = Context.Pages.First();
            }
        }
    }
}
