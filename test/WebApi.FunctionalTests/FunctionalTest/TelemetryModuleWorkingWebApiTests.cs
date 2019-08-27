using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace WebApi.FunctionalTests.FunctionalTest
{
    using FunctionalTestUtils;
    using Xunit.Abstractions;

    public class TelemetryModuleWorkingWebApiTests : TelemetryTestsBase
    {
        private const string assemblyName = "WebApi.FunctionalTests";

        public TelemetryModuleWorkingWebApiTests(ITestOutputHelper output) : base (output)
        {
        }
        // The NET451 conditional check is wrapped inside the test to make the tests visible in the test explorer. We can move them to the class level once if the issue is resolved.

        [Fact(Skip = "Re-Enable once DependencyTrackingModule is updated to latest DiagnosticSource.")]

        public void TestBasicDependencyPropertiesAfterRequestingBasicPage()
        {
            this.ValidateBasicDependency(assemblyName, "/api/values");
        }

        [Fact]
        public void TestIfPerformanceCountersAreCollected()
        {
#if NET451 || NET46
            ValidatePerformanceCountersAreCollected(assemblyName);
#endif
        }
    }
}
