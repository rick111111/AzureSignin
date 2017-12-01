using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace AzureSignin
{
    internal class HttpClientSample
    {
        private string _azureAccessToken;

        const string ResourceGroupUrl = "https://management.azure.com/subscriptions/{0}/resourcegroups/{1}?api-version={2}";
        const string ServicePlanUrl = "https://management.azure.com/subscriptions/{0}/resourceGroups/{1}/providers/Microsoft.Web/serverfarms/{2}/sites?api-version={3}";
        const string ApiVersion = "2016-09-01";

        private async Task<string> GetAccessToken()
        {
            return await AccessWithKeyVaultCertificate.Access();
        }

        private async Task<T> ExecuteWithRetryTokenRefresh<T>(Func<string, Task<T>> func)
        {
            try
            {
                if (string.IsNullOrEmpty(_azureAccessToken))
                {
                    _azureAccessToken = await GetAccessToken();
                }

                return await func(_azureAccessToken);
            }
            catch (WebException e)
            {
                HttpStatusCode? code = (e.Response as HttpWebResponse)?.StatusCode;
                if (code.HasValue && code.Value.Equals(HttpStatusCode.Unauthorized))
                {
                    _azureAccessToken = await GetAccessToken();
                    return await func(_azureAccessToken);
                }
                else
                {
                    throw;
                }
            }
        }

        public async Task<bool> IsResourceGroupExist(string subscriptionId, string resourceGroupName)
        {
            string token = await AccessWithLocalCertificate.Access();
            string url = string.Format(ResourceGroupUrl, subscriptionId, resourceGroupName, ApiVersion);

            return await ExecuteWithRetryTokenRefresh(async (azureAccessToken) => {
                try
                {
                    string response = await AzureRequest.GetResponse(url, token, HttpMethods.GET);
                    return true;
                }
                catch (WebException e)
                {
                    return false;
                }
            });
        }
    }
}
