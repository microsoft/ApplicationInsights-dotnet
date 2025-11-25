namespace Microsoft.ApplicationInsights.Web
{
    using System;
    using System.Diagnostics;
    using System.Web;
    using Microsoft.ApplicationInsights.Web.Implementation;
    using OpenTelemetry;

    /// <summary>
    /// An activity processor that will set the User ID on activities.
    /// User ID is derived from user cookie.
    /// </summary>
    internal class UserActivityProcessor : BaseProcessor<Activity>
    {
        private const string WebUserCookieName = "ai_user";

        /// <summary>
        /// Initializes a new instance of the <see cref="UserActivityProcessor"/> class.
        /// </summary>
        public UserActivityProcessor()
        {
        }

        /// <summary>
        /// Called when an activity ends. Sets the user ID tag.
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

            // Only process if user ID is not already set (check for anonymous user ID)
            var existingUserId = activity.GetTagItem("ai.user.id");
            if (existingUserId == null || string.IsNullOrEmpty(existingUserId?.ToString()))
            {
                // Try Unvalidated first, fall back to regular Cookies for test environments
                var userCookie = context.Request.UnvalidatedGetCookie(WebUserCookieName) ?? context.Request.Cookies[WebUserCookieName];
                if (userCookie != null && !string.IsNullOrEmpty(userCookie.Value))
                {
                    var cookieParts = userCookie.Value.Split('|');

                    if (cookieParts.Length >= 2)
                    {
                        var userId = cookieParts[0];
                        var timestamp = cookieParts[1];

                        // Validate timestamp format (should be ISO 8601 DateTime)
                        if (!string.IsNullOrEmpty(userId) && DateTimeOffset.TryParse(timestamp, out _))
                        {
                            // Set as Application Insights convention for anonymous user
                            activity.SetTag("ai.user.id", userId);
                        }
                        else
                        {
                            WebEventSource.Log.WebUserTrackingUserCookieIsIncomplete(userCookie.Value);
                        }
                    }
                    else
                    {
                        WebEventSource.Log.WebUserTrackingUserCookieIsIncomplete(userCookie.Value);
                    }
                }
                else
                {
                    WebEventSource.Log.WebUserTrackingUserCookieNotAvailable();
                }
            }

            base.OnEnd(activity);
        }
    }
}
