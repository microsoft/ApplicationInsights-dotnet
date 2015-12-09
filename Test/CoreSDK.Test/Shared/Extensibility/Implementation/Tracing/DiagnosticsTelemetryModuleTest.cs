namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;
#if CORE_PCL || NET45 || NET46
    using System.Diagnostics.Tracing;
#endif
    using System.Linq;
#if NET40
    using Microsoft.Diagnostics.Tracing;
#endif
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DiagnosticsTelemetryModuleTest
    {
        [TestMethod]
        public void TestModuleDefaultInitialization()
        {
            using (var initializedModule = new DiagnosticsTelemetryModule())
            {
                initializedModule.Initialize(new TelemetryConfiguration());
                
                Assert.IsTrue(string.IsNullOrEmpty(initializedModule.DiagnosticsInstrumentationKey));
                Assert.AreEqual("Error", initializedModule.Severity);

                Assert.AreEqual(2, initializedModule.Senders.Count);
                Assert.AreEqual(1, initializedModule.Senders.OfType<PortalDiagnosticsSender>().Count());
                Assert.AreEqual(1, initializedModule.Senders.OfType<F5DiagnosticsSender>().Count());
            }
        }

        [TestMethod]
        public void TestDiagnosticsModuleSetInstrumentationKey()
        {
            var diagnosticsInstrumentationKey = Guid.NewGuid().ToString();
            using (var initializedModule = new DiagnosticsTelemetryModule())
            {
                initializedModule.Initialize(new TelemetryConfiguration());
                initializedModule.DiagnosticsInstrumentationKey = diagnosticsInstrumentationKey;

                Assert.AreEqual(diagnosticsInstrumentationKey, initializedModule.DiagnosticsInstrumentationKey);

                Assert.AreEqual(
                    diagnosticsInstrumentationKey,
                    initializedModule.Senders.OfType<PortalDiagnosticsSender>().First().DiagnosticsInstrumentationKey);
            }
        }

        [TestMethod]
        public void TestDiagnosticsModuleSetSeverity()
        {
            using (var initializedModule = new DiagnosticsTelemetryModule())
            {
                initializedModule.Initialize(new TelemetryConfiguration());
                
                Assert.AreEqual(EventLevel.Error.ToString(), initializedModule.Severity);

                initializedModule.Severity = "Informational";

                Assert.AreEqual(EventLevel.Informational, initializedModule.EventListener.LogLevel);
            }
        }

        [TestMethod]
        public void TestDiagnosticModuleDoesNotThrowIfInitailizedTwice()
        {
            DiagnosticsTelemetryModule module = new DiagnosticsTelemetryModule();
            module.Initialize(TelemetryConfiguration.Active);
            module.Initialize(TelemetryConfiguration.Active);
        }
    }
}
