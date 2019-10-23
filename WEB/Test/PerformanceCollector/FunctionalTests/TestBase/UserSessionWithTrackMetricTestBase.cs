namespace Functional
{
    using Functional.Helpers;
    using Functional.UserSessionTests;
    using Microsoft.Developer.Analytics.DataCollection.Model.v2;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Net;

    public abstract class UserSessionWithTrackMetricBase : SingleWebHostTestBase
    {
        /// <summary>
        ///Tests the scenario if user related information is present in the listener when customer invokes TrackMetric()
        ///method from his application without a cookie.
        /// </summary>
        protected void ValidateUserCookieWhenTrackMetricIsInvokedWithoutCookie(string requestPath, int testListenerTimeoutInMs, int testRequestTimeOutInMs)
        {
            Cookie[] cookies = new Cookie[0];
            var requestResponseContainer = new RequestResponseContainer(cookies, requestPath, this.Config.ApplicationUri, testListenerTimeoutInMs, testRequestTimeOutInMs);
            requestResponseContainer.SendRequest();

            var userCookie = requestResponseContainer.CookieCollection.ReceiveUserCookie();
            var item = Listener.ReceiveItemsOfType<TelemetryItem<MetricData>>(
                1,
                testListenerTimeoutInMs)[0];

            Assert.AreEqual("OK", requestResponseContainer.ResponseTask.Result.ReasonPhrase);
            Assert.AreEqual(2, requestResponseContainer.CookieCollection.Count);
            Assert.AreEqual(userCookie, item.UserContext.Id + "|" + item.UserContext.AcquisitionDate.Value.ToString("O"));
        }

        /// <summary>
        ///Tests the scenario if session related information is present in the listener when customer invokes TrackMetric()
        ///method from his application without a cookie.
        /// </summary>
        protected void ValidateSessionCookieWhenTrackMetricIsInvokedWithoutCookie(string requestPath, int testListenerTimeoutInMs, int testRequestTimeOutInMs)
        {
            Cookie[] cookies = new Cookie[0];
            var requestResponseContainer = new RequestResponseContainer(cookies, requestPath, this.Config.ApplicationUri, testListenerTimeoutInMs, testRequestTimeOutInMs);
            requestResponseContainer.SendRequest();

            var sessionCookie = requestResponseContainer.CookieCollection.ReceiveSessionCookie();
            var item = Listener.ReceiveItemsOfType<TelemetryItem<MetricData>>(
                   1,
                   testListenerTimeoutInMs)[0];

            Assert.AreEqual("OK", requestResponseContainer.ResponseTask.Result.ReasonPhrase);
            Assert.AreNotEqual(string.Empty, sessionCookie);
            Assert.AreEqual(2, requestResponseContainer.CookieCollection.Count);
            Assert.AreEqual(sessionCookie.Substring(0, sessionCookie.IndexOf("|", StringComparison.Ordinal)), item.SessionContext.Id);  
        }
    }
}
