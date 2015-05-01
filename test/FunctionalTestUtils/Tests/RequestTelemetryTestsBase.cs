
namespace FunctionalTestUtils.Tests
{
    using System;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Xunit;

    public abstract class RequestTelemetryTestsBase : TelemetryTestsBase
    {
        public RequestTelemetryTestsBase(string assemblyName) : base(assemblyName)
        { }

        public void ValidateBasicRequest(string requestPath, RequestTelemetry expected)
        {
            DateTimeOffset testStart = DateTimeOffset.Now;
            var task = this.HttpClient.GetAsync(this.Server.BaseHost + requestPath);
            task.Wait(TestTimeoutMs);
            var result = task.Result;
            
            Assert.Equal(1, this.Buffer.Count);
            ITelemetry telemetry = this.Buffer[0];
            Assert.IsAssignableFrom(typeof(RequestTelemetry), telemetry);
            var actual = (RequestTelemetry)telemetry;

            Assert.Equal(expected.ResponseCode, actual.ResponseCode);
            Assert.Equal(expected.Name, actual.Name);
            Assert.Equal(expected.Success, actual.Success);
            Assert.Equal(expected.Url, actual.Url);
            Assert.InRange<DateTimeOffset>(actual.Timestamp, testStart, DateTimeOffset.Now);
            Assert.True(actual.Duration < (DateTimeOffset.Now - testStart), "duration");
            Assert.Equal(expected.HttpMethod, actual.HttpMethod);
        }
    }
}