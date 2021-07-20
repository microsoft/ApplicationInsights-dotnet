namespace IntegrationTests.Tests.TestFramework
{
    using Microsoft.ApplicationInsights.AspNetCore.Extensions;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.ApplicationInsights;

    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    public class NewWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
    {
        public readonly string TestHost = "http://localhost:6090";

        //internal ConcurrentBag<ITelemetry> sentItems = new ConcurrentBag<ITelemetry>();
        //public readonly LocalInProcHttpServer LocalInProcHttpServer;
        private bool isDisposed;

        public NewWebApplicationFactory()
        {
            //this.LocalInProcHttpServer = new LocalInProcHttpServer(testHost);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.AddLogging(loggingBuilder => loggingBuilder.AddFilter<ApplicationInsightsLoggerProvider>("Microsoft.AspNetCore.DataProtection.KeyManagement.XmlKeyManager", LogLevel.None));

                //services.AddSingleton<ITelemetryChannel>(new StubChannel() // TODO: THIS NEEDS TO USE 
                //{
                //    OnSend = (item) => this.sentItems.Add(item)
                //});

                //services.AddSingleton<ITelemetryChannel>()

                services.AddApplicationInsightsTelemetry(new ApplicationInsightsServiceOptions
                {
                    AddAutoCollectedMetricExtractor = false,
                    EnableAdaptiveSampling = false,
                    //InstrumentationKey = "ikey",
                    ConnectionString = $"InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint={this.TestHost}"
                });
            });
        }

        protected override void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    //this.LocalInProcHttpServer.Dispose();
                }

                isDisposed = true;
            }

            base.Dispose(disposing);
        }

        public new void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
            base.Dispose();
        }

        public async Task SendRequestToWebAppAsync(string path = "Home/Empty", Dictionary<string, string> requestHeaders = null)
        {
            // Arrange
            var client = this.CreateClient();
            var url = client.BaseAddress + path;

            var httpRequestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
            };

            if (requestHeaders != null)
            {
                foreach (var h in requestHeaders)
                {
                    httpRequestMessage.Headers.Add(h.Key, h.Value);
                }
            }

            // Act
            httpRequestMessage.RequestUri = new Uri(url);
            var response = await client.SendAsync(httpRequestMessage);

            // Assert
            response.EnsureSuccessStatusCode();

            await WaitForTelemetryToArrive();
        }

        private async Task WaitForTelemetryToArrive()
        {
            // The response to the test server request is completed
            // before the actual telemetry is sent from HostingDiagnosticListener.
            // This could be a TestServer issue/feature. (In a real application, the response is not
            // sent to the user until TrackRequest() is called.)
            // The simplest workaround is to do a wait here.
            // This could be improved when entire functional tests are migrated to use this pattern.
            await Task.Delay(1000);
        }

    }
}
