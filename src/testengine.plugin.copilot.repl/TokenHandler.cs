// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;

namespace TestEngine.Samples.Copilot;

/// <summary>
/// This sample uses an MSAL to generate an authentication token to the request.
/// </summary>
/// <param name="settings">Direct To engine connection settings.</param>
public class TokenHandler 
{
    private static readonly string _keyChainServiceName = "copilot_studio_client_app";
    private static readonly string _keyChainAccountName = "copilot_studio_client";

    private SampleConnectionSettings _settings;

    public TokenHandler(SampleConnectionSettings settings)
    {
        _settings = settings;
    }

    public async Task<AuthenticationResult> AuthenticateAsync(CancellationToken ct = default!)
    {
        ArgumentNullException.ThrowIfNull(_settings);

        string[] scopes = new string[] { "https://api.powerplatform.com/.default" };
        //string[] scopes = ["https://api.gov.powerplatform.microsoft.us/CopilotStudio.Copilots.Invoke"];

        IPublicClientApplication app = PublicClientApplicationBuilder.Create(_settings.AppClientId)
                .WithAuthority(AadAuthorityAudience.AzureAdMyOrg)
                .WithTenantId(_settings.TenantId)
                .WithRedirectUri("http://localhost")
                .Build();

        string currentDir = Path.Combine(AppContext.BaseDirectory, "mcs_client_console");

        if (!Directory.Exists(currentDir))
        {
            Directory.CreateDirectory(currentDir);
        }

        StorageCreationPropertiesBuilder storageProperties = new("TokenCache", currentDir);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            storageProperties.WithLinuxUnprotectedFile();
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            storageProperties.WithMacKeyChain(_keyChainServiceName, _keyChainAccountName);
        }
        MsalCacheHelper tokenCacheHelper = await MsalCacheHelper.CreateAsync(storageProperties.Build());
        tokenCacheHelper.RegisterCache(app.UserTokenCache);

        var account = (await app.GetAccountsAsync()).FirstOrDefault();

        AuthenticationResult authResponse;
        try
        {
            authResponse = await app.AcquireTokenSilent(scopes, account).ExecuteAsync(ct);
        }
        catch (MsalUiRequiredException)
        {
            authResponse = await app.AcquireTokenInteractive(scopes).ExecuteAsync(ct);
        }
        return authResponse;
    }
}
