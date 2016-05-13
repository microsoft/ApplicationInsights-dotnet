namespace SampleWebAppIntegration.FunctionalTest
{
    using FunctionalTestUtils;
    using Xunit;

    public class TelemetryModuleWorkingMvcTests : TelemetryTestsBase
    {
        private const string assemblyName = "MVCFramework45.FunctionalTests";

#if NET451
        [Fact]
        public void TestBasicDependencyPropertiesAfterRequestingBasicPage()
        {
            this.ValidateBasicDependency(assemblyName, "/Home/About/5");
        }

        [Fact]
        public void TestIfPerformanceCountersAreCollected()
        {
            ValidatePerformanceCountersAreCollected(assemblyName);
        }
#endif
    }
}
