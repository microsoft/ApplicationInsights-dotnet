using System.Diagnostics;

namespace SampleWebAppIntegration.FunctionalTest
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

    public class DependencyTelemetryMvcTests : TelemetryTestsBase
    {
        private const string assemblyName = "MVCFramework45.FunctionalTests";

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
                Assert.Equal(2, telemetries.Count(t => t.Context?.Operation?.Name != null && t.Context.Operation.Name.Contains(path)));
                var dependency = (DependencyTelemetry)telemetries.Single(t =>
                {
                    var dt = t as DependencyTelemetry;
                    return dt?.Context?.Operation?.Name != null && dt.Context.Operation.Name.Contains(path);
                });

                var request = (RequestTelemetry)telemetries.Single(t => t is RequestTelemetry);
                Assert.Equal(request.Context.Operation.Id, dependency.Context.Operation.Id);
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
                Assert.Equal(2, telemetries.Count());
                DependencyTelemetry dependency = telemetries.OfType<DependencyTelemetry>().FirstOrDefault();
                Assert.NotNull(dependency);
                RequestTelemetry request = telemetries.OfType<RequestTelemetry>().FirstOrDefault();
                Assert.NotNull(request);
                Assert.Equal(request.Id, dependency.Context.Operation.ParentId);
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
