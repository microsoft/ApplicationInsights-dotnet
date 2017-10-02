using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.DependencyCollector.Implementation;
using Microsoft.ApplicationInsights.Extensibility;
#if !NETCORE
using Microsoft.ApplicationInsights.Web.TestFramework;
#else
using Microsoft.ApplicationInsights.Tests;
#endif
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.ApplicationInsights.DependencyCollector
{
    [TestClass]
    public class TelemetryDiagnosticSourceListenerTests
    {
        #region Fields

        private TelemetryConfiguration configuration;
        private List<ITelemetry> sentItems;
        
        #endregion Fields

        #region TestInitialize

        [TestInitialize]
        public void TestInitialize()
        {
            this.configuration = new TelemetryConfiguration();
            this.sentItems = new List<ITelemetry>();
            this.configuration.TelemetryChannel = new StubTelemetryChannel { OnSend = item => this.sentItems.Add(item) };
            this.configuration.InstrumentationKey = Guid.NewGuid().ToString();
        }

        #endregion TestInitiliaze

        #region Subscribtion tests

        [TestMethod]
        public void TelemetryDiagnosticSourceListenerCapturesAllActivitiesByDefault()
        {
            using (new TelemetryDiagnosticSourceListener(this.configuration, null))
            {
                DiagnosticListener listener = new DiagnosticListener("Test.A");
                Activity activity = new Activity("Test.A.Client.Monitoring");

                Assert.IsTrue(listener.IsEnabled(), "There is a subscriber for a new diagnostic source");
                Assert.IsTrue(listener.IsEnabled(activity.OperationName), "There is a subscriber for a new activity");
                Assert.IsTrue(listener.IsEnabled(activity.OperationName + TelemetryDiagnosticSourceListener.ActivityStopNameSuffix),
                    "There is a subscriber for new activity Stop event");
                Assert.IsFalse(listener.IsEnabled(activity.OperationName + TelemetryDiagnosticSourceListener.ActivityStartNameSuffix),
                    "There are no subscribers for new activity Start event");

                int sentCountBefore = this.sentItems.Count;

                listener.StartActivity(activity, null);
                Assert.AreEqual(sentCountBefore, this.sentItems.Count, "No telemetry item should be sent on activity start");

                listener.StopActivity(activity, null);
                Assert.AreEqual(sentCountBefore + 1, this.sentItems.Count, "One new telemetry item should be sent on activity stop");

                DependencyTelemetry telemetryItem = this.sentItems.Last() as DependencyTelemetry;
                Assert.IsNotNull(telemetryItem, "Dependency telemetry item should be sent");
                Assert.AreEqual(activity.OperationName, telemetryItem.Name);
            }
        }

        [TestMethod]
        public void TelemetryDiagnosticSourceListenerIgnoresExcludedSources()
        {
            var exclusionList = new[] { "Test.A" }.ToList();
            using (new TelemetryDiagnosticSourceListener(this.configuration, exclusionList))
            {
                // Diagnostic Source A is ignored
                DiagnosticListener listenerA = new DiagnosticListener("Test.A");
                Activity activityA = new Activity("Test.A.Client.Monitoring");

                Assert.IsFalse(listenerA.IsEnabled(), "There are no subscribers for excluded diagnostic source A");
                Assert.IsFalse(listenerA.IsEnabled(activityA.OperationName), "There are no subscribers for activity A");

                int sentCountBefore = this.sentItems.Count;

                listenerA.StartActivity(activityA, null);
                Assert.AreEqual(sentCountBefore, this.sentItems.Count, "No telemetry item should be sent on activity start");

                listenerA.StopActivity(activityA, null);
                Assert.AreEqual(sentCountBefore, this.sentItems.Count, "No telemetry item should be sent on activity stop");

                // Diagnostic Source B is still captured
                DiagnosticListener listenerB = new DiagnosticListener("Test.B");
                Activity activityB = new Activity("Test.B.Client.Monitoring");

                Assert.IsTrue(listenerB.IsEnabled(), "There is a subscriber for diagnostic source B");
                Assert.IsTrue(listenerB.IsEnabled(activityB.OperationName), "There is a subscriber for activity B");

                listenerB.StartActivity(activityB, null);
                Assert.AreEqual(sentCountBefore, this.sentItems.Count, "No telemetry item should be sent on activity start");

                listenerB.StopActivity(activityB, null);
                Assert.AreEqual(sentCountBefore + 1, this.sentItems.Count, "One new telemetry item should be sent on activity B stop");

                DependencyTelemetry telemetryItem = this.sentItems.Last() as DependencyTelemetry;
                Assert.IsNotNull(telemetryItem, "Dependency telemetry item should be sent");
                Assert.AreEqual(telemetryItem.Name, activityB.OperationName);
            }
        }

        [TestMethod]
        public void TelemetryDiagnosticSourceListenerIgnoresExcludedActivities()
        {
            var exclusionList = new[] { "Test.A:Test.A.Activity1", "Test.A:Test.A.Activity2" }.ToList();
            using (new TelemetryDiagnosticSourceListener(this.configuration, exclusionList))
            {
                // Diagnostic Source is not ignored
                DiagnosticListener listener = new DiagnosticListener("Test.A");

                Assert.IsTrue(listener.IsEnabled(), "There is a subscriber for diagnostic source");

                // Activity1 is ignored per exclusion
                Activity activity1 = new Activity("Test.A.Activity1");
                Assert.IsFalse(listener.IsEnabled(activity1.OperationName), "There are no subscribers for activity 1");

                int sentCountBefore = this.sentItems.Count;

                listener.StartActivity(activity1, null);
                Assert.AreEqual(sentCountBefore, this.sentItems.Count, "No telemetry item should be sent on activity 1 start");

                listener.StopActivity(activity1, null);
                Assert.AreEqual(sentCountBefore, this.sentItems.Count, "No telemetry item should be sent on activity 1 stop");

                // Activity2 is ignored per exclusion
                Activity activity2 = new Activity("Test.A.Activity2");
                Assert.IsFalse(listener.IsEnabled(activity2.OperationName), "There are no subscribers for activity 2");

                listener.StartActivity(activity2, null);
                Assert.AreEqual(sentCountBefore, this.sentItems.Count, "No telemetry item should be sent on activity 2 start");

                listener.StopActivity(activity2, null);
                Assert.AreEqual(sentCountBefore, this.sentItems.Count, "No telemetry item should be sent on activity 2 stop");

                // non-excluded activity from same diagnostic source is captured
                Activity activity = new Activity("Test.A.Client.Monitoring");
                Assert.IsTrue(listener.IsEnabled(activity.OperationName), "There is a subscriber for activity");

                listener.StartActivity(activity, null);
                Assert.AreEqual(sentCountBefore, this.sentItems.Count, "No telemetry item should be sent on activity start");

                listener.StopActivity(activity, null);
                Assert.AreEqual(sentCountBefore + 1, this.sentItems.Count, "One new telemetry item should be sent on activity stop");

                DependencyTelemetry telemetryItem = this.sentItems.Last() as DependencyTelemetry;
                Assert.IsNotNull(telemetryItem, "Dependency telemetry item should be sent");
                Assert.AreEqual(telemetryItem.Name, activity.OperationName);
            }
        }

        #endregion Subscribtion tests

        #region Collection tests

        [TestMethod]
        public void TelemetryDiagnosticSourceListenerCollectsTelemetryFromRawActivity()
        {
            using (new TelemetryDiagnosticSourceListener(this.configuration, null))
            {
                DiagnosticListener listener = new DiagnosticListener("Test.A");

                // generic example
                var tags = new Dictionary<string, string>()
                {
                    ["custom.tag"] = "test"
                };

                DependencyTelemetry telemetryItem = CollectDependencyTelemetryFromActivity(listener, tags);

                Assert.AreEqual(telemetryItem.Name, "Test.A.Client.Monitoring"); // Activity name
                Assert.AreEqual(telemetryItem.Type, listener.Name);
                Assert.IsTrue(string.IsNullOrEmpty(telemetryItem.Data));
                Assert.IsTrue(string.IsNullOrEmpty(telemetryItem.Target));
                Assert.AreEqual(telemetryItem.Success, true);
                Assert.IsTrue(telemetryItem.Properties.ContainsKey("custom.tag"));
                Assert.AreEqual(telemetryItem.Properties["custom.tag"], tags["custom.tag"]);
            }
        }

        private DependencyTelemetry CollectDependencyTelemetryFromActivity(DiagnosticListener listener, Dictionary<string, string> tags)
        {
            Activity activity = new Activity("Test.A.Client.Monitoring");

            foreach (var tag in tags)
            {
                activity.AddTag(tag.Key, tag.Value);
            }

            listener.StartActivity(activity, null);
            listener.StopActivity(activity, null);

            DependencyTelemetry telemetryItem = this.sentItems.Last() as DependencyTelemetry;
            return telemetryItem;
        }

        #endregion Collection tests
    }
}
