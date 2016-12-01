using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace SampleWebAppIntegration.FunctionalTest
{
    using FunctionalTestUtils;

    public class TelemetryModuleWorkingWebApiTests : TelemetryTestsBase
    {
        private const string assemblyName = "WebApiShimFw46.FunctionalTests";

        [Fact]
        public void TestBasicDependencyPropertiesAfterRequestingBasicPage()
        {
#if NET451
            this.ValidateBasicDependency(assemblyName, "/api/values");
#endif
        }

        [Fact]
        public void TestIfPerformanceCountersAreCollected()
        {
#if NET451
            ValidatePerformanceCountersAreCollected(assemblyName);
#endif
        }

    }
}
