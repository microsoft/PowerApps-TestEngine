
// Copyright(c) Microsoft Corporation.
// Licensed under the MIT license.using Microsoft.Extensions.Log

using System.Dynamic;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

namespace Microsoft.PowerApps.TestEngine.Helpers
{
    public class DataverseAIPredictHelper
    {
        private readonly HttpClient _httpClient;
        private readonly Uri _dataverseUrl;
        private readonly string _accessToken;

        /// <summary>
        /// Start an instance of the prediction helper assuming OAuth token to dataverse endpoint
        /// </summary>
        /// <param name="dataverseUrl">The dataverse instance that contains the model to execute</param>
        /// <param name="accessToken">The current access token to make a request to AI Builder model</param>
        public DataverseAIPredictHelper(Uri dataverseUrl, string accessToken)
        {
            _dataverseUrl = dataverseUrl;
            _accessToken = accessToken;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        }

        /// <summary>
        /// Make a Dataverse WebApi Call to predict AI Builder outcome with the provided model parameters
        /// </summary>
        /// <param name="entityId">The id of the msdyn_model to execute</param>
        /// <param name="parameters">The dynamic list of paramaters to pass to the model</param>
        /// <returns>The name value pairs that represent the model execution results</returns>
        public async Task<Dictionary<string, object>> ExecuteRequestAsync(Guid entityId, ExpandoObject parameters)
        {
            var requestUrl = new Uri(_dataverseUrl, new Uri($"/api/data/v9.0/msdyn_aimodels({entityId})/Microsoft.Dynamics.CRM.msdyn_PrettyPredict", UriKind.Relative));

            var jsonContent = JsonConvert.SerializeObject(
                new
                {
                    version = "2.0",
                    simplifiedResponse = true,
                    source = "{ \"consumptionSource\": \"PowerApps\", \"partnerSource\": \"AIBuilder\", \"consumptionSourceVersion\": \"PowerFx\" }",
                    request = parameters
                });
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(requestUrl, content);
            response.EnsureSuccessStatusCode();

            var responseData = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<Dictionary<string, object>>(responseData);
        }
    }
}
