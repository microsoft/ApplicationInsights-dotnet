namespace Microsoft.ApplicationInsights.Web
{
    using System;
    using System.Diagnostics;
    using System.Web;
    using Microsoft.ApplicationInsights.Internal;
    using Microsoft.ApplicationInsights.Web.Implementation;
    using OpenTelemetry;

    /// <summary>
    /// An activity processor that will set the Session properties on activities.
    /// Session information is derived from the session cookie.
    /// </summary>
    internal class SessionActivityProcessor : BaseProcessor<Activity>
    {
        private const string WebSessionCookieName = "ai_session";
        private const int SessionCookieSessionIdIndex = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionActivityProcessor"/> class.
        /// </summary>
        public SessionActivityProcessor()
        {
        }

        /// <summary>
        /// Called when an activity ends. Sets the session ID tag.
        /// </summary>
        /// <param name="activity">The activity that ended.</param>
        public override void OnEnd(Activity activity)
        {
            if (activity == null)
            {
                return;
            }

            var context = HttpContext.Current;
            var request = context.GetRequest();
            if (request == null)
            {
                return;
            }

            // Only process if session ID is not already set
            var existingSessionId = activity.GetTagItem(SemanticConventions.AttributeMicrosoftSessionId);
            if (existingSessionId == null || string.IsNullOrEmpty(existingSessionId.ToString()))
            {
                // Try Unvalidated first, fall back to regular Cookies for test environments
                var sessionCookie = request.UnvalidatedGetCookie(WebSessionCookieName) ?? request.Cookies[WebSessionCookieName];
                if (sessionCookie != null && !string.IsNullOrWhiteSpace(sessionCookie.Value))
                {
                    var parts = sessionCookie.Value.Split('|');

                    if (parts.Length > SessionCookieSessionIdIndex)
                    {
                        var sessionId = parts[SessionCookieSessionIdIndex];
                        if (!string.IsNullOrEmpty(sessionId))
                        {
                            // The exporter maps microsoft.session.id to the ai.session.id envelope tag.
                            activity.SetTag(SemanticConventions.AttributeMicrosoftSessionId, sessionId);
                        }
                    }
                }
                else
                {
                    WebEventSource.Log.WebSessionTrackingSessionCookieIsNotSpecifiedInRequest();
                }
            }

            base.OnEnd(activity);
        }
    }
}
