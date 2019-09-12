namespace EmptyApp20.FunctionalTests.FunctionalTest
{
    using FunctionalTestUtils;
    using Microsoft.ApplicationInsights.DataContracts;
    using Xunit;
    using Xunit.Abstractions;

    public class TelemetryModuleWorkingEmptyAppTests : TelemetryTestsBase
    {
        private const string assemblyName = "EmptyApp20.FunctionalTests20";
        public TelemetryModuleWorkingEmptyAppTests(ITestOutputHelper output) : base (output)
        {
        }

        // The NET451 conditional check is wrapped inside the test to make the tests visible in the test explorer. We can move them to the class level once if the issue is resolved.

        public void TestBasicDependencyPropertiesAfterRequestingBasicPage()
        {
            const string RequestPath = "/";

            using (var server = new InProcessServer(assemblyName, this.output))
            {
                DependencyTelemetry expected = new DependencyTelemetry();
                expected.ResultCode = "200";
                expected.Success = true;
                expected.Name = "GET /";
                expected.Data = server.BaseHost + RequestPath;

                this.ValidateBasicDependency(server, RequestPath, expected);
            }
        }

        [Fact]
        public void TestIfPerformanceCountersAreCollected()
        {
            ValidatePerformanceCountersAreCollected(assemblyName);
        }
    }
}
