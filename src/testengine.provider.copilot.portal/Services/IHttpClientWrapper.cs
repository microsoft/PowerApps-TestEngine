// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerApps.TestEngine.Providers.Services
{
    public interface IHttpClientWrapper
    {
        Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content);
        Task<HttpResponseMessage> GetAsync(string requestUri);
        void SetAuthorizationHeader(string scheme, string parameter);

    }
}
