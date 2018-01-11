using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Azure.Services.AppAuthentication;
using System;
using System.IO;
using System.Threading.Tasks;

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

        public const string AzureManagementConfiguration = @"subscription={0}
client={1}
tenant={2}
certificate={3}
certificatePassword=
managementURI=https\://management.core.windows.net/
baseURL=https\://management.azure.com/
authURL=https\://login.windows.net/
graphURL=https\://graph.windows.net/";
    }

    class Program
    {
        private static string _tempCertFileName;
        private static string _tempAuthFileName;

        static void Main(string[] args)
        {
            _tempCertFileName = Path.GetTempFileName();
            _tempAuthFileName = Path.GetTempFileName();

            byte[] bytes = RetrieveCertificate().Result;
            if (bytes != null)
            {
                _tempCertFileName = Path.GetTempFileName();
                File.WriteAllBytes(_tempCertFileName, bytes);

                string config = string.Format(Configuration.AzureManagementConfiguration, Configuration.SubscriptionId, Configuration.ClientId, Configuration.TenantId, _tempCertFileName);
                File.WriteAllText(_tempAuthFileName, config);

                ResourceManager.RunSample(_tempAuthFileName);
            }
        }

        private static async Task<byte[]> RetrieveCertificate()
        {
            try
            {
                // According to https://docs.microsoft.com/en-us/azure/key-vault/service-to-service-authentication
                // For local development, AzureServiceTokenProvider fetches tokens with Azure AD Integrated Authentication. However we can 
                // only use it to access keyVault, trying  to access management resource will fail because 2FA is required
                var azureServiceTokenProvider = new AzureServiceTokenProvider("AuthenticateAs=User");
                string certString = string.Empty;
                using (var kv = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback)))
                {
                    // If certificate is uploaded into KeyVault as certificate type, the certificate we get back will only contain 
                    // the public key and can't be used to sign into Azure. So instead we upload the certificate as plain string
                    SecretBundle secretBundle = await kv.GetSecretAsync(Configuration.ProddiagVaultBaseUrl, Configuration.ProddiagSecretName);
                    certString = secretBundle.Value;
                }

                return Convert.FromBase64String(certString);
            }
            catch
            {
                return null;
            }
        }
    }
}
