namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation
{
    using Microsoft.ApplicationInsights.Web.TestFramework;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class WebEventSourceTest
    {
        [TestMethod]
        public void MethodsAreImplementedConsistentlyWithTheirAttributes()
        {
            EventSourceTest.MethodsAreImplementedConsistentlyWithTheirAttributes(TelemetryChannelEventSource.Log);
        }
    }
}
