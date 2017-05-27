namespace MVCFramework45.FunctionalTests.FunctionalTest
{
    using FunctionalTestUtils;
    using Microsoft.ApplicationInsights.DataContracts;
    using System;
    using System.Linq;
    using System.Net.Http;
    using Xunit;

    public class CorrelationMvcTests : TelemetryTestsBase
    {
        private const string assemblyName = "MVCFramework45.FunctionalTests";

        [Fact]
        public void CorrelationInfoIsPropagatedToDependendedService()
        {
#if !NET451 // Correlation is not supported in NET451. It works with NET46 and .Net core.
            InProcessServer server;

            using (server = new InProcessServer(assemblyName, InProcessServer.UseApplicationInsights))
            {
                using (var httpClient = new HttpClient())
                {
                    var task = httpClient.GetAsync(server.BaseHost + "/");
                    task.Wait(TestTimeoutMs);
                }
            }

            var telemetries = server.BackChannel.Buffer;

            Assert.True(telemetries.Count >= 2);
            var requestTelemetry = telemetries.OfType<RequestTelemetry>().Single();
            var dependencyTelemetry = telemetries.OfType<DependencyTelemetry>().Single();
            Assert.Equal(requestTelemetry.Context.Operation.Id, dependencyTelemetry.Context.Operation.Id);
            Assert.Equal(requestTelemetry.Context.Operation.ParentId, dependencyTelemetry.Id);
#endif
        }
    }
}
