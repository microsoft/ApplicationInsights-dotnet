namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel
{
    using System.Linq;
    using System.Xml.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TelemetryChannelTests
    {
        [TestMethod]
        public void InstallAddsServerTelemetryChannel()
        {
            string emptyConfig = ConfigurationHelpers.GetEmptyConfig();
            XDocument configAfterTransform = ConfigurationHelpers.InstallTransform(emptyConfig);

            var typeToFind = typeof(ServerTelemetryChannel);

            var node = ConfigurationHelpers.GetTelemetryChannel(configAfterTransform)
                .FirstOrDefault(element => element.Attribute("Type").Value == ConfigurationHelpers.GetPartialTypeName(typeToFind));

            Assert.IsNotNull(node);
        }

        [TestMethod]
        public void UninstallRemovesTelemetryChannel()
        {
            string emptyConfig = ConfigurationHelpers.GetEmptyConfig();
            XDocument configAfterInstall = ConfigurationHelpers.InstallTransform(emptyConfig);

            XDocument configAfterUninstall = ConfigurationHelpers.UninstallTransform(configAfterInstall.ToString());

            Assert.AreEqual(0, ConfigurationHelpers.GetTelemetryChannel(configAfterUninstall).ToList().Count);
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