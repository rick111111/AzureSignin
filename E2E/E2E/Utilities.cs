using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.AppService.Fluent.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace E2E
{
    internal static class Utilities
    {
        public static bool IsRunningMocked { get; set; } = false;

        public static void Log(string s)
        {
            Console.WriteLine(s);
        }

        public static void Log(Exception ex)
        {
            Console.WriteLine(ex);
        }

        public static void Print(IAppServicePlan resource)
        {
            var builder = new StringBuilder().Append("App service certificate order: ").Append(resource.Id)
                    .Append("Name: ").Append(resource.Name)
                    .Append("\n\tResource group: ").Append(resource.ResourceGroupName)
                    .Append("\n\tRegion: ").Append(resource.Region)
                    .Append("\n\tPricing tier: ").Append(resource.PricingTier);
            Utilities.Log(builder.ToString());
        }

        public static void Print(IWebAppBase resource)
        {
            var builder = new StringBuilder().Append("Web app: ").Append(resource.Id)
                    .Append("Name: ").Append(resource.Name)
                    .Append("\n\tState: ").Append(resource.State)
                    .Append("\n\tResource group: ").Append(resource.ResourceGroupName)
                    .Append("\n\tRegion: ").Append(resource.Region)
                    .Append("\n\tDefault hostname: ").Append(resource.DefaultHostName)
                    .Append("\n\tApp service plan: ").Append(resource.AppServicePlanId)
                    .Append("\n\tHost name bindings: ");
            foreach (var binding in resource.GetHostNameBindings().Values)
            {
                builder = builder.Append("\n\t\t" + binding.ToString());
            }
            builder = builder.Append("\n\tSSL bindings: ");
            foreach (var binding in resource.HostNameSslStates.Values)
            {
                builder = builder.Append("\n\t\t" + binding.Name + ": " + binding.SslState);
                if (binding.SslState != null && binding.SslState != SslState.Disabled)
                {
                    builder = builder.Append(" - " + binding.Thumbprint);
                }
            }
            builder = builder.Append("\n\tApp settings: ");
            foreach (var setting in resource.GetAppSettings().Values)
            {
                builder = builder.Append("\n\t\t" + setting.Key + ": " + setting.Value + (setting.Sticky ? " - slot setting" : ""));
            }
            builder = builder.Append("\n\tConnection strings: ");
            foreach (var conn in resource.GetConnectionStrings().Values)
            {
                builder = builder.Append("\n\t\t" + conn.Name + ": " + conn.Value + " - " + conn.Type + (conn.Sticky ? " - slot setting" : ""));
            }
            Utilities.Log(builder.ToString());
        }

        public static string CheckAddress(string url, IDictionary<string, string> headers = null)
        {
            if (!IsRunningMocked)
            {
                try
                {
                    using (var client = new HttpClient())
                    {
                        if (headers != null)
                        {
                            foreach (var header in headers)
                            {
                                client.DefaultRequestHeaders.Add(header.Key, header.Value);
                            }
                        }
                        return client.GetAsync(url).Result.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    }
                }
                catch (Exception ex)
                {
                    Utilities.Log(ex);
                }
            }

            return "[Running in PlaybackMode]";
        }
    }
}
