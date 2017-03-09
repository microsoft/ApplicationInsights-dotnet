namespace Microsoft.ApplicationInsights.DependencyCollector
{
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Common;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;

    /// <summary>
    /// Unit tests for DependencyCollectorDiagnosticListener.
    /// </summary>
    [TestClass]
    public class DependencyCollectorDiagnosticListenerTests
    {
        private const string requestUrl = "www.example.com";
        private const string requestUrlWithScheme = "https://" + requestUrl;
        private const string httpType = "Http";
        private const string applicationInsightsType = "Application Insights";
        private const string okResultCode = "200";
        private const string notFoundResultCode = "404";

        private static string GetApplicationInsightsTarget(string targetInstrumentationKeyHash)
        {
            return $"{requestUrl} | {targetInstrumentationKeyHash}";
        }

        private string instrumentationKey;
        private StubTelemetryChannel telemetryChannel;
        private DependencyCollectorDiagnosticListener listener;

        private List<ITelemetry> sentTelemetry = new List<ITelemetry>();

        /// <summary>
        /// Initialize function that gets called once before any tests in this class are run.
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            instrumentationKey = Guid.NewGuid().ToString();

            telemetryChannel = new StubTelemetryChannel()
            {
                EndpointAddress = "https://endpointaddress",
                OnSend = sentTelemetry.Add
            };

            listener = new DependencyCollectorDiagnosticListener(new TelemetryConfiguration()
            {
                TelemetryChannel = telemetryChannel,
                InstrumentationKey = instrumentationKey,
            });
        }

        /// <summary>
        /// Call OnRequest() with no uri in the HttpRequestMessage.
        /// </summary>
        [TestMethod]
        public void OnRequestWithRequestEventWithNoRequestUri()
        {
            listener.OnRequest(new HttpRequestMessage(), Guid.NewGuid());
            Assert.AreEqual(0, listener.PendingDependencyTelemetry.Count());
            Assert.AreEqual(0, sentTelemetry.Count);
        }

        /// <summary>
        /// Call OnRequest() with valid arguments.
        /// </summary>
        [TestMethod]
        public void OnRequestWithRequestEvent()
        {
            Guid loggingRequestId = Guid.NewGuid();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, requestUrlWithScheme);
            listener.OnRequest(request, loggingRequestId);

            Assert.AreEqual(1, listener.PendingDependencyTelemetry.Count());
            DependencyTelemetry telemetry = listener.PendingDependencyTelemetry.Single();
            Assert.AreEqual("POST /", telemetry.Name);
            Assert.AreEqual(requestUrl, telemetry.Target);
            Assert.AreEqual(httpType, telemetry.Type);
            Assert.AreEqual(requestUrlWithScheme, telemetry.Data);
            Assert.AreEqual("", telemetry.ResultCode);
            Assert.AreEqual(true, telemetry.Success);

            Assert.AreEqual(InstrumentationKeyHashLookupHelper.GetInstrumentationKeyHash(instrumentationKey), GetRequestHeaderValues(request, DependencyCollectorDiagnosticListener.SourceInstrumentationKeyHeader).SingleOrDefault());
            Assert.IsFalse(GetRequestHeaderValues(request, DependencyCollectorDiagnosticListener.StandardRootIdHeader).Any());
            Assert.IsTrue(GetRequestHeaderValues(request, DependencyCollectorDiagnosticListener.StandardParentIdHeader).Any());

            Assert.AreEqual(0, sentTelemetry.Count);
        }

        private static IEnumerable<string> GetRequestHeaderValues(HttpRequestMessage request, string headerName)
        {
            return request != null && request.Headers != null && request.Headers.Contains(headerName) ? request.Headers.GetValues(headerName) : Enumerable.Empty<string>();
        }

        /// <summary>
        /// Call OnResponse() when no matching OnRequest() call has been made.
        /// </summary>
        [TestMethod]
        public void OnResponseWithResponseEventButNoMatchingRequest()
        {
            listener.OnResponse(new HttpResponseMessage(), Guid.NewGuid());
            Assert.AreEqual(0, listener.PendingDependencyTelemetry.Count());
            Assert.AreEqual(0, sentTelemetry.Count);
        }

        /// <summary>
        /// Call OnResponse() with a successful request but no target instrumentation key in the response headers.
        /// </summary>
        [TestMethod]
        public void OnResponseWithSuccessfulResponseEventWithMatchingRequestAndNoTargetInstrumentationKeyHasHeader()
        {
            Guid loggingRequestId = Guid.NewGuid();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, requestUrlWithScheme);
            listener.OnRequest(request, loggingRequestId);
            Assert.AreEqual(1, listener.PendingDependencyTelemetry.Count());
            Assert.AreEqual(0, sentTelemetry.Count);

            DependencyTelemetry telemetry = listener.PendingDependencyTelemetry.Single();
            Assert.AreEqual("", telemetry.ResultCode);
            Assert.AreEqual(true, telemetry.Success);

            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
            listener.OnResponse(response, loggingRequestId);
            Assert.AreEqual(0, listener.PendingDependencyTelemetry.Count());
            Assert.AreEqual(1, sentTelemetry.Count);
            Assert.AreSame(telemetry, sentTelemetry.Single());

            Assert.AreEqual(httpType, telemetry.Type);
            Assert.AreEqual(requestUrl, telemetry.Target);
            Assert.AreEqual(okResultCode, telemetry.ResultCode);
            Assert.AreEqual(true, telemetry.Success);
        }

        /// <summary>
        /// Call OnResponse() with a not found request result but no target instrumentation key in the response headers.
        /// </summary>
        [TestMethod]
        public void OnResponseWithNotFoundResponseEventWithMatchingRequestAndNoTargetInstrumentationKeyHasHeader()
        {
            Guid loggingRequestId = Guid.NewGuid();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, requestUrlWithScheme);
            listener.OnRequest(request, loggingRequestId);
            Assert.AreEqual(1, listener.PendingDependencyTelemetry.Count());
            Assert.AreEqual(0, sentTelemetry.Count);

            DependencyTelemetry telemetry = listener.PendingDependencyTelemetry.Single();
            Assert.AreEqual("", telemetry.ResultCode);
            Assert.AreEqual(true, telemetry.Success);

            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.NotFound);
            listener.OnResponse(response, loggingRequestId);
            Assert.AreEqual(0, listener.PendingDependencyTelemetry.Count());
            Assert.AreEqual(1, sentTelemetry.Count);
            Assert.AreSame(telemetry, sentTelemetry.Single());

            Assert.AreEqual(httpType, telemetry.Type);
            Assert.AreEqual(requestUrl, telemetry.Target);
            Assert.AreEqual(notFoundResultCode, telemetry.ResultCode);
            Assert.AreEqual(false, telemetry.Success);
        }

        /// <summary>
        /// Call OnResponse() with a successful request and same target instrumentation key in the response headers as the source instrumentation key.
        /// </summary>
        [TestMethod]
        public void OnResponseWithSuccessfulResponseEventWithMatchingRequestAndSameTargetInstrumentationKeyHashHeader()
        {
            Guid loggingRequestId = Guid.NewGuid();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, requestUrlWithScheme);
            listener.OnRequest(request, loggingRequestId);
            Assert.AreEqual(1, listener.PendingDependencyTelemetry.Count());
            Assert.AreEqual(0, sentTelemetry.Count);

            DependencyTelemetry telemetry = listener.PendingDependencyTelemetry.Single();
            Assert.AreEqual("", telemetry.ResultCode);
            Assert.AreEqual(true, telemetry.Success);

            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
            string targetInstrumentationKeyHash = InstrumentationKeyHashLookupHelper.GetInstrumentationKeyHash(instrumentationKey);
            response.Headers.Add(DependencyCollectorDiagnosticListener.TargetInstrumentationKeyHeader, targetInstrumentationKeyHash);

            listener.OnResponse(response, loggingRequestId);
            Assert.AreEqual(0, listener.PendingDependencyTelemetry.Count());
            Assert.AreEqual(1, sentTelemetry.Count);
            Assert.AreSame(telemetry, sentTelemetry.Single());

            Assert.AreEqual(httpType, telemetry.Type);
            Assert.AreEqual(requestUrl, telemetry.Target);
            Assert.AreEqual(okResultCode, telemetry.ResultCode);
            Assert.AreEqual(true, telemetry.Success);
        }

        /// <summary>
        /// Call OnResponse() with a not found request result code and same target instrumentation key in the response headers as the source instrumentation key.
        /// </summary>
        [TestMethod]
        public void OnResponseWithFailedResponseEventWithMatchingRequestAndSameTargetInstrumentationKeyHashHeader()
        {
            Guid loggingRequestId = Guid.NewGuid();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, requestUrlWithScheme);
            listener.OnRequest(request, loggingRequestId);
            Assert.AreEqual(1, listener.PendingDependencyTelemetry.Count());
            Assert.AreEqual(0, sentTelemetry.Count);

            DependencyTelemetry telemetry = listener.PendingDependencyTelemetry.Single();
            Assert.AreEqual("", telemetry.ResultCode);
            Assert.AreEqual(true, telemetry.Success);

            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.NotFound);
            string targetInstrumentationKeyHash = InstrumentationKeyHashLookupHelper.GetInstrumentationKeyHash(instrumentationKey);
            response.Headers.Add(DependencyCollectorDiagnosticListener.TargetInstrumentationKeyHeader, targetInstrumentationKeyHash);

            listener.OnResponse(response, loggingRequestId);
            Assert.AreEqual(0, listener.PendingDependencyTelemetry.Count());
            Assert.AreEqual(1, sentTelemetry.Count);
            Assert.AreSame(telemetry, sentTelemetry.Single());

            Assert.AreEqual(httpType, telemetry.Type);
            Assert.AreEqual(requestUrl, telemetry.Target);
            Assert.AreEqual(notFoundResultCode, telemetry.ResultCode);
            Assert.AreEqual(false, telemetry.Success);
        }

        /// <summary>
        /// Call OnResponse() with a successful request and different target instrumentation key in the response headers than the source instrumentation key.
        /// </summary>
        [TestMethod]
        public void OnResponseWithSuccessfulResponseEventWithMatchingRequestAndDifferentTargetInstrumentationKeyHashHeader()
        {
            Guid loggingRequestId = Guid.NewGuid();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, requestUrlWithScheme);
            listener.OnRequest(request, loggingRequestId);
            Assert.AreEqual(1, listener.PendingDependencyTelemetry.Count());
            Assert.AreEqual(0, sentTelemetry.Count);

            DependencyTelemetry telemetry = listener.PendingDependencyTelemetry.Single();
            Assert.AreEqual("", telemetry.ResultCode);
            Assert.AreEqual(true, telemetry.Success);

            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
            string targetInstrumentationKeyHash = InstrumentationKeyHashLookupHelper.GetInstrumentationKeyHash(Guid.NewGuid().ToString());
            response.Headers.Add(DependencyCollectorDiagnosticListener.TargetInstrumentationKeyHeader, targetInstrumentationKeyHash);

            listener.OnResponse(response, loggingRequestId);
            Assert.AreEqual(0, listener.PendingDependencyTelemetry.Count());
            Assert.AreEqual(1, sentTelemetry.Count);
            Assert.AreSame(telemetry, sentTelemetry.Single());

            Assert.AreEqual(applicationInsightsType, telemetry.Type);
            Assert.AreEqual(GetApplicationInsightsTarget(targetInstrumentationKeyHash), telemetry.Target);
            Assert.AreEqual(okResultCode, telemetry.ResultCode);
            Assert.AreEqual(true, telemetry.Success);
        }

        /// <summary>
        /// Call OnResponse() with a not found request result code and different target instrumentation key in the response headers than the source instrumentation key.
        /// </summary>
        [TestMethod]
        public void OnResponseWithFailedResponseEventWithMatchingRequestAndDifferentTargetInstrumentationKeyHashHeader()
        {
            Guid loggingRequestId = Guid.NewGuid();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, requestUrlWithScheme);
            listener.OnRequest(request, loggingRequestId);
            Assert.AreEqual(1, listener.PendingDependencyTelemetry.Count());
            Assert.AreEqual(0, sentTelemetry.Count);

            DependencyTelemetry telemetry = listener.PendingDependencyTelemetry.Single();
            Assert.AreEqual("", telemetry.ResultCode);
            Assert.AreEqual(true, telemetry.Success);

            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.NotFound);
            string targetInstrumentationKeyHash = InstrumentationKeyHashLookupHelper.GetInstrumentationKeyHash(Guid.NewGuid().ToString());
            response.Headers.Add(DependencyCollectorDiagnosticListener.TargetInstrumentationKeyHeader, targetInstrumentationKeyHash);

            listener.OnResponse(response, loggingRequestId);
            Assert.AreEqual(0, listener.PendingDependencyTelemetry.Count());
            Assert.AreEqual(1, sentTelemetry.Count);
            Assert.AreSame(telemetry, sentTelemetry.Single());

            Assert.AreEqual(applicationInsightsType, telemetry.Type);
            Assert.AreEqual(GetApplicationInsightsTarget(targetInstrumentationKeyHash), telemetry.Target);
            Assert.AreEqual(notFoundResultCode, telemetry.ResultCode);
            Assert.AreEqual(false, telemetry.Success);
        }
    }
}
