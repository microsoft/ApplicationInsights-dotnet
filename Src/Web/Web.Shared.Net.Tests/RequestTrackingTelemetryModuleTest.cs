namespace Microsoft.ApplicationInsights.Web
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Threading.Tasks;
    using System.Web;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Common;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.TestFramework;
    using Microsoft.ApplicationInsights.Web.Helpers;
    using Microsoft.ApplicationInsights.Web.Implementation;
    using Microsoft.ApplicationInsights.Web.TestFramework;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Assert = Xunit.Assert;

    [TestClass]
    public class RequestTrackingTelemetryModuleTest
    {
        private CorrelationIdLookupHelper correlationIdLookupHelper = new CorrelationIdLookupHelper((string ikey) =>
        {
            // Pretend App Id is the same as Ikey
            var tcs = new TaskCompletionSource<string>();
            tcs.SetResult(ikey);
            return tcs.Task;
        });

        [TestCleanup]
        public void Cleanup()
        {
            ActivityHelpers.StopRequestActivity();
        }

        [TestMethod]
        public void OnBeginRequestDoesNotSetTimeIfItWasAssignedBefore()
        {
            var startTime = DateTimeOffset.UtcNow;

            var context = HttpModuleHelper.GetFakeHttpContext();
            var requestTelemetry = context.CreateRequestTelemetryPrivate();
            requestTelemetry.Timestamp = startTime;

            this.RequestTrackingTelemetryModuleFactory().OnBeginRequest(context);

            Assert.Equal(startTime, requestTelemetry.Timestamp);
        }

        [TestMethod]
        public void OnBeginRequestSetsTimeIfItWasNotAssignedBefore()
        {
            var context = HttpModuleHelper.GetFakeHttpContext();
            var requestTelemetry = context.CreateRequestTelemetryPrivate();
            requestTelemetry.Timestamp = default(DateTimeOffset);

            this.RequestTrackingTelemetryModuleFactory().OnBeginRequest(context);

            Assert.NotEqual(default(DateTimeOffset), requestTelemetry.Timestamp);
        }

        [TestMethod]
        public void RequestIdIsAvailableAfterOnBegin()
        {
            var context = HttpModuleHelper.GetFakeHttpContext();
            var requestTelemetry = context.CreateRequestTelemetryPrivate();

            this.RequestTrackingTelemetryModuleFactory().OnBeginRequest(context);

            Assert.True(!string.IsNullOrEmpty(requestTelemetry.Id));
        }

        [TestMethod]
        public void OnEndSetsDurationToPositiveValue()
        {
            var context = HttpModuleHelper.GetFakeHttpContext();

            var module = this.RequestTrackingTelemetryModuleFactory();
            module.OnBeginRequest(context);
            module.OnEndRequest(context);

            Assert.True(context.GetRequestTelemetry().Duration.TotalMilliseconds >= 0);
        }

        [TestMethod]
        public void OnEndCreatesRequestTelemetryIfBeginWasNotCalled()
        {
            var context = HttpModuleHelper.GetFakeHttpContext();

            this.RequestTrackingTelemetryModuleFactory().OnEndRequest(context);

            Assert.NotNull(context.GetRequestTelemetry());
        }

        [TestMethod]
        public void OnEndSetsDurationToZeroIfBeginWasNotCalled()
        {
            var context = HttpModuleHelper.GetFakeHttpContext();
            this.RequestTrackingTelemetryModuleFactory().OnEndRequest(context);

            Assert.Equal(0, context.GetRequestTelemetry().Duration.Ticks);
        }

        [TestMethod]
        public void OnEndDoesNotOverrideResponseCode()
        {
            var context = HttpModuleHelper.GetFakeHttpContext();
            context.CreateRequestTelemetryPrivate();
            context.Response.StatusCode = 300;

            var module = this.RequestTrackingTelemetryModuleFactory();
            module.OnBeginRequest(context);
            var requestTelemetry = context.GetRequestTelemetry();
            requestTelemetry.ResponseCode = "Test";

            module.OnEndRequest(context);

            Assert.Equal("Test", requestTelemetry.ResponseCode);
        }

        [TestMethod]
        public void OnEndDoesNotOverrideUrl()
        {
            var context = HttpModuleHelper.GetFakeHttpContext();

            var module = this.RequestTrackingTelemetryModuleFactory();
            module.Initialize(TelemetryConfiguration.CreateDefault());
            module.OnBeginRequest(context);

            var requestTelemetry = context.GetRequestTelemetry();
            requestTelemetry.Url = new Uri("http://test/");

            module.OnEndRequest(context);

            Assert.Equal("http://test/", requestTelemetry.Url.OriginalString);
        }

        [TestMethod]
        public void OnEndSetsResponseCode()
        {
            var context = HttpModuleHelper.GetFakeHttpContext();
            context.Response.StatusCode = 401;

            var module = this.RequestTrackingTelemetryModuleFactory();
            module.OnBeginRequest(context);
            module.OnEndRequest(context);

            Assert.Equal("401", context.GetRequestTelemetry().ResponseCode);
        }

        [TestMethod]
        public void OnEndSetsSuccessToFalseFor400()
        {
            var context = HttpModuleHelper.GetFakeHttpContext();
            context.Response.StatusCode = 400;

            var module = this.RequestTrackingTelemetryModuleFactory();
            module.Initialize(TelemetryConfiguration.CreateDefault());
            module.OnBeginRequest(context);
            module.OnEndRequest(context);

            Assert.Equal(false, context.GetRequestTelemetry().Success);
        }

        [TestMethod]
        public void OnEndSetsSuccessToTrueFor401()
        {
            var context = HttpModuleHelper.GetFakeHttpContext();
            context.Response.StatusCode = 401;

            var module = this.RequestTrackingTelemetryModuleFactory();
            module.Initialize(TelemetryConfiguration.CreateDefault());
            module.OnBeginRequest(context);
            module.OnEndRequest(context);

            Assert.Equal(true, context.GetRequestTelemetry().Success);
        }

        [TestMethod]
        public void OnEndSetsSuccessToTrueFor200()
        {
            var context = HttpModuleHelper.GetFakeHttpContext();
            context.Response.StatusCode = 200;

            var module = this.RequestTrackingTelemetryModuleFactory();
            module.Initialize(TelemetryConfiguration.CreateDefault());
            module.OnBeginRequest(context);
            module.OnEndRequest(context);

            Assert.Equal(true, context.GetRequestTelemetry().Success);
        }

        [TestMethod]
        public void OnEndSetsUrl()
        {
            var context = HttpModuleHelper.GetFakeHttpContext();

            var module = this.RequestTrackingTelemetryModuleFactory();
            module.OnBeginRequest(context);
            module.OnEndRequest(context);

            Assert.Equal(context.Request.Url, context.GetRequestTelemetry().Url);
        }

        [TestMethod]
        public void OnEndTracksRequest()
        {
            var context = HttpModuleHelper.GetFakeHttpContext();

            var sendItems = new List<ITelemetry>();
            var stubTelemetryChannel = new StubTelemetryChannel { OnSend = item => sendItems.Add(item) };
            var configuration = new TelemetryConfiguration
            {
                InstrumentationKey = Guid.NewGuid().ToString(),
                TelemetryChannel = stubTelemetryChannel
            };

            var module = this.RequestTrackingTelemetryModuleFactory(configuration);
            module.OnBeginRequest(context);
            module.OnEndRequest(context);

            Assert.Equal(1, sendItems.Count);
        }

        [TestMethod]
        public void NeedProcessRequestReturnsFalseForDefaultHandler()
        {
            var context = HttpModuleHelper.GetFakeHttpContext();
            context.Response.StatusCode = 200;
            context.Handler = new System.Web.Handlers.AssemblyResourceLoader();

            var requestTelemetry = context.CreateRequestTelemetryPrivate();
            requestTelemetry.Start();

            var module = this.RequestTrackingTelemetryModuleFactory();
            module.Handlers.Add("System.Web.Handlers.AssemblyResourceLoader");

            Assert.False(module.NeedProcessRequest(context));
        }

        [TestMethod]
        public void NeedProcessRequestReturnsTrueForUnknownHandler()
        {
            var context = HttpModuleHelper.GetFakeHttpContext();
            context.Response.StatusCode = 200;
            context.Handler = new FakeHttpHandler();

            var requestTelemetry = context.CreateRequestTelemetryPrivate();
            requestTelemetry.Start();

            var module = this.RequestTrackingTelemetryModuleFactory();

            Assert.True(module.NeedProcessRequest(context));
        }

        [TestMethod]
        public void NeedProcessRequestReturnsFalseForCustomHandler()
        {
            var context = HttpModuleHelper.GetFakeHttpContext();
            context.Response.StatusCode = 200;
            context.Handler = new FakeHttpHandler();

            var requestTelemetry = context.CreateRequestTelemetryPrivate();
            requestTelemetry.Start();

            var module = this.RequestTrackingTelemetryModuleFactory();
            module.Handlers.Add("Microsoft.ApplicationInsights.Web.RequestTrackingTelemetryModuleTest+FakeHttpHandler");

            Assert.False(module.NeedProcessRequest(context));
        }

        [TestMethod]
        public void NeedProcessRequestReturnsTrueForNon200()
        {
            var context = HttpModuleHelper.GetFakeHttpContext();
            context.Response.StatusCode = 500;
            context.Handler = new System.Web.Handlers.AssemblyResourceLoader();

            var requestTelemetry = context.CreateRequestTelemetryPrivate();
            requestTelemetry.Start();

            var module = this.RequestTrackingTelemetryModuleFactory();

            Assert.True(module.NeedProcessRequest(context));
        }

        [TestMethod]
        public void NeedProcessRequestReturnsFalseOnNullHttpContext()
        {
            var module = this.RequestTrackingTelemetryModuleFactory();
            {
                Assert.False(module.NeedProcessRequest(null));
            }
        }

        [TestMethod]
        public void SdkVersionHasCorrectFormat()
        {
            string expectedVersion = SdkVersionHelper.GetExpectedSdkVersion(typeof(RequestTrackingTelemetryModule), prefix: "web:");

            var context = HttpModuleHelper.GetFakeHttpContext();

            var module = this.RequestTrackingTelemetryModuleFactory();
            module.OnBeginRequest(context);
            module.OnEndRequest(context);

            Assert.Equal(expectedVersion, context.GetRequestTelemetry().Context.GetInternalContext().SdkVersion);
        }

        [TestMethod]
        public void OnEndDoesNotAddSourceFieldForRequestForSameComponent()
        {
            // ARRANGE
            string ikey = "b3eb14d6-bb32-4542-9b93-473cd94aaedf";
            string requestContextContainingCorrelationId = this.GetCorrelationIdHeaderValue(ikey); // since per our mock appId = ikey

            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add(RequestResponseHeaders.RequestContextHeader, requestContextContainingCorrelationId);

            var context = HttpModuleHelper.GetFakeHttpContext(headers);

            var config = this.CreateDefaultConfig(context, instrumentationKey: ikey);
            var module = this.RequestTrackingTelemetryModuleFactory(config);

            // ACT
            module.OnBeginRequest(context);
            module.OnEndRequest(context);

            // VALIDATE
            Assert.True(string.IsNullOrEmpty(context.GetRequestTelemetry().Source), "RequestTrackingTelemetryModule should not set source for same ikey as itself.");
        }

        [TestMethod]
        public void OnEndAddsSourceFieldForRequestWithCorrelationId()
        {
            // ARRANGE                       
            string appId = "b3eb14d6-bb32-4542-9b93-473cd94aaedf";

            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add(RequestResponseHeaders.RequestContextHeader, this.GetCorrelationIdHeaderValue(appId));

            var context = HttpModuleHelper.GetFakeHttpContext(headers);

            var module = this.RequestTrackingTelemetryModuleFactory();
            var config = TelemetryConfiguration.CreateDefault();

            // My instrumentation key and hence app id is random / newly generated. The appId header is different - hence a different component.
            config.InstrumentationKey = Guid.NewGuid().ToString();

            // ACT
            module.Initialize(config);
            module.OnBeginRequest(context);
            module.OnEndRequest(context);

            // VALIDATE
            Assert.Equal(this.GetCorrelationIdValue(appId), context.GetRequestTelemetry().Source);
        }

        [TestMethod]
        public void OnEndDoesNotAddSourceFieldForRequestWithOutSourceIkeyHeader()
        {
            // ARRANGE                                   
            // do not add any sourceikey header.
            Dictionary<string, string> headers = new Dictionary<string, string>();

            var context = HttpModuleHelper.GetFakeHttpContext(headers);

            var module = this.RequestTrackingTelemetryModuleFactory();
            var config = TelemetryConfiguration.CreateDefault();
            config.InstrumentationKey = Guid.NewGuid().ToString();

            // ACT
            module.Initialize(config);
            module.OnBeginRequest(context);
            module.OnEndRequest(context);

            // VALIDATE
            Assert.True(string.IsNullOrEmpty(context.GetRequestTelemetry().Source), "RequestTrackingTelemetryModule should not set source if not sourceikey found in header");
        }

        [TestMethod]
        public void OnEndDoesNotOverrideSourceField()
        {
            // ARRANGE                       
            string appIdInHeader = this.GetCorrelationIdHeaderValue("b3eb14d6-bb32-4542-9b93-473cd94aaedf");
            string appIdInSourceField = "9AB8EDCB-21D2-44BB-A64A-C33BB4515F20";

            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add(RequestResponseHeaders.RequestContextHeader, appIdInHeader);

            var context = HttpModuleHelper.GetFakeHttpContext(headers);

            var module = this.RequestTrackingTelemetryModuleFactory();
            module.OnBeginRequest(context);
            context.GetRequestTelemetry().Source = appIdInSourceField;

            // ACT
            module.OnEndRequest(context);

            // VALIDATE
            Assert.Equal(appIdInSourceField, context.GetRequestTelemetry().Source);
        }

        [TestMethod]
        public void OnBeginSetsOperationContextWithStandardHeaders()
        {
            var context = HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string>
            {
                ["Request-Id"] = "|guid1.1",
                ["Correlation-Context"] = "k=v"
            });

            var module = this.RequestTrackingTelemetryModuleFactory();
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

            var module = this.RequestTrackingTelemetryModuleFactory();
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
        }

        [TestMethod]
        public void InitializeFromStandardHeadersAlwaysWinsCustomHeaders()
        {
            var context = HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string>
            {
                ["Request-Id"] = "standard-id",
                ["x-ms-request-id"] = "legacy-id",
                ["x-ms-request-rooit-id"] = "legacy-root-id"
            });

            var config = this.CreateDefaultConfig(context);
            var module = this.RequestTrackingTelemetryModuleFactory(config);
            module.OnBeginRequest(context);

            var requestTelemetry = context.GetRequestTelemetry();

            // initialize telemetry
            module.OnEndRequest(context);
            Assert.Equal("standard-id", requestTelemetry.Context.Operation.ParentId);
            Assert.Equal("standard-id", requestTelemetry.Context.Operation.Id);
            Assert.Equal("standard-id", this.GetActivityRootId(requestTelemetry.Id));
            Assert.NotEqual(requestTelemetry.Context.Operation.Id, requestTelemetry.Id);
        }

        [TestMethod]
        public void OnBeginSetsOperationContextWithLegacyHeaders()
        {
            var context = HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string>
            {
                ["x-ms-request-id"] = "guid1",
                ["x-ms-request-root-id"] = "guid2"
            });

            var module = this.RequestTrackingTelemetryModuleFactory();
            module.OnBeginRequest(context);
            var requestTelemetry = context.GetRequestTelemetry();
            module.OnEndRequest(context);

            Assert.Equal("guid2", requestTelemetry.Context.Operation.Id);
            Assert.Equal("guid1", requestTelemetry.Context.Operation.ParentId);

            Assert.True(requestTelemetry.Id.StartsWith("|guid2.", StringComparison.Ordinal));
        }

        [TestMethod]
        public void InitializeWithInvalidRequestId()
        {
            var context = HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string> { ["Request-Id"] = string.Empty });

            var module = this.RequestTrackingTelemetryModuleFactory(this.CreateDefaultConfig(context));
            module.OnBeginRequest(context);
            var requestTelemetry = context.GetRequestTelemetry();
            module.OnEndRequest(context);

            Assert.Null(requestTelemetry.Context.Operation.ParentId);
            Assert.NotNull(requestTelemetry.Context.Operation.Id);
            Assert.Equal(requestTelemetry.Context.Operation.Id, this.GetActivityRootId(requestTelemetry.Id));
        }

        [TestMethod]
        public void InitializeFromStandardHeaderWithHierarchicalIdAndCorrelationContextId()
        {
            // accoring to the spec: https://github.com/lmolkova/correlation/blob/master/http_protocol_proposal_v1.md
            // service that receives non-hierarchical id, gets Id from the Correlation-Context and creates requestId from it
            // so below example is not valid according to the spec
            var context = HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string>
            {
                ["Request-Id"] = "|guid1.",
                ["Correlation-Context"] = "Id=guid2"
            });

            var module = this.RequestTrackingTelemetryModuleFactory(this.CreateDefaultConfig(context));
            module.OnBeginRequest(context);
            var requestTelemetry = context.GetRequestTelemetry();

            // initialize telemetry
            module.OnEndRequest(context);

            Assert.Equal("|guid1.", requestTelemetry.Context.Operation.ParentId);
            Assert.Equal("guid1", requestTelemetry.Context.Operation.Id);
            Assert.Equal("guid1", this.GetActivityRootId(requestTelemetry.Id));

            Assert.Equal("guid2", requestTelemetry.Context.Properties["Id"]);
        }

        [TestMethod]
        public void InitializeFromStandardHeaderWithNonHierarchicalIdAndCorrelationContextId()
        {
            // accoring to the spec: https://github.com/lmolkova/correlation/blob/master/http_protocol_proposal_v1.md
            // service that receives non-hierarchical id, gets Id from the Correlation-Context and creates requestId from it
            var context = HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string>
            {
                ["Request-Id"] = "guid1",
                ["Correlation-Context"] = "Id=guid2"
            });

            var module = this.RequestTrackingTelemetryModuleFactory(this.CreateDefaultConfig(context));
            module.OnBeginRequest(context);
            var requestTelemetry = context.GetRequestTelemetry();

            // initialize telemetry
            module.OnEndRequest(context);
            Assert.Equal("guid1", requestTelemetry.Context.Operation.ParentId);
            Assert.Equal("guid2", requestTelemetry.Context.Operation.Id);
            Assert.Equal("guid2", this.GetActivityRootId(requestTelemetry.Id));
            Assert.NotEqual("guid2", requestTelemetry.Id);

            Assert.Equal("guid2", requestTelemetry.Context.Properties["Id"]);
        }

        [TestMethod]
        public void OnBeginReadsParentIdFromCustomHeader()
        {
            var context = HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string>
            {
                ["headerName"] = "ParentId"
            });

            var config = this.CreateDefaultConfig(context, parentIdHeaderName: "headerName");
            this.RequestTrackingTelemetryModuleFactory(config).OnBeginRequest(context);

            var requestTelemetry = context.GetRequestTelemetry();

            Assert.Equal("ParentId", requestTelemetry.Context.Operation.ParentId);
        }

        [TestMethod]
        public void OnBeginReadsRootIdFromCustomHeader()
        {
            var context = HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string>
            {
                ["headerName"] = "RootId"
            });

            var config = this.CreateDefaultConfig(context, rootIdHeaderName: "headerName");
            var module = this.RequestTrackingTelemetryModuleFactory(config);
            module.OnBeginRequest(context);
            var requestTelemetry = context.GetRequestTelemetry();

            module.OnEndRequest(context);
            Assert.Equal("RootId", requestTelemetry.Context.Operation.Id);

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
            var module = this.RequestTrackingTelemetryModuleFactory(config);
            module.OnBeginRequest(context);

            var requestTelemetry = context.GetRequestTelemetry();
            var telemetryClient = new TelemetryClient(config);
            var exceptionTelemetry = new ExceptionTelemetry();
            telemetryClient.Initialize(exceptionTelemetry);

            module.OnEndRequest(context);

            Assert.Equal("guid1", exceptionTelemetry.Context.Operation.Id);
            Assert.Equal(requestTelemetry.Id, exceptionTelemetry.Context.Operation.ParentId);
            Assert.Equal("v", exceptionTelemetry.Context.Properties["k"]);
        }

        [TestMethod]
        public void OnPreHandlerTelemetryCreatedWithinRequestScopeIsRequestChild()
        {
            var context = HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string>
            {
                ["Request-Id"] = "|guid1.1",
                ["Correlation-Context"] = "k=v"
            });

            var config = this.CreateDefaultConfig(context);
            var module = this.RequestTrackingTelemetryModuleFactory(config);

            module.OnBeginRequest(context);

            // simulate losing call context by cleaning up activity
            ActivityHelpers.StopRequestActivity();

            // CallContext was lost after OnBegin, so OnPreRequestHandlerExecute will set it
            module.OnPreRequestHandlerExecute(context);

            // if OnPreRequestHandlerExecute set a CallContext, child telemetry will be properly filled
            var telemetryClient = new TelemetryClient(config);

            var trace = new TraceTelemetry();
            telemetryClient.TrackTrace(trace);
            var requestTelemetry = context.GetRequestTelemetry();

            Assert.Equal(requestTelemetry.Context.Operation.Id, trace.Context.Operation.Id);
#if NET40
            Assert.Equal(requestTelemetry.Id, trace.Context.Operation.ParentId);
#else
            // we created Activity for request and assigned Id for the request like guid1.1.12345_
            // then we lost it and restored (started a new child activity), so the Id is guid1.1.12345_abc_
            // so the request is grand parent to the trace
            Assert.Equal(Activity.Current.ParentId, requestTelemetry.Id);
            Assert.True(trace.Context.Operation.ParentId.StartsWith(requestTelemetry.Id, StringComparison.Ordinal));
            Assert.Equal(Activity.Current.Id, trace.Context.Operation.ParentId);
#endif
            Assert.Equal("v", trace.Context.Properties["k"]);
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

            module.OnBeginRequest(context);

            // simulate losing call context by cleaning up activity
            ActivityHelpers.StopRequestActivity();

            var telemetryClient = new TelemetryClient(config);

            var trace = new TraceTelemetry();
            telemetryClient.TrackTrace(trace);
            var requestTelemetry = context.GetRequestTelemetry();

            Assert.Equal(requestTelemetry.Context.Operation.Id, trace.Context.Operation.Id);
#if NET40
            Assert.Equal(requestTelemetry.Id, trace.Context.Operation.ParentId);
#else
            // we created Activity for request and assigned Id for the request like guid1.1.12345
            // then we created Activity for request children and assigned it Id like guid1.1.12345_1
            // then we lost it and restored (started a new child activity), so the Id is guid1.1.123_1.abc
            // so the request is grand parent to the trace
            Assert.True(trace.Context.Operation.ParentId.StartsWith(requestTelemetry.Id, StringComparison.Ordinal));
#endif
            Assert.Equal("v", trace.Context.Properties["k"]);
        }

        private TelemetryConfiguration CreateDefaultConfig(HttpContext fakeContext, string rootIdHeaderName = null, string parentIdHeaderName = null, string instrumentationKey = null)
        {
            var config = TelemetryConfiguration.CreateDefault();
            var telemetryInitializer = new TestableOperationCorrelationTelemetryInitializer(fakeContext);

            if (rootIdHeaderName != null)
            {
                telemetryInitializer.RootOperationIdHeaderName = rootIdHeaderName;
            }

            if (parentIdHeaderName != null)
            {
                telemetryInitializer.ParentOperationIdHeaderName = parentIdHeaderName;
            }

            config.TelemetryInitializers.Add(telemetryInitializer);
            config.InstrumentationKey = instrumentationKey ?? Guid.NewGuid().ToString();
            return config;
        }

        private string GetActivityRootId(string telemetryId)
        {
            return telemetryId.Substring(1, telemetryId.IndexOf('.') - 1);
        }

        private RequestTrackingTelemetryModule RequestTrackingTelemetryModuleFactory(TelemetryConfiguration config = null)
        {
            var module = new RequestTrackingTelemetryModule();
            module.OverrideCorrelationIdLookupHelper(this.correlationIdLookupHelper);
            module.Initialize(config ?? this.CreateDefaultConfig(HttpModuleHelper.GetFakeHttpContext()));
            return module;
        }

        private string GetCorrelationIdValue(string appId)
        {
            return string.Format(CultureInfo.InvariantCulture, "cid-v1:{0}", appId);
        }

        private string GetCorrelationIdHeaderValue(string appId)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}=cid-v1:{1}", RequestResponseHeaders.RequestContextSourceKey, appId);
        }

        internal class FakeHttpHandler : IHttpHandler
        {
            bool IHttpHandler.IsReusable
            {
                get { return false; }
            }

            public void ProcessRequest(System.Web.HttpContext context)
            {
            }
        }

        private class TestableOperationCorrelationTelemetryInitializer : OperationCorrelationTelemetryInitializer
        {
            private readonly HttpContext fakeContext;

            public TestableOperationCorrelationTelemetryInitializer(HttpContext fakeContext)
            {
                this.fakeContext = fakeContext;
            }

            public HttpContext FakeContext
            {
                get { return this.fakeContext; }
            }

            protected override HttpContext ResolvePlatformContext()
            {
                return this.fakeContext;
            }
        }
    }
}