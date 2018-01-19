using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;

namespace AzureSignin.Communication
{
    internal class HttpClientHelper
    {
        private string _azureAccessToken;

        const string ManagementEndpoint = "https://management.azure.com";
        const string ResourceGroupId = "/subscriptions/{0}/resourcegroups/{1}";
        const string ResourceGroupUri = "/subscriptions/{0}/resourcegroups/{1}?api-version=2016-09-01";

        const string ServicePlanUri = "/subscriptions/{0}/resourceGroups/{1}/providers/Microsoft.Web/serverfarms/{2}?api-version=2016-09-01";
        const string WebAppUri = "/subscriptions/{0}/resourceGroups/{1}/providers/Microsoft.Web/sites/{2}/?api-version=2016-08-01";

        const string WebAppDeploymentUri = "/subscriptions/{0}/resourcegroups/{1}/providers/Microsoft.Resources/deployments/template?api-version=2016-09-01";
        const string WebAppDeploymentValidationUri = "/subscriptions/{0}/resourcegroups/{1}/providers/Microsoft.Resources/deployments/template/validate?api-version=2016-09-01";

        const string WebAppPublishProfile = "/subscriptions/{0}/resourcegroups/{1}/providers/Microsoft.Web/sites/{2}/publishxml?api-version=2016-08-01";
        const string WebAppUrl = "https://{0}.azurewebsites.net";

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

        public async Task<bool> CheckResourceGroupExist(string subscriptionId, string resourceGroupName)
        {
            string url = GetUri(ResourceGroupUri, subscriptionId, resourceGroupName);

            return await ExecuteWithRetryTokenRefresh(async (azureAccessToken) => {
                try
                {
                    string response = await AzureRequest.GetResponse(url, azureAccessToken, HttpMethods.HEAD);
                    return true;
                }
                catch (WebException)
                {
                    return false;
                }
            });
        }

        public async Task<bool> CreateResourceGroup(string subscriptionId, string resourceGroupName, string location)
        {
            string url = GetUri(ResourceGroupUri, subscriptionId, resourceGroupName);

            ResourceGroupObject rgo = new ResourceGroupObject()
            {
                id = string.Format(ResourceGroupId, subscriptionId, resourceGroupName),
                location = location,
                name = resourceGroupName
            };
            string body = JsonConvert.SerializeObject(rgo);

            return await ExecuteWithRetryTokenRefresh(async (azureAccessToken) => {
                try
                {
                    string response = await AzureRequest.GetResponse(url, azureAccessToken, HttpMethods.PUT, null, body);
                    return true;
                }
                catch (WebException)
                {
                    return false;
                }
            });
        }
        
        public async Task<bool> DeleteResourceGroup(string subscriptionId, string resourceGroupName)
        {
            string url = GetUri(ResourceGroupUri, subscriptionId, resourceGroupName);

            return await ExecuteWithRetryTokenRefresh(async (azureAccessToken) => {
                try
                {
                    string response = await AzureRequest.GetResponse(url, azureAccessToken, HttpMethods.DELETE);
                    return true;
                }
                catch (WebException)
                {
                    return false;
                }
            });
        }

        public async Task<bool> CheckServicePlanExist(string subscriptionId, string resourceGroupName, string servicePlanName)
        {
            string url = GetUri(ServicePlanUri, subscriptionId, resourceGroupName, servicePlanName);

            return await ExecuteWithRetryTokenRefresh(async (azureAccessToken) => {
                try
                {
                    string response = await AzureRequest.GetResponse(url, azureAccessToken, HttpMethods.GET);
                    return true;
                }
                catch (WebException)
                {
                    return false;
                }
            });
        }

        public async Task<bool> DeleteServicePlan(string subscriptionId, string resourceGroupName, string servicePlanName)
        {
            string url = GetUri(ServicePlanUri, subscriptionId, resourceGroupName, servicePlanName);

            return await ExecuteWithRetryTokenRefresh(async (azureAccessToken) => {
                try
                {
                    string response = await AzureRequest.GetResponse(url, azureAccessToken, HttpMethods.DELETE);
                    return true;
                }
                catch (WebException)
                {
                    return false;
                }
            });
        }

        public async Task<bool> CheckAppServiceExist(string subscriptionId, string resourceGroupName, string appServiceName)
        {
            string url = GetUri(WebAppUri, subscriptionId, resourceGroupName, appServiceName);

            return await ExecuteWithRetryTokenRefresh(async (azureAccessToken) => {
                try
                {
                    string response = await AzureRequest.GetResponse(url, azureAccessToken, HttpMethods.GET);
                    return true;
                }
                catch (WebException)
                {
                    return false;
                }
            });
        }

        public async Task<bool> DeleteAppService(string subscriptionId, string resourceGroupName, string appServiceName)
        {
            string url = GetUri(WebAppUri, subscriptionId, resourceGroupName, appServiceName);

            return await ExecuteWithRetryTokenRefresh(async (azureAccessToken) => {
                try
                {
                    string response = await AzureRequest.GetResponse(url, azureAccessToken, HttpMethods.DELETE);
                    return true;
                }
                catch (WebException)
                {
                    return false;
                }
            });
        }

        public async Task<bool> ValidateResourceTemplate(DeploymentParameterObject parameterObject)
        {
            string url = GetUri(WebAppDeploymentValidationUri, parameterObject.subscriptionId.value, parameterObject.serverFarmResourceGroup.value);

            string body = GenerateDeploymentTemplate(parameterObject);

            return await ExecuteWithRetryTokenRefresh(async (azureAccessToken) => {
                try
                {
                    string response = await AzureRequest.GetResponse(url, azureAccessToken, HttpMethods.POST, null, body);
                    return true;
                }
                catch (WebException)
                {
                    return false;
                }
            });
        }

        public async Task<bool> DeployResourceTemplate(DeploymentParameterObject parameterObject)
        {
            string url = GetUri(WebAppDeploymentUri, parameterObject.subscriptionId.value, parameterObject.serverFarmResourceGroup.value);

            string body = GenerateDeploymentTemplate(parameterObject);

            return await ExecuteWithRetryTokenRefresh(async (azureAccessToken) => {
                try
                {
                    string response = await AzureRequest.GetResponse(url, azureAccessToken, HttpMethods.PUT, null, body);
                    ProvisioningState state = GetProvisioningState(response);
                    if (state == ProvisioningState.Succeeded)
                    {
                        return true;
                    }

                    if (state == ProvisioningState.Accepted)
                    {
                        // Wait for up to 2 mins for provisioning finish
                        for (int i = 0; i < 60; ++i)
                        {
                            await Task.Delay(2000);
                            response = await AzureRequest.GetResponse(url, azureAccessToken, HttpMethods.GET, null, null);
                            state = GetProvisioningState(response);
                            if (state == ProvisioningState.Succeeded)
                            {
                                return true;
                            }
                            if (state != ProvisioningState.Running)
                            {
                                return false;
                            }
                        }
                    }

                    return false;
                }
                catch (WebException)
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

        private ProvisioningState GetProvisioningState(string response)
        {
            try
            {
                JObject jobject = JsonConvert.DeserializeObject(response) as JObject;
                var provisioningState = jobject["properties"]["provisioningState"] as JValue;
                Enum.TryParse<ProvisioningState>(provisioningState.Value as string, out ProvisioningState state);
                return state;
            }
            catch
            {
                return ProvisioningState.None;
            }
        }

        public async Task<string> GetWebAppPublishProfile(string subscriptionId, string resourceGroupName, string webAppName)
        {
            string url = GetUri(WebAppPublishProfile, subscriptionId, resourceGroupName, webAppName);

            return await ExecuteWithRetryTokenRefresh(async (azureAccessToken) => {
                try
                {
                    return await AzureRequest.GetResponse(url, azureAccessToken, HttpMethods.POST, null);
                }
                catch (WebException)
                {
                    return string.Empty;
                }
            });
        }

        public static bool RetrieveFtpPublishInfo(string publishProfile, out string url, out string userName, out string password)
        {
            url = string.Empty;
            userName = string.Empty;
            password = string.Empty;

            if (!string.IsNullOrEmpty(publishProfile))
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(publishProfile);
                XmlNodeList nodeList = xmlDoc.DocumentElement.SelectNodes("/publishData/publishProfile");
                foreach (XmlNode node in nodeList)
                {
                    if (!node.OuterXml.Contains("FTP"))
                        continue;

                    url = node.Attributes.GetNamedItem("publishUrl").InnerText;
                    userName = node.Attributes.GetNamedItem("userName").InnerText;
                    password = node.Attributes.GetNamedItem("userPWD").InnerText;

                    return true;
                }
            }

            return false;
        }

        public async Task<bool> DeployWebApp(string subscriptionId, string resourceGroupName, string webAppName, string localZip)
        {
            string publishProfile = await GetWebAppPublishProfile(subscriptionId, resourceGroupName, webAppName);
            if (string.IsNullOrEmpty(publishProfile))
            {
                return false;
            }
            
            if (!RetrieveFtpPublishInfo(publishProfile, out string url, out string username, out string password))
            {
                return false;
            }

            string localFolder = Path.Combine(Path.GetDirectoryName(localZip), Path.GetRandomFileName());
            try
            {
                ZipFile.ExtractToDirectory(localZip, localFolder);

                FtpClient ftpClient = new FtpClient(url, username, password);
                ftpClient.UploadDirectory(url, Path.Combine(localFolder, Path.GetFileNameWithoutExtension(localZip)));
            }
            catch(Exception)
            {
                return false;
            }
            finally
            {
                if (File.Exists(localFolder))
                {
                    Directory.Delete(localFolder, true);
                }
            }

            return true;
        }

        public async Task<string> VisitWebApp(string webAppName)
        {
            string url = string.Format(WebAppUrl, webAppName);

            try
            {
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(url);
                    return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                }
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private enum ProvisioningState
        {
            None,
            Canceled,
            Failed,
            Succeeded,
            Accepted,
            Running
        }
    }
}
