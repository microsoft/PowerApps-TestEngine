// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.System;

namespace Microsoft.PowerApps.TestEngine.TestInfra
{
    /// <summary>
    /// Infrastructure monitoring class that can be applied to help diagnose login issues
    /// </summary>
    public class EntraLoginMonitor
    {
        private readonly ILogger _logger;
        private readonly IBrowserContext _browserContext;
        private readonly UriRedactionFormatter _uriRedactionFormatter;

        private readonly string[] _loginServices = new[]
           {
                "login.microsoftonline.com",
                "login.microsoftonline.us",
                "login.chinacloudapi.cn",
                "login.microsoftonline.de"
            };

        public EntraLoginMonitor(ILogger logger, IBrowserContext browserContext)
        {
            _logger = logger;
            _browserContext = browserContext;
            _uriRedactionFormatter = new UriRedactionFormatter(logger);
        }

        public async Task MonitorEntraLoginAsync(string desiredUrl)
        {
            var hostName = new Uri(desiredUrl).Host;
            await _browserContext.RouteAsync($"https://{hostName}/**", async route =>
            {
                var request = route.Request;
                var routeUri = new Uri(request.Url);
                _logger.LogDebug("Login request: {Method} {Url}", route.Request.Method, _uriRedactionFormatter.ToString(routeUri));

                await route.ContinueAsync();
            });

            var cookies = await _browserContext.CookiesAsync();
            if (cookies != null)
            {
                foreach (var cookie in cookies)
                {
                    if (cookie.HttpOnly)
                    {
                        var expires = DateTimeOffset.FromUnixTimeSeconds((long)cookie.Expires);
                        _logger.LogDebug($"Cookie: {cookie.Name}, Secure {cookie.Secure}, Expires {expires}");
                    }
                }
            }

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

                    _logger.LogDebug("Login request: {Method} {Url}", request.Method, _uriRedactionFormatter.ToString(routeUri) );
                    
                    await route.ContinueAsync();
                });
            }

            // Listen for requests to be finished
            _browserContext.RequestFinished += async (s, e) => await _browserContext_RequestFinished(s, e, desiredUrl);
        }

        private async Task _browserContext_RequestFinished(object sender, IRequest e, string requestUrl)
        {
            var requestHost = new Uri(e.Url).Host;
            // Only listen for login services
            if (_loginServices.Contains(requestHost) || new Uri(requestUrl).Host == requestHost )
            {
                if ( e.RedirectedFrom != null)
                {
                    _logger.LogDebug("Login redirect from: {Method} {Url}", e.RedirectedFrom.Method, _uriRedactionFormatter.ToString(new Uri(e.RedirectedFrom.Url)));
                }

                if (e.RedirectedTo != null)
                {
                    _logger.LogDebug("Login redirect to: {Method} {Url}", e.RedirectedTo.Method, _uriRedactionFormatter.ToString(new Uri(e.RedirectedTo.Url)));
                }

                var response = await e.ResponseAsync();
                _logger.LogDebug($"Login request : {_uriRedactionFormatter.ToString(new Uri(e.Url))}");
                _logger.LogDebug($"Login response status: {response.Status}");
            }
        }
    }
}
