namespace Microsoft.ApplicationInsights.Extensibility.Web
{
    using System;
    using System.Linq;
    using System.Xml.Linq;
    using Microsoft.ApplicationInsights.Web;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TelemetryInitailizersTestsNet40
    {
        [TestMethod]
        public void InstallAddsWebTestTelemetryInitializer()
        {
            string emptyConfig = ConfigurationHelpers.GetEmptyConfig();
            XDocument configAfterTransform = ConfigurationHelpers.InstallTransform(emptyConfig);

            Type typeToFind = typeof(WebTestTelemetryInitializer);

            Assert.IsNotNull(AssertNodeExistsInConfiguration(configAfterTransform, typeToFind));
        }

        [TestMethod]
        public void InstallAddsSyntheticUserAgentTelemetryInitializer()
        {
            string emptyConfig = ConfigurationHelpers.GetEmptyConfig();
            XDocument configAfterTransform = ConfigurationHelpers.InstallTransform(emptyConfig);

            Type typeToFind = typeof(SyntheticUserAgentTelemetryInitializer);

            Assert.IsNotNull(AssertNodeExistsInConfiguration(configAfterTransform, typeToFind));
        }

        [TestMethod]
        public void InstallAddsClientIpHeaderTelemetryInitializer()
        {
            string emptyConfig = ConfigurationHelpers.GetEmptyConfig();
            XDocument configAfterTransform = ConfigurationHelpers.InstallTransform(emptyConfig);

            Type typeToFind = typeof(ClientIpHeaderTelemetryInitializer);

            Assert.IsNotNull(AssertNodeExistsInConfiguration(configAfterTransform, typeToFind));
        }

        [TestMethod]
        public void InstallAddsOperationNameTelemetryInitializer()
        {
            string emptyConfig = ConfigurationHelpers.GetEmptyConfig();
            XDocument configAfterTransform = ConfigurationHelpers.InstallTransform(emptyConfig);

            Type typeToFind = typeof(OperationNameTelemetryInitializer);

            Assert.IsNotNull(AssertNodeExistsInConfiguration(configAfterTransform, typeToFind));
        }

        [TestMethod]
        public void InstallAddsOperationCorrelationTelemetryInitializer()
        {
            string emptyConfig = ConfigurationHelpers.GetEmptyConfig();
            XDocument configAfterTransform = ConfigurationHelpers.InstallTransform(emptyConfig);

            Type typeToFind = typeof(OperationCorrelationTelemetryInitializer);

            Assert.IsNotNull(AssertNodeExistsInConfiguration(configAfterTransform, typeToFind));
        }

        [TestMethod]
        public void InstallAddsUserTelemetryInitializerTelemetryInitializer()
        {
            string emptyConfig = ConfigurationHelpers.GetEmptyConfig();
            XDocument configAfterTransform = ConfigurationHelpers.InstallTransform(emptyConfig);

            Type typeToFind = typeof(UserTelemetryInitializer);

            Assert.IsNotNull(AssertNodeExistsInConfiguration(configAfterTransform, typeToFind));
        }

        [TestMethod]
        public void InstallAddsSessionTelemetryInitializer()
        {
            string emptyConfig = ConfigurationHelpers.GetEmptyConfig();
            XDocument configAfterTransform = ConfigurationHelpers.InstallTransform(emptyConfig);

            Type typeToFind = typeof(SessionTelemetryInitializer);

            Assert.IsNotNull(AssertNodeExistsInConfiguration(configAfterTransform, typeToFind));
        }

        [TestMethod]
        public void InstallAddsAuthenticatedUserTelemetryInitializer()
        {
            string emptyConfig = ConfigurationHelpers.GetEmptyConfig();
            XDocument configAfterTransform = ConfigurationHelpers.InstallTransform(emptyConfig);

            Type typeToFind = typeof(AuthenticatedUserIdTelemetryInitializer);

            Assert.IsNotNull(AssertNodeExistsInConfiguration(configAfterTransform, typeToFind));
        }

        [TestMethod]
        public void InstallAddsAccountIdTelemetryInitializer()
        {
            string emptyConfig = ConfigurationHelpers.GetEmptyConfig();
            XDocument configAfterTransform = ConfigurationHelpers.InstallTransform(emptyConfig);

            Type typeToFind = typeof(AccountIdTelemetryInitializer);

            Assert.IsNotNull(AssertNodeExistsInConfiguration(configAfterTransform, typeToFind));
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
            string customConfig = configAfterInstall.ToString().Replace("SessionTelemetryInitializer", "blah");

            XDocument configAfterUninstall = ConfigurationHelpers.UninstallTransform(customConfig);

            Assert.AreEqual(1, ConfigurationHelpers.GetTelemetryInitializers(configAfterUninstall).ToList().Count);
            Assert.AreEqual(1, ConfigurationHelpers.GetTelemetryInitializers(configAfterUninstall).Descendants().ToList().Count);
        }

        private static XElement AssertNodeExistsInConfiguration(XDocument configAfterTransform, Type typeToFind)
        {
            return ConfigurationHelpers.GetTelemetryInitializers(configAfterTransform)
                                           .Descendants()
                                           .FirstOrDefault(
                                               element =>
                                               (element.Attribute("Type") != null ? element.Attribute("Type").Value : null) ==
                                               ConfigurationHelpers.GetPartialTypeName(typeToFind));
        }
    }
}