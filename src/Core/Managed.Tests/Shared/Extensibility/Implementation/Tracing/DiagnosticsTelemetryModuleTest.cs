namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;
#if CORE_PCL || NET45 || WINRT
    using System.Diagnostics.Tracing;
#endif
    using System.Linq;
#if NET35 || NET40
    using Microsoft.Diagnostics.Tracing;
#endif
#if WINDOWS_PHONE || WINDOWS_STORE
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#else
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
    using Mocks;

    [TestClass]
    public class DiagnosticsTelemetryModuleTest
    {
        [TestMethod]
        public void TestModuleDefaultInitialization()
        {
            using (var initializedModule = new DiagnosticsTelemetryModuleMock())
            {
                initializedModule.Initialize(new TelemetryConfiguration());
                
                Assert.IsTrue(string.IsNullOrEmpty(initializedModule.DiagnosticsInstrumentationKey));
                Assert.AreEqual("Error", initializedModule.Severity);

                Assert.AreEqual(2, initializedModule.ModuleSenders.Count);
                Assert.AreEqual(1, initializedModule.ModuleSenders.OfType<PortalDiagnosticsSender>().Count());
                Assert.AreEqual(1, initializedModule.ModuleSenders.OfType<F5DiagnosticsSender>().Count());
            }
        }

        [TestMethod]
        public void TestDiagnosticsModuleSetInstrumentationKey()
        {
            var diagnosticsInstrumentationKey = Guid.NewGuid().ToString();
            using (var initializedModule = new DiagnosticsTelemetryModuleMock())
            {
                initializedModule.Initialize(new TelemetryConfiguration());
                initializedModule.DiagnosticsInstrumentationKey = diagnosticsInstrumentationKey;

                Assert.AreEqual(diagnosticsInstrumentationKey, initializedModule.DiagnosticsInstrumentationKey);

                Assert.AreEqual(
                    diagnosticsInstrumentationKey,
                    initializedModule.ModuleSenders.OfType<PortalDiagnosticsSender>().First().DiagnosticsInstrumentationKey);
            }
        }

        [TestMethod]
        public void TestDiagnosticsModuleSetSeverity()
        {
            using (var initializedModule = new DiagnosticsTelemetryModuleMock())
            {
                initializedModule.Initialize(new TelemetryConfiguration());
                
                Assert.AreEqual(EventLevel.Error.ToString(), initializedModule.Severity);

                initializedModule.Severity = "Informational";

                Assert.AreEqual(EventLevel.Informational, initializedModule.ModuleListener.LogLevel);
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
