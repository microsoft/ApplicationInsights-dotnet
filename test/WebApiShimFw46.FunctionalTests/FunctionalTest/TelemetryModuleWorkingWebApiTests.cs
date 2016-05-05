using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace SampleWebAppIntegration.FunctionalTest
{
    using FunctionalTestUtils;
    using Microsoft.ApplicationInsights.DataContracts;

    public class TelemetryModuleWorkingWebApiTests : TelemetryTestsBase
    {
        private const string assemblyName = "WebApiShimFw46.FunctionalTests";

#if NET451

        [Fact]
        public void TestBasicDependencyPropertiesAfterRequestingBasicPage()
        {
            this.ValidateBasicDependency(assemblyName, "/api/values");
        }

        [Fact]
        public void TestIfPerformanceCountersAreCollected()
        {
            ValidatePerformanceCountersAreCollected(assemblyName);
        }
#endif
    }
}
