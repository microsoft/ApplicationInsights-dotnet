namespace Microsoft.ApplicationInsights.Tests
{
    using System;
    using System.Net;
    using Common;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.DependencyCollector.Implementation;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Web.TestFramework;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Shared WebRequestDependencyTrackingHelpers class tests.
    /// </summary>
    [TestClass]
    public class WebRequestDependencyTrackingHelpersTests
    {
        [TestMethod]
        public void SetUserAndSessionContextForWebRequestDoesNothingIfTelemetryItemIsNotInitialized()
        {
            var webRequest = WebRequest.Create(new Uri("http://bing.com"));
            var telemetry = new DependencyTelemetry();

            string cookieValue = DependencyCollectorTestHelpers.GetCookieValueFromWebRequest(webRequest as HttpWebRequest, "ai_session");
            Assert.IsNull(cookieValue);
            WebRequestDependencyTrackingHelpers.SetUserAndSessionContextForWebRequest(telemetry, webRequest);
            cookieValue = DependencyCollectorTestHelpers.GetCookieValueFromWebRequest(webRequest as HttpWebRequest, "ai_session");
            Assert.IsNull(cookieValue);
        }

        [TestMethod]
        public void SetUserAndSessionContextForWebRequestSetsCookiesIfTelemetryItemIsInitialized()
        {
            var webRequest = WebRequest.Create(new Uri("http://bing.com"));
            var telemetry = new DependencyTelemetry();
            telemetry.Context.Session.Id = "SessionID";
            telemetry.Context.User.Id = "UserID";

            string cookieValue = DependencyCollectorTestHelpers.GetCookieValueFromWebRequest(webRequest as HttpWebRequest, "ai_session");
            Assert.IsNull(cookieValue);
            WebRequestDependencyTrackingHelpers.SetUserAndSessionContextForWebRequest(telemetry, webRequest);
            cookieValue = DependencyCollectorTestHelpers.GetCookieValueFromWebRequest(webRequest as HttpWebRequest, "ai_session");
            Assert.IsNotNull(cookieValue);
            Assert.AreEqual("ai_session=SessionID", cookieValue);

            cookieValue = DependencyCollectorTestHelpers.GetCookieValueFromWebRequest(webRequest as HttpWebRequest, "ai_user");
            Assert.IsNotNull(cookieValue);
            Assert.AreEqual("ai_user=UserID", cookieValue);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void SetUserAndSessionContextForWebRequestFailsWithNullTelemetry()
        {
            var webRequest = WebRequest.Create(new Uri("http://bing.com"));
            WebRequestDependencyTrackingHelpers.SetUserAndSessionContextForWebRequest(null, webRequest);
        }

        [TestMethod]
        public void SetUserAndSessionContextForWebRequestDoesNotFailWithNullWebRequest()
        {
            var telemetry = new DependencyTelemetry();
            WebRequestDependencyTrackingHelpers.SetUserAndSessionContextForWebRequest(telemetry, null);
        }

        [TestMethod]
        public void SetCorrelationContextForWebRequestDoesNothingIfOperaitonContextEmpty()
        {
            var webRequest = WebRequest.Create(new Uri("http://bing.com"));
            var telemetry = new DependencyTelemetry();

            string rootId = webRequest.Headers[RequestResponseHeaders.StandardRootIdHeader];
            string operationId = webRequest.Headers[RequestResponseHeaders.StandardParentIdHeader];
            Assert.IsNull(rootId);
            Assert.IsNull(operationId);
            WebRequestDependencyTrackingHelpers.SetUserAndSessionContextForWebRequest(telemetry, webRequest);
            rootId = webRequest.Headers[RequestResponseHeaders.StandardRootIdHeader];
            operationId = webRequest.Headers[RequestResponseHeaders.StandardParentIdHeader];
            Assert.IsNull(rootId);
            Assert.IsNull(operationId);
        }

        [TestMethod]
        public void SetCorrelationContextForWebRequestSetsHeaders()
        {
            var webRequest = WebRequest.Create(new Uri("http://bing.com"));
            var telemetry = new DependencyTelemetry();
            telemetry.Id = "Id";
            telemetry.Context.Operation.Id = "RootId";

            WebRequestDependencyTrackingHelpers.SetCorrelationContextForWebRequest(telemetry, webRequest);
            var rootId = webRequest.Headers[RequestResponseHeaders.StandardRootIdHeader];
            var operationId = webRequest.Headers[RequestResponseHeaders.StandardParentIdHeader];
            Assert.AreEqual("RootId", rootId);
            Assert.AreEqual("Id", operationId);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void SetCorrelationContextForWebRequestFailsWithNullTelemetry()
        {
            var webRequest = WebRequest.Create(new Uri("http://bing.com"));
            WebRequestDependencyTrackingHelpers.SetCorrelationContextForWebRequest(null, webRequest);
        }

        [TestMethod]
        public void SetCorrelationContextForWebRequestDoesNotFailWithNullWebRequest()
        {
            var telemetry = new DependencyTelemetry();
            WebRequestDependencyTrackingHelpers.SetCorrelationContextForWebRequest(telemetry, null);
        }
    }
}
