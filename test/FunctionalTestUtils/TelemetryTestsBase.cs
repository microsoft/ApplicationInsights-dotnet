namespace FunctionalTestUtils
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Net.Http;
    using System.Reflection;
    using System.Threading;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;
#if NET451
    using System.Net;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector;
    using Microsoft.ApplicationInsights.Extensibility;
#endif

    public abstract class TelemetryTestsBase
    {
        protected const int TestTimeoutMs = 10000;

        public void ValidateBasicRequest(InProcessServer server, string requestPath, RequestTelemetry expected)
        {
            DateTimeOffset testStart = DateTimeOffset.Now;
            var timer = Stopwatch.StartNew();

            var httpClientHandler = new HttpClientHandler();
            httpClientHandler.UseDefaultCredentials = true;

            var httpClient = new HttpClient(httpClientHandler, true);
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
            var httpClientHandler = new HttpClientHandler();
            httpClientHandler.UseDefaultCredentials = true;

            var httpClient = new HttpClient(httpClientHandler, true);
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

#if NET451
        public void ValidateBasicDependency(string assemblyName, string requestPath)
        {
            using (InProcessServer server = new InProcessServer(assemblyName))
            {
                DependencyTelemetry expected = new DependencyTelemetry();
                expected.Name = server.BaseHost + requestPath;
                expected.ResultCode = "200";
                expected.Success = true;

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
            }
        }

        public void ValidatePerformanceCountersAreCollected(string assemblyName)
        {
            using (var server = new InProcessServer(assemblyName))
            {
                // Reconfigure the PerformanceCollectorModule timer.
                Type perfModuleType = typeof(PerformanceCollectorModule);
                PerformanceCollectorModule perfModule = (PerformanceCollectorModule)server.ApplicationServices.GetServices<ITelemetryModule>().FirstOrDefault(m => m.GetType() == perfModuleType);
                FieldInfo timerField = perfModuleType.GetField("timer", BindingFlags.NonPublic | BindingFlags.Instance);
                var timer = timerField.GetValue(perfModule);
                timerField.FieldType.InvokeMember("ScheduleNextTick", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance, null, timer, new object[] { TimeSpan.FromMilliseconds(10) });

                DateTime timeout = DateTime.Now.AddSeconds(10);
                int numberOfCountersSent = 0;
                do
                {
                    Thread.Sleep(1000);
                    numberOfCountersSent = server.BackChannel.Buffer.OfType<PerformanceCounterTelemetry>().Distinct().Count();
                } while (numberOfCountersSent == 0 && DateTime.Now < timeout);

                Assert.True(numberOfCountersSent > 0);
            }
        }
#endif
    }
}