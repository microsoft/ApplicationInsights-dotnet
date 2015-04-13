namespace FunctionalTestUtils.Tests
{
    using Microsoft.ApplicationInsights.Channel;
    using System;
    using System.Collections.Generic;
    using System.Net.Http;

    public abstract class TelemetryTestsBase : IDisposable
    {
        protected const int TestTimeoutMs = 5000;

        private InProcessServer server;
        private IList<ITelemetry> buffer = new List<ITelemetry>();
        private HttpClient client = new HttpClient();

        public TelemetryTestsBase(string assemblyName)
        {
            this.server = new InProcessServer(assemblyName);
            BackTelemetryChannelExtensions.InitializeFunctionalTestTelemetryChannel(buffer);
        }

        public IList<ITelemetry> Buffer
        {
            get
            {
                return this.buffer;
            }
        }

        public InProcessServer Server
        {
            get
            {
                return this.server;
            }
        }

        public HttpClient HttpClient
        {
            get
            {
                return this.client;
            }
        }

        public void Dispose()
        {
            if (this.server != null)
            {
                this.server.Dispose();
            }

            if (this.client != null)
            {
                this.client.Dispose();
            }
        }
    }
}