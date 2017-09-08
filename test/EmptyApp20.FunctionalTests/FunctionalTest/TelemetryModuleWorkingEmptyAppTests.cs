namespace EmptyApp20.FunctionalTests.FunctionalTest
{
    using FunctionalTestUtils;
    using Xunit;
    using Xunit.Abstractions;

    public class TelemetryModuleWorkingEmptyAppTests : TelemetryTestsBase
    {
        private const string assemblyName = "EmptyApp20.FunctionalTests";
        public TelemetryModuleWorkingEmptyAppTests(ITestOutputHelper output) : base (output)
        {
        }

        // The NET451 conditional check is wrapped inside the test to make the tests visible in the test explorer. We can move them to the class level once if the issue is resolved.

        [Fact]
        public void TestBasicDependencyPropertiesAfterRequestingBasicPage()
        {
            this.ValidateBasicDependency(assemblyName, "/");
        }

        [Fact]
        public void TestIfPerformanceCountersAreCollected()
        {
#if NET451 || NET461
            ValidatePerformanceCountersAreCollected(assemblyName);
#endif
        }
    }
}
