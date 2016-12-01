namespace EmptyApp.FunctionalTests.FunctionalTest
{
    using FunctionalTestUtils;
    using Xunit;

    public class TelemetryModuleWorkingEmptyAppTests : TelemetryTestsBase
    {
        private const string assemblyName = "EmptyApp.FunctionalTests";

        [Fact]
        public void TestBasicDependencyPropertiesAfterRequestingBasicPage()
        {
#if NET451
            this.ValidateBasicDependency(assemblyName, "/");
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
