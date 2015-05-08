namespace FunctionalTestUtils
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Net.Http;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Xunit;

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

        public void ValidateBasicRequest(string requestPath, RequestTelemetry expected)
        {
            DateTimeOffset testStart = DateTimeOffset.Now;
            var task = this.HttpClient.GetAsync(this.Server.BaseHost + requestPath);
            task.Wait(TestTimeoutMs);
            var result = task.Result;

            var actual = this.Buffer.OfType<RequestTelemetry>().Single();

            Assert.Equal(expected.ResponseCode, actual.ResponseCode);
            Assert.Equal(expected.Name, actual.Name);
            Assert.Equal(expected.Success, actual.Success);
            Assert.Equal(expected.Url, actual.Url);
            Assert.InRange<DateTimeOffset>(actual.Timestamp, testStart, DateTimeOffset.Now);
            Assert.True(actual.Duration < (DateTimeOffset.Now - testStart), "duration");
            Assert.Equal(expected.HttpMethod, actual.HttpMethod);
        }

        public void ValidateBasicException(string requestPath, ExceptionTelemetry expected)
        {
            DateTimeOffset testStart = DateTimeOffset.Now;
            var task = this.HttpClient.GetAsync(this.Server.BaseHost + requestPath);
            task.Wait(TestTimeoutMs);
            var result = task.Result;

            var actual = this.Buffer.OfType<ExceptionTelemetry>().Single();

            Assert.Equal(expected.Exception.GetType(), actual.Exception.GetType());
            Assert.NotEmpty(actual.Exception.StackTrace);
            Assert.Equal(actual.HandledAt, actual.HandledAt);
            Assert.NotEmpty(actual.Context.Operation.Name);
            Assert.NotEmpty(actual.Context.Operation.Id);
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