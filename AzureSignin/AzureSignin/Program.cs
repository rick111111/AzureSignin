
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
            // 1. Install certificate to local and access Azure with local certificate
            // AccessWithLocalCertificate.Access().Wait();

            // 2. Upload certificate to Azure KeyVault and access Azure with KeyVault certificate
            // string accessToken = AccessWithKeyVaultCertificate.Access().Result;

            // 3. Use Rest API to access Azure Resource
            HttpClientSample client = new HttpClientSample();

            string resourceGroupName = "rg4";
            bool exist;
            exist = client.IsResourceGroupExist(Configuration.SubscriptionId, resourceGroupName).Result;
            if (!exist)
            {
                bool success = client.CreateResourceGroup(Configuration.SubscriptionId, resourceGroupName).Result;
                exist = client.IsResourceGroupExist(Configuration.SubscriptionId, resourceGroupName).Result;
            }

            DeploymentParameterObject dpo = new DeploymentParameterObject()
            {
                name = new Name() { value = "faxue-web12" },
                serverFarmResourceGroup = new ServerFarmResourceGroup() { value = resourceGroupName },
                hostingPlanName = new HostingPlanName() { value = "sp12" },
                subscriptionId = new SubscriptionId() { value = Configuration.SubscriptionId }
            };

            exist = client.ValidateResourceTemplate(dpo).Result;
            exist = client.DeployResourceTemplate(dpo).Result;
        }
    }
}
