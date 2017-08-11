namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Platform
{
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Assert = Xunit.Assert;

    [TestClass]
    public class PlatformReferencesTests
    {
        [TestMethod]
        public void NoSystemWebReferences()
        {
            // Validate Platform assembly
            foreach (var assembly in typeof(TelemetryDebugWriter).Assembly.GetReferencedAssemblies())
            {
                Assert.True(!assembly.FullName.Contains("System.Web"));
            }

            // Validate Core assembly
            foreach (var assembly in typeof(EventTelemetry).Assembly.GetReferencedAssemblies())
            {
                Assert.True(!assembly.FullName.Contains("System.Web"));
            }
        }
    }
}
