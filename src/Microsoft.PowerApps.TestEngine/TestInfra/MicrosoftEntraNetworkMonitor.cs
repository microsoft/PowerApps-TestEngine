// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.System;

namespace Microsoft.PowerApps.TestEngine.TestInfra
{
    /// <summary>
    /// Infrastructure monitoring class that can be applied to help diagnose login issues by monitoring request response from Microsoft Entra
    /// </summary>
    public class MicrosoftEntraNetworkMonitor
    {
        private readonly ILogger _logger;
        private readonly IBrowserContext _browserContext;
        private readonly ITestState _testState;
        private readonly UriRedactionFormatter _uriRedactionFormatter;

        private readonly string[] _loginServices = new[]
           {
                "login.microsoftonline.com",
                "login.microsoftonline.us",
                "login.chinacloudapi.cn",
                "login.microsoftonline.de"
            };

        public MicrosoftEntraNetworkMonitor(ILogger logger, IBrowserContext browserContext, ITestState testState)
        {
            _logger = logger;
            _browserContext = browserContext;
            _uriRedactionFormatter = new UriRedactionFormatter(logger);
            _testState = testState;
        }

        public async Task MonitorEntraLoginAsync(string desiredUrl)
        {
            var hostName = new Uri(desiredUrl).Host;
            await _browserContext.RouteAsync($"https://{hostName}/**", async route =>
            {
                var request = route.Request;
                var routeUri = new Uri(request.Url);
                _logger.LogDebug("Start request: {Method} {Url}", route.Request.Method, _uriRedactionFormatter.ToString(routeUri));

                await route.ContinueAsync();
            });

            foreach (var service in _loginServices)
            {
                // Listen for all requests made
                await _browserContext.RouteAsync($"https://{service}/**", async route =>
                {
                    var request = route.Request;
                    var routeUri = new Uri(request.Url);
                    if (!_loginServices.Contains(routeUri.Host))
                    {
                        await route.ContinueAsync();
                    }

                    _logger.LogDebug("Start request: {Method} {Url}", request.Method, _uriRedactionFormatter.ToString(routeUri));

                    await route.ContinueAsync();
                });
            }

            // Listen for requests to be finished
            _browserContext.RequestFinished += async (s, e) => await _browserContext_RequestFinished(s, e, desiredUrl);
        }

        public async Task LogCookies(string desiredUrl)
        {

            var hostName = "";
            if (!string.IsNullOrEmpty(desiredUrl))
            {
                hostName = new Uri(desiredUrl).Host;
            }
            else
            {
                var domain = _testState.GetDomain();
                if (!string.IsNullOrEmpty(domain) && Uri.TryCreate(domain, UriKind.Absolute, out Uri match))
                {
                    hostName = match.Host;
                }
            }

            var cookies = await _browserContext.CookiesAsync();
            if (cookies != null)
            {
                // Get any cookies for Entra related sites or the desired url
                foreach (var cookie in cookies
                    .Where(c => _loginServices.Any(service => c.Name.Contains(service)) || c.Name.Contains(hostName))
                    .OrderBy(c => c.Domain)
                    .ThenBy(c => c.Name))
                {
                    var expires = DateTimeOffset.FromUnixTimeSeconds((long)cookie.Expires);
                    _logger.LogDebug($"Domain {cookie.Domain}, Cookie: {cookie.Name}, Secure {cookie.Secure}, Expires {expires}");
                }
            }
        }

        private async Task _browserContext_RequestFinished(object sender, IRequest e, string requestUrl)
        {
            var requestHost = new Uri(e.Url).Host;
            var requestHash = CreateSHA256(e.Url);
            // Only listen for login services
            if (_loginServices.Any(service => requestHost.Contains(service)) || new Uri(requestUrl).Host == requestHost)
            {
                var response = await e.ResponseAsync();
                _logger.LogDebug($"Login request [{requestHash}]: {e.Method} {_uriRedactionFormatter.ToString(new Uri(e.Url))}");
                _logger.LogDebug($"Login response status [{requestHash}]: {response.Status} ({response.StatusText})");

                switch (response.Status)
                {
                    case 302: // Redirect
                        foreach (var header in response.Headers)
                        {
                            _logger.LogTrace($"Cookie [{requestHash}] {header.Key} = {header.Value}");
                        }
                        break;
                }

                if (e.RedirectedFrom != null)
                {
                    _logger.LogDebug($"Login redirect from [{requestHash}]: {e.RedirectedFrom.Method} {_uriRedactionFormatter.ToString(new Uri(e.RedirectedFrom.Url))}");
                }

                if (e.RedirectedTo != null)
                {
                    _logger.LogDebug($"Login redirect to [{requestHash}]: {e.RedirectedTo.Method} {_uriRedactionFormatter.ToString(new Uri(e.RedirectedTo.Url))}");
                }

                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    await LogCookies(String.Empty);
                }
            }
        }

        public static string CreateSHA256(string input)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] inputBytes = Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = sha256.ComputeHash(inputBytes);

                // Convert the byte array to a hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }
    }
}
