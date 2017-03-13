namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel
{
    using System.Linq;
    using System.Xml.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TelemetryProcessorsTests
    {
        [TestMethod]
        public void InstallAddsAdaptiveTelemetryProcessor()
        {
            string emptyConfig = ConfigurationHelpers.GetEmptyConfig();
            XDocument configAfterTransform = ConfigurationHelpers.InstallTransform(emptyConfig);

            var typeToFind = typeof(AdaptiveSamplingTelemetryProcessor);

            var processors = ConfigurationHelpers.GetTelemetryProcessors(configAfterTransform);

            Assert.AreEqual(1, processors.Count());

            var type = processors.FirstOrDefault(element => element.Attribute("Type").Value == ConfigurationHelpers.GetPartialTypeName(typeToFind));
            Assert.IsNotNull(type);

            var excludedTypes = processors.Descendants().Where(element => element.Name.LocalName == "ExcludedTypes").First().Value;
            Assert.AreEqual("Event", excludedTypes);

            var maxItems = processors.Descendants().Where(element => element.Name.LocalName == "MaxTelemetryItemsPerSecond").First().Value;
            Assert.AreEqual("5", maxItems);
        }

        [TestMethod]
        public void UninstallRemovesAllInstalledTelemetryProcessors()
        {
            string emptyConfig = ConfigurationHelpers.GetEmptyConfig();
            XDocument configAfterInstall = ConfigurationHelpers.InstallTransform(emptyConfig);

            XDocument configAfterUninstall = ConfigurationHelpers.UninstallTransform(configAfterInstall.ToString());

            Assert.AreEqual(0, ConfigurationHelpers.GetTelemetryProcessors(configAfterUninstall).ToList().Count);
        }

        [TestMethod]
        public void UninstallDoesNotRemoveCustomTelemetryProcessors()
        {
            string emptyConfig = ConfigurationHelpers.GetEmptyConfig();
            XDocument configAfterInstall = ConfigurationHelpers.InstallTransform(emptyConfig);

            // Replace valid type on custom so during uninstall it should stay in the config
            string customConfig = configAfterInstall.ToString().Replace("AdaptiveSamplingTelemetryProcessor", "blah");

            XDocument configAfterUninstall = ConfigurationHelpers.UninstallTransform(customConfig);

            Assert.AreEqual(1, ConfigurationHelpers.GetTelemetryProcessors(configAfterUninstall).ToList().Count); 
        }
    }
}