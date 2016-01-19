namespace Functional
{
    using Functional.Helpers;
    using Functional.UserSessionTests;
    using Microsoft.Developer.Analytics.DataCollection.Model.v2;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    public abstract class UserSessionTestBase : SingleWebHostTestBase
    {
        /// <summary>
        /// Tests the scenario when the initial request is sent without user cookie, in which case, a cookie is set by the server
        /// and we need to verify if the cookie set by the server is same as the one collected by listener.
        /// </summary>
        protected void ValidateUserIdWithoutRequestCookie(string requestPath, int testListenerTimeoutInMs, int testRequestTimeOutInMs)
        {
            Cookie[] cookies = new Cookie[0];
            var requestResponseContainer = new RequestResponseContainer(cookies, requestPath, this.Config.ApplicationUri, testListenerTimeoutInMs, testRequestTimeOutInMs);
            requestResponseContainer.SendRequest();

            var userCookie = requestResponseContainer.CookieCollection.ReceiveUserCookie();
            var item = Listener.ReceiveItemsOfType<TelemetryItem<RequestData>>(
                1,
                testListenerTimeoutInMs)[0];

            Assert.AreEqual("OK", requestResponseContainer.ResponseTask.Result.ReasonPhrase);
            Assert.AreEqual(2, requestResponseContainer.CookieCollection.Count);
            Assert.AreNotEqual(string.Empty, userCookie);

            Assert.AreEqual(userCookie, item.UserContext.Id + "|" + item.UserContext.AcquisitionDate.Value.ToString("O"));
        }

        /// <summary>
        /// Tests the scenario when the initial request is sent without a cookie, in which case, a cookie is set by the server
        /// and we need to verify if the cookie set by the server is same as the one collected by listener.
        /// </summary>
        protected void ValidateSessionIdWithoutRequestCookie(string requestPath, int testListenerTimeoutInMs, int testRequestTimeOutInMs)
        {
            Cookie[] cookies = new Cookie[0];
            var requestResponseContainer = new RequestResponseContainer(cookies, requestPath, this.Config.ApplicationUri, testListenerTimeoutInMs, testRequestTimeOutInMs);
            requestResponseContainer.SendRequest();

            var sessionCookie = requestResponseContainer.CookieCollection.ReceiveSessionCookie();

            var item = Listener.ReceiveItemsOfType<TelemetryItem<RequestData>>(
                1,
                testListenerTimeoutInMs)[0];

            Assert.AreEqual("OK", requestResponseContainer.ResponseTask.Result.ReasonPhrase);
            Assert.AreNotEqual(string.Empty, sessionCookie);
            Assert.AreEqual(2, requestResponseContainer.CookieCollection.Count);
            Assert.AreEqual(sessionCookie.Substring(0, sessionCookie.IndexOf("|", StringComparison.Ordinal)), item.SessionContext.Id);    
        }

        /// <summary>
        /// Tests the scenario when the initial request contains a cookie (corresponding to user), in which case, the response should contain a similar cookie with the information
        /// replicated into the listener data.
        /// </summary>
        protected void ValidateUserIdWithRequestCookie(string requestPath, int testListenerTimeoutInMs, int testRequestTimeOutInMs, Cookie[] additionalCookies = null)
        {
            DateTimeOffset time = DateTimeOffset.UtcNow;
            List<Cookie> cookies = new List<Cookie>();
            int additionalCookiesLength = 0;
            if (null != additionalCookies)
            {
                cookies.AddRange(additionalCookies);
                additionalCookiesLength = additionalCookies.Length;
            }
            string userCookieStr = "userId|" + time.ToString("O");
            var cookie = new Cookie(CookieNames.UserCookie, userCookieStr);
            cookies.Add(cookie);

            var requestResponseContainer = new RequestResponseContainer(cookies.ToArray(), requestPath, this.Config.ApplicationUri, testListenerTimeoutInMs, testRequestTimeOutInMs);
            requestResponseContainer.SendRequest();

            var userCookie = requestResponseContainer.CookieCollection.ReceiveUserCookie();
            var item = Listener.ReceiveItemsOfType<TelemetryItem<RequestData>>(
                1,
                testListenerTimeoutInMs)[0];

            Assert.AreEqual("OK", requestResponseContainer.ResponseTask.Result.ReasonPhrase);
            Assert.AreEqual(2 + additionalCookiesLength, requestResponseContainer.CookieCollection.Count);
            Assert.AreEqual("userId", item.UserContext.Id);
            Assert.AreEqual(time, item.UserContext.AcquisitionDate.Value);
            Assert.AreEqual(userCookieStr, userCookie);
            Assert.AreEqual(userCookie, item.UserContext.Id + "|" + item.UserContext.AcquisitionDate.Value.ToString("O"));   
        }

        /// <summary>
        /// Tests the scenario when the initial request contains a cookie (corresponding to user), in which case, the response should contain a similar cookie with the information
        /// replicated into the listener data.
        /// </summary>
        protected void ValidateSessionIdWithRequestCookie(string requestPath, int testListenerTimeoutInMs, int testRequestTimeOutInMs)
        {
            DateTime currentTime = DateTime.Now;
            string currentTimeString = currentTime.ToString("O");
            string actualCookie = "sessionId|" + currentTimeString + "|" + currentTimeString;

            var cookie = new Cookie(CookieNames.SessionCookie, actualCookie);
            Cookie[] cookies = { cookie };

            var requestResponseContainer = new RequestResponseContainer(cookies, requestPath, this.Config.ApplicationUri, testListenerTimeoutInMs, testRequestTimeOutInMs);
            requestResponseContainer.SendRequest();

            var sessionCookie = requestResponseContainer.CookieCollection.ReceiveSessionCookie();
            var item = Listener.ReceiveItemsOfType<TelemetryItem<RequestData>>(
                1,
                testListenerTimeoutInMs)[0];

            Assert.AreEqual("OK", requestResponseContainer.ResponseTask.Result.ReasonPhrase);
            Assert.AreEqual(2, requestResponseContainer.CookieCollection.Count);
            Assert.AreEqual(actualCookie, sessionCookie);
            Assert.AreEqual("sessionId", item.SessionContext.Id);
            Assert.AreEqual(sessionCookie.Substring(0, sessionCookie.IndexOf("|", StringComparison.Ordinal)), item.SessionContext.Id);    
        }

        protected void CheckIfOutpuCashIsNotBroken(string requestPath, string contentMarker, int testRequestTimeoutInMs)
        {
            var responseTask1 = this.HttpClient.GetAsync(requestPath);
            this.ValidateSuccessfullRequest(contentMarker, responseTask1, testRequestTimeoutInMs);
            var header1 = responseTask1.Result.Headers.Single(i => i.Key == "Test_Random").Value.Single();

            var responseTask2 = this.HttpClient.GetAsync(requestPath);
            this.ValidateSuccessfullRequest(contentMarker, responseTask2, testRequestTimeoutInMs);
            var header2 = responseTask2.Result.Headers.Single(i => i.Key == "Test_Random").Value.Single();

            Assert.AreEqual(header1, header2, "Second request was supposed to be taken from cache and have same header");
        }

        protected void CheckIfSessionIsCollectedIfResponseIsFlushedEarly(string requestPath, string contentMarker, int testListenerTimeoutInMs, int testRequestTimeoutInMs)
        {
            Cookie[] cookies = new Cookie[0];
            var requestResponseContainer = new RequestResponseContainer(cookies, requestPath, this.Config.ApplicationUri, testListenerTimeoutInMs, testRequestTimeoutInMs);
            requestResponseContainer.SendRequest();

            this.ValidateSuccessfullRequest(contentMarker, requestResponseContainer.ResponseTask, testRequestTimeoutInMs);
            var sessionCookie = requestResponseContainer.CookieCollection.ReceiveSessionCookie();

            var item = Listener.ReceiveItemsOfType<TelemetryItem<RequestData>>(1, testListenerTimeoutInMs)[0];

            Assert.AreEqual("OK", requestResponseContainer.ResponseTask.Result.ReasonPhrase);
            Assert.AreNotEqual(string.Empty, sessionCookie);
            Assert.AreEqual(2, requestResponseContainer.CookieCollection.Count);
            Assert.AreEqual(sessionCookie.Substring(0, sessionCookie.IndexOf("|", StringComparison.Ordinal)), item.SessionContext.Id);    
        }

        protected void CheckIfUserIsCollectedIfResponseIsFlushedEarly(string requestPath, string contentMarker, int testListenerTimeoutInMs, int testRequestTimeoutInMs)
        {
            Cookie[] cookies = new Cookie[0];
            var requestResponseContainer = new RequestResponseContainer(cookies, requestPath, this.Config.ApplicationUri, testListenerTimeoutInMs, testRequestTimeoutInMs);
            requestResponseContainer.SendRequest();

            var item = Listener.ReceiveItemsOfType<TelemetryItem<RequestData>>(1, testListenerTimeoutInMs)[0];
            
            var userCookie = requestResponseContainer.CookieCollection.ReceiveUserCookie();
            Trace.Write(requestResponseContainer.ResponseData);

            Assert.AreEqual(userCookie, item.UserContext.Id + "|" + item.UserContext.AcquisitionDate.Value.ToString("O"));
        }

        protected void CheckIfSessionIsInitializedWhenPostResolveCacheIsSkipped(string requestPath, int testListenerTimeoutInMs, int testRequestTimeoutInMs)
        {
            Cookie[] cookies = new Cookie[0];
            var requestResponseContainer = new RequestResponseContainer(cookies, requestPath, this.Config.ApplicationUri, testListenerTimeoutInMs, testRequestTimeoutInMs);
            requestResponseContainer.SendRequest();

            this.ValidateFailedRequest(requestResponseContainer.ResponseTask, testRequestTimeoutInMs);

            var item = Listener.ReceiveItemsOfType<TelemetryItem<RequestData>>(1, testListenerTimeoutInMs)[0];
            var sessionCookie = requestResponseContainer.CookieCollection.ReceiveSessionCookie();
     
            Assert.AreNotEqual(string.Empty, sessionCookie);
            Assert.AreEqual(2, requestResponseContainer.CookieCollection.Count);
            Assert.AreEqual(sessionCookie.Substring(0, sessionCookie.IndexOf("|", StringComparison.Ordinal)), item.SessionContext.Id);    
        }

        protected void CheckIfUserIsInitializedWhenPostResolveCacheIsSkipped(string requestPath, int testListenerTimeoutInMs, int testRequestTimeoutInMs)
        {
            Cookie[] cookies = new Cookie[0];
            var requestResponseContainer = new RequestResponseContainer(cookies, requestPath, this.Config.ApplicationUri, testListenerTimeoutInMs, testRequestTimeoutInMs);
            requestResponseContainer.SendRequest();

            this.ValidateFailedRequest(requestResponseContainer.ResponseTask, testRequestTimeoutInMs);

            var item = Listener.ReceiveItemsOfType<TelemetryItem<RequestData>>(1, testListenerTimeoutInMs)[0];
            
            Assert.IsNotNull(item.UserContext.Id);
            Assert.IsNotNull(item.UserContext.AcquisitionDate.Value);
            var userCookie = requestResponseContainer.CookieCollection.ReceiveUserCookie();
            Trace.Write(requestResponseContainer.ResponseData);

            Assert.AreEqual(userCookie, item.UserContext.Id + "|" + item.UserContext.AcquisitionDate.Value.ToString("O"));
        }

        private void ValidateSuccessfullRequest(string contentMarker, Task<HttpResponseMessage> responseTask, int testRequestTimeoutInMs)
        {
            Assert.IsTrue(
                responseTask.Wait(testRequestTimeoutInMs),
                "Request was not executed in time");

            Assert.IsTrue(
                responseTask.Result.IsSuccessStatusCode,
                "Request succeeded");

            Assert.AreEqual(
                HttpStatusCode.OK,
                responseTask.Result.StatusCode,
                "Unexpected response code");

            var responseData = responseTask.Result.Content.ReadAsStringAsync().Result;
            Trace.Write(responseData);

            Assert.IsTrue(
                responseData.Contains(contentMarker),
                "Response content does not contain expected data: {0}",
                responseData);
        }

        private void ValidateFailedRequest(Task<HttpResponseMessage> responseTask, int testRequestTimeoutInMs)
        {
            Assert.IsTrue(
                responseTask.Wait(testRequestTimeoutInMs),
                "Request was not executed in time");

            Assert.IsFalse(
                responseTask.Result.IsSuccessStatusCode,
                "Request failed");
        }
    }
}
