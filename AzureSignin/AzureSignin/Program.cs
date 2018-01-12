using AzureSignin.Communication;
using System.Xml;

namespace AzureSignin
{
    internal static class Configuration
    {
        public const string Authority = "https://login.microsoftonline.com/{0}";
        public const string TenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47";
        public const string ClientId = "2de6aaf5-2694-4563-a47c-110df3c0ffc6";
        public const string SubscriptionId = "1129b994-1ec6-487b-a948-7fef2a413d26";
        public const string CertificateSubjectName = "CN=homecert3";
    }

    class Program
    {
        static void Main(string[] args)
        {
            HttpClientHelper client = new HttpClientHelper();

            string resourceGroupName = "rg4";
            string webAppName = "faxue-web16";
            string appServicePlanName = "sp12";
            bool result;
            result = client.CheckResourceGroupExist(Configuration.SubscriptionId, resourceGroupName).Result;
            if (!result)
            {
                result = client.CreateResourceGroup(Configuration.SubscriptionId, resourceGroupName, "westus2").Result;
            }

            if (result)
            {
                if (client.CheckAppServiceExist(Configuration.SubscriptionId, resourceGroupName, webAppName).Result)
                {
                    result = client.DeleteAppService(Configuration.SubscriptionId, resourceGroupName, webAppName).Result;
                }
            }

            if (result)
            {
                DeploymentParameterObject dpo = new DeploymentParameterObject()
                {
                    name = new Name() { value = webAppName },
                    serverFarmResourceGroup = new ServerFarmResourceGroup() { value = resourceGroupName },
                    hostingPlanName = new HostingPlanName() { value = appServicePlanName },
                    subscriptionId = new SubscriptionId() { value = Configuration.SubscriptionId }
                };

                result = client.DeployResourceTemplate(dpo).Result;
            }

            if (result)
            {
                result = client.DeployWebApp(Configuration.SubscriptionId, resourceGroupName, webAppName, @"C:\Users\faxue\source\repos\faxue-desktopWeb\faxue-desktopWeb\bin\Release\PublishOutput.zip").Result;
            }
        }
    }
}
