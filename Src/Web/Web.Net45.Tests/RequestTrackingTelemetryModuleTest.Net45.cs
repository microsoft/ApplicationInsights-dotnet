namespace Microsoft.ApplicationInsights.Web
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Web;

    using Microsoft.ApplicationInsights.Common;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.W3C;
    using Microsoft.ApplicationInsights.Web.Helpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Assert = Xunit.Assert;

#pragma warning disable 612, 618
    /// <summary>
    /// NET 4.5 specific tests for RequestTrackingTelemetryModule.
    /// </summary>
    public partial class RequestTrackingTelemetryModuleTest
    {
        [TestMethod]
        public void OnBeginSetsOperationContextWithStandardHeaders()
        {
            var context = HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string>
            {
                ["Request-Id"] = "|guid1.1",
                ["Correlation-Context"] = "k=v"
            });
            var module = this.RequestTrackingTelemetryModuleFactory(this.CreateDefaultConfig(context));

            module.OnBeginRequest(context);
            var requestTelemetry = context.GetRequestTelemetry();

            // initialize telemetry
            module.OnEndRequest(context);

            Assert.Equal("guid1", requestTelemetry.Context.Operation.Id);
            Assert.Equal("|guid1.1", requestTelemetry.Context.Operation.ParentId);

            Assert.True(requestTelemetry.Id.StartsWith("|guid1.1.", StringComparison.Ordinal));
            Assert.NotEqual("|guid1.1", requestTelemetry.Id);
            Assert.Equal("guid1", this.GetActivityRootId(requestTelemetry.Id));
            Assert.Equal("v", requestTelemetry.Properties["k"]);
        }

        [TestMethod]
        public void OnBeginSetsOperationContextWithStandardHeadersWithNonHierarchialId()
        {
            var context = HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string>
            {
                ["Request-Id"] = "guid1",
                ["Correlation-Context"] = "k=v"
            });
            var module = this.RequestTrackingTelemetryModuleFactory(this.CreateDefaultConfig(context));

            module.OnBeginRequest(context);
            var requestTelemetry = context.GetRequestTelemetry();
            module.OnEndRequest(context);

            Assert.Equal("guid1", requestTelemetry.Context.Operation.Id);
            Assert.Equal("guid1", requestTelemetry.Context.Operation.ParentId);

            Assert.True(requestTelemetry.Id.StartsWith("|guid1.", StringComparison.Ordinal));
            Assert.NotEqual("|guid1.1.", requestTelemetry.Id);
            Assert.Equal("guid1", this.GetActivityRootId(requestTelemetry.Id));

            // will initialize telemetry
            module.OnEndRequest(context);
            Assert.Equal("v", requestTelemetry.Properties["k"]);
        }

        [TestMethod]
        public void OnBeginSetsOperationContextWithoutHeaders()
        {
            var context = HttpModuleHelper.GetFakeHttpContext();
            var module = this.RequestTrackingTelemetryModuleFactory(this.CreateDefaultConfig(context));

            module.OnBeginRequest(context);
            var requestTelemetry = context.GetRequestTelemetry();
            module.OnEndRequest(context);

            var operationId = requestTelemetry.Context.Operation.Id;
            Assert.NotNull(operationId);
            Assert.Null(requestTelemetry.Context.Operation.ParentId);
            Assert.True(requestTelemetry.Id.StartsWith('|' + operationId + '.', StringComparison.Ordinal));
            Assert.NotEqual(operationId, requestTelemetry.Id);

            // W3C compatible-Id ( should go away when W3C is implemented in .NET https://github.com/dotnet/corefx/issues/30331 TODO)
            Assert.Equal(32, operationId.Length);
            Assert.True(Regex.Match(operationId, @"[a-z][0-9]").Success);
            // end of workaround test
        }

        [TestMethod]
        public void InitializeFromStandardHeadersAlwaysWinsCustomHeaders()
        {
            var context = HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string>
            {
                ["Request-Id"] = "|standard-id.",
                ["x-ms-request-id"] = "legacy-id",
                ["x-ms-request-rooit-id"] = "legacy-root-id"
            });

            var module = this.RequestTrackingTelemetryModuleFactory(this.CreateDefaultConfig(context));
            module.OnBeginRequest(context);

            var requestTelemetry = context.GetRequestTelemetry();

            // initialize telemetry
            module.OnEndRequest(context);
            Assert.Equal("|standard-id.", requestTelemetry.Context.Operation.ParentId);
            Assert.Equal("standard-id", requestTelemetry.Context.Operation.Id);
            Assert.Equal("standard-id", this.GetActivityRootId(requestTelemetry.Id));
            Assert.NotEqual(requestTelemetry.Context.Operation.Id, requestTelemetry.Id);
        }

        [TestMethod]
        public void OnBeginSetsOperationContextWithEnabledLegacyHeaders()
        {
            var context = HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string>
            {
                ["x-ms-request-id"] = "guid1",
                ["x-ms-request-root-id"] = "guid2"
            });

            var module = this.RequestTrackingTelemetryModuleFactory(this.CreateDefaultConfig(context, "x-ms-request-root-id", "x-ms-request-id"));

            module.OnBeginRequest(context);
            var requestTelemetry = context.GetRequestTelemetry();
            module.OnEndRequest(context);

            Assert.Equal("guid2", requestTelemetry.Context.Operation.Id);
            Assert.Equal("guid1", requestTelemetry.Context.Operation.ParentId);

            Assert.True(requestTelemetry.Id.StartsWith("|guid2.", StringComparison.Ordinal));
        }

        [TestMethod]
        public void TrackRequestWithW3CHeaders()
        {
            this.TestRequestTrackingWithW3CSupportEnabled(
                startActivity: true, 
                addRequestId: false);
        }

        [TestMethod]
        public void TrackRequestWithW3CHeadersAndNoParentActivity()
        {
            this.TestRequestTrackingWithW3CSupportEnabled(
                startActivity: false,
                addRequestId: false);
        }

        [TestMethod]
        public void TrackRequestWithW3CAndRequestIdHeaders()
        {
            this.TestRequestTrackingWithW3CSupportEnabled(
                startActivity: true,
                addRequestId: true);
        }

        [TestMethod]
        public void TrackRequestWithW3CAndRequestIdHeadersAndNoParentActivity()
        {
            this.TestRequestTrackingWithW3CSupportEnabled(
                startActivity: false,
                addRequestId: true);
        }

        [TestMethod]
        public void TrackRequestWithW3CEnabledAndNoHeaders()
        {
            this.TestRequestTrackingWithW3CSupportEnabledAndNoW3CHeaders(
                startActivity: true,
                addRequestId: false);
        }

        [TestMethod]
        public void TrackRequestWithW3CEnabledAndNoHeadersAndNoParentActivity()
        {
            this.TestRequestTrackingWithW3CSupportEnabledAndNoW3CHeaders(
                startActivity: false,
                addRequestId: false);
        }

        [TestMethod]
        public void TrackRequestWithW3CEnabledAndRequestIdHeader()
        {
            this.TestRequestTrackingWithW3CSupportEnabledAndNoW3CHeaders(
                startActivity: true,
                addRequestId: true);
        }

        [TestMethod]
        public void TrackRequestWithW3CEnabledAndRequestIdHeaderAndNoParentActivity()
        {
            this.TestRequestTrackingWithW3CSupportEnabledAndNoW3CHeaders(
                startActivity: false,
                addRequestId: true);
        }

        [TestMethod]
        public void TrackRequestWithW3CEnabledAndAppIdInState()
        {
            string expectedAppId = "cid-v1:some-app-id";
            var headers = new Dictionary<string, string>
            {
                ["traceparent"] = "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
                ["tracestate"] = $"state=some,{W3CConstants.AzureTracestateNamespace}={expectedAppId}",
            };

            var context = HttpModuleHelper.GetFakeHttpContext(headers);
            var module = this.RequestTrackingTelemetryModuleFactory(this.CreateDefaultConfig(context), enableW3CTracing: true);

            module.OnBeginRequest(context);
            var activityInitializedByW3CHeader = Activity.Current;
            Assert.Equal("state=some", activityInitializedByW3CHeader.GetTracestate());

            var requestTelemetry = context.GetRequestTelemetry();
            module.OnEndRequest(context);

            Assert.Equal(expectedAppId, requestTelemetry.Source);
        }

        [TestMethod]
        public void TrackRequestWithW3CEnabledAndRequestContextAndAppIdInState()
        {
            string expectedAppId = "cid-v1:some-app-id";
            var headers = new Dictionary<string, string>
            {
                ["traceparent"] = "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
                ["tracestate"] = $"state=some,{W3CConstants.AzureTracestateNamespace}={expectedAppId}",
                ["Request-Context"] = "cid-v1:dummy"
            };

            var context = HttpModuleHelper.GetFakeHttpContext(headers);
            var module = this.RequestTrackingTelemetryModuleFactory(this.CreateDefaultConfig(context), enableW3CTracing: true);

            module.OnBeginRequest(context);
            var activityInitializedByW3CHeader = Activity.Current;
            Assert.Equal("state=some", activityInitializedByW3CHeader.GetTracestate());

            var requestTelemetry = context.GetRequestTelemetry();
            module.OnEndRequest(context);

            Assert.Equal(expectedAppId, requestTelemetry.Source);
        }

        [TestMethod]
        public void OnBeginSetsOperationContextWithDisabledLegacyHeaders()
        {
            var context = HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string>
            {
                ["x-ms-request-id"] = "guid1",
                ["x-ms-request-root-id"] = "guid2"
            });

            var module = this.RequestTrackingTelemetryModuleFactory(this.CreateDefaultConfig(context));

            module.OnBeginRequest(context);
            var requestTelemetry = context.GetRequestTelemetry();
            module.OnEndRequest(context);

            Assert.NotNull(requestTelemetry.Context.Operation.Id);
            Assert.Null(requestTelemetry.Context.Operation.ParentId);
        }

        [TestMethod]
        public void OnBeginReadsRootAndParentIdFromCustomHeader()
        {
            var context = HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string>
            {
                ["parentHeaderName"] = "ParentId",
                ["rootHeaderName"] = "RootId"
            });

            var config = this.CreateDefaultConfig(context, rootIdHeaderName: "rootHeaderName", parentIdHeaderName: "parentHeaderName");
            var module = this.RequestTrackingTelemetryModuleFactory(config);
                      
            module.OnBeginRequest(context);

            var requestTelemetry = context.GetRequestTelemetry();

            Assert.Equal("ParentId", requestTelemetry.Context.Operation.ParentId);

            Assert.Equal("RootId", requestTelemetry.Context.Operation.Id);
            Assert.NotEqual("RootId", requestTelemetry.Id);
            Assert.Equal("RootId", this.GetActivityRootId(requestTelemetry.Id));
        }

        [TestMethod]
        public void OnBeginTelemetryCreatedWithinRequestScopeIsRequestChild()
        {
            var context = HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string>
            {
                ["Request-Id"] = "|guid1.1",
                ["Correlation-Context"] = "k=v"
            });
            var config = this.CreateDefaultConfig(context);
            var module = this.RequestTrackingTelemetryModuleFactory(this.CreateDefaultConfig(context));

            module.OnBeginRequest(context);

            var requestTelemetry = context.GetRequestTelemetry();
            var telemetryClient = new TelemetryClient(config);
            var exceptionTelemetry = new ExceptionTelemetry();
            telemetryClient.Initialize(exceptionTelemetry);

            module.OnEndRequest(context);

            Assert.Equal("guid1", exceptionTelemetry.Context.Operation.Id);
            Assert.Equal(requestTelemetry.Id, exceptionTelemetry.Context.Operation.ParentId);
            Assert.Equal("v", exceptionTelemetry.Properties["k"]);
        }

        [TestMethod]
        public async Task OnPreHandlerTelemetryCreatedWithinRequestScopeIsRequestChild()
        {
            var context = HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string>
            {
                ["Request-Id"] = "|guid1.1",
                ["Correlation-Context"] = "k=v"
            });
            var config = this.CreateDefaultConfig(context);
            var module = this.RequestTrackingTelemetryModuleFactory(config);
            var telemetryClient = new TelemetryClient(config);

            module.OnBeginRequest(context);

            // simulate losing call context by cleaning up activity
            Assert.NotNull(Activity.Current);
            var activity = Activity.Current;
            activity.Stop();
            Assert.Null(Activity.Current);

            // CallContext was lost after OnBegin, so Asp.NET Http Module will restore it in OnPreRequestHandlerExecute
            new Activity("restored").SetParentId(activity.Id).AddBaggage("k", "v").Start();

            var trace = new TraceTelemetry();

            // run track trace in the async task, so that HttpContext.Current is not available and we could be sure
            // telemetry is not initialized from it.
            await Task.Run(() =>
            {
                // if OnPreRequestHandlerExecute set a CallContext, child telemetry will be properly filled
                telemetryClient.TrackTrace(trace);
            });

            var requestTelemetry = context.GetRequestTelemetry();

            Assert.Equal(requestTelemetry.Context.Operation.Id, trace.Context.Operation.Id);

            // we created Activity for request and assigned Id for the request like guid1.1.12345_
            // then we lost it and restored (started a new child activity), so the Id is guid1.1.12345_abc_
            // so the request is grand parent to the trace
            Assert.Equal(Activity.Current.ParentId, requestTelemetry.Id);
            Assert.True(trace.Context.Operation.ParentId.StartsWith(requestTelemetry.Id, StringComparison.Ordinal));
            Assert.Equal(Activity.Current.Id, trace.Context.Operation.ParentId);
            Assert.Equal("v", trace.Properties["k"]);
        }

        [TestMethod]
        public void TelemetryCreatedWithinRequestScopeIsRequestChildWhenActivityIsLost()
        {
            var context = HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string>
            {
                ["Request-Id"] = "|guid1.1",
                ["Correlation-Context"] = "k=v"
            });
            var config = this.CreateDefaultConfig(context);
            var module = this.RequestTrackingTelemetryModuleFactory(config);
            var telemetryClient = new TelemetryClient(config);

            module.OnBeginRequest(context);

            // simulate losing call context by cleaning up activity
            Assert.NotNull(Activity.Current);
            Activity.Current.Stop();
            Assert.Null(Activity.Current);

            var trace = new TraceTelemetry();
            telemetryClient.TrackTrace(trace);
            var requestTelemetry = context.GetRequestTelemetry();

            Assert.Equal(requestTelemetry.Context.Operation.Id, trace.Context.Operation.Id);

            // we created Activity for request and assigned Id for the request like guid1.1.12345
            // then we created Activity for request children and assigned it Id like guid1.1.12345_1
            // then we lost it and restored (started a new child activity), so the Id is guid1.1.123_1.abc
            // so the request is grand parent to the trace
            Assert.True(trace.Context.Operation.ParentId.StartsWith(requestTelemetry.Id, StringComparison.Ordinal));
            Assert.Equal("v", trace.Properties["k"]);
        }

        [TestMethod]
        public void TelemetryTrackedBeforeOnBegin()
        {
            var context = HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string>());
            var config = this.CreateDefaultConfig(context);
            config.TelemetryInitializers.Add(new OperationCorrelationTelemetryInitializer());
            var module = this.RequestTrackingTelemetryModuleFactory(config);
            var client = new TelemetryClient(config);

            client.TrackTrace("test1");

            module.OnBeginRequest(context);

            client.TrackTrace("test2");

            // initialize telemetry
            module.OnEndRequest(context);

            var trace1 = (TraceTelemetry)this.sentTelemetry.Single(t => t is TraceTelemetry tt && tt.Message == "test1");
            var trace2 = (TraceTelemetry)this.sentTelemetry.Single(t => t is TraceTelemetry tt && tt.Message == "test2");

            var request = (RequestTelemetry)this.sentTelemetry.Single(t => t is RequestTelemetry);
            Assert.Equal(trace1.Context.Operation.Id, request.Context.Operation.Id);
            Assert.Equal(trace2.Context.Operation.Id, request.Context.Operation.Id);

            Assert.Equal(trace2.Context.Operation.ParentId, request.Id);
            Assert.Equal(trace1.Context.Operation.ParentId, request.Id);
        }

        [TestMethod]
        public void TelemetryTrackedBeforeOnBeginWithHeaders()
        {
            var context = HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string>
            {
                ["Request-Id"] = "|guid1.1"
            });

            var config = this.CreateDefaultConfig(context);
            var module = this.RequestTrackingTelemetryModuleFactory(config);
            var client = new TelemetryClient(config);

            client.TrackTrace("test1");

            module.OnBeginRequest(context);

            client.TrackTrace("test2");

            // initialize telemetry
            module.OnEndRequest(context);

            var trace1 = (TraceTelemetry)this.sentTelemetry.Single(t => t is TraceTelemetry tt && tt.Message == "test1");
            var trace2 = (TraceTelemetry)this.sentTelemetry.Single(t => t is TraceTelemetry tt && tt.Message == "test2");

            var request = (RequestTelemetry)this.sentTelemetry.Single(t => t is RequestTelemetry);
            Assert.Equal(trace1.Context.Operation.Id, request.Context.Operation.Id);
            Assert.Equal(trace2.Context.Operation.Id, request.Context.Operation.Id);

            Assert.Equal(trace1.Context.Operation.ParentId, request.Id);
            Assert.Equal(trace2.Context.Operation.ParentId, request.Id);
        }

        [TestMethod]
        public void TelemetryTrackedBeforeOnBeginW3CEnabled()
        {
            var context = HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string>
            {
                ["traceparent"] = "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
            });
            var config = this.CreateDefaultConfig(context);
            var module = this.RequestTrackingTelemetryModuleFactory(config, enableW3CTracing: true);
            var client = new TelemetryClient(config);

            client.TrackTrace("test1");

            module.OnBeginRequest(context);

            client.TrackTrace("test2");

            // initialize telemetry
            module.OnEndRequest(context);

            var trace1 = (TraceTelemetry)this.sentTelemetry.Single(t => t is TraceTelemetry tt && tt.Message == "test1");
            var trace2 = (TraceTelemetry)this.sentTelemetry.Single(t => t is TraceTelemetry tt && tt.Message == "test2");

            var request = (RequestTelemetry)this.sentTelemetry.Single(t => t is RequestTelemetry);
            Assert.Equal("4bf92f3577b34da6a3ce929d0e0e4736", request.Context.Operation.Id);
            Assert.Equal("|4bf92f3577b34da6a3ce929d0e0e4736.00f067aa0ba902b7.", request.Context.Operation.ParentId);

            Assert.Equal(trace1.Context.Operation.Id, request.Context.Operation.Id);
            Assert.Equal(trace2.Context.Operation.Id, request.Context.Operation.Id);

            Assert.Equal(trace1.Context.Operation.ParentId, request.Id);
            Assert.Equal(trace2.Context.Operation.ParentId, request.Id);
        }

        [TestMethod]
        public void TelemetryTrackedBeforeOnBeginW3CEnabledWithHeaders()
        {
            var context = HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string>());
            var config = this.CreateDefaultConfig(context);
            var module = this.RequestTrackingTelemetryModuleFactory(config, enableW3CTracing: true);
            var client = new TelemetryClient(config);

            client.TrackTrace("test1");

            module.OnBeginRequest(context);

            client.TrackTrace("test2");

            // initialize telemetry
            module.OnEndRequest(context);

            var trace1 = (TraceTelemetry)this.sentTelemetry.Single(t => t is TraceTelemetry tt && tt.Message == "test1");
            var trace2 = (TraceTelemetry)this.sentTelemetry.Single(t => t is TraceTelemetry tt && tt.Message == "test2");

            var request = (RequestTelemetry)this.sentTelemetry.Single(t => t is RequestTelemetry);
            Assert.Equal(trace1.Context.Operation.Id, request.Context.Operation.Id);
            Assert.Equal(trace2.Context.Operation.Id, request.Context.Operation.Id);

            Assert.Equal(trace2.Context.Operation.ParentId, request.Id);
            Assert.Equal(trace1.Context.Operation.ParentId, request.Id);
        }

        private void TestRequestTrackingWithW3CSupportEnabled(bool startActivity, bool addRequestId)
        {
            var headers = new Dictionary<string, string>
            {
                ["traceparent"] = "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
                ["tracestate"] = "state=some",
                ["Correlation-Context"] = "k=v"
            };

            if (addRequestId)
            {
                headers.Add("Request-Id", "|abc.1.2.3.");
            }

            var context = HttpModuleHelper.GetFakeHttpContext(headers);
            var module = this.RequestTrackingTelemetryModuleFactory(this.CreateDefaultConfig(context), enableW3CTracing: true);

            if (startActivity)
            {
                var activity = new Activity("operation");
                activity.Start();
            }

            module.OnBeginRequest(context);
            var activityInitializedByW3CHeader = Activity.Current;
            
            Assert.Equal("4bf92f3577b34da6a3ce929d0e0e4736", activityInitializedByW3CHeader.GetTraceId());
            Assert.Equal("00f067aa0ba902b7", activityInitializedByW3CHeader.GetParentSpanId());
            Assert.Equal(16, activityInitializedByW3CHeader.GetSpanId().Length);
            Assert.Equal("state=some", activityInitializedByW3CHeader.GetTracestate());
            Assert.Equal("v", activityInitializedByW3CHeader.Baggage.Single(t => t.Key == "k").Value);

            var requestTelemetry = context.GetRequestTelemetry();
            module.OnEndRequest(context);

            Assert.Equal($"|4bf92f3577b34da6a3ce929d0e0e4736.{activityInitializedByW3CHeader.GetSpanId()}.", requestTelemetry.Id);
            Assert.Equal("4bf92f3577b34da6a3ce929d0e0e4736", requestTelemetry.Context.Operation.Id);
            Assert.Equal("|4bf92f3577b34da6a3ce929d0e0e4736.00f067aa0ba902b7.", requestTelemetry.Context.Operation.ParentId);

            Assert.Equal("state=some", requestTelemetry.Properties[W3CConstants.TracestateTag]);
        }

        private void TestRequestTrackingWithW3CSupportEnabledAndNoW3CHeaders(bool startActivity, bool addRequestId)
        {
            var headers = new Dictionary<string, string>();

            if (addRequestId)
            {
                headers.Add("Request-Id", "|abc.1.2.3.");
            }

            var context = HttpModuleHelper.GetFakeHttpContext(headers);

            var module = this.RequestTrackingTelemetryModuleFactory(this.CreateDefaultConfig(context), enableW3CTracing: true);

            if (startActivity)
            {
                var activity = new Activity("operation");
                activity.Start();
            }

            module.OnBeginRequest(context);
            var activityInitializedByW3CHeader = Activity.Current;

            Assert.Equal(32, activityInitializedByW3CHeader.GetTraceId().Length);
            Assert.Equal(16, activityInitializedByW3CHeader.GetSpanId().Length);
            Assert.Null(activityInitializedByW3CHeader.GetParentSpanId());

            Assert.Null(activityInitializedByW3CHeader.GetTracestate());
            Assert.False(activityInitializedByW3CHeader.Baggage.Any());

            var requestTelemetry = context.GetRequestTelemetry();
            module.OnEndRequest(context);

            Assert.Equal($"|{activityInitializedByW3CHeader.GetTraceId()}.{activityInitializedByW3CHeader.GetSpanId()}.", requestTelemetry.Id);
            Assert.Equal(activityInitializedByW3CHeader.GetTraceId(), requestTelemetry.Context.Operation.Id);

            if (addRequestId)
            {
                Assert.Equal("|abc.1.2.3.", requestTelemetry.Context.Operation.ParentId);
            }
            else
            {
                Assert.Null(requestTelemetry.Context.Operation.ParentId);
            }
        }
    }
#pragma warning restore 612, 618
}