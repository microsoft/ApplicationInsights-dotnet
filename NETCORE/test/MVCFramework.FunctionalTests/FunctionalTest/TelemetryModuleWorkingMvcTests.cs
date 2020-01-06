namespace MVCFramework.FunctionalTests.FunctionalTest
{
    using System.Reflection;
    using FunctionalTestUtils;
    using Xunit;
    using Xunit.Abstractions;

    public class TelemetryModuleWorkingMvcTests : TelemetryTestsBase
    {
        private readonly string assemblyName;

        public TelemetryModuleWorkingMvcTests(ITestOutputHelper output) : base(output)
        {
            this.assemblyName = this.GetType().GetTypeInfo().Assembly.GetName().Name;
        }

        // The NET451 conditional check is wrapped inside the test to make the tests visible in the test explorer. We can move them to the class level once if the issue is resolved.
        
        [Fact]
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
