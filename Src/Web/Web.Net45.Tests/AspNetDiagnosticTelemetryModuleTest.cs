namespace Microsoft.ApplicationInsights
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Web;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Web;
    using Microsoft.ApplicationInsights.Web.Helpers;
    using Microsoft.ApplicationInsights.Web.TestFramework;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class AspNetDiagnosticTelemetryModuleTest : IDisposable
    {
        private FakeAspNetDiagnosticSource aspNetDiagnosticsSource;
        private TelemetryConfiguration configuration;
        private IList<ITelemetry> sendItems;
        private AspNetDiagnosticTelemetryModule module;

        [TestInitialize]
        public void TestInit()
        {
            this.aspNetDiagnosticsSource = new FakeAspNetDiagnosticSource();
            this.sendItems = new List<ITelemetry>();
            var stubTelemetryChannel = new StubTelemetryChannel { OnSend = item => this.sendItems.Add(item) };
            this.configuration = new TelemetryConfiguration
            {
                InstrumentationKey = Guid.NewGuid().ToString(),
                TelemetryChannel = stubTelemetryChannel
            };
        }

        [TestCleanup]
        public void Cleanup()
        {
            this.Dispose(true);
            while (Activity.Current != null)
            {
                Activity.Current.Stop();
            }
        }

        [TestMethod]
        public void InitializeWithoutRequestAndExceptionModulesMakesModuleNoop()
        {
            this.module = new AspNetDiagnosticTelemetryModule();
            this.module.Initialize(this.configuration);

            this.aspNetDiagnosticsSource.StartActivity();
            Assert.AreEqual(0, this.sendItems.Count);
        }

        [TestMethod]
        public void IsEnabledWithNullActivityDoesNotThrow()
        {
            this.module = this.CreateModule();

            Assert.IsTrue(this.aspNetDiagnosticsSource.IsEnabled(FakeAspNetDiagnosticSource.IncomingRequestEventName));
            Assert.AreEqual(0, this.sendItems.Count);
        }

        [TestMethod]
        public void BeginEndRequestReportsTelemetry()
        {
            this.module = this.CreateModule();

            Assert.IsTrue(this.aspNetDiagnosticsSource.StartActivity());
            this.aspNetDiagnosticsSource.StopActivity();
            Assert.AreEqual(1, this.sendItems.Count);
        }

        [TestMethod]
        public void ChildTelemetryIsReportedProperlyBetweenBeginEndRequest()
        {
            this.module = this.CreateModule();

            this.aspNetDiagnosticsSource.StartActivity();

            var trace = new TraceTelemetry();
            var client = new TelemetryClient(this.configuration);
            client.TrackTrace(trace);

            this.aspNetDiagnosticsSource.StopActivity();
            Assert.AreEqual(2, this.sendItems.Count);

            var requestTelemetry = this.sendItems[0] as RequestTelemetry ?? this.sendItems[1] as RequestTelemetry;
            Assert.IsNotNull(requestTelemetry);

            Assert.AreEqual(requestTelemetry.Id, trace.Context.Operation.ParentId);
            Assert.AreEqual(requestTelemetry.Context.Operation.Id, trace.Context.Operation.Id);
        }

        [TestMethod]
        public void GrandChildTelemetryIsReportedProperlyBetweenBeginEndRequestWhenActivityIsLost()
        {
            this.module = this.CreateModule();

            this.aspNetDiagnosticsSource.StartActivity();

            var activity = Activity.Current;
            Activity.Current.Stop();
            Assert.IsNull(Activity.Current);

            this.aspNetDiagnosticsSource.RestoreLostActivity(activity);
            var restoredActivity = Activity.Current;

            var trace = new TraceTelemetry();
            var client = new TelemetryClient(this.configuration);
            client.TrackTrace(trace);

            this.aspNetDiagnosticsSource.ReportRestoredActivity(restoredActivity);
            Assert.AreEqual(2, this.sendItems.Count);
            var requestRestoredTelemetry = (RequestTelemetry)this.sendItems.SingleOrDefault(i => i is RequestTelemetry);
            Assert.IsNotNull(requestRestoredTelemetry);

            this.aspNetDiagnosticsSource.StopActivity();
            Assert.AreEqual(3, this.sendItems.Count);

            var requestTelemetry = (RequestTelemetry)this.sendItems[2];
            Assert.IsNotNull(requestTelemetry);

            Assert.AreEqual(requestTelemetry.Id, requestRestoredTelemetry.Context.Operation.ParentId);
            Assert.AreEqual(restoredActivity.Id, requestRestoredTelemetry.Id);
            Assert.AreEqual(requestTelemetry.Context.Operation.Id, requestRestoredTelemetry.Context.Operation.Id);

            Assert.AreEqual(restoredActivity.ParentId, requestTelemetry.Id);
            Assert.AreEqual(restoredActivity.Id, trace.Context.Operation.ParentId);
            Assert.AreEqual(requestTelemetry.Context.Operation.Id, trace.Context.Operation.Id);
        }

        [TestMethod]
        public void DoubleBeginEndRequestReportsOneTelemetry()
        {
            this.module = this.CreateModule();

            Assert.IsTrue(this.aspNetDiagnosticsSource.StartActivity());
            var activity = Activity.Current;
            Assert.IsTrue(this.aspNetDiagnosticsSource.StartActivity());

            this.aspNetDiagnosticsSource.StopActivity();
            this.aspNetDiagnosticsSource.StopActivity();
            Assert.AreEqual(1, this.sendItems.Count);

            var requestTelemetry = this.sendItems[0] as RequestTelemetry;
            Assert.IsNotNull(requestTelemetry);
            Assert.IsNull(requestTelemetry.Context.Operation.ParentId);
            Assert.AreEqual(activity.RootId, requestTelemetry.Context.Operation.Id);
            Assert.AreEqual(activity.Id, requestTelemetry.Id);
        }

        [TestMethod]
        public void ReportLostActivityReportsTelemetry()
        {
            this.module = this.CreateModule();

            var activity = new Activity(FakeAspNetDiagnosticSource.IncomingRequestEventName).SetParentId("|guid.").AddBaggage("k", "v");
            Assert.IsTrue(this.aspNetDiagnosticsSource.StartActivity(activity));

            Activity.Current.Stop();
            Assert.IsNull(Activity.Current);

            this.aspNetDiagnosticsSource.StopLostActivity(activity);

            Assert.AreEqual(1, this.sendItems.Count);

            var requestTelemetry = this.sendItems[0] as RequestTelemetry;
            Assert.IsNotNull(requestTelemetry);
            Assert.AreEqual("guid", requestTelemetry.Context.Operation.Id);
            Assert.AreEqual("|guid.", requestTelemetry.Context.Operation.ParentId);
            Assert.AreEqual(activity.Id, requestTelemetry.Id);
            Assert.AreEqual("v", requestTelemetry.Context.Properties["k"]);
        }

        [TestMethod]
        public void RequestTelemetryIsSetWithLegacyHeaders()
        {
            FakeAspNetDiagnosticSource.FakeContext =
                HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string>
                {
                    ["x-ms-request-id"] = "guid1",
                    ["x-ms-request-root-id"] = "guid2"
                });

            this.module = this.CreateModule();

            var activity = new Activity(FakeAspNetDiagnosticSource.IncomingRequestEventName);
            Assert.IsTrue(this.aspNetDiagnosticsSource.IsEnabled(FakeAspNetDiagnosticSource.IncomingRequestEventName, activity));
            Assert.AreEqual("guid2", activity.ParentId);

            this.aspNetDiagnosticsSource.StartActivityWithoutChecks(activity);
            this.aspNetDiagnosticsSource.StopActivity();

            Assert.AreEqual(1, this.sendItems.Count);

            var requestTelemetry = this.sendItems[0] as RequestTelemetry;
            Assert.IsNotNull(requestTelemetry);
            Assert.AreEqual("guid2", requestTelemetry.Context.Operation.Id);
            Assert.AreEqual("guid1", requestTelemetry.Context.Operation.ParentId);
            Assert.AreEqual(activity.Id, requestTelemetry.Id);
        }

        [TestMethod]
        public void RequestTelemetryIsSetWithCustomHeaders()
        {
            this.module = this.CreateModule("rootHeaderName", "parentHeaderName");
            FakeAspNetDiagnosticSource.FakeContext =
                HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string>
                {
                    ["parentHeaderName"] = "ParentId",
                    ["rootHeaderName"] = "RootId"
                });

            var activity = new Activity(FakeAspNetDiagnosticSource.IncomingRequestEventName);
            Assert.IsTrue(this.aspNetDiagnosticsSource.IsEnabled(FakeAspNetDiagnosticSource.IncomingRequestEventName, activity));
            Assert.AreEqual("RootId", activity.ParentId);

            this.aspNetDiagnosticsSource.StartActivityWithoutChecks(activity);
            this.aspNetDiagnosticsSource.StopActivity();

            Assert.AreEqual(1, this.sendItems.Count);

            var requestTelemetry = this.sendItems[0] as RequestTelemetry;
            Assert.IsNotNull(requestTelemetry);
            Assert.AreEqual("RootId", requestTelemetry.Context.Operation.Id);
            Assert.AreEqual("ParentId", requestTelemetry.Context.Operation.ParentId);
            Assert.AreEqual(activity.Id, requestTelemetry.Id);
        }

        [TestMethod]
        public void StandardHeadersWinOverLegacyHeaders()
        {
            FakeAspNetDiagnosticSource.FakeContext =
                HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string>
                {
                    ["x-ms-request-id"] = "legacy-id",
                    ["x-ms-request-rooit-id"] = "legacy-root-id"
                });

            this.module = this.CreateModule();

            var activity = new Activity(FakeAspNetDiagnosticSource.IncomingRequestEventName);
            activity.SetParentId("|standard-id.");
            Assert.IsTrue(this.aspNetDiagnosticsSource.IsEnabled(FakeAspNetDiagnosticSource.IncomingRequestEventName, activity));
            Assert.AreEqual("|standard-id.", activity.ParentId);

            this.aspNetDiagnosticsSource.StartActivityWithoutChecks(activity);
            this.aspNetDiagnosticsSource.StopActivity();

            Assert.AreEqual(1, this.sendItems.Count);

            var requestTelemetry = this.sendItems[0] as RequestTelemetry;
            Assert.IsNotNull(requestTelemetry);
            Assert.AreEqual("standard-id", requestTelemetry.Context.Operation.Id);
            Assert.AreEqual("|standard-id.", requestTelemetry.Context.Operation.ParentId);
            Assert.AreEqual(activity.Id, requestTelemetry.Id);
        }

        [TestMethod]
        public void TestActivityIdGeneratioWithEmptyHeaders()
        {
            this.module = this.CreateModule();

            var activities = new Activity[5];
            for (int i = 0; i < activities.Length; i++)
            {
                this.aspNetDiagnosticsSource.StartActivity();
                activities[i] = Activity.Current;
                this.aspNetDiagnosticsSource.StopActivity();
                
                // clean up
                HttpContext.Current = HttpModuleHelper.GetFakeHttpContext();
            }

            Assert.AreEqual(activities.Length, this.sendItems.Count);

            var ids = this.sendItems.Select(i => ((RequestTelemetry)i).Context.Operation.Id);

            // This code should go away when Activity is fixed: https://github.com/dotnet/corefx/issues/18418
            // check that Ids are not generated by Activity
            // so they look like OperationTelemetry.Id
            foreach (var operationId in ids)
            {
                // length is like default RequestTelemetry.Id length
                Assert.AreEqual(new RequestTelemetry().Id.Length, operationId.Length);

                // operationId is ulong base64 encoded
                byte[] data = Convert.FromBase64String(operationId);
                Assert.AreEqual(8, data.Length);
                BitConverter.ToUInt64(data, 0);

                // does not look like root Id generated by Activity
                Assert.AreEqual(1, operationId.Split('-').Length);
            }

            //// end of workaround test
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private AspNetDiagnosticTelemetryModule CreateModule(string rootIdHeaderName = null, string parentIdHeaderName = null)
        {
            var initializer = new Web.OperationCorrelationTelemetryInitializer();
            if (rootIdHeaderName != null)
            {
                initializer.RootOperationIdHeaderName = rootIdHeaderName;
            }

            if (parentIdHeaderName != null)
            {
                initializer.ParentOperationIdHeaderName = parentIdHeaderName;
            }

            this.configuration.TelemetryInitializers.Add(new Extensibility.OperationCorrelationTelemetryInitializer());

            AspNetDiagnosticTelemetryModule result = new AspNetDiagnosticTelemetryModule();

            var requestModule = new RequestTrackingTelemetryModule()
            {
                EnableChildRequestTrackingSuppression = false
            };
            var exceptionModule = new ExceptionTrackingTelemetryModule();
            requestModule.Initialize(this.configuration);
            exceptionModule.Initialize(this.configuration);

            TelemetryModules.Instance.Modules.Add(requestModule);
            TelemetryModules.Instance.Modules.Add(exceptionModule);

            result.Initialize(this.configuration);

            return result;
        }

        private void Dispose(bool dispose)
        {
            if (dispose)
            {
                this.aspNetDiagnosticsSource.Dispose();
                this.configuration.Dispose();
                this.module.Dispose();
            }
        }

        private class FakeAspNetDiagnosticSource : IDisposable
        {
            public const string IncomingRequestEventName = "Microsoft.AspNet.HttpReqIn";
            private const string AspNetListenerName = "Microsoft.AspNet.TelemetryCorrelation";
            private const string IncomingRequestStopLostActivity = "Microsoft.AspNet.HttpReqIn.ActivityLost.Stop";
            private const string IncomingRequestStopRestoredActivity = "Microsoft.AspNet.HttpReqIn.ActivityRestored.Stop";

            private readonly DiagnosticListener listener;

            public FakeAspNetDiagnosticSource()
            {
                this.listener = new DiagnosticListener(AspNetListenerName);
                HttpContext.Current = HttpModuleHelper.GetFakeHttpContext();
            }

            public static HttpContext FakeContext
            {
                set
                {
                    HttpContext.Current = value;
                }
            }

            public bool IsEnabled(string eventName, object arg1 = null, object arg2 = null)
            {
                return this.listener.IsEnabled(eventName, arg1, arg2);
            }

            public void StartActivityWithoutChecks(Activity activity)
            {
                Debug.Assert(activity != null, "Activity is null");
                this.listener.StartActivity(activity, new { });
            }

            public bool StartActivity(Activity activity = null)
            {
                if (this.listener.IsEnabled() && this.listener.IsEnabled(IncomingRequestEventName))
                {
                    if (activity == null)
                    {
                        activity = new Activity(IncomingRequestEventName);
                    }

                    if (this.listener.IsEnabled(IncomingRequestEventName, activity))
                    {
                        this.listener.StartActivity(activity, new { });
                    }

                    return true;
                }

                return false;
            }

            public void StopActivity(Activity activity = null)
            {
                if (activity == null)
                {
                    Debug.Assert(Activity.Current != null, "Activity is null and Activity.Current is null, there is nothing to stop");
                    this.listener.StopActivity(Activity.Current, new { });
                    return;
                }

                while (Activity.Current != activity && Activity.Current != null)
                {
                    Activity.Current.Stop();
                }

                Debug.Assert(Activity.Current != null, "have not found activity in the stack");

                this.listener.StopActivity(activity, new { });
            }

            public void RestoreLostActivity(Activity activity)
            {
                Debug.Assert(activity != null, "Activity is null");
                var restoredActivity = new Activity(IncomingRequestEventName);
                restoredActivity.SetParentId(activity.Id);
                foreach (var item in activity.Baggage)
                {
                    restoredActivity.AddBaggage(item.Key, item.Value);
                }

                restoredActivity.Start();
            }

            public void StopLostActivity(Activity activity)
            {
                Debug.Assert(activity != null, "Activity is null");
                Debug.Assert(Activity.Current == null, "Activity.Current is not null");

                this.listener.Write(IncomingRequestStopLostActivity, new { activity });
            }

            public void ReportRestoredActivity(Activity activity)
            {
                Debug.Assert(activity != null, "Activity is null");

                this.listener.Write(IncomingRequestStopRestoredActivity, new { Activity = activity });
            }

            public void Dispose()
            {
                this.Dispose(true);
                GC.SuppressFinalize(this);
            }

            private void Dispose(bool dispose)
            {
                if (dispose)
                {
                    this.listener.Dispose();
                }
            }
        }
    }
}