using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.ApplicationInsights;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace IntegrationTests.Tests.TestFramework
{
    public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
    {
        internal TelemetryBag sentItems = new TelemetryBag();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.AddLogging(loggingBuilder => loggingBuilder.AddFilter<ApplicationInsightsLoggerProvider>("Microsoft.AspNetCore.DataProtection.KeyManagement.XmlKeyManager", LogLevel.None));

                services.AddSingleton<ITelemetryChannel>(new StubChannel()
                {
                    OnSend = (item) => this.sentItems.Add(item)
                });

                var aiOptions = new ApplicationInsightsServiceOptions
                {
                    AddAutoCollectedMetricExtractor = false,
                    EnableAdaptiveSampling = false,
                    InstrumentationKey = "ikey",
                };

                services.AddApplicationInsightsTelemetry(aiOptions);
            });
        }

        public Uri MakeUri(string requestPath) => new Uri(this.ClientOptions.BaseAddress + requestPath);

        public async Task<HttpResponseMessage> SendRequestAsync(Uri requestUri, Dictionary<string, string> requestHeaders = null)
        {
            var client = this.CreateClient();

            var httpRequestMessage = CreateRequestMessage(requestUri);
            var response = await client.SendAsync(httpRequestMessage);


            await WaitForTelemetryToArrive();

            return response;
        }

        private HttpRequestMessage CreateRequestMessage(Uri requestUri, Dictionary<string, string> requestHeaders = null)
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
        private async Task WaitForTelemetryToArrive() => await Task.Delay(1000);
    }
}
