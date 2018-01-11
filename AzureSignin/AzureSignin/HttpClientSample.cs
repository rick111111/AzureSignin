using Newtonsoft.Json;
using System;
using System.IO;
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

        const string ManagementEndpoint = "https://management.azure.com";
        const string ResourceGroupId = "/subscriptions/{0}/resourcegroups/{1}";
        const string ResourceGroupUri = "/subscriptions/{0}/resourcegroups/{1}?api-version={2}";
        const string ServicePlanUri = "/subscriptions/{0}/resourceGroups/{1}/providers/Microsoft.Web/serverfarms/{2}/sites?api-version={3}";
        const string WebAppDeploymentUri = "/subscriptions/{0}/resourcegroups/{1}/providers/Microsoft.Resources/deployments/template?api-version={2}";
        const string WebAppDeploymentValidationUri = "/subscriptions/{0}/resourcegroups/{1}/providers/Microsoft.Resources/deployments/template/validate?api-version={2}";
        const string ApiVersion = "2016-09-01";
        const string DeploymentLocation = "westus2";

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

        private string GetUri(string UriTemplate, params string[] args)
        {
            string uri = string.Format(UriTemplate, args);
            return ManagementEndpoint + uri;
        }

        public async Task<bool> IsResourceGroupExist(string subscriptionId, string resourceGroupName)
        {
            string url = GetUri(ResourceGroupUri, subscriptionId, resourceGroupName, ApiVersion);

            return await ExecuteWithRetryTokenRefresh(async (azureAccessToken) => {
                try
                {
                    string response = await AzureRequest.GetResponse(url, azureAccessToken, HttpMethods.HEAD);
                    return true;
                }
                catch (WebException e)
                {
                    return false;
                }
            });
        }

        public async Task<bool> CreateResourceGroup(string subscriptionId, string resourceGroupName)
        {
            string url = GetUri(ResourceGroupUri, subscriptionId, resourceGroupName, ApiVersion);

            ResourceGroupObject rgo = new ResourceGroupObject()
            {
                id = string.Format(ResourceGroupId, subscriptionId, resourceGroupName),
                location = DeploymentLocation,
                name = resourceGroupName
            };
            string body = JsonConvert.SerializeObject(rgo);

            return await ExecuteWithRetryTokenRefresh(async (azureAccessToken) => {
                try
                {
                    string response = await AzureRequest.GetResponse(url, azureAccessToken, HttpMethods.PUT, null, body);
                    return true;
                }
                catch (WebException e)
                {
                    return false;
                }
            });
        }

        public async Task<bool> ValidateResourceTemplate(DeploymentParameterObject parameterObject)
        {
            string url = GetUri(WebAppDeploymentValidationUri, parameterObject.subscriptionId.value, parameterObject.serverFarmResourceGroup.value, ApiVersion);

            string body = GenerateDeploymentTemplate(parameterObject);

            return await ExecuteWithRetryTokenRefresh(async (azureAccessToken) => {
                try
                {
                    string response = await AzureRequest.GetResponse(url, azureAccessToken, HttpMethods.POST, null, body);
                    return true;
                }
                catch (WebException e)
                {
                    return false;
                }
            });
        }

        public async Task<bool> DeployResourceTemplate(DeploymentParameterObject parameterObject)
        {
            string url = GetUri(WebAppDeploymentUri, parameterObject.subscriptionId.value, parameterObject.serverFarmResourceGroup.value, ApiVersion);

            string body = GenerateDeploymentTemplate(parameterObject);

            return await ExecuteWithRetryTokenRefresh(async (azureAccessToken) => {
                try
                {
                    string response = await AzureRequest.GetResponse(url, azureAccessToken, HttpMethods.PUT, null, body);
                    return true;
                }
                catch (WebException e)
                {
                    return false;
                }
            });
        }

        private string GenerateDeploymentTemplate(DeploymentParameterObject parameterObject)
        {
            DeploymentTemplateObject dto = new DeploymentTemplateObject()
            {
                properties = new DeploymentRootProperties()
                {
                    template = JsonConvert.DeserializeObject(DeploymentRootProperties.TemplateJsonString),
                    parameters = parameterObject
                }
            };

            return JsonConvert.SerializeObject(dto);
        }
    }
}
