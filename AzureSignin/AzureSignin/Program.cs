using System;
using System.Linq;
using System.Xml;

namespace Microsoft.Test.Apex.VisualStudio.Debugger.Tests.SnapshotDebugger
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
        public static string RandomString(int length)
        {
            Random random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        static void Main(string[] args)
        {
            AzureRestClient client = new AzureRestClient();

            string suffix = RandomString(5);
            string resourceGroupName = "rg" + suffix;
            string webAppName = "faxue-web" + suffix;
            string appServicePlanName = "sp" + suffix;
            string location = "westus2";

            bool result;
            result = client.GetResourceGroup(Configuration.SubscriptionId, resourceGroupName).Result;
            if (!result)
            {
                result = client.CreateResourceGroup(Configuration.SubscriptionId, resourceGroupName, location).Result;
            }

            if (result)
            {
                if (client.GetAppService(Configuration.SubscriptionId, resourceGroupName, webAppName).Result)
                {
                    result = client.DeleteAppService(Configuration.SubscriptionId, resourceGroupName, webAppName).Result;
                }
                
                // check ServicePlan after deleting WebApp, deleting a WebApp might cause the ServicePlan also being deleted
                if (!client.GetServicePlan(Configuration.SubscriptionId, resourceGroupName, appServicePlanName).Result)
                {
                    result = client.CreateServicePlan(Configuration.SubscriptionId, resourceGroupName, appServicePlanName, location).Result;
                }

                if (result)
                {
                    result = client.CreateAppService(Configuration.SubscriptionId, resourceGroupName, appServicePlanName, webAppName, location).Result;
                }
            }

            if (result)
            {
                result = client.DeployWebApp(Configuration.SubscriptionId, resourceGroupName, webAppName, @"C:\Users\faxue\source\repos\faxue-desktopWeb\faxue-desktopWeb\bin\Release\PublishOutput.zip").Result;
            }

            if (result)
            {
                bool s = client.VisitWebApp(webAppName, 60).Result;
                if (!s)
                    System.Diagnostics.Debugger.Break();
            }
        }
    }
}
