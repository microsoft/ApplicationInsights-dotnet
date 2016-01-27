namespace FunctionalTestUtils
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Net.Http;
    using Microsoft.ApplicationInsights.DataContracts;
    using Xunit;
#if dnx451
    using System.Net;
#endif
    using System.Threading;

    public abstract class TelemetryTestsBase
    {
        protected const int TestTimeoutMs = 10000;

        public void ValidateBasicRequest(InProcessServer server, string requestPath, RequestTelemetry expected)
        {
            DateTimeOffset testStart = DateTimeOffset.Now;
            var timer = Stopwatch.StartNew();
            var httpClient = new HttpClient();
            var task = httpClient.GetAsync(server.BaseHost + requestPath);
            task.Wait(TestTimeoutMs);
            var result = task.Result;

            var actual = server.BackChannel.Buffer.OfType<RequestTelemetry>().Single();

            timer.Stop();
            Assert.Equal(expected.ResponseCode, actual.ResponseCode);
            Assert.Equal(expected.Name, actual.Name);
            Assert.Equal(expected.Success, actual.Success);
            Assert.Equal(expected.Url, actual.Url);
            Assert.InRange<DateTimeOffset>(actual.Timestamp, testStart, DateTimeOffset.Now);
            Assert.True(actual.Duration < timer.Elapsed, "duration");
            Assert.Equal(expected.HttpMethod, actual.HttpMethod);
        }

        public void ValidateBasicException(InProcessServer server, string requestPath, ExceptionTelemetry expected)
        {
            DateTimeOffset testStart = DateTimeOffset.Now;
            var httpClient = new HttpClient();
            var task = httpClient.GetAsync(server.BaseHost + requestPath);
            task.Wait(TestTimeoutMs);
            var result = task.Result;

            var actual = server.BackChannel.Buffer.OfType<ExceptionTelemetry>().Single();

            Assert.Equal(expected.Exception.GetType(), actual.Exception.GetType());
            Assert.NotEmpty(actual.Exception.StackTrace);
            Assert.Equal(actual.HandledAt, actual.HandledAt);
            Assert.NotEmpty(actual.Context.Operation.Name);
            Assert.NotEmpty(actual.Context.Operation.Id);
        }

        public void ValidateBasicDependency(InProcessServer server, string requestPath, DependencyTelemetry expected)
        {

#if dnx451 
            DateTimeOffset testStart = DateTimeOffset.Now;
            var timer = Stopwatch.StartNew();
            var httpClient = new HttpClient();
            var task = httpClient.GetAsync(server.BaseHost + requestPath);
            task.Wait(TestTimeoutMs);
            var result = task.Result;

            var actual = server.BackChannel.Buffer.OfType<DependencyTelemetry>().Single();
            timer.Stop();

            Assert.Equal(expected.Name, actual.Name);
            Assert.Equal(expected.Success, actual.Success);         
            Assert.Equal(expected.ResultCode, actual.ResultCode);
#endif
        }

        public void ValidateBasicPerfCounters(InProcessServer server, string requestPath)
        {

#if dnx451 
            DateTimeOffset testStart = DateTimeOffset.Now;
            var timer = Stopwatch.StartNew();
            var httpClient = new HttpClient();
            var task = httpClient.GetAsync(server.BaseHost + requestPath);
            task.Wait(TestTimeoutMs);
            var result = task.Result;
            Thread.Sleep(70000);
            var actual = server.BackChannel.Buffer.OfType<PerformanceCounterTelemetry>().Distinct();

            Assert.True(actual.Count() > 0);
#endif
        }
    }
}