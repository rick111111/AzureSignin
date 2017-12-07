namespace E2E
{
    internal static class Configuration
    {
        public const string Authority = "https://login.microsoftonline.com/{0}";
        public const string TenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47";
        public const string ClientId = "2de6aaf5-2694-4563-a47c-110df3c0ffc6";
        public const string SubscriptionId = "1129b994-1ec6-487b-a948-7fef2a413d26";
        public const string ProddiagVaultBaseUrl = "https://vsproddiag-test.vault.azure.net/";
        public const string ProddiagSecretName = "CertString";
    }

    class Program
    {
        static void Main(string[] args)
        {
            var result = AccessWithKeyVaultCertificate.Access().Result;

            ResourceManager.RunSample();
        }
    }
}
