namespace SampleWebAppIntegration.FunctionalTest
{
    using FunctionalTestUtils;
    using Xunit;

    public class TelemetryModuleWorkingWebApiTests : TelemetryTestsBase
    {
        private const string assemblyName = "WebApiShimFw46.FunctionalTests";

#if dnx451
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
