namespace FunctionalTestUtils
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Net.Http;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Text;
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
        public const int TestListenerTimeoutInMs = 10000;
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

            this.ExecuteRequest(server.BaseHost + requestPath);

            var actual = server.Listener.ReceiveItemsOfType<TelemetryItem<RequestData>>(1, TestListenerTimeoutInMs);
            this.DebugTelemetryItems(actual);

            timer.Stop();

            var item = actual.OfType<TelemetryItem<RequestData>>().FirstOrDefault();
            Assert.NotNull(item);
            var data = ((TelemetryItem<RequestData>)item).data.baseData;
            
            Assert.Equal(expected.ResponseCode, data.responseCode);
            Assert.Equal(expected.Name, data.name);
            Assert.Equal(expected.Success, data.success);
            Assert.Equal(expected.Url, new Uri(data.url));
            output.WriteLine("actual.Duration: " + data.duration);
            output.WriteLine("timer.Elapsed: " + timer.Elapsed);
            Assert.True(TimeSpan.Parse(data.duration) < timer.Elapsed.Add(TimeSpan.FromMilliseconds(20)), "duration");
        }

        public void ValidateBasicException(InProcessServer server, string requestPath, ExceptionTelemetry expected)
        {
            this.ExecuteRequest(server.BaseHost + requestPath);

            var actual = server.Listener.ReceiveItemsOfType<TelemetryItem<ExceptionData>>(1, TestListenerTimeoutInMs);
            this.DebugTelemetryItems(actual);
            
            var item = actual.OfType<TelemetryItem<ExceptionData>>().FirstOrDefault();
            Assert.NotNull(item);
            var data = ((TelemetryItem<ExceptionData>)item).data.baseData;

            Assert.Equal(expected.Exception.GetType().ToString(), data.exceptions[0].typeName);
            Assert.NotEmpty(data.exceptions[0].parsedStack);
        }

        public void ValidateBasicDependency(InProcessServer server, string requestPath, DependencyTelemetry expected)
        {
            this.ExecuteRequest(server.BaseHost + requestPath);

            var actual = server.Listener.ReceiveItems(2, TestListenerTimeoutInMs);
            this.DebugTelemetryItems(actual);

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
        public void ValidatePerformanceCountersAreCollected(string assemblyName)
        {
            using (var server = new InProcessServer(assemblyName, this.output))
            {
                // Reconfigure the PerformanceCollectorModule timer.
                Type perfModuleType = typeof(PerformanceCollectorModule);
                PerformanceCollectorModule perfModule = (PerformanceCollectorModule)server.ApplicationServices.GetServices<ITelemetryModule>().FirstOrDefault(m => m.GetType() == perfModuleType);
                FieldInfo timerField = perfModuleType.GetField("timer", BindingFlags.NonPublic | BindingFlags.Instance);
                var timer = timerField.GetValue(perfModule);
                timerField.FieldType.InvokeMember("ScheduleNextTick", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance, null, timer, new object[] { TimeSpan.FromMilliseconds(10) });

                var actual = server.Listener.ReceiveItems(TestListenerTimeoutInMs);
                this.DebugTelemetryItems(actual);
                Assert.True(actual.Length > 0);
            }
        }
#endif

        protected void ExecuteRequest(string requestPath)
        {
            var httpClientHandler = new HttpClientHandler();
            httpClientHandler.UseDefaultCredentials = true;

            using (HttpClient httpClient = new HttpClient(httpClientHandler, true))
            {
                this.output.WriteLine(string.Format("{0}: Executing request: {1}", DateTime.Now.ToString("G"), requestPath));
                var task = httpClient.GetAsync(requestPath);
                task.Wait(TestListenerTimeoutInMs);
                this.output.WriteLine(string.Format("{0}: Ended request: {1}", DateTime.Now.ToString("G"), requestPath));
            }
        }

        protected void DebugTelemetryItems(Envelope[] telemetries)
        {
            StringBuilder builder = new StringBuilder();
            foreach (Envelope telemetry in telemetries)
            {
                TelemetryItem<RemoteDependencyData> dependency = telemetry as TelemetryItem<RemoteDependencyData>;
                if (dependency != null)
                {
                    var data = ((TelemetryItem<RemoteDependencyData>)dependency).data.baseData;
                    builder.AppendLine($"{dependency.ToString()} - {data.data} - {data.duration} - {data.id} - {data.name} - {data.resultCode} - {data.success} - {data.target} - {data.type}");
                }
                else
                {
                    TelemetryItem<RequestData> request = telemetry as TelemetryItem<RequestData>;
                    if (request != null)
                    {
                        var data = ((TelemetryItem<RequestData>)request).data.baseData;
                        builder.AppendLine($"{request.ToString()} - {data.url} - {data.duration} - {data.id} - {data.name} - {data.success} - {data.responseCode}");
                    }
                    else
                    {
                        TelemetryItem<ExceptionData> exception = telemetry as TelemetryItem<ExceptionData>;
                        if (exception != null)
                        {
                            var data = ((TelemetryItem<ExceptionData>)exception).data.baseData;
                            builder.AppendLine($"{exception.ToString()} - {data.exceptions[0].message} - {data.exceptions[0].stack} - {data.exceptions[0].typeName} - {data.severityLevel}");
                        }
                        else
                        {
                            TelemetryItem<MessageData> message = telemetry as TelemetryItem<MessageData>;
                            if (message != null)
                            {
                                var data = ((TelemetryItem<MessageData>)message).data.baseData;
                                builder.AppendLine($"{message.ToString()} - {data.message} - {data.severityLevel}");
                            }
                            else
                            {
                                builder.AppendLine($"{telemetry.ToString()} - {telemetry.name}");
                            }
                        }
                    }
                }
            }

            this.output.WriteLine(builder.ToString());
        }
    }
}