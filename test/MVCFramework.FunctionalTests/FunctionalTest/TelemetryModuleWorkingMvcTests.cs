namespace MVCFramework.FunctionalTests.FunctionalTest
{
    using FunctionalTestUtils;
    using Xunit;
    using Xunit.Abstractions;

    public class TelemetryModuleWorkingMvcTests : TelemetryTestsBase
    {
        private const string assemblyName = "MVCFramework.FunctionalTests";

        public TelemetryModuleWorkingMvcTests(ITestOutputHelper output) : base(output)
        {
        }

        // The NET451 conditional check is wrapped inside the test to make the tests visible in the test explorer. We can move them to the class level once if the issue is resolved.
        
        [Fact(Skip = "Re-Enable once DependencyTrackingModule is updated to latest DiagnosticSource.")]
        public void TestBasicDependencyPropertiesAfterRequestingBasicPage()
        {
            this.ValidateBasicDependency(assemblyName, "/Home/About/5", InProcessServer.UseApplicationInsights);
        }

        [Fact]
        public void TestIfPerformanceCountersAreCollected()
        {
#if NET451 || NET46
            ValidatePerformanceCountersAreCollected(assemblyName, InProcessServer.UseApplicationInsights);
#endif
        }
    }
}
