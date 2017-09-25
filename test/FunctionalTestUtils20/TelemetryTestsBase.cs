namespace FunctionalTestUtils
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Net.Http;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using AI;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;
    using Xunit.Abstractions;
#if NET451 || NET461
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector;
    using Microsoft.ApplicationInsights.Extensibility;    
#endif

    public abstract class TelemetryTestsBase
    {
        public const int TestListenerTimeoutInMs = 5000;
        protected const int TestTimeoutMs = 10000;
        protected readonly ITestOutputHelper output;
        
        public TelemetryTestsBase(ITestOutputHelper output)
        {
            this.output = output;
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void ValidateBasicRequest(InProcessServer server, string requestPath, RequestTelemetry expected)
        {
            // Subtract 50 milliseconds to hack around strange behavior on build server where the RequestTelemetry.Timestamp is somehow sometimes earlier than now by a few milliseconds.
            expected.Timestamp = DateTimeOffset.Now.Subtract(TimeSpan.FromMilliseconds(50));
            Stopwatch timer = Stopwatch.StartNew();

            var httpClientHandler = new HttpClientHandler();
            httpClientHandler.UseDefaultCredentials = true;

            Task<HttpResponseMessage> task;
            using (HttpClient httpClient = new HttpClient(httpClientHandler, true))
            {
                task = httpClient.GetAsync(server.BaseHost + requestPath);
                task.Wait(TestTimeoutMs);
            }

            timer.Stop();

            var actual = server.Execute<RequestData>(() => server.Listener.ReceiveItemsOfType<RequestData>(1, TestListenerTimeoutInMs))[0];

            Assert.Equal(expected.ResponseCode, actual.responseCode);
            Assert.Equal(expected.Name, actual.name);
            Assert.Equal(expected.Success, actual.success);
            Assert.Equal(expected.Url, new Uri(actual.url));
            output.WriteLine("actual.Duration: " + actual.duration);
            output.WriteLine("timer.Elapsed: " + timer.Elapsed);
            Assert.True(TimeSpan.Parse(actual.duration) < timer.Elapsed.Add(TimeSpan.FromMilliseconds(20)), "duration");
        }

        public void ValidateBasicException(InProcessServer server, string requestPath, ExceptionTelemetry expected)
        {
            var httpClientHandler = new HttpClientHandler();
            httpClientHandler.UseDefaultCredentials = true;

            Task<HttpResponseMessage> task;
            using (var httpClient = new HttpClient(httpClientHandler, true))
            {
                task = httpClient.GetAsync(server.BaseHost + requestPath);
                task.Wait(TestTimeoutMs);
            }
            var result = task.Result;

            var actual = server.Execute<ExceptionData>(() => server.Listener.ReceiveItemsOfType<ExceptionData>(1, TestListenerTimeoutInMs))[0];
            
            Assert.Equal(expected.Exception.GetType().ToString(), actual.exceptions[0].typeName);
            Assert.NotEmpty(actual.exceptions[0].parsedStack);
        }

        public void ValidateBasicDependency(InProcessServer server, string requestPath, DependencyTelemetry expected)
        {
            var httpClientHandler = new HttpClientHandler();
            httpClientHandler.UseDefaultCredentials = true;

            Task<HttpResponseMessage> task;
            using (var httpClient = new HttpClient(httpClientHandler, true))
            {
                task = httpClient.GetAsync(server.BaseHost + requestPath);
                task.Wait(TestTimeoutMs);
            }
            var result = task.Result;

            var actual = server.Execute<Envelope>(() => server.Listener.ReceiveItems(2, TestListenerTimeoutInMs));

            var dependencyTelemetry = actual.OfType<TelemetryItem<RemoteDependencyData>>().FirstOrDefault();
            Assert.NotNull(dependencyTelemetry);
            var dependencyData = ((TelemetryItem<RemoteDependencyData>)dependencyTelemetry).data.baseData;
            Assert.Equal(expected.Data, dependencyData.data);
            Assert.Equal(expected.ResultCode, dependencyData.resultCode);
            Assert.Equal(expected.Success, dependencyData.success);

#if !NET461
            var requestTelemetry = actual.OfType<TelemetryItem<RequestData>>().FirstOrDefault();
            Assert.NotNull(requestTelemetry);

            Assert.Contains(dependencyTelemetry.tags["ai.operation.id"], requestTelemetry.tags["ai.operation.parentId"]);
            Assert.Equal(requestTelemetry.tags["ai.operation.id"], dependencyTelemetry.tags["ai.operation.id"]);
#endif
        }

#if NET451 || NET461
        public void ValidatePerformanceCountersAreCollected(string assemblyName, Func<IWebHostBuilder, IWebHostBuilder> configureHost = null)
        {
            using (var server = new InProcessServer(assemblyName, configureHost))
            {
                // Reconfigure the PerformanceCollectorModule timer.
                Type perfModuleType = typeof(PerformanceCollectorModule);
                PerformanceCollectorModule perfModule = (PerformanceCollectorModule)server.ApplicationServices.GetServices<ITelemetryModule>().FirstOrDefault(m => m.GetType() == perfModuleType);
                FieldInfo timerField = perfModuleType.GetField("timer", BindingFlags.NonPublic | BindingFlags.Instance);
                var timer = timerField.GetValue(perfModule);
                timerField.FieldType.InvokeMember("ScheduleNextTick", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance, null, timer, new object[] { TimeSpan.FromMilliseconds(10) });

                var actual = server.Execute<Envelope>(() => server.Listener.ReceiveItems(TestListenerTimeoutInMs));
                Assert.True(actual.Length > 0);
            }
        }
#endif
    }
}