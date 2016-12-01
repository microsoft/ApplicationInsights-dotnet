namespace SampleWebAppIntegration.FunctionalTest
{
    using FunctionalTestUtils;
    using Xunit;

    public class TelemetryModuleWorkingMvcTests : TelemetryTestsBase
    {
        private const string assemblyName = "MVCFramework45.FunctionalTests";

        [Fact]
        public void TestBasicDependencyPropertiesAfterRequestingBasicPage()
        {
#if NET451
            this.ValidateBasicDependency(assemblyName, "/Home/About/5");
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
