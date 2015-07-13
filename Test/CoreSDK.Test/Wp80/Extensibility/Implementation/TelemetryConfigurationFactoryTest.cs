namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Platform;
    using Microsoft.ApplicationInsights.TestFramework;
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;

    [TestClass]
    public class TelemetryConfigurationFactoryTest
    {
        [TestMethod]
        public void InitializingTelemetryConfigurationWithoutAnIkey()
        {
            string profile = Configuration("<InstrumentationKey>    </InstrumentationKey>");
            PlatformSingleton.Current = new StubPlatform { OnReadConfigurationXml = () => { return profile; } };

            var configuration = new TelemetryConfiguration();
            TelemetryConfigurationFactory.Instance.Initialize(configuration);

            Assert.IsTrue(string.IsNullOrEmpty(configuration.InstrumentationKey));
        }

        [TestMethod]
        public void InitializingTelemetryConfigurationWithAnIkey()
        {
            string expected = Guid.NewGuid().ToString();
            string profile = Configuration("<InstrumentationKey>" + expected + "</InstrumentationKey>");
            PlatformSingleton.Current = new StubPlatform { OnReadConfigurationXml = () => { return profile; } };

            var configuration = new TelemetryConfiguration();
            TelemetryConfigurationFactory.Instance.Initialize(configuration);

            Assert.AreEqual(expected.ToString(), configuration.InstrumentationKey);
        }

        [TestMethod]
        public void InitializingEmptyConfigurationAddsCoreSdkDefaultComponents()
        {
            var configuration = new TelemetryConfiguration();
            TelemetryConfigurationFactory.Instance.Initialize(configuration);

            Assert.AreEqual(1, configuration.ContextInitializers.Count);
            Assert.AreEqual(1, configuration.TelemetryInitializers.Count);
            Assert.IsInstanceOfType(configuration.TelemetryChannel, typeof(InMemoryChannel));
        }

        private static string Configuration(string innerXml)
        {
            return
              @"<?xml version=""1.0"" encoding=""utf-8"" ?>
                <ApplicationInsights xmlns=""http://schemas.microsoft.com/ApplicationInsights/2013/Settings"">
" + innerXml + @"
                </ApplicationInsights>";
        }
    }
}
