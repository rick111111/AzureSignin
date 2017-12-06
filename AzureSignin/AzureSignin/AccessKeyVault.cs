using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Threading.Tasks;

namespace AzureSignin
{
    internal static class AccessKeyVault
    {
        const string ProddiagVaultBaseUrl = "https://vsproddiag-test.vault.azure.net/";
        const string ProddiagSecretName = "CertString";

        public static async Task<string> Access()
        {
            string authority = String.Format(Configuration.Authority, Configuration.TenantId);
            AuthenticationContext authContext = new AuthenticationContext(authority);
            AuthenticationResult result = null;

            try
            {
                // See if token is present in cache
                result = await authContext.AcquireTokenSilentAsync("https://vault.azure.net/", Configuration.ClientId).ConfigureAwait(false);

            }
            catch (Exception e)
            {
                UserCredential userCredential = new UserCredential();

                try
                {
                    result = await authContext.AcquireTokenAsync("https://vault.azure.net/", Configuration.ClientId, userCredential).ConfigureAwait(false);
                }
                catch (Exception exp)
                {
                }
            }

            return "";
        }
    }
}
