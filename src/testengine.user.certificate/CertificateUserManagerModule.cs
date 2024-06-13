// Copyright(c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Modules;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerApps.TestEngine.Users;
using Microsoft.PowerFx;

namespace testengine.user.environment
{
    [Export(typeof(IUserManager))]
    public class CertificateUserManagerModule : IUserManager
    {
        public string Name { get { return "certificate"; } }

        public int Priority { get { return 50; } }

        public bool UseStaticContext { get { return false; } }

        public string Location { get; set; } = string.Empty;

        public IPage? Page { get; set; }

        private IBrowserContext? Context { get; set; }

        public static string EmailSelector = "input[type=\"email\"]";
        public static string SubmitButtonSelector = "input[type=\"submit\"]";
        public static string StaySignedInSelector = "[id=\"KmsiCheckboxField\"]";

        private TaskCompletionSource<bool> responseReceived = new TaskCompletionSource<bool>();

        public async Task LoginAsUserAsync(
            string desiredUrl,
            IBrowserContext context,
            ITestState testState,
            ISingleTestInstanceState singleTestInstanceState,
            IEnvironmentVariable environmentVariable,
            IUserCertificateProvider userCertificateProvider)
        {
            Context = context;

            var testSuiteDefinition = singleTestInstanceState.GetTestSuiteDefinition();
            var logger = singleTestInstanceState.GetLogger();

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

            var user = environmentVariable.GetVariable(userConfig.EmailKey);
            bool missingUserOrCert = false;
            if (string.IsNullOrEmpty(user))
            {
                logger.LogError(("User email cannot be null. Please check if the environment variable is set properly."));
                missingUserOrCert = true;
            }
            var cert = userCertificateProvider.RetrieveCertificateForUser(user);
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
            await InterceptRestApiCallsAsync(Page, interceptUri, cert);

            await Page.ClickAsync(SubmitButtonSelector);


            // Handle pre-authentication dialogs
            var workOrSchoolAccount = Page.GetByRole(AriaRole.Button, new() { Name = "Work or school account" });
            var useCertificateAuth = Page.GetByRole(AriaRole.Link, new() { Name = "Use a certificate or smart card" }).Or(Page.GetByRole(AriaRole.Button, new() { Name = "certificate" })); ;
            await Task.WhenAny(workOrSchoolAccount.Or(useCertificateAuth).IsVisibleAsync(), responseReceived.Task);

            if (responseReceived.Task.IsCompletedSuccessfully) return;

            if (await workOrSchoolAccount.IsVisibleAsync())
            {
                await workOrSchoolAccount.ClickAsync();
                await Task.WhenAny(useCertificateAuth.WaitForAsync(), responseReceived.Task);
            }

            if (responseReceived.Task.IsCompletedSuccessfully) return;

            await useCertificateAuth.ClickAsync();

            // Wait for certificate authentication response
            await responseReceived.Task;

            await Page.PauseAsync();
            await Task.Delay(500);

        }

        public async Task HandleUserEmailScreen(string selector, string value)
        {
            ValidatePage();
            await Page.Locator(selector).WaitForAsync();
            await Page.TypeAsync(selector, value, new PageTypeOptions { Delay = 50 });
            await Page.Keyboard.PressAsync("Tab", new KeyboardPressOptions { Delay = 20 });
        }

        internal async Task<string> GetCertAuthGlob(string endpoint)
        {
            return $"https://*certauth.{endpoint}/**";
        }
        public async Task InterceptRestApiCallsAsync(IPage page, string endpoint, X509Certificate2 cert)
        {
            // Define the route to intercept
            await page.RouteAsync(endpoint, async route =>
            {
                await HandleRequest(route, cert);
            });
        }

        private async Task HandleRequest(IRoute route, X509Certificate2 cert)
        {
            var request = route.Request;

            Console.WriteLine($"Intercepted request: {request.Method} {request.Url}");
            if (request.Method == "POST")
            {
                try
                {
                    var response = await DoCertAuthPostAsync(request, cert);

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
                    Console.Error.WriteLine($"Failed to handle request: {ex.Message}");
                    await route.AbortAsync("failed");
                }
            }
            else
            {
                await route.ContinueAsync();
            }
        }

        public async Task<HttpResponseMessage> DoCertAuthPostAsync(IRequest request, X509Certificate2 cert)
        {
            using (var handler = new HttpClientHandler())
            {
                handler.ClientCertificates.Add(cert);
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;

                using (var httpClient = new HttpClient(handler))
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
                        Console.Error.WriteLine($"Failed to send cert auth request: {ex.Message}");
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
