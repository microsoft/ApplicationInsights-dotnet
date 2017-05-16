namespace Microsoft.ApplicationInsights.DependencyCollector
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Net.Http;

    using Implementation;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Common;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.TestFramework;
#if !NETCORE
    using Microsoft.ApplicationInsights.Web.TestFramework;
#endif
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Unit tests for DependencyCollectorDiagnosticListener.
    /// </summary>
    [TestClass]
    public partial class DependencyCollectorDiagnosticListenerTests
    {
        private const string RequestUrl = "www.example.com";
        private const string RequestUrlWithScheme = "https://" + RequestUrl;
        private const string HttpOkResultCode = "200";
        private const string NotFoundResultCode = "404";
        private const string MockAppId = "MOCK_APP_ID";
        private const string MockAppId2 = "MOCK_APP_ID_2";

        private readonly List<ITelemetry> sentTelemetry = new List<ITelemetry>();

        private string instrumentationKey;
        private StubTelemetryChannel telemetryChannel;
        private MockCorrelationIdLookupHelper mockCorrelationIdLookupHelper;
        private HttpCoreDiagnosticSourceListener listener;

        /// <summary>
        /// Initialize function that gets called once before any tests in this class are run.
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            this.instrumentationKey = Guid.NewGuid().ToString();

            this.telemetryChannel = new StubTelemetryChannel()
            {
                EndpointAddress = "https://endpointaddress",
                OnSend = this.sentTelemetry.Add
            };

            this.mockCorrelationIdLookupHelper = new MockCorrelationIdLookupHelper(new Dictionary<string, string>()
            {
                [this.instrumentationKey] = MockAppId
            });

            var configuration = new TelemetryConfiguration
            {
                TelemetryChannel = this.telemetryChannel,
                InstrumentationKey = this.instrumentationKey,
            };

            configuration.TelemetryInitializers.Add(new OperationCorrelationTelemetryInitializer());
            this.listener = new HttpCoreDiagnosticSourceListener(
                configuration,
                this.telemetryChannel.EndpointAddress,
                setComponentCorrelationHttpHeaders: true,
                correlationDomainExclusionList: new string[] { "excluded.host.com" },
                correlationIdLookupHelper: this.mockCorrelationIdLookupHelper);
        }

        /// <summary>
        /// Cleans up.
        /// </summary>
        [TestCleanup]
        public void Cleanup()
        {
            while (Activity.Current != null)
            {
                Activity.Current.Stop();
            }

            this.listener?.Dispose();
        }

        /// <summary>
        /// Call OnRequest() with no uri in the HttpRequestMessage.
        /// </summary>
        [TestMethod]
        public void OnRequestWithRequestEventWithNoRequestUri()
        {
            var request = new HttpRequestMessage();

            this.listener.OnRequest(request, Guid.NewGuid());

            IOperationHolder<DependencyTelemetry> dependency;
            Assert.IsFalse(this.listener.PendingDependencyTelemetry.TryGetValue(request, out dependency));
            Assert.AreEqual(0, this.sentTelemetry.Count);
        }

        /// <summary>
        /// Call OnRequest() with uri that is in the excluded domain list.
        /// </summary>
        [TestMethod]
        public void OnRequestWithUriInExcludedDomainList()
        {
            Guid loggingRequestId = Guid.NewGuid();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "http://excluded.host.com/path/to/file.html");
            this.listener.OnRequest(request, loggingRequestId);

            IOperationHolder<DependencyTelemetry> dependency;
            Assert.IsTrue(this.listener.PendingDependencyTelemetry.TryGetValue(request, out dependency));
            Assert.AreEqual(0, this.sentTelemetry.Count);

            Assert.IsNull(HttpHeadersUtilities.GetRequestContextKeyValue(request.Headers, RequestResponseHeaders.RequestContextCorrelationSourceKey));
            Assert.IsNull(HttpHeadersUtilities.GetRequestContextKeyValue(request.Headers, RequestResponseHeaders.RequestIdHeader));
            Assert.IsNull(HttpHeadersUtilities.GetRequestContextKeyValue(request.Headers, RequestResponseHeaders.StandardParentIdHeader));
            Assert.IsNull(HttpHeadersUtilities.GetRequestContextKeyValue(request.Headers, RequestResponseHeaders.StandardRootIdHeader));
        }

        /// <summary>
        /// Call OnRequest() with valid arguments.
        /// </summary>
        [TestMethod]
        public void OnRequestWithRequestEvent()
        {
            Guid loggingRequestId = Guid.NewGuid();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, RequestUrlWithScheme);
            this.listener.OnRequest(request, loggingRequestId);

            IOperationHolder<DependencyTelemetry> dependency;
            Assert.IsTrue(this.listener.PendingDependencyTelemetry.TryGetValue(request, out dependency));

            DependencyTelemetry telemetry = dependency.Telemetry;
            Assert.AreEqual("POST /", telemetry.Name);
            Assert.AreEqual(RequestUrl, telemetry.Target);
            Assert.AreEqual(RemoteDependencyConstants.HTTP, telemetry.Type);
            Assert.AreEqual(RequestUrlWithScheme, telemetry.Data);
            Assert.AreEqual(string.Empty, telemetry.ResultCode);
            Assert.AreEqual(true, telemetry.Success);

            Assert.AreEqual(MockAppId, GetRequestContextKeyValue(request, RequestResponseHeaders.RequestContextCorrelationSourceKey));
            Assert.AreEqual(null, GetRequestContextKeyValue(request, RequestResponseHeaders.StandardRootIdHeader));

            var legacyParentIdHeader = GetRequestHeaderValues(request, RequestResponseHeaders.StandardParentIdHeader).Single();
            var requestIdHeader = GetRequestHeaderValues(request, RequestResponseHeaders.RequestIdHeader).Single();
            Assert.IsFalse(string.IsNullOrEmpty(legacyParentIdHeader));
            Assert.IsFalse(string.IsNullOrEmpty(requestIdHeader));
            Assert.AreEqual(requestIdHeader, legacyParentIdHeader);
            Assert.AreEqual(0, this.sentTelemetry.Count);
        }

        /// <summary>
        /// Call OnResponse() when no matching OnRequest() call has been made.
        /// </summary>
        [TestMethod]
        public void OnResponseWithResponseEventButNoMatchingRequest()
        {
            var response = new HttpResponseMessage
            {
                RequestMessage = new HttpRequestMessage(HttpMethod.Get, RequestUrlWithScheme)
            };

            this.listener.OnResponse(response, Guid.NewGuid());
            Assert.AreEqual(0, this.sentTelemetry.Count);
        }

        /// <summary>
        /// Call OnResponse() with a successful request but no target instrumentation key in the response headers.
        /// </summary>
        [TestMethod]
        public void OnResponseWithSuccessfulResponseEventWithMatchingRequestAndNoTargetInstrumentationKeyHasHeader()
        {
            Guid loggingRequestId = Guid.NewGuid();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, RequestUrlWithScheme);
            this.listener.OnRequest(request, loggingRequestId);

            IOperationHolder<DependencyTelemetry> dependency;
            Assert.IsTrue(this.listener.PendingDependencyTelemetry.TryGetValue(request, out dependency));

            Assert.AreEqual(0, this.sentTelemetry.Count);

            DependencyTelemetry telemetry = dependency.Telemetry;
            Assert.AreEqual(string.Empty, telemetry.ResultCode);
            Assert.AreEqual(true, telemetry.Success);

            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                RequestMessage = request
            };

            this.listener.OnResponse(response, loggingRequestId);
            Assert.IsFalse(this.listener.PendingDependencyTelemetry.TryGetValue(request, out dependency));
            Assert.AreEqual(1, this.sentTelemetry.Count);
            Assert.AreSame(telemetry, this.sentTelemetry.Single());

            Assert.AreEqual(RemoteDependencyConstants.HTTP, telemetry.Type);
            Assert.AreEqual(RequestUrl, telemetry.Target);
            Assert.AreEqual(HttpOkResultCode, telemetry.ResultCode);
            Assert.AreEqual(true, telemetry.Success);

            string expectedVersion =
                SdkVersionHelper.GetExpectedSdkVersion(typeof(DependencyTrackingTelemetryModule), prefix: "rdddsc:");
            Assert.AreEqual(expectedVersion, telemetry.Context.GetInternalContext().SdkVersion);
        }

        /// <summary>
        /// Call OnResponse() with a not found request result but no target instrumentation key in the response headers.
        /// </summary>
        [TestMethod]
        public void OnResponseWithNotFoundResponseEventWithMatchingRequestAndNoTargetInstrumentationKeyHasHeader()
        {
            Guid loggingRequestId = Guid.NewGuid();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, RequestUrlWithScheme);
            this.listener.OnRequest(request, loggingRequestId);

            IOperationHolder<DependencyTelemetry> dependency;
            Assert.IsTrue(this.listener.PendingDependencyTelemetry.TryGetValue(request, out dependency));

            Assert.AreEqual(0, this.sentTelemetry.Count);

            DependencyTelemetry telemetry = dependency.Telemetry;
            Assert.AreEqual(string.Empty, telemetry.ResultCode);
            Assert.AreEqual(true, telemetry.Success);

            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                RequestMessage = request
            };

            this.listener.OnResponse(response, loggingRequestId);
            Assert.IsFalse(this.listener.PendingDependencyTelemetry.TryGetValue(request, out dependency));
            Assert.AreEqual(1, this.sentTelemetry.Count);
            Assert.AreSame(telemetry, this.sentTelemetry.Single());

            Assert.AreEqual(RemoteDependencyConstants.HTTP, telemetry.Type);
            Assert.AreEqual(RequestUrl, telemetry.Target);
            Assert.AreEqual(NotFoundResultCode, telemetry.ResultCode);
            Assert.AreEqual(false, telemetry.Success);
        }

        /// <summary>
        /// Call OnResponse() with a successful request and same target instrumentation key in the response headers as the source instrumentation key.
        /// </summary>
        [TestMethod]
        public void OnResponseWithSuccessfulResponseEventWithMatchingRequestAndSameTargetInstrumentationKeyHashHeader()
        {
            Guid loggingRequestId = Guid.NewGuid();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, RequestUrlWithScheme);
            this.listener.OnRequest(request, loggingRequestId);
            IOperationHolder<DependencyTelemetry> dependency;
            Assert.IsTrue(this.listener.PendingDependencyTelemetry.TryGetValue(request, out dependency));

            Assert.AreEqual(0, this.sentTelemetry.Count);

            DependencyTelemetry telemetry = dependency.Telemetry;
            Assert.AreEqual(string.Empty, telemetry.ResultCode);
            Assert.AreEqual(true, telemetry.Success);

            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                RequestMessage = request
            };

            response.Headers.Add(RequestResponseHeaders.RequestContextCorrelationTargetKey, MockAppId);

            this.listener.OnResponse(response, loggingRequestId);
            Assert.IsFalse(this.listener.PendingDependencyTelemetry.TryGetValue(request, out dependency));
            Assert.AreEqual(1, this.sentTelemetry.Count);
            Assert.AreSame(telemetry, this.sentTelemetry.Single());

            Assert.AreEqual(RemoteDependencyConstants.HTTP, telemetry.Type);
            Assert.AreEqual(RequestUrl, telemetry.Target);
            Assert.AreEqual(HttpOkResultCode, telemetry.ResultCode);
            Assert.AreEqual(true, telemetry.Success);
        }

        /// <summary>
        /// Call OnResponse() with a not found request result code and same target instrumentation key in the response headers as the source instrumentation key.
        /// </summary>
        [TestMethod]
        public void OnResponseWithFailedResponseEventWithMatchingRequestAndSameTargetInstrumentationKeyHashHeader()
        {
            Guid loggingRequestId = Guid.NewGuid();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, RequestUrlWithScheme);
            this.listener.OnRequest(request, loggingRequestId);
            IOperationHolder<DependencyTelemetry> dependency;
            Assert.IsTrue(this.listener.PendingDependencyTelemetry.TryGetValue(request, out dependency));
            Assert.AreEqual(0, this.sentTelemetry.Count);

            DependencyTelemetry telemetry = dependency.Telemetry;
            Assert.AreEqual(string.Empty, telemetry.ResultCode);
            Assert.AreEqual(true, telemetry.Success);

            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                RequestMessage = request
            };

            response.Headers.Add(RequestResponseHeaders.RequestContextCorrelationTargetKey, MockAppId);

            this.listener.OnResponse(response, loggingRequestId);
            Assert.IsFalse(this.listener.PendingDependencyTelemetry.TryGetValue(request, out dependency));
            Assert.AreEqual(1, this.sentTelemetry.Count);
            Assert.AreSame(telemetry, this.sentTelemetry.Single());

            Assert.AreEqual(RemoteDependencyConstants.HTTP, telemetry.Type);
            Assert.AreEqual(RequestUrl, telemetry.Target);
            Assert.AreEqual(NotFoundResultCode, telemetry.ResultCode);
            Assert.AreEqual(false, telemetry.Success);
        }

        /// <summary>
        /// Call OnResponse() with a successful request and different target instrumentation key in the response headers than the source instrumentation key.
        /// </summary>
        [TestMethod]
        public void OnResponseWithSuccessfulResponseEventWithMatchingRequestAndDifferentTargetInstrumentationKeyHashHeader()
        {
            Guid loggingRequestId = Guid.NewGuid();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, RequestUrlWithScheme);
            this.listener.OnRequest(request, loggingRequestId);
            IOperationHolder<DependencyTelemetry> dependency;
            Assert.IsTrue(this.listener.PendingDependencyTelemetry.TryGetValue(request, out dependency));
            Assert.AreEqual(0, this.sentTelemetry.Count);

            DependencyTelemetry telemetry = dependency.Telemetry;
            Assert.AreEqual(string.Empty, telemetry.ResultCode);
            Assert.AreEqual(true, telemetry.Success);

            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                RequestMessage = request
            };

            string targetApplicationId = MockAppId2;
            HttpHeadersUtilities.SetRequestContextKeyValue(response.Headers, RequestResponseHeaders.RequestContextCorrelationTargetKey, targetApplicationId);

            this.listener.OnResponse(response, loggingRequestId);
            Assert.IsFalse(this.listener.PendingDependencyTelemetry.TryGetValue(request, out dependency));
            Assert.AreEqual(1, this.sentTelemetry.Count);
            Assert.AreSame(telemetry, this.sentTelemetry.Single());

            Assert.AreEqual(RemoteDependencyConstants.AI, telemetry.Type);
            Assert.AreEqual(GetApplicationInsightsTarget(targetApplicationId), telemetry.Target);
            Assert.AreEqual(HttpOkResultCode, telemetry.ResultCode);
            Assert.AreEqual(true, telemetry.Success);
        }

        /// <summary>
        /// Call OnResponse() with a not found request result code and different target instrumentation key in the response headers than the source instrumentation key.
        /// </summary>
        [TestMethod]
        public void OnResponseWithFailedResponseEventWithMatchingRequestAndDifferentTargetInstrumentationKeyHashHeader()
        {
            Guid loggingRequestId = Guid.NewGuid();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, RequestUrlWithScheme);
            this.listener.OnRequest(request, loggingRequestId);
            IOperationHolder<DependencyTelemetry> dependency;
            Assert.IsTrue(this.listener.PendingDependencyTelemetry.TryGetValue(request, out dependency));
            Assert.AreEqual(0, this.sentTelemetry.Count);

            DependencyTelemetry telemetry = dependency.Telemetry;
            Assert.AreEqual(string.Empty, telemetry.ResultCode);
            Assert.AreEqual(true, telemetry.Success);

            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                RequestMessage = request
            };

            string targetApplicationId = MockAppId2;
            HttpHeadersUtilities.SetRequestContextKeyValue(response.Headers, RequestResponseHeaders.RequestContextCorrelationTargetKey, targetApplicationId);

            this.listener.OnResponse(response, loggingRequestId);
            Assert.IsFalse(this.listener.PendingDependencyTelemetry.TryGetValue(request, out dependency));
            Assert.AreEqual(1, this.sentTelemetry.Count);
            Assert.AreSame(telemetry, this.sentTelemetry.Single());

            Assert.AreEqual(RemoteDependencyConstants.AI, telemetry.Type);
            Assert.AreEqual(GetApplicationInsightsTarget(targetApplicationId), telemetry.Target);
            Assert.AreEqual(NotFoundResultCode, telemetry.ResultCode);
            Assert.AreEqual(false, telemetry.Success);
        }

        /// <summary>
        /// Tests that outgoing request has proper context when done in scope of incoming request (incoming request activity).
        /// </summary>
        [TestMethod]
        public void OnResponseWithParentActivity()
        {
            var parentActivity = new Activity("incoming_request");
            parentActivity.AddBaggage("k1", "v1");
            parentActivity.AddBaggage("k2", "v2");
            parentActivity.Start();

            Guid loggingRequestId = Guid.NewGuid();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, RequestUrlWithScheme);
            this.listener.OnRequest(request, loggingRequestId);
            Assert.IsNotNull(Activity.Current);

            var parentId = request.Headers.GetValues(RequestResponseHeaders.RequestIdHeader).Single();
            Assert.AreEqual(Activity.Current.Id, parentId);

            var correlationContextHeader = request.Headers.GetValues(RequestResponseHeaders.CorrelationContextHeader).ToArray();
            Assert.AreEqual(2, correlationContextHeader.Length);
            Assert.IsTrue(correlationContextHeader.Contains("k1=v1"));
            Assert.IsTrue(correlationContextHeader.Contains("k2=v2"));

            IOperationHolder<DependencyTelemetry> dependency;
            Assert.IsTrue(this.listener.PendingDependencyTelemetry.TryGetValue(request, out dependency));

            DependencyTelemetry telemetry = dependency.Telemetry;

            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                RequestMessage = request
            };

            this.listener.OnResponse(response, loggingRequestId);
            Assert.AreEqual(parentActivity, Activity.Current);
            Assert.AreEqual(parentId, telemetry.Id);
            Assert.AreEqual(parentActivity.RootId, telemetry.Context.Operation.Id);
            Assert.AreEqual(parentActivity.Id, telemetry.Context.Operation.ParentId);

            parentActivity.Stop();
        }

        private static string GetApplicationInsightsTarget(string targetApplicationId)
        {
            return $"{RequestUrl} | {targetApplicationId}";
        }

        private static IEnumerable<string> GetRequestHeaderValues(HttpRequestMessage request, string headerName)
        {
            return HttpHeadersUtilities.GetHeaderValues(request.Headers, headerName);
        }

        private static string GetRequestContextKeyValue(HttpRequestMessage request, string keyName)
        {
            return HttpHeadersUtilities.GetRequestContextKeyValue(request.Headers, keyName);
        }
    }
}