using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace AzureSignin
{
    internal static class AccessWithLocalCertificate
    {
        public static async Task<string> Access()
        {
            // option 1: directly using certificate
            List<X509Certificate2> certs = GetCertificates(Configuration.CertificateSubjectName, false, StoreLocation.CurrentUser);
            if (certs.Count == 0)
            {
                certs = GetCertificates(Configuration.CertificateSubjectName, false, StoreLocation.LocalMachine);
            }

            foreach (var cert in certs)
            {
                try
                {
                    string authority = String.Format(Configuration.Authority, Configuration.TenantId);
                    AuthenticationContext authContext = new AuthenticationContext(authority);

                    var token = await authContext.AcquireTokenAsync("https://management.azure.com/", new ClientAssertionCertificate(Configuration.ClientId, cert));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }

            // option 2: use AzureServiceTokenProvider
            string accessToken = "";
            try
            {
                string connectionString = string.Format("RunAs=App;TenantId={0};AppId={1};CertificateSubjectName={2};CertificateStoreLocation=LocalMachine", 
                    Configuration.TenantId, Configuration.ClientId, Configuration.CertificateSubjectName);
                AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider(connectionString);
                // string accessToken1 = await azureServiceTokenProvider.GetAccessTokenAsync("https://vault.azure.net/");
                accessToken = await azureServiceTokenProvider.GetAccessTokenAsync("https://management.azure.com/");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            return accessToken;
        }

        private static List<X509Certificate2> GetCertificates(string subjectNameOrThumbprint, bool isThumbprint, StoreLocation location)
        {
            string cacheKeyType = isThumbprint ? "Thumbprint" : "SubjectName";
            string cacheKey = $"{cacheKeyType}:{subjectNameOrThumbprint}|location:{location}";
            List<X509Certificate2> certs = new List<X509Certificate2>();

            X509Store store = new X509Store(location);
            try
            {
                store.Open(OpenFlags.ReadOnly);
                X509Certificate2Collection certCollection = store.Certificates;

                foreach (X509Certificate2 cert in certCollection)
                {
                    if (cert != null && cert.HasPrivateKey)
                    {
                        if ((isThumbprint && string.Equals(subjectNameOrThumbprint, cert.Thumbprint, StringComparison.OrdinalIgnoreCase))
                            || string.Equals(subjectNameOrThumbprint, cert.Subject, StringComparison.OrdinalIgnoreCase))
                        {
                            // Add cert if subject name matches
                            certs.Add(cert);
                        }
                    }
                }
            }
            finally
            {
                store.Close();
            }

            return certs;
        }
    }
}
