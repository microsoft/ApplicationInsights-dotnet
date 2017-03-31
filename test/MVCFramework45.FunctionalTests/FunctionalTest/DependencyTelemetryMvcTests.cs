namespace SampleWebAppIntegration.FunctionalTest
{
    using System.Net.Http;
    using FunctionalTestUtils;
    using Xunit;
    using Microsoft.ApplicationInsights.DataContracts;

    public class DependencyTelemetryMvcTests : TelemetryTestsBase
    {
        private const string assemblyName = "MVCFramework45.FunctionalTests";

        [Fact]
        public void OperationIdOfRequestIsPropagatedToChildDependency()
        {
            // https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/340
            // Verify operation of OperationIdTelemetryInitializer
            InProcessServer server;
            using (server = new InProcessServer(assemblyName, InProcessServer.UseApplicationInsights))
            {
                using (var httpClient = new HttpClient())
                {
                    var task = httpClient.GetAsync(server.BaseHost + "/Home/Dependency");
                    task.Wait(TestTimeoutMs);
                }
            }
            var telemetries = server.BackChannel.Buffer;
            Assert.Equal(2, telemetries.Count);
            Assert.Equal(telemetries[0].Context.Operation.Id, telemetries[1].Context.Operation.Id);
        }

        [Fact]
        public void ParentIdOfChildDependencyIsIdOfRequest()
        {
            // https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/333
            // Verify operation of OperationCorrelationTelemetryInitializer
            InProcessServer server;
            using (server = new InProcessServer(assemblyName, InProcessServer.UseApplicationInsights))
            {
                using (var httpClient = new HttpClient())
                {
                    var task = httpClient.GetAsync(server.BaseHost + "/Home/Dependency");
                    task.Wait(TestTimeoutMs);
                }
            }
            var telemetries = server.BackChannel.Buffer;
            Assert.Equal(2, telemetries.Count);
            Assert.IsType(typeof(DependencyTelemetry), telemetries[0]);
            Assert.IsType(typeof(RequestTelemetry), telemetries[1]);
            Assert.Equal(((RequestTelemetry)telemetries[1]).Id, telemetries[0].Context.Operation.ParentId);
        }
    }
}
