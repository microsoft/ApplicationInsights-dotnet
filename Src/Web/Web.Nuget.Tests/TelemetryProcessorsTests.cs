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
        public void InstallAddsHandlerProcessor()
        {
            string emptyConfig = ConfigurationHelpers.GetEmptyConfig();
            XDocument configAfterTransform = ConfigurationHelpers.InstallTransform(emptyConfig);

            var typeToFind = typeof(HandlerTelemetryProcessor);

            var node = ConfigurationHelpers.GetTelemetryProcessors(configAfterTransform)
                .Descendants()
                .FirstOrDefault(element => (element.Attribute("Type") != null ? element.Attribute("Type").Value : null) == ConfigurationHelpers.GetPartialTypeName(typeToFind));

            Assert.IsNotNull(node);
        }

        [TestMethod]
        public void InstallAddsHandlerProcessorWithDefaultConfiguration()
        {
            string emptyConfig = ConfigurationHelpers.GetEmptyConfig();
            XDocument configAfterTransform = ConfigurationHelpers.InstallTransform(emptyConfig);

            var typeToFind = typeof(HandlerTelemetryProcessor);

            var handler = ConfigurationHelpers.GetTelemetryProcessors(configAfterTransform)
                .Descendants()
                .FirstOrDefault(element => (element.Attribute("Type") != null ? element.Attribute("Type").Value : null) == ConfigurationHelpers.GetPartialTypeName(typeToFind));

            var node = handler
                .Descendants()
                .FirstOrDefault(element => (element.Attribute("Value") != null ? element.Attribute("Value").Value : null) == "System.Web.Handlers.TransferRequestHandler");

            Assert.IsNotNull(node);

            node = handler
                .Descendants()
                .FirstOrDefault(element => (element.Attribute("Value") != null ? element.Attribute("Value").Value : null) == "Microsoft.VisualStudio.Web.PageInspector.Runtime.Tracing.RequestDataHttpHandler");

            Assert.IsNotNull(node);

            node = handler
                .Descendants()
                .FirstOrDefault(element => (element.Attribute("Value") != null ? element.Attribute("Value").Value : null) == "System.Web.StaticFileHandler");

            Assert.IsNotNull(node);

            node = handler
                .Descendants()
                .FirstOrDefault(element => (element.Attribute("Value") != null ? element.Attribute("Value").Value : null) == "System.Web.Handlers.AssemblyResourceLoader");

            Assert.IsNotNull(node);

            node = handler
                .Descendants()
                .FirstOrDefault(element => (element.Attribute("Value") != null ? element.Attribute("Value").Value : null) == "System.Web.Optimization.BundleHandler");

            Assert.IsNotNull(node);

            node = handler
                .Descendants()
                .FirstOrDefault(element => (element.Attribute("Value") != null ? element.Attribute("Value").Value : null) == "System.Web.Script.Services.ScriptHandlerFactory");

            Assert.IsNotNull(node);

            node = handler
                .Descendants()
                .FirstOrDefault(element => (element.Attribute("Value") != null ? element.Attribute("Value").Value : null) == "System.Web.Handlers.TraceHandler");

            Assert.IsNotNull(node);

            node = handler
                .Descendants()
                .FirstOrDefault(element => (element.Attribute("Value") != null ? element.Attribute("Value").Value : null) == "System.Web.Services.Discovery.DiscoveryRequestHandler");

            Assert.IsNotNull(node);

            node = handler
                .Descendants()
                .FirstOrDefault(element => (element.Attribute("Value") != null ? element.Attribute("Value").Value : null) == "System.Web.HttpDebugHandler");

            Assert.IsNotNull(node);
        }

        [TestMethod]
        public void InstallAddsAdaptiveSamplingProcessorLast()
        {
            string emptyConfig = ConfigurationHelpers.GetEmptyConfig();
            XDocument configAfterTransform = ConfigurationHelpers.InstallTransform(emptyConfig);

            var children = ConfigurationHelpers.GetTelemetryProcessors(configAfterTransform)
                .Descendants().ToList();
            var handler = children
                .FirstOrDefault(element => (element.Attribute("Type") != null ? element.Attribute("Type").Value : null) == ConfigurationHelpers.GetPartialTypeName(typeof(HandlerTelemetryProcessor)));

            var sampler = children
                .FirstOrDefault(element => (element.Attribute("Type") != null ? element.Attribute("Type").Value : null) == "Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.AdaptiveSamplingTelemetryProcessor, Microsoft.AI.ServerTelemetryChannel");
            
            var handlerIndex = children.IndexOf(handler);
            var samplerIndex = children.IndexOf(sampler);

            Assert.IsTrue(samplerIndex > handlerIndex, "AdaptiveSamplingTelemetryProcessor is not placed before HandlerTelemetryProcessor");
        }

        [TestMethod]
        public void UninstallRemovesAllTelemetryProcessorsExceptAdaptiveSampling()
        {
            string emptyConfig = ConfigurationHelpers.GetEmptyConfig();
            XDocument configAfterInstall = ConfigurationHelpers.InstallTransform(emptyConfig);

            XDocument configAfterUninstall = ConfigurationHelpers.UninstallTransform(configAfterInstall.ToString());

            var children = ConfigurationHelpers.GetTelemetryProcessors(configAfterUninstall)
                .Descendants().ToList();
            var handler = children
                .FirstOrDefault(element => (element.Attribute("Type") != null ? element.Attribute("Type").Value : null) == ConfigurationHelpers.GetPartialTypeName(typeof(HandlerTelemetryProcessor)));

            var sampler = children
                .FirstOrDefault(element => (element.Attribute("Type") != null ? element.Attribute("Type").Value : null) == "Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.AdaptiveSamplingTelemetryProcessor, Microsoft.AI.ServerTelemetryChannel");

            Assert.IsNull(handler, "HandlerTelemetryProcessor was not removed.");
            Assert.IsNotNull(sampler, "AdaptiveSamplingTelemetryProcessor was removed");
        }
    }
}
