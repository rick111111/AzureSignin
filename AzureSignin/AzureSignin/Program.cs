
namespace AzureSignin
{
    internal static class Configuration
    {
        public const string Authority = "https://login.microsoftonline.com/{0}";
        public const string TenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47";
        public const string ClientId = "49c71ff0-df1e-47b7-89c5-057c4030e4c9";
        public const string SubscriptionId = "1129b994-1ec6-487b-a948-7fef2a413d26";
        public const string CertificateSubjectName = "CN=exampleselfsignCert";
    }

    class Program
    {
        static void Main(string[] args)
        {
            AccessWithLocalCertificate.Access().Wait();

            AccessWithKeyVaultCertificate.Access().Wait();
        }
    }
}
