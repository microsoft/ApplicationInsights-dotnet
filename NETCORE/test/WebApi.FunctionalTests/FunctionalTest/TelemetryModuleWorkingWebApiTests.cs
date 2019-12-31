using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace WebApi.FunctionalTests.FunctionalTest
{
    using System.Reflection;
    using FunctionalTestUtils;
    using Xunit.Abstractions;

    public class TelemetryModuleWorkingWebApiTests : TelemetryTestsBase
    {
        private readonly string assemblyName;

        public TelemetryModuleWorkingWebApiTests(ITestOutputHelper output) : base (output)
        {
            this.assemblyName = this.GetType().GetTypeInfo().Assembly.GetName().Name;
        }
        // The NET451 conditional check is wrapped inside the test to make the tests visible in the test explorer. We can move them to the class level once if the issue is resolved.

        [Fact]
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
