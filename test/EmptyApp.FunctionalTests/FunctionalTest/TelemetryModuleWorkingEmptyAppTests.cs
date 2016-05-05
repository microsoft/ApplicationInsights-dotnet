namespace EmptyApp.FunctionalTests.FunctionalTest
{
    using FunctionalTestUtils;
    using Xunit;

    public class TelemetryModuleWorkingEmptyAppTests : TelemetryTestsBase
    {
        private const string assemblyName = "EmptyApp.FunctionalTests";

#if NET451

        [Fact]
        public void TestBasicDependencyPropertiesAfterRequestingBasicPage()
        {
            this.ValidateBasicDependency(assemblyName, "/");
        }

        [Fact]
        public void TestIfPerformanceCountersAreCollected()
        {
            ValidatePerformanceCountersAreCollected(assemblyName);
        }
#endif
    }
}
