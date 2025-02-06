using System.Net.Http.Headers;

namespace testengine.service.copilot
{
    internal class AddTokenHandler : DelegatingHandler(new HttpClientHandler())
    {
        string _token = string.Empty;
        AddTokenHandler(string token)
        {
            _token = token;
        }

        /// <summary>
        /// Handles sending the request and adding the token to the request.
        /// </summary>
        /// <param name="request">Request to be sent</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Headers.Authorization is null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
            }
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
