#pragma warning disable 612, 618  // obsolete TelemetryConfigration.Active
namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Tracing;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Tracing.Mocks;

    [TestClass]
    public class DiagnosticsListenerTest
    {
        [TestMethod]
        public void TestConstructorThrowsArgumentException()
        {
            bool failedWithExpectedException = false;
            try
            {
                using (var listener = new DiagnosticsListener(null))
                {
                    // nop
                }
            }
            catch (ArgumentNullException)
            {
                failedWithExpectedException = true;
            }

            Assert.IsTrue(failedWithExpectedException);
        }

        [TestMethod]
        public void TestEventSending()
        {
            var senderMock = new DiagnosticsSenderMock();
            var senders = new List<IDiagnosticsSender> { senderMock };
            using (var listener = new DiagnosticsListener(senders))
            {
                listener.LogLevel = EventLevel.Verbose;
                CoreEventSource.Log.LogVerbose("failure");
            }

            Assert.AreEqual(1, senderMock.Messages.Count);
            Assert.AreEqual("[msg=Log verbose];[msg=failure]", senderMock.Messages[0]);
        }

        [TestMethod]
        public void TestListenerWithDifferentSeverity()
        {
            // Ensure there are no left-over DiagnosticTelemetryModules
            // from previous tests that will mess up this one.
            TelemetryConfiguration.Active.Dispose();
            var modules = TelemetryModules.Instance.Modules;
            if (modules != null)
            {
                foreach (var module in modules)
                {
                    (module as IDisposable)?.Dispose();
                }

                modules.Clear();
            }

            var senderMock = new DiagnosticsSenderMock();
            var senders = new List<IDiagnosticsSender> { senderMock };
            using (var listener = new DiagnosticsListener(senders))
            {
                listener.LogLevel = EventLevel.Informational;

                CoreEventSource.Log.LogVerbose("Some verbose tracing");
                Assert.AreEqual(0, senderMock.Messages.Count);

                CoreEventSource.Log.DiagnosticsEventThrottlingHasBeenResetForTheEvent(10, 1);
                Assert.AreEqual(1, senderMock.Messages.Count);

                senderMock.Messages.Clear();

                listener.LogLevel = EventLevel.Verbose;
                CoreEventSource.Log.LogVerbose("Some verbose tracing");
                Assert.AreEqual(1, senderMock.Messages.Count);

                senderMock.Messages.Clear();

                CoreEventSource.Log.DiagnosticsEventThrottlingHasBeenResetForTheEvent(10, 1);
                Assert.AreEqual(1, senderMock.Messages.Count);

                senderMock.Messages.Clear();

                listener.LogLevel = EventLevel.Error;
                CoreEventSource.Log.LogError("Logging an error");

                // If you see the following assert fail, it's because another test has
                // leaked a DiagnosticsTelemetryModule (via TelemetryConfiguration.Active
                // for example). There will be a DiagnosticEventListener which forwards
                // error messages to a PortalDiagnosticsSender. That listener is still
                // getting events from EventSources. We send an Error event, which goes
                // not only to our listener here, but also to the leaked one. The event
                // gets forwarded to the PortalDiagnosticsSender. That turns the event
                // into TraceTelemetry and tries to transmit it. Since we set the event
                // level to verbose earlier, RichPayloadEventSource is is enabled and
                // it writes the TraceTelemetry to all listeners (including us).
                // Unfortuantely, the TraceTelemetry contains a nullable field which
                // triggers a known .NET Core 2.0 bug inside WriteEvent. The internal
                // exception gets reported as an error from WriteEvent resulting in another
                // event with EventLevel.Error being reported here.
                Assert.AreEqual(1, senderMock.Messages.Count);
            }
        }

        [TestMethod]
        public void TestEventSourceLogLevelWhenEventSourceIsAlreadyCreated()
        {
            using (var testEventSource = new TestEventSource())
            {
                var senderMock = new DiagnosticsSenderMock();
                var senders = new List<IDiagnosticsSender> { senderMock };
                using (var listener = new DiagnosticsListener(senders))
                {
                    const EventKeywords AllKeyword = (EventKeywords)(-1);
                    // The default level is EventLevel.Error
                    Assert.IsTrue(testEventSource.IsEnabled(EventLevel.Error, AllKeyword));

                    // So Verbose should not be enabled
                    Assert.IsFalse(testEventSource.IsEnabled(EventLevel.Verbose, AllKeyword));

                    listener.LogLevel = EventLevel.Verbose;
                    Assert.IsTrue(testEventSource.IsEnabled(EventLevel.Verbose, AllKeyword));
                }
            }
        }

        [EventSource(Name = "Microsoft-ApplicationInsights-" + nameof(TestEventSource))]
        private class TestEventSource : EventSource
        {
        }
    }
}
#pragma warning restore 612, 618  // obsolete TelemetryConfigration.Active