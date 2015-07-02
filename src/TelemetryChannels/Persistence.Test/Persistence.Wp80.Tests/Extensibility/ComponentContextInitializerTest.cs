namespace Microsoft.ApplicationInsights.Extensibility
{
    using System;
    using System.Xml.Linq;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
    using Assert = Xunit.Assert;
    
    /// <summary>
    /// Silverlight-specific tests for <see cref="ComponentContextInitializer"/>.
    /// </summary>
    public partial class ComponentContextInitializerTest
    {
        [TestMethod]
        public void ReadingVersionWithNoManifestYieldsDefaultValue()
        {
            ComponentContextInitializer source = new ComponentContextInitializer();
            var telemetryContext = new TelemetryContext();

            Assert.Null(telemetryContext.Component.Version);

            using (new ManifestSaver())
            {
                ComponentContextReader.Instance = new TestComponentContextReader(null);
                source.Initialize(telemetryContext);
            }

            Assert.Equal(ComponentContextReader.UnknownComponentVersion, telemetryContext.Component.Version);
        }

        [TestMethod]
        public void ReadingVersionWithNoIdentityElementYieldsDefaultValue()
        {
            ComponentContextInitializer source = new ComponentContextInitializer();
            var telemetryContext = new TelemetryContext();

            Assert.Null(telemetryContext.Component.Version);

            using (new ManifestSaver())
            {
                XElement root = new XElement(XName.Get("Deployment", "http://schemas.microsoft.com/windowsphone/2012/deployment"));
                ComponentContextReader.Instance = new TestComponentContextReader(root);
                source.Initialize(telemetryContext);
            }

            Assert.Equal(ComponentContextReader.UnknownComponentVersion, telemetryContext.Component.Version);
        }

        [TestMethod]
        public void ReadingVersionWithWrongIdentityElementNamespaceYieldsDefaultValue()
        {
            ComponentContextInitializer source = new ComponentContextInitializer();
            var telemetryContext = new TelemetryContext();

            Assert.Null(telemetryContext.Component.Version);

            using (new ManifestSaver())
            {
                XElement root = new XElement(XName.Get("Deployment", "http://schemas.microsoft.com/windowsphone/2012/deployment"));
                root.Add(new XElement(XName.Get("App", "http://tempuri.org")));
                ComponentContextReader.Instance = new TestComponentContextReader(root);
                source.Initialize(telemetryContext);
            }

            Assert.Equal(ComponentContextReader.UnknownComponentVersion, telemetryContext.Component.Version);
        }

        [TestMethod]
        public void ReadingVersionWithNoVersionAttributeYieldsDefaultValue()
        {
            ComponentContextInitializer source = new ComponentContextInitializer();
            var telemetryContext = new TelemetryContext();

            Assert.Null(telemetryContext.Component.Version);

            using (new ManifestSaver())
            {
                XElement root = new XElement(XName.Get("Deployment", "http://schemas.microsoft.com/windowsphone/2012/deployment"));
                root.Add(new XElement(XName.Get("App", string.Empty)));
                ComponentContextReader.Instance = new TestComponentContextReader(root);
                source.Initialize(telemetryContext);
            }

            Assert.Equal(ComponentContextReader.UnknownComponentVersion, telemetryContext.Component.Version);
        }

        private class TestComponentContextReader : 
            ComponentContextReader
        {
            private XElement manifest;

            public TestComponentContextReader(XElement rootElement)
            {
                this.manifest = rootElement;
            }

            internal override XElement Manifest
            {
                get
                {
                    return this.manifest;
                }
            }
        }

        private class ManifestSaver :
            IDisposable
        {
            private readonly IComponentContextReader savedReader;

            public ManifestSaver()
            {
                this.savedReader = ComponentContextReader.Instance;
            }

            public void Dispose()
            {
                ComponentContextReader.Instance = this.savedReader;
            }
        }
    }
}
