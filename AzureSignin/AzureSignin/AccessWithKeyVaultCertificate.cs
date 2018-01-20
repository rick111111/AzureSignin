using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Microsoft.Test.Apex.VisualStudio.Debugger.Tests.SnapshotDebugger
{
    public static class AccessWithKeyVaultCertificate
    {
        const string ManagementEndpoint = "https://management.azure.com/";
        const string ProddiagVaultBaseUrl = "https://vsproddiag-test.vault.azure.net/";
        const string ProddiagSecretName = "CertString";
        const string BearerPrefix = "Bearer ";

        public static async Task<string> Access()
        {           
            // According to https://docs.microsoft.com/en-us/azure/key-vault/service-to-service-authentication
            // For local development, AzureServiceTokenProvider fetches tokens with Azure AD Integrated Authentication. However we can 
            // only use it to access keyVault, trying  to access management resource will fail because 2FA is required

            var azureServiceTokenProvider = new AzureServiceTokenProvider("RunAs=CurrentUser");
            string certString = string.Empty;
            using (var kv = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback)))
            {
                SecretBundle secretBundle = await kv.GetSecretAsync(ProddiagVaultBaseUrl, ProddiagSecretName);
                certString = secretBundle.Value;
            }

            X509Certificate2 cert = new X509Certificate2();
            cert.Import(Convert.FromBase64String(certString));

            string authority = String.Format(Configuration.Authority, Configuration.TenantId);
            AuthenticationContext authContext = new AuthenticationContext(authority);
            AuthenticationResult token = await authContext.AcquireTokenAsync(ManagementEndpoint, new ClientAssertionCertificate(Configuration.ClientId, cert));

            return token.AccessToken.StartsWith(BearerPrefix) ? token.AccessToken : BearerPrefix + token.AccessToken;
        }
    }
}
