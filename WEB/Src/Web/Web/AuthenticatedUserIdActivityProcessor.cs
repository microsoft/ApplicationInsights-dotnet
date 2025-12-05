namespace Microsoft.ApplicationInsights.Web
{
    using System;
    using System.Diagnostics;
    using System.Web;
    using Microsoft.ApplicationInsights.Web.Implementation;
    using OpenTelemetry;

    /// <summary>
    /// An activity processor that will set the Authenticated User ID on activities.
    /// Authenticated User ID is derived from authenticated user cookie.
    /// </summary>
    internal class AuthenticatedUserIdActivityProcessor : BaseProcessor<Activity>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticatedUserIdActivityProcessor"/> class.
        /// </summary>
        public AuthenticatedUserIdActivityProcessor()
        {
        }

        /// <summary>
        /// Called when an activity ends. Sets the authenticated user ID tag.
        /// </summary>
        /// <param name="activity">The activity that ended.</param>
        public override void OnEnd(Activity activity)
        {
            if (activity == null)
            {
                return;
            }

            var context = HttpContext.Current;
            if (context == null)
            {
                return;
            }

            // Only process if authenticated user ID is not already set
            var existingUserId = activity.GetTagItem("enduser.id");
            if (existingUserId == null || string.IsNullOrEmpty(existingUserId?.ToString()))
            {
                var authUserCookie = context.Request.UnvalidatedGetCookie(RequestTrackingConstants.WebAuthenticatedUserCookieName);
                if (authUserCookie != null && !string.IsNullOrEmpty(authUserCookie.Value))
                {
                    var authUserCookieString = HttpUtility.UrlDecode(authUserCookie.Value);
                    var cookieParts = authUserCookieString.Split('|');

                    if (cookieParts.Length > 0)
                    {
                        var authenticatedUserId = cookieParts[0];
                        if (!string.IsNullOrEmpty(authenticatedUserId))
                        {
                            // Set as OpenTelemetry semantic convention for authenticated user
                            activity.SetTag("enduser.id", authenticatedUserId);
                        }
                    }
                }
                else
                {
                    WebEventSource.Log.AuthIdTrackingCookieNotAvailable();
                }
            }

            base.OnEnd(activity);
        }
    }
}
