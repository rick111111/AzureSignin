using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;

namespace Microsoft.Test.Apex.VisualStudio.Debugger.Tests.SnapshotDebugger
{
    internal class AzureRestClient
    {
        private string _azureAccessToken;

        const string ManagementEndpoint = "https://management.azure.com";
        const string ResourceGroupId = "/subscriptions/{0}/resourcegroups/{1}";
        const string ResourceGroupUri = "/subscriptions/{0}/resourcegroups/{1}?api-version=2016-09-01";

        const string ServicePlanId = "/subscriptions/{0}/resourceGroups/{1}/providers/Microsoft.Web/serverfarms/{2}";
        const string ServicePlanUri = "/subscriptions/{0}/resourceGroups/{1}/providers/Microsoft.Web/serverfarms/{2}?api-version=2016-09-01";
        const string ServicePlanUriListApps = "/subscriptions/{0}/resourceGroups/{1}/providers/Microsoft.Web/serverfarms/{2}/sites?api-version=2016-09-01";

        const string WebAppUri = "/subscriptions/{0}/resourceGroups/{1}/providers/Microsoft.Web/sites/{2}/?api-version=2016-08-01";
        const string WebAppDeleteUri = "/subscriptions/{0}/resourceGroups/{1}/providers/Microsoft.Web/sites/{2}/?deleteEmptyServerFarm=true&api-version=2016-08-01";
        const string WebAppDeploymentUri = "/subscriptions/{0}/resourcegroups/{1}/providers/Microsoft.Resources/deployments/template?api-version=2016-09-01";
        const string WebAppDeploymentValidationUri = "/subscriptions/{0}/resourcegroups/{1}/providers/Microsoft.Resources/deployments/template/validate?api-version=2016-09-01";

        const string WebAppPublishProfile = "/subscriptions/{0}/resourcegroups/{1}/providers/Microsoft.Web/sites/{2}/publishxml?api-version=2016-08-01";
        const string WebAppUrl = "https://{0}.azurewebsites.net";
        const string WebAppKuduUrl = "https://{0}.scm.azurewebsites.net";

        private async Task<string> GetAccessToken()
        {
            return await AccessWithKeyVaultCertificate2.Access();
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

        public async Task<bool> GetResourceGroup(string subscriptionId, string resourceGroupName)
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
        
        public async Task<bool> GetServicePlan(string subscriptionId, string resourceGroupName, string servicePlanName)
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

        public async Task<bool> CreateServicePlan(string subscriptionId, string resourceGroupName, string servicePlanName, string location)
        {
            string url = GetUri(ServicePlanUri, subscriptionId, resourceGroupName, servicePlanName);

            ServicePlanObject spo = new ServicePlanObject()
            {
                kind = "app",
                sku = new ServicePlanObject.Sku()
                {
                    name = "S1",
                    tier = "Standard",
                    size = "S1"
                },
                location = location,
            };
            string body = JsonConvert.SerializeObject(spo);

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

        public async Task<bool> DeleteServicePlan(string subscriptionId, string resourceGroupName, string servicePlanName)
        {
            // TODO: Need to delete all the apps inside ServicePlan, ServicePlan with app can't be deleted unless it's empty
            // string listUrl = GetUri(ServicePlanUriListApps, subscriptionId, resourceGroupName, servicePlanName);

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

        public async Task<bool> GetAppService(string subscriptionId, string resourceGroupName, string appServiceName)
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

        public async Task<bool> CreateAppService(string subscriptionId, string resourceGroupName, string servicePlanName, string appServiceName, string location)
        {
            string servicePlanId = GetUri(ServicePlanId, subscriptionId, resourceGroupName, servicePlanName);
            string appServiceUrl = GetUri(WebAppUri, subscriptionId, resourceGroupName, appServiceName);

            AppServiceObject aso = new AppServiceObject()
            {
                kind = "app",
                properties = new AppServiceObject.Properties()
                {
                    serverFarmId = servicePlanId,
                },
                location = location
            };
            string body = JsonConvert.SerializeObject(aso);

            return await ExecuteWithRetryTokenRefresh(async (azureAccessToken) => {
                try
                {
                    string response = await AzureRequest.GetResponse(appServiceUrl, azureAccessToken, HttpMethods.PUT, null, body);
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
            string url = GetUri(WebAppDeleteUri, subscriptionId, resourceGroupName, appServiceName);

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

        public async Task<bool> VisitWebApp(string webAppName, int timeout)
        {
            string url = string.Format(WebAppUrl, webAppName);

            DateTime dt = DateTime.Now;
            bool succeeded = false;

            while (!succeeded && (DateTime.Now - dt).TotalSeconds < timeout)
            {
                try
                {
                    using (var client = new HttpClient())
                    {
                        var response = await client.GetAsync(url);
                        succeeded = response.IsSuccessStatusCode;
                    }
                }
                catch (Exception)
                {
                }
            }

            return succeeded;
        }

        public async Task<bool> VisitWebAppKudu(string webAppName, int timeout)
        {
            string url = string.Format(WebAppKuduUrl, webAppName);
            
            DateTime dt = DateTime.Now;
            bool succeeded = false;

            while (!succeeded && (DateTime.Now - dt).TotalSeconds < timeout)
            {
                // TODO: need username/password to access Kudu
            }

            return succeeded;
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
