namespace Microsoft.ApplicationInsights
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.XPath;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Web.XmlTransform;
    using Assert = Xunit.Assert;

    [TestClass]
    public class TelemetryInitializerTransformTests
    {
        private const string AppInsightsNamespace = "http://schemas.microsoft.com/ApplicationInsights/2013/Settings";
        private const string HttpTransformTelemetryInitializer = "Microsoft.ApplicationInsights.DependencyCollector.HttpDependenciesParsingTelemetryInitializer, Microsoft.AI.DependencyCollector";

        private readonly XElement baseDocument;

        public TelemetryInitializerTransformTests()
        {
            this.baseDocument = new XElement(XName.Get("ApplicationInsights", TelemetryInitializerTransformTests.AppInsightsNamespace));
            this.baseDocument.Add(new XElement(XName.Get("Foo", "http://tempuri.org")));
        }

        [TestMethod]
        public void VerifyTelemetryInitializerInstall()
        {
            this.TelemetryInitializerInstall(this.baseDocument.ToString(), HttpTransformTelemetryInitializer);
        }

        [TestMethod]
        public void VerifyTelemetryInitializerUninstall()
        {
            this.TelemetryInitializerUninstall(this.baseDocument.ToString());
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "Disposing of the stream multiple times will not be harmful int he context of a test")]
        protected void TelemetryInitializerInstall(string sourceDocument, params string[] telemetryInitializerTypes)
        {
            string resourceName = "Microsoft.ApplicationInsights.Resources.ApplicationInsights.config.install.xdt";
            Stream stream = typeof(ModuleTransformTests).Assembly.GetManifestResourceStream(resourceName);
            using (StreamReader reader = new StreamReader(stream))
            {
                string transform = reader.ReadToEnd();
                XmlTransformation transformation = new XmlTransformation(transform, false, null);

                XmlDocument targetDocument = new XmlDocument();
                targetDocument.LoadXml(sourceDocument);
                transformation.Apply(targetDocument);

                XmlNamespaceManager manager = new XmlNamespaceManager(new NameTable());
                manager.AddNamespace("ai", AppInsightsNamespace);
                int moduleIndex = 0;
                foreach (XPathNavigator module in targetDocument.CreateNavigator().Select("/ai:ApplicationInsights/ai:TelemetryInitializers/ai:Add/@Type", manager))
                {
                    string contextInitializerType = telemetryInitializerTypes[moduleIndex++];
                    Assert.Equal(module.Value, contextInitializerType);
                }

                Assert.Equal(moduleIndex, telemetryInitializerTypes.Length);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "Disposing of the stream multiple times will not be harmful int he context of a test")]
        protected void TelemetryInitializerUninstall(string sourceDocument)
        {
            string resourceName = "Microsoft.ApplicationInsights.Resources.ApplicationInsights.config.install.xdt";
            string intermediaryDocument;

            Assembly assembly = typeof(ModuleTransformTests).Assembly;

            Stream stream = assembly.GetManifestResourceStream(resourceName);
            using (StreamReader reader = new StreamReader(stream))
            {
                string transform = reader.ReadToEnd();
                XmlTransformation transformation = new XmlTransformation(transform, false, null);

                XmlDocument targetDocument = new XmlDocument();
                targetDocument.LoadXml(sourceDocument);
                transformation.Apply(targetDocument);
                intermediaryDocument = targetDocument.OuterXml;
            }

            resourceName = "Microsoft.ApplicationInsights.Resources.ApplicationInsights.config.uninstall.xdt";
            stream = assembly.GetManifestResourceStream(resourceName);
            using (StreamReader reader = new StreamReader(stream))
            {
                string transform = reader.ReadToEnd();
                XmlTransformation transformation = new XmlTransformation(transform, false, null);

                XmlDocument targetDocument = new XmlDocument();
                targetDocument.LoadXml(intermediaryDocument);
                transformation.Apply(targetDocument);

                string uninstalledDocument = targetDocument.OuterXml;

                XElement cleanDocument = XElement.Parse(sourceDocument);
                XElement dirtyDocument = XElement.Parse(uninstalledDocument);
                Assert.True(XNode.DeepEquals(cleanDocument, dirtyDocument));
            }
        }
    }
}
