namespace WindowsServer.Nuget.Tests
{
    using System;
    using System.Linq;
    using System.Xml.Linq;
    using Microsoft.ApplicationInsights.WindowsServer;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    
    [TestClass]
    public class TelemetryInitailizersTests
    {
        [TestMethod]
        public void InstallAddsAzureRoleEnvironmentTelemetryInitializer()
        {
            string emptyConfig = ConfigurationHelpers.GetEmptyConfig();
            XDocument configAfterTransform = ConfigurationHelpers.InstallTransform(emptyConfig);

            Type typeToFind = typeof(AzureRoleEnvironmentTelemetryInitializer);

            var nodes = ConfigurationHelpers.GetTelemetryInitializers(configAfterTransform).Descendants().ToList();
            var node = nodes.FirstOrDefault(element => element.Attribute("Type").Value == ConfigurationHelpers.GetPartialTypeName(typeToFind));
            
            Assert.IsNotNull(node);
        }

        [TestMethod]
        public void InstallAddsAzureWebAppTelemetryInitializer()
        {
            string emptyConfig = ConfigurationHelpers.GetEmptyConfig();
            XDocument configAfterTransform = ConfigurationHelpers.InstallTransform(emptyConfig);

            Type typeToFind = typeof(AzureWebAppRoleEnvironmentTelemetryInitializer);

            var node = ConfigurationHelpers.GetTelemetryInitializers(configAfterTransform)
                .Descendants()
                .FirstOrDefault(element => element.Attribute("Type").Value == ConfigurationHelpers.GetPartialTypeName(typeToFind));

            Assert.IsNotNull(node);
        }

        [TestMethod]
        public void InstallAddsBuildInfoConfigComponentVersionTelemetryInitializer()
        {
            string emptyConfig = ConfigurationHelpers.GetEmptyConfig();
            XDocument configAfterTransform = ConfigurationHelpers.InstallTransform(emptyConfig);

            Type typeToFind = typeof(BuildInfoConfigComponentVersionTelemetryInitializer);

            var node = ConfigurationHelpers.GetTelemetryInitializers(configAfterTransform)
                .Descendants()
                .FirstOrDefault(element => element.Attribute("Type").Value == ConfigurationHelpers.GetPartialTypeName(typeToFind));
            
            Assert.IsNotNull(node);
        }

        [TestMethod]
        public void InstallDoesNotAddDeviceTelemetryInitializerByDefault()
        {
            string emptyConfig = ConfigurationHelpers.GetEmptyConfig();
            XDocument configAfterTransform = ConfigurationHelpers.InstallTransform(emptyConfig);

            Type typeToFind = typeof(DeviceTelemetryInitializer);

            var node = ConfigurationHelpers.GetTelemetryInitializers(configAfterTransform)
                .Descendants()
                .FirstOrDefault(element => element.Attribute("Type").Value == ConfigurationHelpers.GetPartialTypeName(typeToFind));
            
            Assert.IsNull(node);
        }

        [TestMethod]
        public void InstallAddsAzureSpecificInitializerFirstSoItIsFirstToFillCommonProperties()
        {
            string emptyConfig = ConfigurationHelpers.GetEmptyConfig();
            XDocument configAfterTransform = ConfigurationHelpers.InstallTransform(emptyConfig);

            Type typeToFind1 = typeof(AzureRoleEnvironmentTelemetryInitializer);
            Type typeToFind2 = typeof(AzureWebAppRoleEnvironmentTelemetryInitializer);

            var node = ConfigurationHelpers.GetTelemetryInitializers(configAfterTransform)
                .Descendants()
                .Where(element =>
                    element.Attribute("Type").Value == ConfigurationHelpers.GetPartialTypeName(typeToFind1)
                    || element.Attribute("Type").Value == ConfigurationHelpers.GetPartialTypeName(typeToFind2))
                .ToList();

            Assert.AreEqual(node[0].Attribute("Type").Value, ConfigurationHelpers.GetPartialTypeName(typeToFind1));
        }

        [TestMethod]
        public void UninstallRemovesAllTelemetryInitailizers()
        {
            string emptyConfig = ConfigurationHelpers.GetEmptyConfig();
            XDocument configAfterInstall = ConfigurationHelpers.InstallTransform(emptyConfig);

            XDocument configAfterUninstall = ConfigurationHelpers.UninstallTransform(configAfterInstall.ToString());

            Assert.AreEqual(0, ConfigurationHelpers.GetTelemetryInitializers(configAfterUninstall).ToList().Count);
        }

        [TestMethod]
        public void UninstallDoesNotRemovesTelemetryInitailizersTagIfCustomTelemetryInitializerPresent()
        {
            string emptyConfig = ConfigurationHelpers.GetEmptyConfig();
            XDocument configAfterInstall = ConfigurationHelpers.InstallTransform(emptyConfig);

            // Replace valid type on custom so during uninstall it should stay in the config
            string customConfig = configAfterInstall.ToString().Replace("BuildInfoConfigComponentVersionTelemetryInitializer", "blah");

            XDocument configAfterUninstall = ConfigurationHelpers.UninstallTransform(customConfig);

            Assert.AreEqual(1, ConfigurationHelpers.GetTelemetryInitializers(configAfterUninstall).ToList().Count);
            Assert.AreEqual(1, ConfigurationHelpers.GetTelemetryInitializers(configAfterUninstall).Descendants().ToList().Count);
        }
    }
}
