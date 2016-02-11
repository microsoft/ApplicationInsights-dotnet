namespace Microsoft.ApplicationInsights.Extensibility.Web
{
    using System;
    using System.Linq;
    using System.Xml.Linq;
    using Microsoft.ApplicationInsights.Web;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TelemetryProcessorsTests
    {
        [TestMethod]
        public void InstallAddsAdaptiveSamplingProcessor()
        {
            string emptyConfig = ConfigurationHelpers.GetEmptyConfig();
            XDocument configAfterTransform = ConfigurationHelpers.InstallTransform(emptyConfig);

            var children = ConfigurationHelpers.GetTelemetryProcessors(configAfterTransform)
                .Descendants().ToList();

            var sampler = children
                .FirstOrDefault(element => (element.Attribute("Type") != null ? element.Attribute("Type").Value : null) == "Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.AdaptiveSamplingTelemetryProcessor, Microsoft.AI.ServerTelemetryChannel");

            Assert.IsNotNull(sampler, "AdaptiveSamplingTelemetryProcessor is not there.");
        }

        [TestMethod]
        public void UninstallRemovesAllTelemetryProcessorsExceptAdaptiveSampling()
        {
            string emptyConfig = ConfigurationHelpers.GetEmptyConfig();
            XDocument configAfterInstall = ConfigurationHelpers.InstallTransform(emptyConfig);

            XDocument configAfterUninstall = ConfigurationHelpers.UninstallTransform(configAfterInstall.ToString());

            var children = ConfigurationHelpers.GetTelemetryProcessors(configAfterUninstall)
                .Descendants().ToList();

            var sampler = children
                .FirstOrDefault(element => (element.Attribute("Type") != null ? element.Attribute("Type").Value : null) == "Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.AdaptiveSamplingTelemetryProcessor, Microsoft.AI.ServerTelemetryChannel");

            Assert.IsNotNull(sampler, "AdaptiveSamplingTelemetryProcessor was removed");
        }
    }
}
