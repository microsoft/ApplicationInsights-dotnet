using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.DependencyCollector.Implementation;
using Microsoft.ApplicationInsights.Extensibility;
#if !NETCORE
using Microsoft.ApplicationInsights.Web.TestFramework;
#endif
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.ApplicationInsights.DependencyCollector
{
    [TestClass]
    public class DependencyDiagnosticSourceListenerTests
    {
        #region Fields

        private TelemetryConfiguration configuration;
        private List<ITelemetry> sentItems;
        private DependencyDiagnosticSourceListener dependencyDiagnosticSourceListener;
        
        #endregion Fields

        #region TestInitialize

        [TestInitialize]
        public void TestInitialize()
        {
            this.configuration = new TelemetryConfiguration();
            this.sentItems = new List<ITelemetry>();
            this.configuration.TelemetryChannel = new StubTelemetryChannel { OnSend = item => this.sentItems.Add(item) };
            this.configuration.InstrumentationKey = Guid.NewGuid().ToString();
            this.dependencyDiagnosticSourceListener = new DependencyDiagnosticSourceListener(this.configuration);
        }

        [TestCleanup]
        public void Cleanup()
        {
        }

        #endregion TestInitiliaze

        #region Subscribe by convention tests

        [TestMethod]
        public void DependencyDiagnosticSourceListenerCapturesEventsByConvention()
        {
            DiagnosticListener listener = new DiagnosticListener("Test.A.Monitoring");
            Activity activity = new Activity("Test.A.Client.OutboundCall");

            Assert.IsTrue(listener.IsEnabled(), "There is a subscriber for diagnostic source following naming convention");
            Assert.IsTrue(listener.IsEnabled(activity.OperationName), "There is a subscriber for activity/event following naming convention");
            int sentCountBefore = this.sentItems.Count;

            listener.StartActivity(activity, null);

            Assert.AreEqual(sentCountBefore, this.sentItems.Count, "No telemetry item should be sent on activity start");

            activity.AddTag("operation.name", "TestName");
            listener.StopActivity(activity, null);

            Assert.AreEqual(sentCountBefore + 1, this.sentItems.Count, "One new telemetry item should be sent on activity stop");

            DependencyTelemetry telemetryItem = this.sentItems.Last() as DependencyTelemetry;
            Assert.IsNotNull(telemetryItem, "Dependency telemetry item should be sent");
            Assert.AreEqual(telemetryItem.Name, "TestName");
            // TODO more correctness tests should be added after the contract/convention is complete
        }

        [TestMethod]
        public void DependencyDiagnosticSourceListenerIgnoresEventsBySourceNameConvention()
        {
            DiagnosticListener listener = new DiagnosticListener("Test.A.NonStandard"); // does not follow convention
            Activity activity = new Activity("Test.A.Client.OutboundCall"); // does follow convention

            Assert.IsFalse(listener.IsEnabled(), "No subscriber for diagnostic source not following naming convention");
            Assert.IsFalse(listener.IsEnabled(activity.OperationName), "No subscriber for activity/event (despite following naming convention)");
            int sentCountBefore = this.sentItems.Count;

            listener.StartActivity(activity, null);
            listener.StopActivity(activity, null);

            Assert.AreEqual(sentCountBefore, this.sentItems.Count, "No telemetry item should be sent after activity stops");
        }

        [TestMethod]
        public void DependencyDiagnosticSourceListenerIgnoresEventsByActivityNameConvention()
        {
            DiagnosticListener listener = new DiagnosticListener("Test.A.Monitoring"); // does follow convention
            Activity activity = new Activity("Test.A.Client.OutboundRequest"); // does not follow convention

            Assert.IsTrue(listener.IsEnabled(), "There is a subscriber for diagnostic source following naming convention");
            Assert.IsFalse(listener.IsEnabled(activity.OperationName), "No subscriber for activity/event not following naming convention");
            int sentCountBefore = this.sentItems.Count;

            listener.StartActivity(activity, null);
            listener.StopActivity(activity, null);

            Assert.AreEqual(sentCountBefore, this.sentItems.Count, "No telemetry item should be sent after activity stops");
        }

        #endregion Subscribe by convention tests
    }
}
