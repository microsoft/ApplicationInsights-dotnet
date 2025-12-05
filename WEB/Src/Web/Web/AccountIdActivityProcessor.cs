namespace Microsoft.ApplicationInsights.Web
{
    using System;
    using System.Diagnostics;
    using System.Web;
    using Microsoft.ApplicationInsights.Web.Implementation;
    using OpenTelemetry;

    /// <summary>
    /// An activity processor that will set the User Account ID on activities.
    /// Account ID is derived from authenticated user cookie.
    /// </summary>
    internal class AccountIdActivityProcessor : BaseProcessor<Activity>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AccountIdActivityProcessor"/> class.
        /// </summary>
        public AccountIdActivityProcessor()
        {
        }

        /// <summary>
        /// Called when an activity ends. Sets the user account ID tag.
        /// </summary>
        /// <param name="activity">The activity that ended.</param>
#pragma warning disable RS0016 // Add public types and members to the declared API
        public override void OnEnd(Activity activity)
#pragma warning restore RS0016
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

            // Only process if account ID is not already set
            var existingAccountId = activity.GetTagItem("enduser.id");
            if (existingAccountId == null || string.IsNullOrEmpty(existingAccountId?.ToString()))
            {
                var authUserCookie = context.Request.UnvalidatedGetCookie(RequestTrackingConstants.WebAuthenticatedUserCookieName);
                if (authUserCookie != null && !string.IsNullOrEmpty(authUserCookie.Value))
                {
                    var authUserCookieString = HttpUtility.UrlDecode(authUserCookie.Value);
                    var cookieParts = authUserCookieString.Split('|');

                    if (cookieParts.Length > 1)
                    {
                        var accountId = cookieParts[1];
                        if (!string.IsNullOrEmpty(accountId))
                        {
                            // Set as OpenTelemetry semantic convention for user account
                            activity.SetTag("enduser.account", accountId);
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
