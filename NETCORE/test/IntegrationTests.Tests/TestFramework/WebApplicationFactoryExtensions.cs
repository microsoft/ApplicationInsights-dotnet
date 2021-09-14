namespace IntegrationTests.Tests.TestFramework
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Mvc.Testing;

    public static class WebApplicationFactoryExtensions
    {
        public static Uri MakeUri<T>(this WebApplicationFactory<T> webApplicationFactory, string requestPath) where T : class
            => new Uri(webApplicationFactory.ClientOptions.BaseAddress + requestPath);

        public static async Task<HttpResponseMessage> SendRequestAsync<T>(this WebApplicationFactory<T> webApplicationFactory, Uri requestUri, Dictionary<string, string> requestHeaders = null) where T : class
        {
            var client = webApplicationFactory.CreateClient();

            var httpRequestMessage = CreateRequestMessage(requestUri, requestHeaders);
            var response = await client.SendAsync(httpRequestMessage);

            await WaitForTelemetryToArrive();

            return response;
        }

        private static HttpRequestMessage CreateRequestMessage(Uri requestUri, Dictionary<string, string> requestHeaders = null)
        {
            var httpRequestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = requestUri,
            };

            if (requestHeaders != null)
            {
                foreach (var h in requestHeaders)
                {
                    httpRequestMessage.Headers.Add(h.Key, h.Value);
                }
            }

            return httpRequestMessage;
        }

        /// <summary>
        /// The response to the test server request is completed before the actual telemetry is sent from HostingDiagnosticListener.
        /// This could be a TestServer issue/feature. 
        /// (In a real application, the response is not sent to the user until TrackRequest() is called.)
        /// The simplest workaround is to do a wait here.
        /// This could be improved when entire functional tests are migrated to use this pattern. 
        /// </summary>
        private static async Task WaitForTelemetryToArrive() => await Task.Delay(1000);
    }
}
