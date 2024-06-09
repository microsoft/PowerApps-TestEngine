// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.ComponentModel.Composition;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.Users;
using System.Security.Cryptography.X509Certificates;

namespace testengine.user.environment
{
    [Export(typeof(IUserManager))]
    public class CertificateUserManagerModule : IUserManager
    {
        public string Name { get { return "certificate"; } }

        public int Priority { get { return 200; } }

        public bool UseStaticContext { get { return false; } }

        public string Location { get; set; } = "BrowserContext";

        private IBrowserContext? Context { get; set; }

        public IPage? Page { get; set; }

        public static string EmailSelector = "input[type=\"email\"]";
        public static string SubmitButtonSelector = "input[type=\"submit\"]";
        public static string StaySignedInSelector = "[id=\"KmsiCheckboxField\"]";

        private TaskCompletionSource<bool> responseReceived = new TaskCompletionSource<bool>();

        public async Task LoginAsUserAsync(
            string desiredUrl,
            IBrowserContext context,
            ITestState testState,
            ISingleTestInstanceState singleTestInstanceState,
            IEnvironmentVariable environmentVariable)
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

            if (string.IsNullOrEmpty(userConfig.CertificateNameKey))
            {
                logger.LogError("Certificate name key for persona cannot be empty");
                throw new InvalidOperationException();
            }

            var user = environmentVariable.GetVariable(userConfig.EmailKey);
            var cert = environmentVariable.GetVariable(userConfig.CertificateNameKey);

            bool missingUserOrCert = false;

            if (string.IsNullOrEmpty(user))
            {
                logger.LogError(("User email cannot be null. Please check if the environment variable is set properly."));
                missingUserOrCert = true;
            }

            if (string.IsNullOrEmpty(cert))
            {
                logger.LogError("Certificate cannot be null. Please check if the environment variable is set properly.");
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

            //await InterceptRestApiCallsAsync1(Page, "**microsoft**");

            //wait for response of the intercepted request
            //Task<IResponse> certAuthTask = WaitForCertAuthResponse(Page, interceptUri);


            // start listener to intercept certificate auth call
            await InterceptRestApiCallsAsync(Page, interceptUri);

            await Page.ClickAsync(SubmitButtonSelector);

            await Page.PauseAsync();
            //await Page.WaitForResponseAsync(Page.Url);
            var l = await responseReceived.Task;
            // Wait for the sliding animation to finish
            //await Task.Delay(1000);
            //PageWaitForSelectorOptions selectorOptions = new PageWaitForSelectorOptions();
            //selectorOptions.Timeout = 8000;
            //await Page.WaitForSelectorAsync(StaySignedInSelector, selectorOptions);
            //logger.LogDebug("Was asked to 'stay signed in'.");
            ////certificate logic
            //var endpoint = new Uri(Page.Url).Host;

            //var useCertificateAuth = Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions() { Name = "certificate" }).Or(Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions() { Name = "certificate" }));
            //await useCertificateAuth.WaitForAsync();
            //var ok = Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions() { Name = "ok" });
            //await ok.ClickAsync();
            //await Task.Delay(5000);

        }

        public async Task HandleUserEmailScreen(string selector, string value)
        {
            ValidatePage();
            await Page.TypeAsync(selector, value, new PageTypeOptions { Delay = 50 });
            await Page.Keyboard.PressAsync("Tab", new KeyboardPressOptions { Delay = 20 });
        }

        private void ValidatePage()
        {
            if (Page == null)
            {
                Page = Context.Pages.First();
            }
        }

        public async Task InterceptRestApiCallsAsync(IPage page, string endpoint)
        {
            // Define the route to intercept
            await page.RouteAsync(endpoint, async route =>
            {
                await HandleRequest(route);
            });
        }

        public Task<IResponse> WaitForCertAuthResponse(IPage page, string endpoint)
        {
            return page.WaitForResponseAsync(endpoint);
        }

        internal async Task<string> GetCertAuthGlob(string endpoint)
        {
            return $"https://*certauth.{endpoint}/**";
        }

        private async Task HandleRequest(IRoute route)
        {
            var request = route.Request;

            Console.WriteLine($"Intercepted request: {request.Method} {request.Url}");
            if (request.Method == "POST")
            {
                var options = new AgentOptions(File.ReadAllBytes(@"C:\Users\snamilikonda\Downloads\aurorauser04.Pfx"), "");
                try
                {
                    var _certAuth = new CertificateAuthentication();
                    var response = await _certAuth.DoCertAuthPostAsync(request, options);

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

        public class AgentOptions
        {
            public byte[] Pfx { get; set; }
            public string Passphrase { get; set; }

            public AgentOptions(byte[] pfx, string passphrase)
            {
                Pfx = pfx ?? throw new ArgumentNullException(nameof(pfx));
                Passphrase = passphrase ?? throw new ArgumentNullException(nameof(passphrase));
            }
        }


        public class CertificateAuthentication
        {
            public async Task<HttpResponseMessage> DoCertAuthPostAsync(IRequest request, AgentOptions options)
            {
                using (var handler = new HttpClientHandler())
                {
                    // Load the certificate from the provided PFX and passphrase
                    var certificate = new X509Certificate2(options.Pfx, options.Passphrase);
                    handler.ClientCertificates.Add(certificate);
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
        }


        public async Task InterceptRestApiCallsAsync1(IPage page, string endpoint)
        {
            // Define the route to intercept
            await page.RouteAsync(endpoint, async route =>
            {
                await HandleRequest1(route);
            });
        }

        private async Task HandleRequest1(IRoute route)
        {
            var request = route.Request;

            Console.WriteLine($"Intercepted request: {request.Method} {request.Url}");
            await route.ContinueAsync();
        }
    }
}

        /*internal async Task AddCertAuthRoute(IPage page, ICertificateAuthOptions options)
        {
            const string AUTH_ENDPOINT = "login.microsoftonline.com";
            var endpoint = options.authEndpoint ?? AUTH_ENDPOINT;
            string uri = await GetCertAuthGlob(endpoint);
            var pfx = options.pfx;
            var passphrase = options.passphrase;
            //await page.RouteAsync(uri, CertAuthHandler(new CertAuthHandlerOptions { Pfx = pfx, Passphrase = passphrase }));
            await page.RouteAsync(uri, handler => { }); 
        }

        internal async Task<string> GetCertAuthGlob(string endpoint)
        {
            return $"https://*certauth.${endpoint}/**";
        }

        // See https://learn.microsoft.com/en-us/entra/identity/authentication/concept-certificate-based-authentication-technical-deep-dive
        internal async  certAuthHandler(options: AgentOptions)
        {
            return async(route: Route, request: Request) => {
                try
                {
                    const resp = await doCertAuthPost(request, options);
                    await route.fulfill(resp);
                }
                catch (e)
                {
                    console.error(`Failed to send cert auth request: ${ (e as Error).message}`);
                    await route.abort('failed');
                }
            };
        }
    }
}




//public interface User
//{
//    // Alias used to reference the user
//    string Alias { get; set; }

//    // Username of the user
//    string Username { get; set; }

//    // The certificate secret in a key vault
//    string Certificate { get; set; }
//}

public class Authentication
{
    private ITest test;
    private IRoute route;
    private IKeyVault keyVault;

    public Authentication(ITest test, IRoute route, IKeyVault keyVault)
    {
        this.test = test;
        this.route = route;
        this.keyVault = keyVault;
    }

    public async Task AuthenticateUser(IPage page, User user)
    {
        // Retrieve the certificate from Key Vault
        var certificate = await test.Step("Retrieve certificate from Key Vault", async () =>
        {
            return await keyVault.RetrieveCertificatePfx(user.Certificate);
        });

        // Get information about the sign-in page
        await Expect(page, "Expected Entra sign-in page").ToHaveURL(new Regex("https://login.(?:partner.)?microsoftonline.(?:com|us|cn)"));

        var endpoint = new Uri(page.Url).Host;

        // Add the certificate endpoint handler
        await route.AddCertAuthRoute(page, new CertificateOptions { Pfx = certificate, AuthEndpoint = endpoint });

        // Watch for navigation away from cert auth endpoint
        bool succeeded = false;
        var redirectPromise = page.WaitForURL(url => !new Uri(url).Host.EndsWith(endpoint))
            .ContinueWith(_ => succeeded = true);

        // Watch for the certificate auth request
        bool certAuthOccurred = false;
        var certAuthPromise = route.WaitForCertAuthResponse(page, endpoint)
            .ContinueWith(_ => certAuthOccurred = true);

        // Enter the username
        await test.Step("Enter username", async () =>
        {
            await page.GetByRole("textbox", new RoleOptions { Name = "email" }).FillAsync(user.Username);
            await page.GetByRole("button", new RoleOptions { Name = "next" }).ClickAsync();
        });

        // Handle pre-authentication dialogs
        await test.Step("Handle pre-authentication dialogs", async () =>
        {
            var workOrSchoolAccount = page.GetByRole("button", new RoleOptions { Name = "Work or school account" });

            var useCertificateAuth = page
                .GetByRole("link", new RoleOptions { Name = "certificate" })
                .Or(page.GetByRole("button", new RoleOptions { Name = "certificate" }));

            await Task.WhenAny(workOrSchoolAccount.Or(useCertificateAuth).WaitForAsync(), certAuthPromise);
            if (certAuthOccurred) return;

            if (await workOrSchoolAccount.IsVisibleAsync())
            {
                await workOrSchoolAccount.ClickAsync();
                await Task.WhenAny(useCertificateAuth.WaitForAsync(), certAuthPromise);
            }

            if (certAuthOccurred) return;

            await useCertificateAuth.ClickAsync();
        });

        await test.Step("Wait for certificate authentication response", async () =>
        {
            await certAuthPromise;
        });

        // Handle post-authentication dialogs
        await test.Step("Handle post-authentication dialogs", async () =>
        {
        var staySignedIn = page.GetByRole("heading", new RoleOptions { Name = "Stay signed in?" });
        var authFailure = page.GetByRole("heading", new RoleOptions { Name = "Certificate validation failed" });

        await Task.WhenAny(staySignedIn.Or(authFailure).WaitForAsync(), redirectPromise);
        if (succeeded) return;

        if (await authFailure.IsVisibleAsync())
        {
            await page.GetByRole("button", new RoleOptions { Name = "More details" }).ClickAsync();
            await page.Context.GrantPermissionsAsync(new[] { "clipboard-read" });
            await page.GetByRole("button", new RoleOptions { Name = "Copy" }).ClickAsync();
            var error = await page.EvaluateAsync<string>("() => navigator.clipboard.readText()");
            throw new Exception($"Certificate validation failed. Please check Entra sign-in logs for more information.\n{error}");
        }*/



