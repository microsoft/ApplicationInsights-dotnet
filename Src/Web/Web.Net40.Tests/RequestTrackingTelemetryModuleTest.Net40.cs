namespace Microsoft.ApplicationInsights.Web
{
    using System;
    using System.Collections.Generic;
    using System.Web;

    using Microsoft.ApplicationInsights.Common;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Web.Helpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Assert = Xunit.Assert;

    /// <summary>
    /// NET 4.0 specific tests for RequestTrackingTelemetryModule.
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
                ["Request-Id"] = "|standard-id.",
                ["x-ms-request-id"] = "legacy-id",
                ["x-ms-request-rooit-id"] = "legacy-root-id"
            });

            var config = this.CreateDefaultConfig(context);
            var module = this.RequestTrackingTelemetryModuleFactory(config);
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
        public void OnBeginSetsOperationContextWithLegacyHeaders()
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
        public void OnBeginReadsRootAndParentIdFromCustomHeader()
        {
            var context = HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string>
            {
                ["parentHeaderName"] = "ParentId",
                ["rootHeaderName"] = "RootId"
            });

            var config = this.CreateDefaultConfig(context, rootIdHeaderName: "rootHeaderName", parentIdHeaderName: "parentHeaderName");
            this.RequestTrackingTelemetryModuleFactory(config).OnBeginRequest(context);

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
            ActivityHelpers.CleanOperationContext();

            // CallContext was lost after OnBegin, so OnPreRequestHandlerExecute will set it
            module.OnPreRequestHandlerExecute(context);

            // if OnPreRequestHandlerExecute set a CallContext, child telemetry will be properly filled
            var telemetryClient = new TelemetryClient(config);

            var trace = new TraceTelemetry();
            telemetryClient.TrackTrace(trace);
            var requestTelemetry = context.GetRequestTelemetry();

            Assert.Equal(requestTelemetry.Context.Operation.Id, trace.Context.Operation.Id);
            Assert.Equal(requestTelemetry.Id, trace.Context.Operation.ParentId);
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
            ActivityHelpers.CleanOperationContext();

            var telemetryClient = new TelemetryClient(config);

            var trace = new TraceTelemetry();
            telemetryClient.TrackTrace(trace);
            var requestTelemetry = context.GetRequestTelemetry();

            Assert.Equal(requestTelemetry.Context.Operation.Id, trace.Context.Operation.Id);
            Assert.Equal(requestTelemetry.Id, trace.Context.Operation.ParentId);
            Assert.Equal("v", trace.Context.Properties["k"]);
        }
    }
}