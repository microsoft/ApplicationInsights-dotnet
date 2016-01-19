namespace Functional
{
    using Functional.Helpers;
    using Functional.UserSessionTests;
    using System.Net;
    using Microsoft.Developer.Analytics.DataCollection.Model.v2;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    public abstract class UserSessionIsFirstIsNewTestBase : SingleWebHostTestBase
    {
        protected void ValidateIsFirstSessionTrueAndNewSessionStarted(string requestPath, int testListenerTimeoutInMs, int testRequestTimeOutInMs)
        {
            // For the first request that is being sent we create a new user and a new session 
            // Check: IsFirst true; new SessionState is generated

            Cookie[] cookies = new Cookie[0];
            var requestResponseContainer = new RequestResponseContainer(cookies, requestPath, this.Config.ApplicationUri, testListenerTimeoutInMs, testRequestTimeOutInMs);
            requestResponseContainer.SendRequest();

            TelemetryItem<RequestData> requestItem;
            TelemetryItem<SessionStateData> sessionStateItem;

            this.ReceiveRequestAndSession(testListenerTimeoutInMs, out requestItem, out sessionStateItem);

            Assert.IsTrue(requestItem.SessionContext.IsFirst.Value, "A new userId is associated with isFirstSession False");
            Assert.AreEqual(sessionStateItem.Data.BaseData.State, SessionState.Start);
        }

        protected void ValidateIsFirstSessionFalseAndSessionNotStarted(string requestPath, int testListenerTimeoutInMs,
            int testRequestTimeOutInMs)
        {
            // If a second request is made with the session cookie that is obtained from the first request,
            // IsFirst is false and no SessionState telemetry is generated

            Cookie[] cookies = new Cookie[0];
            var requestResponseContainer = new RequestResponseContainer(cookies, requestPath, this.Config.ApplicationUri, testListenerTimeoutInMs, testRequestTimeOutInMs);
            requestResponseContainer.SendRequest();

            Listener.ReceiveItemsOfType<TelemetryItem<RequestData>>(1, testListenerTimeoutInMs);
            
            var sessionCookie = new Cookie(CookieNames.SessionCookie, requestResponseContainer.CookieCollection.ReceiveSessionCookie());
            
            requestResponseContainer = new RequestResponseContainer(new []{sessionCookie}, requestPath, this.Config.ApplicationUri, testListenerTimeoutInMs, testRequestTimeOutInMs);
            requestResponseContainer.SendRequest();

            var items = Listener.ReceiveAllItemsDuringTimeOfType<TelemetryItem<RequestData>, TelemetryItem<SessionStateData>>(testListenerTimeoutInMs);
            Assert.AreEqual(1, items.Length, "We expected only 1 request. SessionState should not be generated.");
            var request = items[0] as TelemetryItem<RequestData>;
            Assert.IsTrue((request.SessionContext.IsFirst == null) || !request.SessionContext.IsFirst.Value, "IsFirst should be false or null because cookie existed");
        }

        private void ReceiveRequestAndSession(
            int testListenerTimeoutInMs,
            out TelemetryItem<RequestData> requestItem,
            out TelemetryItem<SessionStateData> sessionItem)
        {
            var items = Listener.ReceiveItemsOfTypes<TelemetryItem<RequestData>, TelemetryItem<SessionStateData>>(
                2,
                testListenerTimeoutInMs);

            // One item is request, the other one is sessionState.
            int requestItemIndex = (items[0] is TelemetryItem<RequestData>) ? 0 : 1;
            int sessionStateItemIndex = (requestItemIndex == 0) ? 1 : 0;

            sessionItem = (TelemetryItem<SessionStateData>)items[sessionStateItemIndex];
            requestItem = (TelemetryItem<RequestData>)items[requestItemIndex];
        }

    }
}
