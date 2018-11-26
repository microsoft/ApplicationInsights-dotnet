namespace Microsoft.ApplicationInsights
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Web;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.W3C.Internal;
    using Microsoft.ApplicationInsights.Web;
    using Microsoft.ApplicationInsights.Web.Helpers;
    using Microsoft.ApplicationInsights.Web.TestFramework;
    using Microsoft.AspNet.TelemetryCorrelation;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

#pragma warning disable 612, 618
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

        // When telemetry is reported before AspNetDiagnosticsSource gets Start event
        // we create an activity in App Insights.
        // If this activity is lost on the way to BeginRequest on TelemteryCorrelation module
        // there is no way to correlate before/after telemetry -
        // TelemetryCorrelation module must be first in the pipeline, otherwise correlation is not guaranteed
        // see https://github.com/Microsoft/ApplicationInsights-dotnet-server/issues/1049
        [Ignore]
        [TestMethod]
        public async Task TelemetryReportedBeforeAndAfterOnBeginAndLostActivity()
        {
            this.module = this.CreateModule();
            var client = new TelemetryClient(this.configuration);

            var trace1 = new TraceTelemetry("test1");
            await Task.Run(() =>
            {
                HttpContext.Current = this.aspNetDiagnosticsSource.FakeContext;
                client.TrackTrace(trace1);
            });

            HttpContext.Current = this.aspNetDiagnosticsSource.FakeContext;

            this.aspNetDiagnosticsSource.StartActivity();

            var trace2 = new TraceTelemetry("test2");
            client.TrackTrace(trace2);

            this.aspNetDiagnosticsSource.StopActivity();
            Assert.AreEqual(3, this.sendItems.Count);

            var requestTelemetry = this.sendItems.OfType<RequestTelemetry>().SingleOrDefault();
            Assert.IsNotNull(requestTelemetry);

            Assert.AreEqual(requestTelemetry.Context.Operation.Id, trace1.Context.Operation.Id);
            Assert.AreEqual(requestTelemetry.Context.Operation.Id, trace2.Context.Operation.Id);

            Assert.AreEqual(requestTelemetry.Id, trace1.Context.Operation.ParentId);
            Assert.AreEqual(requestTelemetry.Id, trace2.Context.Operation.ParentId);
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
            Assert.AreEqual(1, this.sendItems.Count);

            restoredActivity.Stop();
            this.aspNetDiagnosticsSource.StopLostActivity(activity);

            Assert.AreEqual(3, this.sendItems.Count);
            var requestRestoredTelemetry = (RequestTelemetry)this.sendItems[1];
            Assert.IsNotNull(requestRestoredTelemetry);

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
            Assert.AreEqual("v", requestTelemetry.Properties["k"]);
        }

        [TestMethod]
        public void RequestTelemetryIsNotSetWithLegacyHeaders()
        {
            this.aspNetDiagnosticsSource.FakeContext =
                HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string>
                {
                    ["x-ms-request-id"] = "guid1",
                    ["x-ms-request-root-id"] = "guid2"
                });

            this.module = this.CreateModule();

            var activity = new Activity(FakeAspNetDiagnosticSource.IncomingRequestEventName);
            this.aspNetDiagnosticsSource.StartActivityWithoutChecks(activity);
            this.aspNetDiagnosticsSource.StopActivity();

            Assert.AreEqual(1, this.sendItems.Count);

            var requestTelemetry = this.sendItems[0] as RequestTelemetry;
            Assert.IsNotNull(requestTelemetry);
            Assert.AreEqual(activity.RootId, requestTelemetry.Context.Operation.Id);
            Assert.IsNull(requestTelemetry.Context.Operation.ParentId);
            Assert.AreEqual(activity.Id, requestTelemetry.Id);
        }

        [TestMethod]
        public void RequestTelemetryIsSetWithLegacyHeaders()
        {
            this.aspNetDiagnosticsSource.FakeContext =
                HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string>
                {
                    ["x-ms-request-id"] = "guid1",
                    ["x-ms-request-root-id"] = "guid2"
                });

            this.module = this.CreateModule("x-ms-request-root-id", "x-ms-request-id");

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
            this.aspNetDiagnosticsSource.FakeContext =
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
            this.aspNetDiagnosticsSource.FakeContext =
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
        public void TestActivityIdGenerationWithEmptyHeaders()
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
                // W3C compatible-Id ( should go away when W3C is implemented in .NET https://github.com/dotnet/corefx/issues/30331 TODO)
                Assert.AreEqual(32, operationId.Length);
                Assert.IsTrue(Regex.Match(operationId, @"[a-z][0-9]").Success);
                // end of workaround test
            }

            //// end of workaround test
        }

        [TestMethod]
        public void TestActivityIdGenerationWithW3CEnabled()
        {
            this.module = this.CreateModule(enableW3cSupport: true);
            
            this.aspNetDiagnosticsSource.StartActivity();
            Activity activity = Activity.Current;

            this.aspNetDiagnosticsSource.StopActivity();

            var request = this.sendItems.OfType<RequestTelemetry>().Single();

            Assert.AreEqual(32, request.Context.Operation.Id.Length);
            Assert.IsTrue(Regex.Match(request.Context.Operation.Id, @"[a-z][0-9]").Success);

            Assert.AreEqual(request.Context.Operation.Id, activity.RootId);
            Assert.AreEqual(request.Context.Operation.ParentId, activity.GetParentSpanId());
            Assert.AreEqual(request.Id, $"|{activity.GetTraceId()}.{activity.GetSpanId()}.");

            Assert.IsFalse(request.Properties.ContainsKey(W3CConstants.LegacyRootIdProperty));
        }

        [TestMethod]
        public void W3CHeadersWinOverLegacyWhenEnabled()
        {
            this.aspNetDiagnosticsSource.FakeContext =
                HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string>
                {
                    ["traceparent"] = "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
                    ["x-ms-request-id"] = "legacy-id",
                    ["x-ms-request-rooit-id"] = "legacy-root-id"
                });

            this.module = this.CreateModule("x-ms-request-root-id", "x-ms-request-id", enableW3cSupport: true);

            var activity = new Activity(FakeAspNetDiagnosticSource.IncomingRequestEventName);
            Assert.IsTrue(this.aspNetDiagnosticsSource.IsEnabled(FakeAspNetDiagnosticSource.IncomingRequestEventName, activity));
            this.aspNetDiagnosticsSource.StartActivityWithoutChecks(activity);
            this.aspNetDiagnosticsSource.StopActivity();

            Assert.AreEqual("4bf92f3577b34da6a3ce929d0e0e4736", activity.RootId);
            Assert.AreEqual("00f067aa0ba902b7", activity.GetParentSpanId());

            Assert.AreEqual(1, this.sendItems.Count);

            var requestTelemetry = this.sendItems[0] as RequestTelemetry;
            Assert.IsNotNull(requestTelemetry);
            Assert.AreEqual("4bf92f3577b34da6a3ce929d0e0e4736", requestTelemetry.Context.Operation.Id);
            Assert.AreEqual("4bf92f3577b34da6a3ce929d0e0e4736", activity.GetTraceId());
            Assert.AreEqual("|4bf92f3577b34da6a3ce929d0e0e4736.00f067aa0ba902b7.", requestTelemetry.Context.Operation.ParentId);
            Assert.AreEqual($"|4bf92f3577b34da6a3ce929d0e0e4736.{activity.GetSpanId()}.", requestTelemetry.Id);

            Assert.IsFalse(requestTelemetry.Properties.ContainsKey(W3CConstants.LegacyRootIdProperty));
        }

        [TestMethod]
        public void W3CHeadersWinOverRequestIdWhenEnabled()
        {
            this.aspNetDiagnosticsSource.FakeContext =
                HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string>
                {
                    ["traceparent"] = "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
                });

            this.module = this.CreateModule(enableW3cSupport: true);

            var activity = new Activity(FakeAspNetDiagnosticSource.IncomingRequestEventName);
            activity.SetParentId("|requestId.");
            Assert.IsTrue(this.aspNetDiagnosticsSource.IsEnabled(FakeAspNetDiagnosticSource.IncomingRequestEventName, activity));
            this.aspNetDiagnosticsSource.StartActivityWithoutChecks(activity);
            this.aspNetDiagnosticsSource.StopActivity();

            Assert.AreEqual("4bf92f3577b34da6a3ce929d0e0e4736", activity.GetTraceId());
            Assert.AreEqual("00f067aa0ba902b7", activity.GetParentSpanId());

            Assert.AreEqual(1, this.sendItems.Count);

            var requestTelemetry = this.sendItems[0] as RequestTelemetry;
            Assert.IsNotNull(requestTelemetry);
            Assert.AreEqual("4bf92f3577b34da6a3ce929d0e0e4736", requestTelemetry.Context.Operation.Id);
            Assert.AreEqual("|4bf92f3577b34da6a3ce929d0e0e4736.00f067aa0ba902b7.", requestTelemetry.Context.Operation.ParentId);
            Assert.AreEqual($"|4bf92f3577b34da6a3ce929d0e0e4736.{activity.GetSpanId()}.", requestTelemetry.Id);

            Assert.IsTrue(requestTelemetry.Properties.ContainsKey(W3CConstants.LegacyRootIdProperty));
            Assert.AreEqual("requestId", requestTelemetry.Properties[W3CConstants.LegacyRootIdProperty]);

            Assert.IsTrue(requestTelemetry.Properties.ContainsKey(W3CConstants.LegacyRequestIdProperty));
            Assert.IsTrue(requestTelemetry.Properties[W3CConstants.LegacyRequestIdProperty].StartsWith("|requestId."));
        }

        [TestMethod]
        public void RequestIdBecomesParentWhenThereAreNoW3CHeaders()
        {
            this.aspNetDiagnosticsSource.FakeContext =
                HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string>
                {
                    ["Request-Id"] = "|requestId."
                });
            this.module = this.CreateModule(enableW3cSupport: true);

            var activity = new Activity(FakeAspNetDiagnosticSource.IncomingRequestEventName);

            activity.Extract(HttpContext.Current.Request.Headers);

            Assert.IsTrue(this.aspNetDiagnosticsSource.IsEnabled(FakeAspNetDiagnosticSource.IncomingRequestEventName, activity));
            this.aspNetDiagnosticsSource.StartActivityWithoutChecks(activity);
            this.aspNetDiagnosticsSource.StopActivity();

            Assert.AreEqual(32, activity.GetTraceId().Length);
            Assert.AreEqual(16, activity.GetSpanId().Length);
            Assert.IsNull(activity.GetParentSpanId());

            Assert.AreEqual(1, this.sendItems.Count);

            var requestTelemetry = this.sendItems[0] as RequestTelemetry;
            Assert.IsNotNull(requestTelemetry);
            Assert.AreEqual(activity.GetTraceId(), requestTelemetry.Context.Operation.Id);
            Assert.AreEqual("|requestId.", requestTelemetry.Context.Operation.ParentId);
            Assert.AreEqual($"|{activity.GetTraceId()}.{activity.GetSpanId()}.", requestTelemetry.Id);

            Assert.IsTrue(requestTelemetry.Properties.ContainsKey(W3CConstants.LegacyRootIdProperty));
            Assert.AreEqual("requestId", requestTelemetry.Properties[W3CConstants.LegacyRootIdProperty]);

            Assert.IsTrue(requestTelemetry.Properties.ContainsKey(W3CConstants.LegacyRequestIdProperty));
            Assert.IsTrue(requestTelemetry.Properties[W3CConstants.LegacyRequestIdProperty].StartsWith("|requestId."));
        }

        [TestMethod]
        public void CustomHeadersBecomeParentWhenThereAreNoW3CHeaders()
        {
            this.aspNetDiagnosticsSource.FakeContext =
                HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string>
                {
                    ["rootHeaderName"] = "root",
                    ["parentHeaderName"] = "parent"
                });
            this.module = this.CreateModule("rootHeaderName", "parentHeaderName", enableW3cSupport: true);

            var activity = new Activity(FakeAspNetDiagnosticSource.IncomingRequestEventName);

            activity.Extract(HttpContext.Current.Request.Headers);

            Assert.IsTrue(this.aspNetDiagnosticsSource.IsEnabled(FakeAspNetDiagnosticSource.IncomingRequestEventName, activity));
            this.aspNetDiagnosticsSource.StartActivityWithoutChecks(activity);
            this.aspNetDiagnosticsSource.StopActivity();

            Assert.AreEqual(32, activity.GetTraceId().Length);
            Assert.AreEqual(16, activity.GetSpanId().Length);
            Assert.IsNull(activity.GetParentSpanId());

            Assert.AreEqual(1, this.sendItems.Count);

            var requestTelemetry = this.sendItems[0] as RequestTelemetry;
            Assert.IsNotNull(requestTelemetry);
            Assert.AreEqual(activity.GetTraceId(), requestTelemetry.Context.Operation.Id);
            Assert.AreEqual("parent", requestTelemetry.Context.Operation.ParentId);
            Assert.AreEqual($"|{activity.GetTraceId()}.{activity.GetSpanId()}.", requestTelemetry.Id);

            Assert.IsTrue(requestTelemetry.Properties.ContainsKey(W3CConstants.LegacyRootIdProperty));
            Assert.AreEqual("root", requestTelemetry.Properties[W3CConstants.LegacyRootIdProperty]);

            Assert.IsTrue(requestTelemetry.Properties.ContainsKey(W3CConstants.LegacyRequestIdProperty));
            Assert.IsTrue(requestTelemetry.Properties[W3CConstants.LegacyRequestIdProperty].StartsWith("|root."));
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private AspNetDiagnosticTelemetryModule CreateModule(string rootIdHeaderName = null, string parentIdHeaderName = null, bool enableW3cSupport = false)
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

            if (enableW3cSupport)
            {
                this.configuration.TelemetryInitializers.Add(new W3COperationCorrelationTelemetryInitializer());
            }

            AspNetDiagnosticTelemetryModule result = new AspNetDiagnosticTelemetryModule();

            var requestModule = new RequestTrackingTelemetryModule()
            {
                EnableChildRequestTrackingSuppression = false,
                EnableW3CHeadersExtraction = enableW3cSupport
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
            private HttpContext fakeContext;

            public FakeAspNetDiagnosticSource()
            {
                this.listener = new DiagnosticListener(AspNetListenerName);
                this.fakeContext = HttpModuleHelper.GetFakeHttpContext();
                HttpContext.Current = this.fakeContext;
            }

            public HttpContext FakeContext
            {
                get => this.fakeContext;

                set
                {
                    this.fakeContext = value;
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
#pragma warning restore 612, 618
}