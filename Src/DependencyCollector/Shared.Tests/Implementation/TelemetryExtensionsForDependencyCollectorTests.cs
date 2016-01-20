namespace Microsoft.ApplicationInsights.DependencyCollector.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Data.SqlClient;
    using System.Net;
    using System.Threading;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Web.TestFramework;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Tests for client server dependency tracker.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "Test application"), TestClass]
    public class TelemetryExtensionsForDependencyCollectorTests
    {
        private List<ITelemetry> sendItems;
        private DependencyTelemetry telemetry;
        private TelemetryClient telemetryClient;
        private WebRequest webRequest;
        private SqlCommand sqlRequest;

        [TestInitialize]
        public void TestInitialize()
        {
            var configuration = new TelemetryConfiguration();
            this.sendItems = new List<ITelemetry>();
            configuration.TelemetryChannel = new StubTelemetryChannel { OnSend = item => this.sendItems.Add(item) };
            configuration.InstrumentationKey = Guid.NewGuid().ToString();
            configuration.TelemetryInitializers.Add(new MockTelemetryInitializer());
            DependencyTrackingTelemetryModule module = new DependencyTrackingTelemetryModule();
            module.Initialize(configuration);
            this.telemetryClient = new TelemetryClient(configuration);
            var operationHolder = this.telemetryClient.StartOperation<DependencyTelemetry>("operationName");
            this.telemetry = operationHolder.Telemetry;
            this.webRequest = WebRequest.Create(new Uri("http://bing.com"));
            this.sqlRequest = new SqlCommand("select * from table;");
        }

        [TestCleanup]
        public void TestClean()
        {
            this.sqlRequest.Dispose();
        }

        /// <summary>
        /// Tests if AssociateTelemetryWithWebRequest() does not fail on null web request.
        /// </summary>
        [TestMethod]
        public void AssociateTelemetryWithWebRequestDoesNotFailOnNullWebRequest()
        {
            this.telemetry.AssociateTelemetryWithWebRequest(null);
        }

        /// <summary>
        /// Tests if AssociateTelemetryWithSQLRequest() does not fail on null SQL request.
        /// </summary>
        [TestMethod]
        public void AssociateTelemetryWithSqlRequestDoesNotFailOnNullSqlRequest()
        {
            this.telemetry.AssociateTelemetryWithSqlRequest(null);
        }

        /// <summary>
        /// Tests the scenario if AssociateTelemetryWithWebRequest handles same web request multiple times.
        /// </summary>
        [TestMethod]
        public void AssociateTelemetryWithSqlRequestHandlesRedundantTelemetryItemsFromSameWebRequest()
        {
            var dependencyTelemetry = this.telemetry.AssociateTelemetryWithWebRequest(this.webRequest);
            Assert.IsNotNull(dependencyTelemetry);
            Thread.Sleep(3000);

            var dependencyTelemetryDuplicate = this.telemetry.AssociateTelemetryWithWebRequest(this.webRequest);
            Assert.IsNotNull(dependencyTelemetryDuplicate);

            Assert.AreEqual(dependencyTelemetry, dependencyTelemetryDuplicate);
        }

        /// <summary>
        /// Tests the scenario if AssociateTelemetryWithSQLRequest handles same SQL request multiple times.
        /// </summary>
        [TestMethod]
        public void AssociateTelemetryWithSqlRequestHandlesRedundantTelemetryItemsFromSameSqlRequest()
        {
            var dependencyTelemetry = this.telemetry.AssociateTelemetryWithSqlRequest(this.sqlRequest);
            Assert.IsNotNull(dependencyTelemetry);
            Thread.Sleep(3000);

            var dependencyTelemetryDuplicate = this.telemetry.AssociateTelemetryWithSqlRequest(this.sqlRequest);
            Assert.IsNotNull(dependencyTelemetryDuplicate);

            Assert.AreEqual(dependencyTelemetry, dependencyTelemetryDuplicate);
        }

        /// <summary>
        /// Tests if AssociateTelemetryWithWebRequest() does not populate cookies with setCookies = false.
        /// </summary>
        [TestMethod]
        public void AssociateTelemetryWithWebRequestDoNotPopulateWebRequestCookiesByDefault()
        {
            this.telemetry.AssociateTelemetryWithWebRequest(this.webRequest);
            string sessionCookie = DependencyCollectorTestHelpers.GetCookieValueFromWebRequest(this.webRequest as HttpWebRequest, "ai_session");
            Assert.IsNull(sessionCookie);
            string userCookie = DependencyCollectorTestHelpers.GetCookieValueFromWebRequest(this.webRequest as HttpWebRequest, "ai_user");
            Assert.IsNull(userCookie);

            var newTelemetry = new DependencyTelemetry();
            newTelemetry.Context.User.Id = "UserId";
            newTelemetry.Context.Session.Id = "SessionId";

            // Note, webRequest is already associated with the telemetry. And it should not be overriden.
            newTelemetry.AssociateTelemetryWithWebRequest(this.webRequest);
            sessionCookie = DependencyCollectorTestHelpers.GetCookieValueFromWebRequest(this.webRequest as HttpWebRequest, "ai_session");
            Assert.IsNull(sessionCookie);
            userCookie = DependencyCollectorTestHelpers.GetCookieValueFromWebRequest(this.webRequest as HttpWebRequest, "ai_user");
            Assert.IsNull(userCookie);
        }

        /// <summary>
        /// Tests if AssociateTelemetryWithWebRequest() populates cookies of webRequests with user and session ids.
        /// </summary>
        [TestMethod]
        public void AssociateTelemetryWithWebRequestPopulateWebRequestCookiesWithSessionIdOnlyIfSetCookiesIsEnabled()
        {
            this.telemetry.AssociateTelemetryWithWebRequest(this.webRequest, setCookies: true);
            string sessionCookie = DependencyCollectorTestHelpers.GetCookieValueFromWebRequest(this.webRequest as HttpWebRequest, "ai_session");
            Assert.IsNotNull(sessionCookie);
            Assert.AreEqual("ai_session=SessionID", sessionCookie);

            string userCookie = DependencyCollectorTestHelpers.GetCookieValueFromWebRequest(this.webRequest as HttpWebRequest, "ai_user");
            Assert.IsNotNull(userCookie);
            Assert.AreEqual("ai_user=UserID", userCookie);
        }
    }
}
