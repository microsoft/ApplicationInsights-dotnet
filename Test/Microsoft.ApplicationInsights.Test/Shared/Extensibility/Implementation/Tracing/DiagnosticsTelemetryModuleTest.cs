namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;
#if !NET40
    using System.Diagnostics.Tracing;
#endif
    using System.Linq;
#if NET40
    using Microsoft.Diagnostics.Tracing;
#endif
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Assert = Xunit.Assert;
#if !NET40
    using TaskEx = System.Threading.Tasks.Task;
#endif

    [TestClass]
    public class DiagnosticsTelemetryModuleTest
    {
        [TestMethod]
        public void TestModuleDefaultInitialization()
        {
            using (var initializedModule = new DiagnosticsTelemetryModule())
            {
                initializedModule.Initialize(new TelemetryConfiguration());
                
                Assert.True(string.IsNullOrEmpty(initializedModule.DiagnosticsInstrumentationKey));
                Assert.Equal("Error", initializedModule.Severity);

                Assert.Equal(1, initializedModule.Senders.Count);
                Assert.Equal(1, initializedModule.Senders.OfType<PortalDiagnosticsSender>().Count());
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

                Assert.Equal(diagnosticsInstrumentationKey, initializedModule.DiagnosticsInstrumentationKey);

                Assert.Equal(
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
                
                Assert.Equal(EventLevel.Error.ToString(), initializedModule.Severity);

                initializedModule.Severity = "Informational";

                Assert.Equal(EventLevel.Informational, initializedModule.EventListener.LogLevel);
            }
        }

        [TestMethod]
        public void TestDiagnosticModuleDoesNotThrowIfInitailizedTwice()
        {
            using (DiagnosticsTelemetryModule module = new DiagnosticsTelemetryModule())
            {
                module.Initialize(new TelemetryConfiguration());
                module.Initialize(new TelemetryConfiguration());
            }
        }

        [TestMethod]
        public void DiagnosticModuleDoesNotThrowIfQueueSenderContinuesRecieveEvents()
        {
            using (DiagnosticsTelemetryModule module = new DiagnosticsTelemetryModule())
            {
                var queueSender = module.Senders.OfType<PortalDiagnosticsQueueSender>().First();

                using (var cancellationTokenSource = new CancellationTokenSource())
                {
                    var taskStarted = new AutoResetEvent(false);
                    TaskEx.Run(() =>
                    {
                        taskStarted.Set();
                        while (!cancellationTokenSource.IsCancellationRequested)
                        {
                            queueSender.Send(new TraceEvent());
                            Thread.Sleep(1);
                        }
                    }, cancellationTokenSource.Token);

                    taskStarted.WaitOne(TimeSpan.FromSeconds(5));

                    //Assert.DoesNotThrow
                    module.Initialize(new TelemetryConfiguration());

                    cancellationTokenSource.Cancel();
                }
            }
        }
    }
}
