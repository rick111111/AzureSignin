using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace AzureSignin
{
    class Program
    {
        const string Authority = "https://login.microsoftonline.com/{0}";
        const string TenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47";
        const string ClientId = "f137e62f-8762-41fd-bea3-d5240280720a";
        const string SubscriptionId = "1129b994-1ec6-487b-a948-7fef2a413d26";
        const string CertificateSubjectName = "CN=exampleappScriptCert";

        static void Main(string[] args)
        {
            Access().Wait();
        }

        static async Task<bool> Access()
        {
            // option 1: directly using certificate
            List<X509Certificate2> certs = GetCertificates(CertificateSubjectName, false, StoreLocation.CurrentUser);
            if (certs.Count == 0)
            {
                certs = GetCertificates("CN=exampleScriptCert", false, StoreLocation.LocalMachine);
            }

            foreach (var cert in certs)
            {
                try
                {
                    string authority = String.Format(Authority, TenantId);
                    AuthenticationContext authContext = new AuthenticationContext(authority);

                    var token = await authContext.AcquireTokenAsync("https://management.azure.com/", new ClientAssertionCertificate(ClientId, cert));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }

            // option 2: use AzureServiceTokenProvider
            try
            {
                // for exmaple: "AuthenticateAs=App;TenantId=72f988bf-86f1-41af-91ab-2d7cd011db47;AppId=91dd484c-2ed8-477f-aa68-d3d122f65ee9;CertificateSubjectName=CN=exampleappScriptCert";
                string connectionString = string.Format("AuthenticateAs=App;TenantId={0};AppId={1};CertificateSubjectName={2}", TenantId, ClientId, CertificateSubjectName);
                AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider(connectionString);
                string accessToken1 = await azureServiceTokenProvider.GetAccessTokenAsync("https://management.azure.com/");
                string accessToken2 = await azureServiceTokenProvider.GetAccessTokenAsync("https://vault.azure.net/");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            return true;
        }

        public static List<X509Certificate2> GetCertificates(string subjectNameOrThumbprint, bool isThumbprint, StoreLocation location)
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
