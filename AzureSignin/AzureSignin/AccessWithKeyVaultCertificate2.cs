using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Microsoft.Test.Apex.VisualStudio.Debugger.Tests.SnapshotDebugger
{
    public class AccessWithKeyVaultCertificate2
    {
        const string ManagementEndpoint = "https://management.azure.com/";
        const string ProddiagVaultBaseUrl = "https://vsproddiag-test.vault.azure.net/";
        const string ProddiagSecretName = "CertString";
        const string BearerPrefix = "Bearer ";
        const string VisualStudioClientId = "872cd9fa-d31f-45e0-9eab-6e460a02d1f1";

        public static async Task<string> Access()
        {
            string certString = string.Empty;
            using (var kv = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(AuthenticationCallback)))
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

        private static async Task<string> AuthenticationCallback(string authority, string resource, string scope)
        {
            // If authority of user is not specified, 
            if (string.IsNullOrWhiteSpace(authority))
            {
                // Use common
                authority = $"https://login.microsoftonline.com/common";
            }

            // Use ADAL's default token cache, instead of file based cache.
            // This prevents dependency on file and DPAPI, and is fine for IWA scenarios.
            // IWA will only be used the first time a program runs. After that, access and refresh tokens will be used, 
            // unless the refresh token expires (for a very long running program), and then IWA will be used again. 
            // This design is fine for both local development, and on-premise services that need to talk to Azure services. 
            AuthenticationContext authContext = new AuthenticationContext(authority);
            AuthenticationResult result = null;

            try
            {
                // See if token is present in cache
                result = await authContext.AcquireTokenSilentAsync(resource, VisualStudioClientId).ConfigureAwait(false);
            }
            catch
            {
                // If fails, use AcquireTokenAsync
            }

            // If token not in cache, acquire it
            if (result == null)
            {
                // This causes ADAL to use IWA
                UserCredential userCredential = new UserCredential();

                try
                {
                    result = await authContext.AcquireTokenAsync(resource, VisualStudioClientId, userCredential).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    throw;
                }
            }

            if (result != null)
            {
                return result.AccessToken;
            }

            return "";
        }
    }
}
