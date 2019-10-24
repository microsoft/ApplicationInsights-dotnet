namespace Microsoft.ApplicationInsights.Tests
{
    using Microsoft.ApplicationInsights.DependencyCollector;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Shared DependencyTrackingTelemetryModuleTest class.
    /// </summary>
    [TestClass]
    public partial class DependencyTrackingTelemetryModuleTest
    {
        [TestMethod]
        public void DependencyTrackingTelemetryModuleIsNotInitializedTwiceToPreventProfilerAttachFailure()
        {
            using (var module = new DependencyTrackingTelemetryModule())
            {
                PrivateObject privateObject = new PrivateObject(module);

                module.Initialize(TelemetryConfiguration.CreateDefault());
                object config1 = privateObject.GetField("telemetryConfiguration");

                module.Initialize(TelemetryConfiguration.CreateDefault());
                object config2 = privateObject.GetField("telemetryConfiguration");

                Assert.AreSame(config1, config2);
            }
        }
    }
}
