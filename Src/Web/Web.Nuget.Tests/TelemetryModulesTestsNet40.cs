namespace Microsoft.ApplicationInsights.Extensibility.Web
{
    using System.Linq;
    using System.Xml.Linq;
    using Microsoft.ApplicationInsights.Web;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TelemetryModulesTestsNet40
    {
        [TestMethod]
        public void InstallAddsRequestTrackingTelemetryModule()
        {
            string emptyConfig = ConfigurationHelpers.GetEmptyConfig();
            XDocument configAfterTransform = ConfigurationHelpers.InstallTransform(emptyConfig);

            var typeToFind = typeof(RequestTrackingTelemetryModule);

            var node = ConfigurationHelpers.GetTelemetryModules(configAfterTransform)
                .Descendants()
                .FirstOrDefault(
                    element => element.Attribute("Type").Value == ConfigurationHelpers.GetPartialTypeName(typeToFind));

            Assert.IsNotNull(node);
        }

        [TestMethod]
        public void InstallAddsExceptionTrackingTelemetryModule()
        {
            string emptyConfig = ConfigurationHelpers.GetEmptyConfig();
            XDocument configAfterTransform = ConfigurationHelpers.InstallTransform(emptyConfig);

            var typeToFind = typeof(ExceptionTrackingTelemetryModule);

            var node = ConfigurationHelpers.GetTelemetryModules(configAfterTransform)
                .Descendants()
                .FirstOrDefault(
                    element =>
                        (element.Attribute("Type") != null ? element.Attribute("Type").Value : null) ==
                        ConfigurationHelpers.GetPartialTypeName(typeToFind));

            Assert.IsNotNull(node);
        }

        [TestMethod]
        public void InstallAddsAspNetDiagnosticModule()
        {
            string emptyConfig = ConfigurationHelpers.GetEmptyConfig();
            XDocument configAfterTransform = ConfigurationHelpers.InstallTransform(emptyConfig);

            var typeToFind = typeof(AspNetDiagnosticTelemetryModule);

            var node = ConfigurationHelpers.GetTelemetryModules(configAfterTransform)
                .Descendants()
                .FirstOrDefault(
                    element =>
                        (element.Attribute("Type") != null ? element.Attribute("Type").Value : null) ==
                        ConfigurationHelpers.GetPartialTypeName(typeToFind));

            Assert.IsNotNull(node);
        }

        [TestMethod]
        public void UninstallRemovesAllTelemetryModules()
        {
            string emptyConfig = ConfigurationHelpers.GetEmptyConfig();
            XDocument configAfterInstall = ConfigurationHelpers.InstallTransform(emptyConfig);

            XDocument configAfterUninstall = ConfigurationHelpers.UninstallTransform(configAfterInstall.ToString());

            Assert.AreEqual(0, ConfigurationHelpers.GetTelemetryModules(configAfterUninstall).ToList().Count);
        }

        [TestMethod]
        public void UninstallDoesNotRemoveTelemetryModulesTagIfCustomTelemetryModuleIsPresent()
        {
            string emptyConfig = ConfigurationHelpers.GetEmptyConfig();
            XDocument configAfterInstall = ConfigurationHelpers.InstallTransform(emptyConfig);

            // Replace valid type on custom so during uninstall it should stay in the config
            string customConfig = configAfterInstall.ToString().Replace("ExceptionTrackingTelemetryModule", "blah");

            XDocument configAfterUninstall = ConfigurationHelpers.UninstallTransform(customConfig);

            Assert.AreEqual(1, ConfigurationHelpers.GetTelemetryModules(configAfterUninstall).ToList().Count);
            Assert.AreEqual(1, ConfigurationHelpers.GetTelemetryModules(configAfterUninstall).Descendants().ToList().Count);
        }
    }
}