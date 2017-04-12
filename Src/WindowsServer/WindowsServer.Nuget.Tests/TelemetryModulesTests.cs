namespace WindowsServer.Nuget.Tests
{
    using System;
    using System.Linq;
    using System.Xml.Linq;
    using Microsoft.ApplicationInsights.WindowsServer;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TelemetryModulesTests
    {
        private const string DiagnosticsModuleName = "Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing.DiagnosticsTelemetryModule, Microsoft.ApplicationInsights";

        [TestMethod]
        public void InstallAddsDeveloperModeWithDebuggerAttachedTelemetryModule()
        {
            string emptyConfig = ConfigurationHelpers.GetEmptyConfig();
            XDocument configAfterTransform = ConfigurationHelpers.InstallTransform(emptyConfig);

            Type typeToFind = typeof(DeveloperModeWithDebuggerAttachedTelemetryModule);

            var node = ConfigurationHelpers.GetTelemetryModules(configAfterTransform)
                .Descendants()
                .FirstOrDefault(element => element.Attribute("Type").Value == ConfigurationHelpers.GetPartialTypeName(typeToFind));
            
            Assert.IsNotNull(node);
        }

        [TestMethod]
        public void InstallAddsUnhandledExceptionTelemetryModule()
        {
            string emptyConfig = ConfigurationHelpers.GetEmptyConfig();
            XDocument configAfterTransform = ConfigurationHelpers.InstallTransform(emptyConfig);

            Type typeToFind = typeof(UnhandledExceptionTelemetryModule);

            var node = ConfigurationHelpers.GetTelemetryModules(configAfterTransform)
                .Descendants()
                .FirstOrDefault(element => element.Attribute("Type").Value == ConfigurationHelpers.GetPartialTypeName(typeToFind));

            Assert.IsNotNull(node);
        }

        [TestMethod]
        public void InstallAddsFirstChanceExceptionTelemetryModule()
        {
            string emptyConfig = ConfigurationHelpers.GetEmptyConfig();
            XDocument configAfterTransform = ConfigurationHelpers.InstallTransform(emptyConfig);

            Type typeToFind = typeof(FirstChanceExceptionStatisticsTelemetryModule);

            var node = ConfigurationHelpers.GetTelemetryModules(configAfterTransform)
                .Descendants()
                .FirstOrDefault(element => element.Attribute("Type").Value == ConfigurationHelpers.GetPartialTypeName(typeToFind));

            Assert.IsNotNull(node);
        }

        [TestMethod]
        public void InstallDoesNotInstallExtraModules()
        {
            string emptyConfig = ConfigurationHelpers.GetEmptyConfig();
            XDocument configAfterTransform = ConfigurationHelpers.InstallTransform(emptyConfig);

            Type typeToFind = typeof(FirstChanceExceptionStatisticsTelemetryModule);

            Assert.AreEqual(4, ConfigurationHelpers.GetTelemetryModules(configAfterTransform).Descendants().Count());
        }

        [TestMethod]
        public void InstallAddsUnobservedExceptionTelemetryModule()
        {
            string emptyConfig = ConfigurationHelpers.GetEmptyConfig();
            XDocument configAfterTransform = ConfigurationHelpers.InstallTransform(emptyConfig);

            Type typeToFind = typeof(UnobservedExceptionTelemetryModule);

            var node = ConfigurationHelpers.GetTelemetryModules(configAfterTransform)
                .Descendants()
                .FirstOrDefault(element => element.Attribute("Type").Value == ConfigurationHelpers.GetPartialTypeName(typeToFind));

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
            string customConfig = configAfterInstall.ToString().Replace("DeveloperModeWithDebuggerAttachedTelemetryModule", "blah");

            XDocument configAfterUninstall = ConfigurationHelpers.UninstallTransform(customConfig);

            Assert.AreEqual(1, ConfigurationHelpers.GetTelemetryModules(configAfterUninstall).ToList().Count);
            Assert.AreEqual(1, ConfigurationHelpers.GetTelemetryModules(configAfterUninstall).Descendants().ToList().Count);
        }
    }
}
