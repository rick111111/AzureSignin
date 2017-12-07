using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Azure.Services.AppAuthentication;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace E2E
{
    public static class AccessWithKeyVaultCertificate
    {
        public static async Task<bool> Access()
        {           
            // According to https://docs.microsoft.com/en-us/azure/key-vault/service-to-service-authentication
            // For local development, AzureServiceTokenProvider fetches tokens with Azure AD Integrated Authentication. However we can 
            // only use it to access keyVault, trying  to access management resource will fail because 2FA is required
            //var azureServiceTokenProvider = new AzureServiceTokenProvider();
            //string accessToken = await azureServiceTokenProvider.GetAccessTokenAsync("https://vault.azure.net/").ConfigureAwait(false);

            var azureServiceTokenProvider = new AzureServiceTokenProvider("AuthenticateAs=User");
            string certString = string.Empty;
            using (var kv = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback)))
            {
                SecretBundle secretBundle = await kv.GetSecretAsync(Configuration.ProddiagVaultBaseUrl, Configuration.ProddiagSecretName);
                certString = secretBundle.Value;

                // The following code won't work, once certificate is uploaded into KeyVault, the certificate we get back from 
                // KeyVault will only contain the public key and AcquireTokenAsync will complain about missing private key
                /*
                CertificateBundle certBundle = await kv.GetCertificateAsync(ProddiagVaultBaseUrl, ProddiagCertName);
                X509Certificate2 cert = new X509Certificate2(certBundle.Cer);
                string authority = String.Format(Authority, TenantId);
                AuthenticationContext authContext = new AuthenticationContext(authority);
                var token = await authContext.AcquireTokenAsync("https://management.azure.com/", new ClientAssertionCertificate(ClientId, cert));
                */
            }

            X509Certificate2 cert = new X509Certificate2();
            cert.Import(Convert.FromBase64String(certString));

            // If you want to serialize the certificate to a file
            byte[] bytes = Convert.FromBase64String(certString);
            System.IO.File.WriteAllBytes("D:\\cert.pfx", bytes);

            return true;
        }
    }
}
