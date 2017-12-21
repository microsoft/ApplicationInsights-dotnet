namespace MVCFramework20.FunctionalTests.FunctionalTest
{   
    using System;
    using System.Linq;
    using System.Net.Http;
    using System.Text;

    using AI;
    using FunctionalTestUtils;
    using Microsoft.ApplicationInsights.DependencyCollector;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;
    using Xunit.Abstractions;

    public class DependencyTelemetryMvcTests : TelemetryTestsBase
    {
        private const string assemblyName = "MVCFramework20.FunctionalTests20";
        public DependencyTelemetryMvcTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void CorrelationInfoIsNotAddedToRequestHeaderIfUserAddDomainToExcludedList()
        {
#if netcoreapp2_0 // Correlation is supported on .Net core.
            using (var server = new InProcessServer(assemblyName, this.output))
            {
                var dependencyCollectorModule = server.ApplicationServices.GetServices<ITelemetryModule>().OfType<DependencyTrackingTelemetryModule>().Single();
                dependencyCollectorModule.ExcludeComponentCorrelationHttpHeadersOnDomains.Add(server.BaseHost);

                using (var httpClient = new HttpClient())
                {
                    var task = httpClient.GetAsync(server.BaseHost + "/");
                    task.Wait(TestTimeoutMs);
                }

                var actual = server.Execute<Envelope>(() => server.Listener.ReceiveItems(TestListenerTimeoutInMs));
                this.DebugTelemetryItems(actual);

                try
                {
                    var dependencyTelemetry = actual.OfType<TelemetryItem<RemoteDependencyData>>().FirstOrDefault();
                    Assert.NotNull(dependencyTelemetry);                         

                    var requestTelemetry = actual.OfType<TelemetryItem<RequestData>>().FirstOrDefault();
                    Assert.NotNull(requestTelemetry);

                    Assert.NotEqual(requestTelemetry.tags["ai.operation.id"], dependencyTelemetry.tags["ai.operation.id"]);
                }
                catch (Exception e)
                {
                    string data = DebugTelemetryItems(actual);
                    throw new Exception(data, e);
                }
            }
#endif
        }

        [Fact]
        public void OperationIdOfRequestIsPropagatedToChildDependency()
        {
            // https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/340
            // Verify operation of OperationIdTelemetryInitializer
            string path = "Home/Dependency";
            InProcessServer server;

            using (server = new InProcessServer(assemblyName, this.output))
            {
                this.ExecuteRequest(server.BaseHost + "/" + path);

                var actual = server.Listener.ReceiveItems(TestListenerTimeoutInMs);
                this.DebugTelemetryItems(actual);

                var dependencyTelemetry = actual.OfType<TelemetryItem<RemoteDependencyData>>()
                    .First(t => ((TelemetryItem<RemoteDependencyData>)t).data.baseData.name == "MyDependency");
                Assert.NotNull(dependencyTelemetry);

                var requestTelemetry = actual.OfType<TelemetryItem<RequestData>>().FirstOrDefault();
                Assert.NotNull(requestTelemetry);

                Assert.Equal(requestTelemetry.tags["ai.operation.id"], dependencyTelemetry.tags["ai.operation.id"]);
            }
        }

        [Fact]
        public void ParentIdOfChildDependencyIsIdOfRequest()
        {
            // https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/333
            // Verify operation of OperationCorrelationTelemetryInitializer
            string path = "Home/Dependency";
            InProcessServer server;
            using (server = new InProcessServer(assemblyName, this.output))
            {
                this.ExecuteRequest(server.BaseHost + "/" + path);

                var actual = server.Listener.ReceiveItems(TestListenerTimeoutInMs);
                this.DebugTelemetryItems(actual);

                var dependencyTelemetry = actual.OfType<TelemetryItem<RemoteDependencyData>>()
                    .First(t => ((TelemetryItem<RemoteDependencyData>)t).data.baseData.name == "MyDependency");
                Assert.NotNull(dependencyTelemetry);

                var requestTelemetry = actual.OfType<TelemetryItem<RequestData>>().FirstOrDefault();
                Assert.NotNull(requestTelemetry);

                Assert.Contains(requestTelemetry.tags["ai.operation.id"], dependencyTelemetry.tags["ai.operation.parentId"]);
            }
        }
    }
}
