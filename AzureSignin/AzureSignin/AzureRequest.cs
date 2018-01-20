using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Test.Apex.VisualStudio.Debugger.Tests.SnapshotDebugger
{
    internal enum HttpMethods
    {
        HEAD,
        POST,
        GET,
        PUT,
        PATCH,
        DELETE
    }

    internal static class AzureRequest
    {
        private static HttpStatusCode[] retriableStatusCodes = new HttpStatusCode[]
        {
            (HttpStatusCode)429 /* Too many requests */,
            HttpStatusCode.ServiceUnavailable
        };

        /// <summary>
        /// Gets the response of a request to the given url with the given configuration.
        /// </summary>
        public static async Task<string> GetResponse(string url, string token, HttpMethods method, Dictionary<string, string> headers = null, string body = null, CookieContainer cookieContainer = null)
        {
            using (var response = await GetResponseInternal(url, token, method, headers, body, cookieContainer))
            {
                using (var responseStream = new StreamReader(response.GetResponseStream()))
                {
                    return responseStream.ReadToEnd();
                }
            }
        }

        private static async Task<HttpWebResponse> GetResponseInternal(string url, string token, HttpMethods method, Dictionary<string, string> headers = null, string body = null, CookieContainer cookieContainer = null)
        {
            HttpWebResponse response = null;
            int backOffMilliseconds = 250;
            TimeSpan maxRetryElapseTime = TimeSpan.FromMinutes(2);
            DateTime startTime = DateTime.Now;

            while (response == null)
            {
                HttpWebRequest request = await CreateRequest(url, token, method, headers, body);
                if (cookieContainer != null)
                {
                    request.CookieContainer = cookieContainer;
                }

                try
                {
                    response = (HttpWebResponse)await request.GetResponseAsync();

                    if (response.StatusCode >= HttpStatusCode.Ambiguous)
                    {
                        // TODO: Get this string from central resource manager.
                        throw new Exception("Error getting response from Azure");
                    }
                }
                catch (WebException e)
                {
                    var httpResponse = e.Response as HttpWebResponse;
                    if (httpResponse != null && 
                        retriableStatusCodes.Contains((httpResponse.StatusCode)) && 
                        DateTime.Now - startTime < maxRetryElapseTime)
                    {
                        // Retry on retriable errors
                        backOffMilliseconds *= 2;
                        await Task.Delay(backOffMilliseconds);
                    }
                    else
                    {
                        // All other fatal status codes are assumed to be legitimate problems.
                        // Allow the request to fail and report the error.
                        throw;
                    }
                }
            }

            return response;
        }

        /// <summary>
        /// Creates an Azure Web Request.
        /// </summary>
        private static async Task<HttpWebRequest> CreateRequest(string url, string token, HttpMethods method, Dictionary<string, string> headers = null, string body = null)
        {
            const string contentType = "application/json; charset=utf-8";

            // Prepare request
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = method.ToString();

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }
            }

            request.UserAgent = "Apex Test";
            request.Headers.Add(HttpRequestHeader.Authorization, token);
            request.Accept = contentType;

            // Add content information
            request.ContentType = contentType;

            // Add content only if there is any body
            if (!string.IsNullOrEmpty(body))
            {
                UTF8Encoding encoding = new UTF8Encoding();
                byte[] contentBytes = encoding.GetBytes(body);
                using (Stream stream = await request.GetRequestStreamAsync())
                {
                    await stream.WriteAsync(contentBytes, 0, contentBytes.Length);
                }
            }
            else
            {
                request.ContentLength = 0;
            }

            return request;
        }
    }
}
