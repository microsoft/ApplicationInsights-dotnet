using System.Diagnostics;

namespace MVCFramework.FunctionalTests.FunctionalTest
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using FunctionalTestUtils;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Xunit;
    using Xunit.Abstractions;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.DependencyCollector;
    using Microsoft.Extensions.DependencyInjection;
    using System.Reflection;

    public class DependencyTelemetryMvcTests : TelemetryTestsBase
    {
        private readonly string assemblyName;

        public DependencyTelemetryMvcTests(ITestOutputHelper output) : base(output)
        {
            this.assemblyName = this.GetType().GetTypeInfo().Assembly.GetName().Name;
        }

        [Fact]
        public void CorrelationInfoIsNotAddedToRequestHeaderIfUserAddDomainToExcludedList()
        {
#if netcoreapp1_0 // Correlation is supported on .Net core.
            InProcessServer server;

            using (server = new InProcessServer(assemblyName, InProcessServer.UseApplicationInsights))
            {
                var dependencyCollectorModule = server.ApplicationServices.GetServices<ITelemetryModule>().OfType<DependencyTrackingTelemetryModule>().Single();
                dependencyCollectorModule.ExcludeComponentCorrelationHttpHeadersOnDomains.Add(server.BaseHost);

                using (var httpClient = new HttpClient())
                {
                    var task = httpClient.GetAsync(server.BaseHost + "/");
                    task.Wait(TestTimeoutMs);
                }
            }

            var telemetries = server.BackChannel.Buffer;
            try
            {
                Assert.True(telemetries.Count >= 2);
                var requestTelemetry = telemetries.OfType<RequestTelemetry>().Single();
                var dependencyTelemetry = telemetries.OfType<DependencyTelemetry>().Single();
                Assert.NotEqual(requestTelemetry.Context.Operation.Id, dependencyTelemetry.Context.Operation.Id);
            }
            catch (Exception e)
            {
                string data = DebugTelemetryItems(telemetries);
                throw new Exception(data, e);
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

            using (server = new InProcessServer(assemblyName, InProcessServer.UseApplicationInsights))
            {
                using (var httpClient = new HttpClient())
                {
                    var task = httpClient.GetAsync(server.BaseHost + "/" + path);
                    task.Wait(TestTimeoutMs);
                }
            }

            var telemetries = server.BackChannel.Buffer;
            try
            {
                Assert.True(telemetries.Count >= 2);
                var requestTelemetry = telemetries.OfType<RequestTelemetry>().Single();
                var dependencyTelemetry = telemetries.OfType<DependencyTelemetry>().First(t => t.Name == "MyDependency");
                Assert.Equal(requestTelemetry.Context.Operation.Id, dependencyTelemetry.Context.Operation.Id);
            }
            catch (Exception e)
            {
                string data = DebugTelemetryItems(telemetries);
                throw new Exception(data, e);
            }
        }

        [Fact]
        public void ParentIdOfChildDependencyIsIdOfRequest()
        {
            // https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/333
            // Verify operation of OperationCorrelationTelemetryInitializer
            string path = "Home/Dependency";
            InProcessServer server;
            using (server = new InProcessServer(assemblyName, InProcessServer.UseApplicationInsights))
            {
                using (var httpClient = new HttpClient())
                {
                    var task = httpClient.GetAsync(server.BaseHost + "/" + path);
                    task.Wait(TestTimeoutMs);
                }
            }

            // Filter out any unexpected telemetry items.
            IEnumerable<ITelemetry> telemetries = server.BackChannel.Buffer.Where((t) => t.Context?.Operation?.Name != null && t.Context.Operation.Name.Contains(path));
            try
            {
                Assert.NotNull(telemetries);
                var requestTelemetry = telemetries.OfType<RequestTelemetry>().Single();
                var dependencyTelemetry = telemetries.First(t => t is DependencyTelemetry && (t as DependencyTelemetry).Name == "MyDependency");
                Assert.Equal(requestTelemetry.Id, dependencyTelemetry.Context.Operation.ParentId);
            }
            catch (Exception e)
            {
                string data = DebugTelemetryItems(server.BackChannel.Buffer);
                throw new Exception(data, e);
            }
        }

        private string DebugTelemetryItems(IList<ITelemetry> telemetries)
        {
            StringBuilder builder = new StringBuilder();
            foreach (ITelemetry telemetry in telemetries)
            {
                DependencyTelemetry dependency = telemetry as DependencyTelemetry;
                if (dependency != null) {
                    builder.AppendLine($"{dependency.ToString()} - {dependency.Data} - {dependency.Duration} - {dependency.Id} - {dependency.Name} - {dependency.ResultCode} - {dependency.Sequence} - {dependency.Success} - {dependency.Target} - {dependency.Type}");
                } else {
                    builder.AppendLine($"{telemetry.ToString()} - {telemetry.Context?.Operation?.Name}");
                }
            }

            return builder.ToString();
        }
    }
}
