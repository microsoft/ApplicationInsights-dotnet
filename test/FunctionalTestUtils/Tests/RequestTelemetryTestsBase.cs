
namespace FunctionalTestUtils.Tests
{
    using System;
    using System.Linq;
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
            
            var items = this.Buffer.Where((item) => { return item is RequestTelemetry; });
            Assert.Equal(1, items.Count());
            ITelemetry telemetry = items.First();
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

        public void ValidateBasicException(string requestPath, ExceptionTelemetry expected)
        {
            DateTimeOffset testStart = DateTimeOffset.Now;
            var task = this.HttpClient.GetAsync(this.Server.BaseHost + requestPath);
            task.Wait(TestTimeoutMs);
            var result = task.Result;

            var items = this.Buffer.Where((item) => { return item is ExceptionTelemetry; });
            Assert.Equal(1, items.Count());
            ITelemetry telemetry = items.First();
            Assert.IsAssignableFrom(typeof(ExceptionTelemetry), telemetry);
            var actual = (ExceptionTelemetry)telemetry;

            Assert.Equal(expected.Exception.GetType(), actual.Exception.GetType());
            Assert.NotEmpty(actual.Exception.StackTrace);
            Assert.Equal(actual.HandledAt, actual.HandledAt);
            Assert.NotEmpty(actual.Context.Operation.Name);
            Assert.NotEmpty(actual.Context.Operation.Id);
        }
    }
}