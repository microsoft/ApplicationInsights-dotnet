namespace SampleWebAppIntegration.FunctionalTest
{
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using FunctionalTestUtils;
    using Microsoft.ApplicationInsights.DataContracts;
    using Xunit;

    public class TelemetryModuleWorkingMvcTests : TelemetryTestsBase
    {
        private const string assemblyName = "MVCFramework45.FunctionalTests";

#if dnx451

        [Fact]
        public void TestBasicDependencyPropertiesAfterRequestingBasicPage()
        {
            using (var server = new InProcessServer(assemblyName))
            {
                const string RequestPath = "/Home/About/5";

                var expectedDependencyTelemetry = new DependencyTelemetry();
                expectedDependencyTelemetry.Name = server.BaseHost + RequestPath;
                expectedDependencyTelemetry.ResultCode = "200";
                expectedDependencyTelemetry.Success = true;
                this.ValidateBasicDependency(server, RequestPath, expectedDependencyTelemetry);
            }
        }

        [Fact]
        public void TestIfPerformanceCountersAreCollected()
        {
            using (var server = new InProcessServer(assemblyName))
            {
                const string RequestPath = "/";
                var httpClient = new HttpClient();
                var task = httpClient.GetAsync(server.BaseHost + RequestPath);
                task.Wait(TestTimeoutMs);
                var result = task.Result;
                Thread.Sleep(70000);
                var actual = server.BackChannel.Buffer.OfType<PerformanceCounterTelemetry>().Distinct();

                Assert.True(actual.Count() > 0);

            }
        }

#endif
    }
}
